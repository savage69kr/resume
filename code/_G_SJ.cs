//====================
// BusJam 류의 퍼즐 게임의 일부입니다.
//====================
	async UniTaskVoid CheckReady(Action onChecked=null)
	{
		waitGameReady = true;
		
		//
		ctsCheckReady = new CancellationTokenSource();
		var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ctsCheckReady.Token, gameObject.GetCancellationTokenOnDestroy());
		
		// unit이 움직이고
		if(refUnits.Any((g) => g.isBusy))
		{
			await UniTask.WaitUntil(() => !refUnits.Any((g) => g.isBusy), cancellationToken: linkedTokenSource.Token);
			
			UpdateMovable();
		}
		
		//
		while(true)
		{
			// box가 클리어면 나가고 다음것 들어오고
			if(boxes[0].isComplete)
			{
				MoveBox();
				
				if(boxes.Any((g) => g.isBusy))
				{
					await UniTask.WaitUntil(() => !boxes.Any((g) => g.isBusy), cancellationToken: linkedTokenSource.Token);
					
					UpdateMovable();
				}
				
				boxes.RemoveAt(0);
				
				// 새로운 box에 맞는 unit이 슬롯에 있으면 이동시키고
				if(!boxes.IsNullOrEmpty())
				{
					CheckSlotUnitMove();
					
					if(refUnits.Any((g) => g.isBusy))
					{
						await UniTask.WaitUntil(() => !refUnits.Any((g) => g.isBusy), cancellationToken: linkedTokenSource.Token);
						
						UpdateMovable();
					}
				}
				
				// 다음 box가 없으면 클리어
				if(boxes.IsNullOrEmpty())
				{
					ResetCancellationTokenSource();
					
					WillGameClear();
					return;
				}
			}
			
			if(!boxes[0].isComplete)break;
		}
		
		// slot이 꽉찼으면 게임오버
		if(env.slot.slotRemain == 0)
		{
			ResetCancellationTokenSource();
			
			WillGameOver();
			return;
		}
		
		// unit 상태 변경
		UpdateSecretState();
		UpdateChainState();
		UpdateFreezeState();
		UpdateBombState();
		
		await UniTask.WaitUntil(() => !refUnits.Any((g) => g.isBusy), cancellationToken: linkedTokenSource.Token);
		
		// bomb터진 unit 있으면 게임오버
		if(refUnits.Any((u) => u.isDead))
		{
			ResetCancellationTokenSource();
			
			WillGameOver();
			return;
		}
		
		// lock 상태 변경
		if(lockGroups.Count > 0)
		{
			UpdateLockGroupState();
			
			if(refUnits.Any((g) => g.isBusy))
			{
				await UniTask.WaitUntil(() => !refUnits.Any((g) => g.isBusy), cancellationToken: linkedTokenSource.Token);
			}
			if(refSpawners.Any((g) => g.isBusy))
			{
				await UniTask.WaitUntil(() => !refSpawners.Any((g) => g.isBusy), cancellationToken: linkedTokenSource.Token);
			}
		}
		
		// spawner 동작
		if(refSpawners.Count > 0 && refSpawners.Any((g) => g.canSpawn))
		{
			SpawnUnit();
			
			// unit 상태 변경: 이건 생성된 유닛의 이동이 secret해제 애니랑 관계없다는 가정
			UpdateSecretState();
			
			if(refUnits.Any((g) => g.isBusy))
			{
				await UniTask.WaitUntil(() => !refUnits.Any((g) => g.isBusy), cancellationToken: linkedTokenSource.Token);
				
				UpdateMovable();
			}
			if(refSpawners.Any((g) => g.isBusy))
			{
				await UniTask.WaitUntil(() => !refSpawners.Any((g) => g.isBusy), cancellationToken: linkedTokenSource.Token);
			}
		}
		
		//
		waitGameReady = false;
		
		//
		// NOTE: 최초 레벨 생성할때와 continue에서만 사용
		if(onChecked != null)onChecked();
	}