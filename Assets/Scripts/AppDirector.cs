using System;
using UniRx;
using UnityEngine;
using static Enums;

public class AppDirector : MonoBehaviour
{
    public static AppDirector I { get; private set; }

    [SerializeField] CardDatabase cardDB;
    [SerializeField] SoundManager soundManager;

    public CardDatabase CardDB => cardDB;
    public SoundManager Sound => soundManager;
    public Difficulty SelectedDifficulty { get; private set; } = Difficulty.Normal;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    void Start()
    {
        Observable.Interval(TimeSpan.FromSeconds(5))
            .Where(_ => DataManager.SaveFlag)
            .Subscribe(_ => DataManager.Save())
            .AddTo(this);
    }

    void Initialize()
    {
#if UNITY_ANDROID || UNITY_IOS
    Screen.sleepTimeout = SleepTimeout.NeverSleep;
    Application.targetFrameRate = 60;
    Application.runInBackground = false;
#else
        Application.targetFrameRate = 144;
        Application.runInBackground = true;
#endif

        if (cardDB != null) cardDB.Initialize();

        bool isNewUser = DataManager.Load();
        if (isNewUser) SetupNewUserData();

        soundManager?.Initialize();
    }

    private void SetupNewUserData()
    {
        for (int i = 0; i < 8; i++)
            CardManager.Add(i);

        for (int i = 0; i < DeckManager.MaxDeckSize; i++)
            DeckManager.AddToDeck(i);

        DataManager.Save();
    }

    public void SetDifficulty(Difficulty difficulty)
    {
        SelectedDifficulty = difficulty;
    }

    void OnApplicationPause(bool pause)
    {
        if (pause) DataManager.Save();
    }

    void OnApplicationQuit()
    {
        DataManager.Save();
    }
}
