//====================
// 블럭 게임의 일부입니다
//====================
	void RestoreGameStatus(bool passAppResume=false)
	{
		string data = GameData.GetGameData();
		GameData.ClearGameData();
		
		// NOTE: 튜토리얼 이후 보드상태, 가운데에 4놓여있고 다음유닛은 4
		// data = "E?|1|6|0|1|1|0;2:2:0|0;2:2:6||0:2|13|0";
		
		// NOTE: 가운데 x인 환경
		// data = "E?|1|8|17|3|6|0;6:0:0|0;7:2:6,0;6:2:5,0;4:2:4,0;5:2:3,0;2:2:2,0;6:2:1,0;5:0:6|||14|0";
		
		// D.Log(data);
		// DebugTool.Report(data, false);
		
		if(!string.IsNullOrEmpty(data))
		{
			MakeSavedLevel(data, passAppResume);
		}
		else
		{
			if(Game.skipLevelId > 0)
			{
				int skippedGoal = Game.skipLevelId;
				
				int lastClearGoal = GameData.GetLastClearedGoal(GameSettings.initialGoalA);
				if(lastClearGoal < skippedGoal)
				{
					GameData.SetLastPlayedGoal(skippedGoal);
					GameData.SetLastClearedGoal(skippedGoal);
				}
				
				int skippedLevel = 1;
				for(int i = 0; i < skippedGoal + 10; i += 1)
				{
					if(skippedGoal != GetGoalLevel(i + 1))continue;
					
					skippedLevel = i + 1;
					break;
				}
				
				int nextLevel = skippedLevel + 1;
				
				OpenLevelStart(nextLevel, () => {
					MakeLevel(nextLevel, null, skippedGoal);
				});
			}
			else
			{
				OpenLevelStart(1);
			}
		}
		
		// D.Log(GameData.GetGameData());
	}
	void SaveGameStatus(bool isPlaying=true)
	{
		VUnit goalUnit = model.units.Find((u) => (u.type == UnitType.Block && u.level == goal));
		List<VUnit> remainUnits = model.units.Where((u) => (u != goalUnit && u != model.currUnit)).ToList();
		
		string serializedNextUnit = (model.currUnit == null ? "" : model.currUnit.GetSerializedData());
		if(model.isItemMode && model.keepedUnit != null)serializedNextUnit = model.keepedUnit.GetSerializedData();
		string serializedRemainUnits = remainUnits.Select((u) => u.GetSerializedData()).Join(",");
		string serializedGoalUnit = (goalUnit == null ? "" : goalUnit.GetSerializedData());
		string serializedQueueUnits = unitQueue.Select((u) => $"{((int)u.type)}:{u.level}").Join(",");
		
		string serializedReviceCount = GameData.GetReviveCount().ToString();
		
		GameData.SetGameData($"E?|{stage}|{goal}|{GameData.GetMergeCount()}|{GameData.GetUserLevel()}|{GameData.GetScore()}|{serializedNextUnit}|{serializedRemainUnits}|{serializedGoalUnit}|{serializedQueueUnits}|{GameData.guestId}|{serializedReviceCount}");
	}
}

//====================
	void MergeCoins(List<VUnit> mergeFrom, VUnit mergeTo)
	{
		mergeTo.SetDirty();
		
		Vector3 targetPos = mergeTo.localPos;
		
		//
		int mergeCount = mergeFrom.Count;
		
		int coinIncrAmount = 0;
		for(int i = 0; i < mergeCount; i += 1)
		{
			coinIncrAmount += mergeFrom[i].level - 1 + 3;
		}
		
		if(mergeFrom.Count == 0)
		{
			mergeTo.PlayAnim(VUnit.ANIM_STATE_MERGE_OUT, () => {
				mergeTo.IncrLevel(coinIncrAmount);
				
				mergeTo.PlayAnim(VUnit.ANIM_STATE_MERGE_NEW);
			});
			mergeTo.SetDirty(false);
		}
		else
		{
			for(int i = 0; i < mergeCount; i += 1)
			{
				if(i < mergeCount - 1)
				{
					MergeTo(mergeFrom[i], targetPos);
				}
				else
				{
					MergeTo(mergeFrom[i], targetPos, () => {
						mergeTo.PlayAnim(VUnit.ANIM_STATE_MERGE_OUT, () => {
							mergeTo.IncrLevel(coinIncrAmount);
							
							mergeTo.PlayAnim(VUnit.ANIM_STATE_MERGE_NEW);
						});
						mergeTo.SetDirty(false);
					});
				}
			}
		}
		
		//
		Effect.Spawn(VfxEffectType.Merge, targetPos);
		Effect.Play(model.GetComboSFX());
		Effect.Vibrate((model.GetCombo() > 2 ? VibrationStyle.HEAVY : VibrationStyle.MEDIUM));
		
		//
		model.ReorderUnits(mergeTo);
	}

//====================
	async UniTask WaitEndOfMovingAsync()
	{
		while(true)
		{
			await UniTask.Yield();
			
			int bCount = model.units.Count((u) => u.isMoving);
			
			int kCount = model.removedUnits.Count((u) => u.isMoving);
			if(kCount == 0 && !model.removedUnits.IsNullOrEmpty())
			{
				model.removedUnits.ForEach((u) => u.Release());
				model.removedUnits = new List<VUnit>();
			}
			
			if(bCount + kCount == 0)break;
		}
		
		model.ReorderUnits();
		
		CheckArrows();
		
		SaveGameStatus();
	}
	
	async UniTaskVoid CheckMoveAsync(VUnit unit)
	{
		navigationController.ToggleBlockerPanel(true);
		
		//
		int currentMergeCount = GameData.GetMergeCount();
		
		//
		isWaitingDropAndHit = true;
		currDroppedUnit = unit;
		
		//
		if(model.units.Any((u) => u.isMoving))
		{
			await WaitEndOfMovingAsync();
		}
		
		//
		while(true)
		{
			int changed = 0;
			
			//
			if(DropUnit())
			{
				changed += 1;
				await WaitEndOfMovingAsync();
			}
			
			//
			if(CheckHit())
			{
				changed += 1;
				await WaitEndOfMovingAsync();
			}
			
			//
			if(changed == 0)break;
		}
		
		// NOTE: 딜레이 없고, 콤보는 한번 drop 에서만
		model.UpdateCombo();
		
		await WaitEndOfMovingAsync();
		
		//
		navigationController.ToggleBlockerPanel(false);
		
		//
		isWaitingDropAndHit = false;
		currDroppedUnit = null;
		
		//
		model.CalcActionIds();
		
		//
		CheckArrows();
		
		SaveGameStatus();
		
		//
		if(model.CheckDead() || model.isTimeOver)
		{
			WillGameOver();
			
			if(model.isTimeOver)
			{
				OnTimeOver();
			}
			else
			{
				Effect.Spawn(VfxUIEffectType.BoardFull, () => {
					OnGameOver();
				});
			}
		}
		else
		{
			// int x = (model.IsEmptyCol(model.centerCol, true) ? model.centerCol : -1);
			// NOTE: 마지막 떨어진 곳 사용, 안되면 빈곳
			int x = model.GetEmptyCol(model.lastCol);
			
			VUnit spawnedUnit = (x == -1 ? null : SpawnUnit(x));
			GameData.AddEarnedUnit(spawnedUnit.level);
			
			model.currUnit = spawnedUnit;
			
			//
			env.ShowNextUnit(GetNextUnit(true));
			
			FillNextUnits();
			
			model.ReorderUnits(model.currUnit, true);
			
			CheckArrows();
			
			SaveGameStatus();
		}
	}