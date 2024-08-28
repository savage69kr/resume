using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using D = Unit5.WebPlayerDebug;
#else
using D = Unit5.DebugTool;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unit5;
using Unit5.MiniJSON;

//----------
namespace Unit5.Service
{

//----------
sealed public class PromotionControl
{
	
	//----------
	const string PROMOTION_URL = "_PROMOTION_URL";
	const string PROMOTION_CACHE = "_PROMOTION_CACHE";
	
	//----------
	static public bool isReady{get; private set;}
	static public bool hasData{get; private set;}
	
	//----------
	static public List<PromotionInfo> infos{get; private set;}
	
	//----------
	static public PromotionInfo Get(bool notInstalled=false)
	{
		if(!hasData)return null;
		
		//
		List<PromotionInfo> samples = (notInstalled ? GetNotInstalled() : infos);
		if(samples.IsNullOrEmpty())samples = infos;
		if(samples.IsNullOrEmpty())return null;
		
		if(samples.Count == 1)return samples[0];
		
		//
		if(notInstalled)return GetNotInstalled().Choice();
		
		int[] weights = samples.Select((d) => d.weight).ToArray();
		int weightTotal = weights.Sum();
		
		int dataIndex = MathUtil.RandomChoice(weights, weightTotal);
		dataIndex = Mathf.Max(0, Mathf.Min(infos.Count - 1, dataIndex));
		return infos[dataIndex];
	}
	static public PromotionInfo Get(string appId)
	{
		if(!hasData)return null;
		
		return infos.Find(i => i.appId == appId);
	}
	
	static public List<PromotionInfo> GetNotInstalled()
	{
		if(!hasData)return null;
		
		return infos.Where(i => !IsInstalled(i)).ToList();
	}
	
	static public List<PromotionInfo> GetAll(PromotionReward type)
	{
		if(!hasData)return null;
		
		return infos.Where(i => i.reward == type).ToList();
	}
	static public List<PromotionInfo> GetAll(bool hasExtra)
	{
		if(!hasData)return null;
		
		return infos.Where(i => !string.IsNullOrEmpty(i.extra)).ToList();
	}
	static public List<PromotionInfo> GetAll(PromotionReward type, bool hasExtra)
	{
		if(!hasData)return null;
		
		return infos.Where(i => i.reward == type && !string.IsNullOrEmpty(i.extra)).ToList();
	}
	
	//----------
	static public void CheckData(string url, Action<bool> callback)
	{
		if(string.IsNullOrEmpty(url))
		{
			callback(false);
			return;
		}
		
		if(hasData)
		{
			callback(true);
			return;
		}
		
		//
		string oldCacheFilepath = EncryptedPlayerPrefs.GetString(PROMOTION_CACHE, string.Empty);
		string newCacheFilepath = RequestUtil.GetCacheFilepath(url);
		
		FetchData(url, newCacheFilepath, (bool success) => {
			if(success)
			{
				if(!string.IsNullOrEmpty(oldCacheFilepath))
				{
					StreamingAssetUtil.RemoveFile(oldCacheFilepath);
				}
				
				EncryptedPlayerPrefs.SetString(PROMOTION_URL, url);
				EncryptedPlayerPrefs.SetString(PROMOTION_CACHE, newCacheFilepath);
			}
			
			callback((success && hasData));
		});
	}
	
	//----------
	static public void FetchData(string url, Action<bool> callback)
	{
		FetchData(url, null, callback);
	}
	static public void FetchData(string url, string cacheFilepath, Action<bool> callback)
	{
		RequestUtil.Text(url, cacheFilepath, (string data) => {
#if !NO_DEBUG
			D.Log($"[UNIT5::PromotionControl] FetchData:: url={url} / cache={cacheFilepath} / data={data}");
#endif
			
#if UNITY_EDITOR
			if(string.IsNullOrEmpty(data))
			{
				MakeTestData();
				
				callback(true);
				return;
			}
#endif
			
			if(string.IsNullOrEmpty(data))
			{
				isReady = false;
				hasData = false;
				
				callback(false);
			}
			else
			{
				isReady = true;
				
				try
				{
					Dictionary<string, object> json = Json.Deserialize(data) as Dictionary<string, object>;
					
					string appId = UnityUtil.GetApplicationID();
					
					infos = json.GetDictionaryList("p")
						.Select((d) => new PromotionInfo(d))
						.Where((i) => i.appBundleId != appId)
						.ToList()
					;
					
					hasData = (infos.Count > 0);
				}
				catch
				{
					hasData = false;
				}
				
				callback(hasData);
			}
		});
	}
	
#if UNITY_EDITOR
	static void MakeTestData()
	{
		isReady = true;
		hasData = true;
		
		infos = new List<PromotionInfo>(){
			new PromotionInfo(){
				appBundleId = "com.ftt.cubie.aos",
				appId = "com.ftt.cubie.aos",
				appScheme = "cubieadventure",
				
				weight = 1,
				
				appName = "Cubie Adventure",
				appDesc = "Adventure with your Cubie and Cupet friends!\nFrom cute looks and immersive gameplay! Cubie Adventure welcomes you!",
				appLink = "http://onelink.to/mjnhpp",
				
				imgThumb = "https://share.unit5soft.com/_crosspromotion/ca_thumb.png",
				imgBanner = "https://share.unit5soft.com/_crosspromotion/ca_banner.jpg",
				imgPopup = "https://share.unit5soft.com/_crosspromotion/ca_full.jpg",
				
				vidClip = "https://share.unit5soft.com/_crosspromotion/ca_clip.mp4",
				
				reward = PromotionReward.None,
				
				extra = "",
			},
		};
	}
#endif
	
	//----------
	static public bool IsInstalled(PromotionInfo info)
	{
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_TVOS || UNITY_IPHONE)
		return UnityUtil.IsInstalled(info.appScheme);
#else
		return UnityUtil.IsInstalled(info.appId);
#endif
	}
	
	//----------
	static public bool IsMarked(string appId)
	{
		return (EncryptedPlayerPrefs.GetInt("_XP_" + appId, 0) == 1);
	}
	static public void MarkApp(string appId)
	{
		EncryptedPlayerPrefs.SetInt("_XP_" + appId, 1);
	}
}

}
