//====================
// 포커 게임의 일부입니다.
//====================
	void ReplaceCard(Card target)
	{
		DoReplaceCardAsync(target).Forget();
	}
	async UniTaskVoid DoReplaceCardAsync(Card target)
	{
		CancellationToken ct = gameObject.GetCancellationTokenOnDestroy();
		
		//
		target.order = 301;
		
		int x = target.gx;
		int y = target.gy;
		
		float delay = 0.0f;
		
		await MoveBoardCardToTrash.Create(target, Vector3.zero, ref delay).Play().AwaitForComplete(cancellationToken: ct);
		
		model.RemoveCard(target);
		
		//
		Card card = null;
		Sequence seq = DOTween.Sequence();
		
		bool hasNextCard = model.hasNextCard;
		if(hasNextCard)
		{
			card = model.GetCardFromDeck(x, y);
			
			MoveDeckCardToBoard.Create(seq, card, new Vector3(env.GetRealX(x), env.GetRealY(y), 0), card.gy * env.cols + card.gx, ref delay);
			
			hasNextCard = model.hasNextCard;
		}
		if(hasNextCard)
		{
			card = model.GetCardFromDeck(-1, -1, true);
			
			if(!card.isFront)OpenDeckCard.Create(seq, card, ref delay);
		}
		if(seq.Duration() > 0.0f)
		{
			await seq.Play().AwaitForComplete(cancellationToken: ct);
		}
		
		//
		model.UpdateOrders();
		
		//
		if(!hasNextCard)
		{
			WillGameOver();
			
			OnOutOfCards();
		}
	}

