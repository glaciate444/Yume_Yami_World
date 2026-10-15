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
    [Export] public string ResultBgmPath = "";
    [Export(PropertyHint.File, "*.tscn")]
    public string DefaultWorldMapScenePath = "res://Scene/WorldMap.tscn";

    private bool _isCounting = false;
    private bool _isTransitioning = false;
    private bool _anyKeyPressed = false;

    public override void _Ready() {
        // ボス戦等で変更されたタイムスケールを通常に戻す[cite: 22]
        Engine.TimeScale = 1.0;

        if (SoundManager.Instance != null && !string.IsNullOrEmpty(ResultBgmPath)) {
            SoundManager.Instance.PlayBGM(ResultBgmPath);
        }

        if (GameManager.Instance != null) {
            UpdateUI(GameManager.Instance.stageCoins, GameManager.Instance.totalCoins);
            _ = CoinCountRoutine();
        }
    }

    public override void _UnhandledInput(InputEvent @event) {
        // キーボードの任意のキー、またはコントローラーのボタンが押された瞬間を検知[cite: 22]
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
                // カウント中に押されたらループを抜けて即座に最終結果を表示する[cite: 22]
                _isCounting = false;
            } else {
                // カウント終了後に押されたら次のシーンへ遷移[cite: 22]
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

            if (SoundManager.Instance != null && CountSE != null) {
                SoundManager.Instance.PlaySE(CountSE);
            }
            await ToSignal(GetTree().CreateTimer(CountSpeed, true), SceneTreeTimer.SignalName.Timeout);
        }

        // 最終的な枚数を確定させてUIに反映[cite: 22]
        int finalTotalCoins = GameManager.Instance.totalCoins + GameManager.Instance.stageCoins;
        UpdateUI(0, finalTotalCoins);
        _isCounting = false;

        if (SoundManager.Instance != null && FinishSE != null) {
            SoundManager.Instance.PlaySE(FinishSE);
        }

        // GameManagerに保存してセーブ実行
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