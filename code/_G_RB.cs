//====================
// 퍼즐 게임의 일부입니다.
//====================
	HashSet<int> checkedPairs = new HashSet<int>();
	HashSet<VUnit> cachedRemovableUnits = new HashSet<VUnit>();
	
	VUnit[] targetUnits;
	
	bool waitRemoveConnectUnits = false;
	
	void UpdateJoints()
	{
		bool hasRemovableUnits = (!removableUnits.IsNullOrEmpty());
		if(hasRemovableUnits)removableUnitJoints = new List<UnitBallJoint>();
		
		cachedRemovableUnits.Clear();
		for(int i = 0; i < removableUnits.Count; i += 1)cachedRemovableUnits.Add(removableUnits[i]);
		
		checkedPairs.Clear();
		int pairId = 0;
		
		VUnit unit1 = null;
		VUnit unit2 = null;
		UnitBallJoint joint = null;
		
		Vector3 pos1 = Constants.V0;
		Vector3 pos2 = Constants.V0;
		
		bool forceRemoveJoint = false;
		float dist = 0.0f;
		int distCache = 0;
		float distPxl = 0.0f;
		float distSpanPxl = 0.0f;
		int jointType = 0;
		
		targetUnits = units.Where((u) => (u.type == UnitType.Ball)).ToArray();
		
		int total = targetUnits.Length;
		int i, j;
		for(i = 0; i < total; i += 1)
		{
			unit1 = targetUnits[i];
			
			for(j = total - 1; j > -1; j -= 1)
			{
				if(i == j)continue;
				
				//
				unit2 = targetUnits[j];
				
				pairId = (1 + Mathf.Max(unit1.hash, unit2.hash)) * 1000 + (1 + Mathf.Min(unit1.hash, unit2.hash));
				if(checkedPairs.Contains(pairId))continue;
				checkedPairs.Add(pairId);
				
				if(!unit1.Equals(unit2))continue;
				
				if(!VUnit.CanConnect(unit1, unit2))
				{
					RemoveJoint(pairId);
					continue;
				}
				
				if(!unit1.isReady || !unit2.isReady)
				{
					RemoveJoint(pairId);
					continue;
				}
				
				if(unit1.willRemove || unit2.willRemove)
				{
					RemoveJoint(pairId);
					continue;
				}
				
				//
				forceRemoveJoint = (hasRemovableUnits ? (cachedRemovableUnits.Contains(unit1) || cachedRemovableUnits.Contains(unit2)) : false);
				
				pos1 = unit1.pos;
				pos2 = unit2.pos;
				
				dist = Vector3.Distance(pos1, pos2);
				if(dist > env.unitDistanceMax || forceRemoveJoint)
				{
					RemoveJoint(pairId, forceRemoveJoint);
					continue;
				}
				
				distCache = Mathf.FloorToInt(dist * 10000);
				if(cachedJointDistances.ContainsKey(pairId) && cachedJointDistances[pairId] == distCache)continue;
				cachedJointDistances[pairId] = distCache;
				
				distPxl = dist * env.TO_PXL;
				distSpanPxl = distPxl - env.unitPxlSize;
				
				//
				if(!cachedJoints.ContainsKey(pairId))cachedJoints[pairId] = SpawnJoint(env.canvas, unit1.shape);
				joint = cachedJoints[pairId];
				
				jointType = env.CalcJointType(distSpanPxl);
				joint.UpdateLinks(unit1.shape, jointType, pos1, pos2, (distPxl - env.unitSpansEx[jointType]) * env.TO_UNT);
			}
		}
		
		if(hasRemovableUnits)
		{
			waitRemoveConnectUnits = true;
			
			units = units.Except(removableUnits).ToList();
			
			units.ForEach((u) => {
				u.isEnabled = false;
				u.ResetAllVelocity();
			});
			
			//
			removableUnits.ForEach((u) => u.isEnabled = false);
			
			RemoveConnected(
				new List<VUnit>(removableUnits.ToArray()),
				new List<UnitBallJoint>(removableUnitJoints.ToArray())
			);
			
			removableUnits.Clear();
			removableConnectedUnits.Clear();
			removableUnitJoints.Clear();
		}
		else
		{
			if(waitRemoveConnectUnits)
			{
				units.ForEach((u) => u.isEnabled = true);
				
				waitRemoveConnectUnits = false;
			}
		}
	}
	void UpdateRemovableUnitJoints()
	{
		if(removableConnectedUnits.IsNullOrEmpty())return;
		
		//
		checkedPairs.Clear();
		int pairId = 0;
		
		VUnit unit1 = null;
		VUnit unit2 = null;
		UnitBallJoint joint = null;
		
		Vector3 pos1 = Constants.V0;
		Vector3 pos2 = Constants.V0;
		
		float dist = 0.0f;
		int distCache = 0;
		float distPxl = 0.0f;
		float distSpanPxl = 0.0f;
		int jointType = 0;
		
		int total = removableConnectedUnits.Count;
		int i, j;
		for(i = 0; i < total; i += 1)
		{
			unit1 = removableConnectedUnits[i];
			
			for(j = total - 1; j > -1; j -= 1)
			{
				if(i == j)continue;
				
				//
				unit2 = removableConnectedUnits[j];
				
				pairId = (1 + Mathf.Max(unit1.hash, unit2.hash)) * 1000 + (1 + Mathf.Min(unit1.hash, unit2.hash));
				if(checkedPairs.Contains(pairId))continue;
				checkedPairs.Add(pairId);
				
				//
				pos1 = unit1.pos;
				pos2 = unit2.pos;
				
				dist = Vector3.Distance(pos1, pos2);
				if(dist > env.unitDistanceMax)
				{
					RemoveJoint(pairId);
					continue;
				}
				
				distCache = Mathf.FloorToInt(dist * 10000);
				if(cachedJointDistances.ContainsKey(pairId) && cachedJointDistances[pairId] == distCache)continue;
				cachedJointDistances[pairId] = distCache;
				
				distPxl = dist * env.TO_PXL;
				distSpanPxl = distPxl - env.unitPxlSize;
				
				//
				if(!cachedJoints.ContainsKey(pairId))cachedJoints[pairId] = SpawnJoint(env.canvas, unit1.shape);
				joint = cachedJoints[pairId];
				
				jointType = env.CalcJointType(distSpanPxl);
				joint.UpdateLinks(unit1.shape, jointType, pos1, pos2, (distPxl - env.unitSpansEx[jointType]) * env.TO_UNT);
			}
		}
	}
	
	void RemoveJoint(int pairId, bool forceRemoveJoint=false)
	{
		if(cachedJointDistances.ContainsKey(pairId))cachedJointDistances.Remove(pairId);
		if(cachedJoints.ContainsKey(pairId))
		{
			if(forceRemoveJoint)
			{
				removableUnitJoints.Add(cachedJoints[pairId]);
			}
			else
			{
				cachedJoints[pairId].Release();
			}
			
			cachedJoints.Remove(pairId);
		}
	}
	void RemoveConnected(List<VUnit> units, List<UnitBallJoint> joints)
	{
		// TODO: 이 흐름을 더 합리적으로
		List<VUnit> sideEffectedUnits = new List<VUnit>();
		
		units.ForEach((u) => {
			if(u.type != UnitType.Stone && u.type != UnitType.Minion)
			{
				sideEffectedUnits.AddRange(FindNeighborUnits(units, u, env.explodeDistanceMax));
			}
			
			KillUnit(u);
			
			ReleaseUnit(u);
		});
		joints.ForEach((j) => j.Release(false));
		
		//
		sideEffectedUnits = sideEffectedUnits.Distinct().Where((u) => (u != null && !u.willRemove)).ToList();
		if(sideEffectedUnits.IsNullOrEmpty())
		{
			units.ForEach((u) => u.isEnabled = true);
			
			waitRemoveConnectUnits = false;
		}
		else
		{
			TaskUtil.DelayCall(0.15f, () => {
				sideEffectedUnits.ForEach((u) => {
					// if(u.type == UnitType.Ball && (u.behavior == UnitBehaviorType.Anchor || u.behavior == UnitBehaviorType.Freeze))
					if(u.behavior == UnitBehaviorType.Anchor || u.behavior == UnitBehaviorType.Freeze)
					{
						ActKillUnit(u);
					}
					else if(u.type == UnitType.Bomb)
					{
						ActBomb(u);
					}
					else if(u.type == UnitType.Stone)
					{
						ActKillUnit(u);
					}
				});
			});
		}
	}