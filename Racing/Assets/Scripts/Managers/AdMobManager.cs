using System;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdMobManager : MonoBehaviour
{
    [SerializeField] private bool disableAds = false;
    [SerializeField] private bool testAds = false;
    
#if UNITY_EDITOR
    private string _bannerAdId = "ca-app-pub-3940256099942544/2934735716";
    private string _interstitialAdId = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_ANDROID
    private string _bannerAdId = "unused";
    private string _interstitialAdId = "unused";
#elif UNITY_IPHONE
    private string _bannerAdId = "unused";
    private string _interstitialAdId = "ca-app-pub-5943998976493272/2807529765";
#else
    private string _bannerAdId = "unused";
    private string _interstitialAdId = "unused";
#endif

    public bool isInitialized = false;
    
    private BannerView _bannerView;
    private InterstitialAd _interstitialAd;
    
    private AdRequest _bannerRequest;
    
    private static AdMobManager _instance;

    private float _lastBannerRequest = -60f;
    private float _lastInterstitialShown = 0f;
    
    private void Awake()
    {
        if (_instance)
        {
            DestroyImmediate(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        _instance = this;
        
        if (testAds)
        {
            _bannerAdId = "ca-app-pub-3940256099942544/2934735716";
            _interstitialAdId = "ca-app-pub-3940256099942544/1033173712";
        }
    }

    public static AdMobManager Get()
    {
        return _instance;
    }

    public void InitializeAds()
    {
        if (disableAds) return;
        
        if (isInitialized)
        {
            LoadInterstitialAd();
            return;
        }

        try
        {
            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("Ads initialized");
                isInitialized = true;
                LoadInterstitialAd();
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void LoadBanner()
    {
        if (!isInitialized) return;
        
        if (_bannerView == null)
        {
            CreateBannerView();
        }

        if (Time.time - _lastBannerRequest > 90f)
        {
            _bannerRequest = new AdRequest();
            Debug.Log("Making new banner request.");
            Debug.Log("Loading banner ad.");
            _bannerView?.LoadAd(_bannerRequest);
            _lastBannerRequest = Time.time;
        }
    }
    
    public void CreateBannerView()
    {
        Debug.Log("Creating banner view");
        
        if (_bannerView != null)
        {
            DestroyBannerView();
        }
        
        _bannerView = new BannerView(_bannerAdId, AdSize.Banner, AdPosition.Bottom);
    }
    
    public void DestroyBannerView()
    {
        if (_bannerView == null) return;
        
        Debug.Log("Destroying banner view.");
        _bannerView.Destroy();
        _bannerView = null;
    }
    
    public void LoadInterstitialAd()
    {
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        Debug.Log("Loading the interstitial ad.");
        
        AdRequest adRequest = new();
        
        InterstitialAd.Load(_interstitialAdId, adRequest, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("interstitial ad failed to load with error : " + error);
                    return;
                }

                Debug.Log("Interstitial ad loaded");

                _interstitialAd = ad;
                
                RegisterReloadHandler(_interstitialAd);
            });
    }
    
    public void ShowInterstitialAd(Action onAdFinish)
    {
        bool cooldownReady = Time.time - _lastInterstitialShown > 90f;
        
        if (_interstitialAd != null && _interstitialAd.CanShowAd() && cooldownReady)
        {
            Debug.Log("Showing interstitial ad.");

            Time.timeScale = 0f;
            Settings.Get().MuteGame();
            
            void OnAdClosed()
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    Time.timeScale = 1f;
                    Settings.Get().RestoreVolume();
                    
                    try
                    {
                        _interstitialAd.OnAdFullScreenContentClosed -= OnAdClosed;
                    }
                    catch
                    {
                        Debug.Log("Action already unsubscribed");
                    }
                    
                    _lastInterstitialShown = Time.time;
                    onAdFinish?.Invoke();
                });
            }
            
            _interstitialAd.OnAdFullScreenContentClosed += OnAdClosed;
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("Interstitial ad is not ready yet.");
            
            MainThreadDispatcher.Enqueue(() => onAdFinish?.Invoke());
        }
    }
    
    private void RegisterReloadHandler(InterstitialAd interstitialAd)
    {
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial Ad full screen content closed.");
            
            LoadInterstitialAd();
        };
        
        interstitialAd.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content " + "with error : " + error);
            
            LoadInterstitialAd();
        };
    }
}