using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Enums;

public class TitleSceneDirector : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] ShopPanel shopPanel;
    [SerializeField] CollectionPanel collectionPanel;
    [SerializeField] DeckPanel deckPanel;
    [SerializeField] SettingsPanel settingsPanel;
    [SerializeField] GameObject difficultyPanel;
    [SerializeField] GameObject quitPanel;

    [Header("Buttons")]
    [SerializeField] Button shopButton;
    [SerializeField] Button collectionButton;
    [SerializeField] Button deckButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button startButton;
    [SerializeField] Button quitButton;

    [Header("Difficulty")]
    [SerializeField] Button easyButton;
    [SerializeField] Button normalButton;
    [SerializeField] Button hardButton;
    [SerializeField] Button difficultyCloseButton;

    [Header("Quit")]
    [SerializeField] Button quitConfirmButton;
    [SerializeField] Button quitCancelButton;

    [Header("Gold")]
    [SerializeField] TextMeshProUGUI goldText;

    [Header("Scene")]
    [SerializeField] string battleSceneName = "BattleScene";

    void Start()
    {
        AppDirector.I.Sound?.PlayBgm(BgmKey.Title);
        BindButtons();
        RefreshGoldUI();
    }

    private void BindButtons()
    {
        shopButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); shopPanel?.Open(); })
            .AddTo(this);

        collectionButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); collectionPanel?.Open(); })
            .AddTo(this);

        deckButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); deckPanel?.Open(); })
            .AddTo(this);

        settingsButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); settingsPanel?.Open(); })
            .AddTo(this);

        startButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); TryStartGame(); })
            .AddTo(this);

        quitButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); quitPanel?.SetActive(true); })
            .AddTo(this);

        easyButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); StartBattle(Difficulty.Easy); })
            .AddTo(this);

        normalButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); StartBattle(Difficulty.Normal); })
            .AddTo(this);

        hardButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); StartBattle(Difficulty.Hard); })
            .AddTo(this);

        difficultyCloseButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); difficultyPanel?.SetActive(false); })
            .AddTo(this);

        quitConfirmButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); Application.Quit(); })
            .AddTo(this);

        quitCancelButton?.OnClickAsObservable()
            .Subscribe(_ => { AppDirector.I.Sound?.PlaySfx(SfxKey.ButtonClick); quitPanel?.SetActive(false); })
            .AddTo(this);
    }

    private void TryStartGame()
    {
        if (!DeckManager.IsDeckComplete())
        {
            Debug.Log("덱 구성이 완료 되지 않았습니다.");
            return;
        }
        difficultyPanel?.SetActive(true);
    }

    private void StartBattle(Difficulty difficulty)
    {
        AppDirector.I.SetDifficulty(difficulty);
        difficultyPanel?.SetActive(false);
        SceneManager.LoadScene(battleSceneName);
    }

    public void RefreshGoldUI()
    {
        if (goldText != null)
            goldText.text = $"{DataManager.Data.Gold}";
    }
}
