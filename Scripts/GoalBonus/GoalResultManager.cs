using Godot;
using System;
using System.Threading.Tasks;

public partial class GoalResultManager : Control {
    [ExportCategory("UI設定")]
    [Export] public Label StageCoinText;
    [Export] public Label TotalCoinText;

    [ExportCategory("演出設定")]
    [Export] public float CountSpeed = 0.05f;
    [Export] public AudioStream CountSE;
    [Export] public AudioStream FinishSE;

    [ExportCategory("サウンド・遷移設定")]
    [Export] public AudioStream ResultBGM;
    [Export(PropertyHint.File, "*.tscn")]
    public string DefaultWorldMapScenePath = "res://Scene/WorldMap.tscn";

    private bool _isCounting = false;
    private bool _isTransitioning = false;
    private bool _anyKeyPressed = false;

    // 集計画面専用のサウンドプレイヤー（SoundManagerに依存せず確実に鳴らす用）
    private AudioStreamPlayer _sePlayer;
    private AudioStreamPlayer _bgmPlayer;

    public override void _Ready() {
        Engine.TimeScale = 1.0;

        // 音再生用のノードを自動準備
        _sePlayer = new AudioStreamPlayer();
        AddChild(_sePlayer);

        if (ResultBGM != null) {
            _bgmPlayer = new AudioStreamPlayer();
            _bgmPlayer.Stream = ResultBGM;
            AddChild(_bgmPlayer);
            _bgmPlayer.Play();
        }

        if (GameManager.Instance != null) {
            UpdateUI(GameManager.Instance.stageCoins, GameManager.Instance.totalCoins);
            _ = CoinCountRoutine();
        }
    }

    // 効果音を確実に鳴らすヘルパーメソッド
    private void PlayLocalSE(AudioStream se) {
        if (se == null) return;

        if (SoundManager.Instance != null) {
            SoundManager.Instance.PlaySE(se);
        } else if (_sePlayer != null) {
            _sePlayer.Stream = se;
            _sePlayer.Play();
        }
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo) {
            _anyKeyPressed = true;
        } else if (@event is InputEventJoypadButton joyEvent && joyEvent.Pressed) {
            _anyKeyPressed = true;
        }
    }

    public override void _Process(double delta) {
        if (_isTransitioning) return;

        bool isButtonPressed = _anyKeyPressed ||
                               Input.IsActionJustPressed("attack") ||
                               Input.IsActionJustPressed("jump") ||
                               Input.IsActionJustPressed("dash") ||
                               Input.IsActionJustPressed("ui_accept");
        _anyKeyPressed = false;

        if (isButtonPressed) {
            if (_isCounting) {
                _isCounting = false;
            } else {
                _ = WaitAndTransitionRoutine();
            }
        }
    }

    private async Task CoinCountRoutine() {
        _isCounting = true;

        int currentStageCoins = GameManager.Instance.stageCoins;
        int currentTotalCoins = GameManager.Instance.totalCoins;

        await ToSignal(GetTree().CreateTimer(0.5f, true), SceneTreeTimer.SignalName.Timeout);

        while (currentStageCoins > 0 && _isCounting) {
            currentStageCoins--;
            currentTotalCoins++;
            UpdateUI(currentStageCoins, currentTotalCoins);

            // カウント音を鳴らす
            PlayLocalSE(CountSE);

            await ToSignal(GetTree().CreateTimer(CountSpeed, true), SceneTreeTimer.SignalName.Timeout);
        }

        int finalTotalCoins = GameManager.Instance.totalCoins + GameManager.Instance.stageCoins;
        UpdateUI(0, finalTotalCoins);
        _isCounting = false;

        // 完了音を鳴らす
        PlayLocalSE(FinishSE);

        GameManager.Instance.totalCoins = finalTotalCoins;
        GameManager.Instance.stageCoins = 0;
        GameManager.Instance.SaveGame();
    }

    private void UpdateUI(int stageCoins, int totalCoins) {
        if (StageCoinText != null) StageCoinText.Text = stageCoins.ToString("D3");
        if (TotalCoinText != null) TotalCoinText.Text = totalCoins.ToString("D6");
    }

    private async Task WaitAndTransitionRoutine() {
        _isTransitioning = true;
        await ToSignal(GetTree().CreateTimer(1.0f, true), SceneTreeTimer.SignalName.Timeout);

        string nextScene = DefaultWorldMapScenePath;

        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.returnMapSceneName)) {
            nextScene = GameManager.Instance.returnMapSceneName;
        }

        if (SceneTransitionManager.Instance != null) {
            SceneTransitionManager.Instance.LoadScene(nextScene, TransitionType.Fade);
        } else if (!string.IsNullOrEmpty(nextScene)) {
            GetTree().ChangeSceneToFile(nextScene);
        }
    }
}