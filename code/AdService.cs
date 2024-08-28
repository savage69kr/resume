using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using D = Unit5.WebPlayerDebug;
#else
using D = Unit5.DebugTool;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Unit5.Relays;

//----------
namespace Unit5
{

//----------
sealed public partial class AdControl : SingletonMB<AdControl>
{
	
	//----------
	public enum AdState
	{
		None,
		
		AdLoaded,
		AdFailedToLoad,
		AdOpening,
		AdFailedToShow,
		UserEarnedReward,
		AdClosed,
		
		AdWaitReward,
	}
	
	//----------
	AdServiceImpl _impl = null;
	
	//----------
	public AdServiceImpl impl => _impl;
	
	public bool available => (_impl != null && _impl.available);
	public bool isInited => (_impl != null && _impl.isInited);
	
	//----------
	public IRelayLink<bool> onInitialized => _impl?.onInitialized;
	public IRelayLink<float> onOpenedAd => _impl?.onOpenedAd;
	
	public int reqCountBanner => (_impl != null ? _impl.reqCountBanner : 0);
	public int reqCountInterstitial => (_impl != null ? _impl.reqCountInterstitial : 0);
	public int reqCountRewardVideo => (_impl != null ? _impl.reqCountRewardVideo : 0);
	
	//----------
	public bool GetGDPRConsent()
	{
		if(_impl == null)return false;
		return _impl.GetGDPRConsent();
	}
	public void SetGDPRConsent(bool confirm)
	{
		if(_impl == null)return;
		_impl.SetGDPRConsent(confirm);
	}
	
	//----------
	public void Init(AdServiceImpl impl, Dictionary<string, string> options=null, bool disabledDefaultAd=false, bool checkGDPRConsent=false, bool autoFetch=true, Action<bool> onInitialized=null)
	{
		_impl = impl;
		_impl.Init(options, disabledDefaultAd, checkGDPRConsent, autoFetch, onInitialized);
	}
	public void Init(Dictionary<string, string> options=null, bool disabledDefaultAd=false, bool checkGDPRConsent=false, bool autoFetch=true, Action<bool> onInitialized=null)
	{
		if(_impl == null)
		{
#if ADS_NONE
			_impl = new AdServiceImpl();
#elif ADS_ADMOB
			_impl = new AdServiceAdmob();
#elif UNITY_EDITOR || ADS_FAKE
			_impl = new AdServiceEditor();
#elif ADS_APPLOVIN
			_impl = new AdServiceAppLovin();
#elif ADS_IRONSOURCE
			_impl = new AdServiceIronSource();
#elif ADS_MINTEGRAL
			_impl = new AdServiceMintegral();
#elif ADS_CRAZYGAMES
			_impl = new AdServiceCrazyGames();
#elif ADS_GAMEDISTRIBUTION
			_impl = new AdServiceGameDistribution();
#else
			_impl = new AdServiceImpl();
#endif
		}
		
		_impl.Init(options, disabledDefaultAd, checkGDPRConsent, autoFetch, onInitialized);
	}
	
	//----------
	public bool showBanner => (_impl != null && _impl.showBanner);
	public bool isVisibleBanner => (_impl != null && _impl.isVisibleBanner);
	public int bannerHeight => (_impl != null ? _impl.bannerHeight : -1);
	
	public void SetFirstBannerDelay(int duration)
	{
		if(_impl == null)return;
		_impl.SetFirstBannerDelay(duration);
	}
	public void SetBannerPosition(AdBannerPositionType position)
	{
		if(_impl == null)return;
		_impl.SetBannerPosition(position);
	}
	
	public void ShowBanner(bool forceReload=false)
	{
		if(_impl == null)return;
		_impl.ShowBanner(forceReload);
	}
	public void HideBanner(bool forceDestroy=false)
	{
		if(_impl == null)return;
		_impl.HideBanner(forceDestroy);
	}
	
	//----------
	public bool isInterstitialTimeout => (_impl != null ? _impl.isInterstitialTimeout : false);
	
	public bool CanShowInterstitial()
	{
		if(_impl == null)return false;
		return _impl.CanShowInterstitial();
	}
	
	public void ShowInterstitial(Action<bool> onComplete=null, int timeout=10)
	{
		if(_impl == null)
		{
			onComplete(false);
			return;
		}
		
		_impl.ShowInterstitial(onComplete, timeout);
	}
	public void CancelInterstitial()
	{
		if(_impl == null)return;
		_impl.CancelInterstitial();
	}
	public void FetchInterstitial()
	{
		if(_impl == null)return;
		_impl.FetchInterstitial();
	}
	
	//----------
	public bool isRewardVideoTimeout => (_impl != null ? _impl.isRewardVideoTimeout : false);
	
	public bool CanShowRewardVideo()
	{
		if(_impl == null)return false;
		return _impl.CanShowRewardVideo();
	}
	
	public void ShowRewardVideo(Action<bool> onComplete=null, int timeout=10)
	{
		if(_impl == null)
		{
			onComplete(false);
			return;
		}
		
		_impl.ShowRewardVideo(onComplete, timeout);
	}
	public void CancelRewardVideo()
	{
		if(_impl == null)return;
		_impl.CancelRewardVideo();
	}
	public void FetchRewardVideo()
	{
		if(_impl == null)return;
		_impl.FetchRewardVideo();
	}
	
	//----------
	public void FetchAll()
	{
		if(_impl == null)return;
		_impl.FetchAll();
	}
	
	//----------
	public void SetNoAdsPurchased(bool purchased)
	{
		if(_impl == null)return;
		_impl.SetNoAdsPurchased(purchased);
	}
	
	//----------
	public void OpenTestSuite()
	{
		if(_impl == null)return;
		_impl.OpenTestSuite();
	}
}

}