//====================
	async UniTaskVoid DoNextAsync(List<Card> hands)
	{
		IncrQuest(hands.Count);
		
		//
		await DoMoveHandsToTrashAsync(hands);
		
		await DoDropBoardCardsAsync();
		
		await DoMoveDeckToBoardAsync();
		
		//
		if(!model.hasNextCard)
		{
			WillGameOver();
			OnOutOfCards();
		}
	}
	async UniTask DoMoveHandsToTrashAsync(List<Card> hands)
	{
		List<CardData> cards;
		HandType result = HandSolver.Solve(ref hands, out cards);
		
		if(hands[0].data.Equals(CardData.JOKER))hands[0].SetData(cards[0]);
		hands.Sort();
		
		if(result != HandType.ROYAL_STRAIGHT_FLUSH_NO_WILD && result != HandType.ROYAL_STRAIGHT_FLUSH && result != HandType.STRAIGHT_FLUSH && result != HandType.STRAIGHT)hands.Reverse();
		
		for(int i = 0; i < hands.Count; i += 1)hands[i].order = Mathf.RoundToInt(301 + i);
		
		Dictionary<int, List<Card>> groups = new Dictionary<int, List<Card>>();
		Dictionary<int, int> ranks;
		int[] rks;
		switch(result)
		{
			case HandType.ROYAL_STRAIGHT_FLUSH_NO_WILD:
			case HandType.FIVE_OF_A_KIND:
			case HandType.ROYAL_STRAIGHT_FLUSH:
			case HandType.STRAIGHT_FLUSH:
			case HandType.FLUSH:
			case HandType.STRAIGHT:
				groups[1] = hands;
				break;
				
			case HandType.FOUR_OF_A_KIND:
			case HandType.THREE_OF_A_KIND:
			case HandType.ONE_PAIR:
				ranks = new Dictionary<int, int>();
				hands.ForEach((c) => {
					if(!ranks.ContainsKey(c.data.rank))ranks[c.data.rank] = 0;
					ranks[c.data.rank] += 1;
				});
				rks = ranks.Keys.OrderBy((r) => -ranks[r]).ToArray();
				
				groups[1] = hands.Where((c) => (c.data.rank == rks[0])).ToList();
				break;
				
			case HandType.FULL_HOUSE:
			case HandType.TWO_PAIR:
				ranks = new Dictionary<int, int>();
				hands.ForEach((c) => {
					if(!ranks.ContainsKey(c.data.rank))ranks[c.data.rank] = 0;
					ranks[c.data.rank] += 1;
				});
				rks = ranks.Keys.OrderBy((r) => -ranks[r]).ToArray();
				
				groups[1] = hands.Where((c) => (c.data.rank == rks[0])).ToList();
				groups[2] = hands.Where((c) => (c.data.rank == rks[1])).ToList();
				break;
		}
		
		Sequence seq = DOTween.Sequence();
		
		float margin = -env.cardMargin;
		float cardWidth = -env.cardWidth;
		float groupScale = -env.groupScale;
		float sx = ((hands.Count - 1) * margin) * 0.5f;
		float delay = 0.0f;
		
		env.SetHandBackground(true);
		env.SetHandGroup1(false);
		env.SetHandGroup2(false);
		if(groups.ContainsKey(1))
		{
			env.SetHandGroup1(
				true,
				groups[1].Select((c) => new Vector3(sx + hands.IndexOf(c) * cardWidth, 0.0f, 0.0f)).Avg(),
				new Vector3(groups[1].Count * groupScale, groupScale, 0.0f)
			);
		}
		if(groups.ContainsKey(2))
		{
			env.SetHandGroup2(
				true,
				groups[2].Select((c) => new Vector3(sx + hands.IndexOf(c) * cardWidth, 0.0f, 0.0f)).Avg(),
				new Vector3(groups[2].Count * groupScale, groupScale, 0.0f)
			);
		}
		ShowResult.Create(seq, env.handBackground);
		
		for(int i = 0; i < hands.Count; i += 1)
		{
			MoveBoardCardToResult.Create(seq, hands[i], new Vector3(sx + i * 6.0f, 0.0f, 0.0f), ref delay);
		}
		await seq.Play().AwaitForComplete(cancellationToken: ct);
		
		await UniTask.Delay(600, cancellationToken: ct);
		
		hands.ForEach((c) => c.Release());
		
		env.SetHandBackground(false);
	}
	async UniTask DoDropBoardCardsAsync()
	{
		Card card = null;
		
		//
		float delay = 0.0f;
		
		Sequence seq = DOTween.Sequence();
		
		for(int y = env.rows - 1; y > -1; y -= 1)
		{
			for(int x = 0; x < env.cols; x += 1)
			{
				card = model.GetCard(x, y);
				if(card == null)continue;
				
				int ny = -1;
				for(int by = y + 1; by < env.rows; by += 1)
				{
					if(env.IsInside(x, by) && model.GetCard(x, by) != null)break;
					
					ny = by;
				}
				if(ny == -1)continue;
				
				if(card.gy == ny)continue;
				
				model.MoveCard(card, x, ny);
				
				DropBoardCard.Create(seq, card, env.GetRealY(card.gy), card.gy - y, ref delay);
			}
		}
		await seq.Play().AwaitForComplete(cancellationToken: ct);
		
		//
		model.UpdateOrders();
	}
	async UniTask DoMoveDeckToBoardAsync()
	{
		Card card = null;
		
		//
		float delay = 0.0f;
		
		Sequence seq = DOTween.Sequence();
		
		bool hasNextCard = model.hasNextCard;
		if(hasNextCard)
		{
			int loopLimit = model.gridPositions.Count;
			int x, y;
			for(int i = 0; i < loopLimit; i += 1)
			{
				x = model.gridPositions[i].x;
				y = model.gridPositions[i].y;
				
				if(model.GetCard(x, y) != null)continue;
				
				//
				card = model.GetCardFromDeck(x, y);
				
				if(card.isFront)
				{
					MoveDeckCardToBoard.Create(seq, card, new Vector3(env.GetRealX(x), env.GetRealY(y), 0), card.gy * env.cols + card.gx, delay);
					delay += MoveDeckCardToBoard.NextDelay();
				}
				else
				{
					OpenDeckCard.Create(seq, card, delay);
					MoveDeckCardToBoard.Create(seq, card, new Vector3(env.GetRealX(x), env.GetRealY(y), 0), card.gy * env.cols + card.gx, delay + OpenDeckCard.NextDelay());
					delay += MoveDeckCardToBoard.NextDelay();
				}
				
				hasNextCard = model.hasNextCard;
				
				//
				if(!hasNextCard)break;
			}
		}
		
		if(hasNextCard)
		{
			card = model.GetCardFromDeck(-1, -1, true);
			
			if(!card.isFront)OpenDeckCard.Create(seq, card, ref delay);
		}
		
		if(seq.Duration() > 0.0f)
		{
			await seq.Play().AwaitForComplete(cancellationToken: ct);
		}
		
		//
		model.UpdateOrders();
	}
	async UniTask _DoMoveTrashToDeck()
	{
		model.MovePoolToDeck();
		
		//
		float delay = 0.0f;
		
		Sequence seq = DOTween.Sequence();
		
		for(int i = 0; i < model.decks.Count; i += 1)
		{
			MoveTrashCardToDeck.Create(model.decks[i], new Vector3(env.deckX + i * env.deckSpan, env.deckY, 0), ref delay);
		}
		await seq.Play().AwaitForComplete(cancellationToken: ct);
		
		//
		await OpenDeckCard.Create(model.GetCardFromDeck(-1, -1, true)).Play().AwaitForComplete(cancellationToken: ct);
	}