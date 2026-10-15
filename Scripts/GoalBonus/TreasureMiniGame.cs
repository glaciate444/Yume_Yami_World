using Godot;
using System;

public partial class TreasureMiniGame : Control {
    [ExportCategory("宝箱の設定")]
    // マウスを使わないため、単なる画像ノード（TextureRect）8個で管理します
    [Export] public TextureRect[] ChestImages = Array.Empty<TextureRect>();
    [Export] public Texture2D OpenChestSprite;

    // すべてコインなので数値配列だけでOK（マイナスを入れれば減点箱にもなります）
    [Export] public int[] RewardAmounts = new int[8] { 5, 10, 10, 20, 20, 30, 50, -10 };

    [ExportCategory("UIナビゲーション設定")]
    [Export] public Control CursorImage;
    [Export] public Control[] MenuPositions = Array.Empty<Control>(); // 0〜7:宝箱, 8:SKIP
    [Export] public Vector2 CursorOffset = Vector2.Zero;

    [ExportCategory("サウンド・遷移設定")]
    [Export] public string MiniGameBgmPath = "";
    [Export(PropertyHint.File, "*.tscn")]
    public string ResultScenePath = "res://Scene/ResultScene.tscn";

    [ExportCategory("HUD連携")]
    [Export] public Label HudCoinText;
    [Export] public Label RemainingOpensText;

    [ExportCategory("ポップアップ演出")]
    [Export] public PackedScene RewardPopupPrefab;
    [Export] public Texture2D CoinIconSprite;

    private bool[] _isChestOpened = new bool[8];
    private int _openedCount = 0;
    private const int MAX_OPENS = 2;
    private bool _isGameOver = false;

    private int _currentIndex = 0;
    private float _inputCooldown = 0f;

    // 9箇所のカーソル移動テーブル（上, 下, 左, 右）
    private readonly int[,] _navigation = new int[9, 4] {
        { 0, 4, 0, 1 }, { 1, 5, 0, 2 }, { 2, 6, 1, 3 }, { 3, 7, 2, 3 },
        { 0, 8, 4, 5 }, { 1, 8, 4, 6 }, { 2, 8, 5, 7 }, { 3, 8, 6, 7 },
        { 5, 8, 8, 8 }
    };

    public override void _Ready() {
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(MiniGameBgmPath)) {
            SoundManager.Instance.PlayBGM(MiniGameBgmPath);
        }

        if (RewardAmounts.Length < ChestImages.Length) {
            GD.PrintErr("【設定エラー】RewardAmountsの数が足りません！");
            return;
        }
        if (MenuPositions.Length < 9) {
            GD.PrintErr("【設定エラー】Menu Positions が足りません！9個設定してください。");
            return;
        }

        ShuffleRewards();
        UpdateCursorPosition();
        UpdateHUD();
    }

    private void ShuffleRewards() {
        for (int i = 0; i < RewardAmounts.Length; i++) {
            int temp = RewardAmounts[i];
            int randomIndex = GD.RandRange(i, RewardAmounts.Length - 1);
            RewardAmounts[i] = RewardAmounts[randomIndex];
            RewardAmounts[randomIndex] = temp;
        }
    }

    public override void _Process(double delta) {
        if (_isGameOver) return;

        if (_inputCooldown > 0f) {
            _inputCooldown -= (float)delta;
        }

        bool isUp = false, isDown = false, isLeft = false, isRight = false;

        if (_inputCooldown <= 0f) {
            if (Input.IsActionPressed("ui_up") || Input.IsActionPressed("move_up")) isUp = true;
            else if (Input.IsActionPressed("ui_down") || Input.IsActionPressed("move_down")) isDown = true;
            else if (Input.IsActionPressed("ui_left") || Input.IsActionPressed("move_left")) isLeft = true;
            else if (Input.IsActionPressed("ui_right") || Input.IsActionPressed("move_right")) isRight = true;
        }

        bool isSubmit = Input.IsActionJustPressed("ui_accept") ||
                        Input.IsActionJustPressed("jump") ||
                        Input.IsActionJustPressed("attack");

        bool moved = false;
        if (isUp) { _currentIndex = _navigation[_currentIndex, 0]; moved = true; } else if (isDown) { _currentIndex = _navigation[_currentIndex, 1]; moved = true; } else if (isLeft) { _currentIndex = _navigation[_currentIndex, 2]; moved = true; } else if (isRight) { _currentIndex = _navigation[_currentIndex, 3]; moved = true; }

        if (moved) {
            UpdateCursorPosition();
            _inputCooldown = 0.2f;
        }

        if (isSubmit) {
            ExecuteMenu();
        }
    }

    private void UpdateCursorPosition() {
        if (MenuPositions.Length > _currentIndex && CursorImage != null && MenuPositions[_currentIndex] != null) {
            CursorImage.GlobalPosition = MenuPositions[_currentIndex].GlobalPosition + CursorOffset;
        }
    }

    private void ExecuteMenu() {
        if (_currentIndex == 8) {
            OnClickSkip();
        } else if (_currentIndex < ChestImages.Length) {
            if (!_isChestOpened[_currentIndex]) {
                OpenChest(_currentIndex);
            }
        }
    }

    private async void OpenChest(int chestIndex) {
        if (_openedCount >= MAX_OPENS || _isGameOver || _isChestOpened[chestIndex]) return;

        _isChestOpened[chestIndex] = true;
        _openedCount++;

        TextureRect targetChest = ChestImages[chestIndex];
        if (OpenChestSprite != null && targetChest != null) {
            targetChest.Texture = OpenChestSprite;
        }

        ApplyReward(RewardAmounts[chestIndex], targetChest);

        if (_openedCount >= MAX_OPENS) {
            _isGameOver = true;
            await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
            GoToResult();
        }
    }

    private void ApplyReward(int amount, Control chestNode) {
        ShowPopup(chestNode, amount);

        if (GameManager.Instance != null) {
            GameManager.Instance.stageCoins += amount;
            if (GameManager.Instance.stageCoins < 0) GameManager.Instance.stageCoins = 0;
        }
        UpdateHUD();
    }

    private void UpdateHUD() {
        if (GameManager.Instance != null && HudCoinText != null) {
            HudCoinText.Text = GameManager.Instance.stageCoins.ToString("D3");
        }
        if (RemainingOpensText != null) {
            RemainingOpensText.Text = (MAX_OPENS - _openedCount).ToString();
        }
    }

    private void ShowPopup(Control chest, int amount) {
        if (RewardPopupPrefab == null || chest == null) return;

        Control popupObj = RewardPopupPrefab.Instantiate<Control>();
        AddChild(popupObj);
        popupObj.GlobalPosition = chest.GlobalPosition;

        if (popupObj is RewardPopup popup) {
            popup.Setup(CoinIconSprite, amount);
        }
    }

    public void OnClickSkip() {
        if (_isGameOver) return;
        _isGameOver = true;
        GoToResult();
    }

    private void GoToResult() {
        if (SceneTransitionManager.Instance != null) {
            SceneTransitionManager.Instance.LoadScene(ResultScenePath, TransitionType.Fade);
        } else if (!string.IsNullOrEmpty(ResultScenePath)) {
            GetTree().ChangeSceneToFile(ResultScenePath);
        }
    }
}