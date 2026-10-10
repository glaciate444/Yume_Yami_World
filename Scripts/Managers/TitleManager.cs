using Godot;
using System;
using System.Collections.Generic;

public enum TitleState {
    PressAnyKey,
    MainMenu,
    FileMenu,
    Options,
    Credits
}

public partial class TitleManager : Control {
    [ExportGroup("状態管理")]
    [Export] public TitleState currentState = TitleState.PressAnyKey;

    [ExportGroup("UIパネル設定")]
    // GameObject -> Control (CanvasItem) に変更
    [Export] public Control pressAnyKeyPanel;
    [Export] public Control mainPanel;
    [Export] public Control fileMenuPanel;
    [Export] public Control optionsPanel;
    [Export] public Control creditsPanel;

    [ExportGroup("メインメニュー設定")]
    [Export] public Control cursorImage; // RectTransform -> Control
    [Export] public Control[] menuPositions;
    [Export] public float cursorOffsetX = 150f;

    [ExportGroup("ファイルテキスト設定")]
    [Export] public Label[] fileTexts; // TMP_Text -> Label

    [ExportGroup("サブメニュー設定")]
    [Export] public Control subMenuCursor;
    [Export] public Control[] subMenuPositions;
    [Export] public float subMenuCursorOffsetX = 80f;

    private int currentIndex = 0;
    private int subMenuIndex = 0;
    private int selectedSlot = 1;

    private float inputCooldown = 0f;

    private readonly int[,] navigation = new int[6, 4] {
        { 4, 2, 1, 1 },
        { 5, 3, 0, 0 },
        { 0, 4, 3, 3 },
        { 1, 5, 2, 2 },
        { 2, 0, 5, 5 },
        { 3, 1, 4, 4 }
    };

    public override void _Ready() {
        ChangeState(TitleState.PressAnyKey);
        // フォーカスを強制的に自分自身（TitleManager）に合わせる
        GrabFocus();
    }

    public override void _Process(double delta) {
        // 連続入力を防ぐためのクールダウンを減らす処理
        // これがないと、一度キーを押した後に一生入力がブロックされてしまいます
        if (inputCooldown > 0f) {
            inputCooldown -= (float)delta;
        }
    }

    public override void _Input(InputEvent @event) {
        // クールダウン中は入力を受け付けない
        if (inputCooldown > 0f) return;

        // キーボードの矢印キー、またはパッドの十字キーが「押された瞬間」を検知
        bool isUp = @event.IsActionPressed("ui_up");
        bool isDown = @event.IsActionPressed("ui_down");
        bool isLeft = @event.IsActionPressed("ui_left");
        bool isRight = @event.IsActionPressed("ui_right");
        bool isSubmit = @event.IsActionPressed("ui_accept"); // Enter, Zキー等
        bool isCancel = @event.IsActionPressed("ui_cancel"); // Esc, Xキー等

        // タイトル画面の「Press Any Button」を抜けるための検知
        bool isAnyKey = @event is InputEventKey keyEvent && keyEvent.Pressed;
        bool isAnyJoy = @event is InputEventJoypadButton joyButton && joyButton.Pressed;

        switch (currentState) {
            case TitleState.PressAnyKey:
                if (isAnyKey || isAnyJoy || isSubmit) {
                    ChangeState(TitleState.MainMenu);
                    inputCooldown = 0.2f;
                }
                break;

            case TitleState.MainMenu:
                if (isUp || isDown || isLeft || isRight || isSubmit) {
                    HandleMainMenuInput(isUp, isDown, isLeft, isRight, isSubmit);
                }
                break;

            case TitleState.FileMenu:
                if (isUp || isDown || isSubmit || isCancel) {
                    HandleFileMenuInput(isUp, isDown, isSubmit, isCancel);
                }
                break;

            case TitleState.Options:
            case TitleState.Credits:
                if (isCancel || isSubmit) {
                    ChangeState(TitleState.MainMenu);
                    inputCooldown = 0.2f;
                }
                break;
        }
    }
    private void HandleMainMenuInput(bool isUp, bool isDown, bool isLeft, bool isRight, bool isSubmit) {
        bool moved = false;
        if (isUp) { currentIndex = navigation[currentIndex, 0]; moved = true; } else if (isDown) { currentIndex = navigation[currentIndex, 1]; moved = true; } else if (isLeft) { currentIndex = navigation[currentIndex, 2]; moved = true; } else if (isRight) { currentIndex = navigation[currentIndex, 3]; moved = true; }

        if (moved) {
            UpdateCursorPosition();
            inputCooldown = 0.2f;
        }

        if (isSubmit) {
            ExecuteMainMenu();
        }
    }

    private void HandleFileMenuInput(bool isUp, bool isDown, bool isSubmit, bool isCancel) {
        if (isUp || isDown) {
            subMenuIndex = (subMenuIndex == 0) ? 1 : 0;
            UpdateSubMenuCursorPosition();
            inputCooldown = 0.2f;
        }

        if (isSubmit) {
            if (subMenuIndex == 0) {
                GameManager.Instance.currentSaveSlot = selectedSlot;
                GameManager.Instance.LoadGame();
                GD.Print($"ファイル {selectedSlot} をロードしました。現在のHPは {GameManager.Instance.currentMaxHp} です。");
                // ※ SceneTransitionManager はGodotの GetTree().ChangeSceneToFile() などに置き換え推奨
                // GetTree().ChangeSceneToFile("res://Scene/WorldMap.tscn");
            } else {
                GameManager.Instance.DeleteSaveData(selectedSlot);
                ChangeState(TitleState.MainMenu);
                GD.Print($"ファイル {selectedSlot} を新規作成しました。");
            }
        }

        if (isCancel) {
            ChangeState(TitleState.MainMenu);
            inputCooldown = 0.2f;
        }
    }

    private void UpdateCursorPosition() {
        if (menuPositions != null && cursorImage != null && menuPositions.Length > currentIndex && menuPositions[currentIndex] != null) {
            // Position で UIのローカル座標を更新
            Vector2 newPos = menuPositions[currentIndex].Position;
            newPos.X -= cursorOffsetX;
            cursorImage.Position = newPos;
        }
    }

    private void UpdateSubMenuCursorPosition() {
        if (subMenuPositions != null && subMenuCursor != null && subMenuPositions.Length > subMenuIndex && subMenuPositions[subMenuIndex] != null) {
            Vector2 newPos = subMenuPositions[subMenuIndex].Position;
            newPos.X -= subMenuCursorOffsetX;
            subMenuCursor.Position = newPos;
        }
    }

    private void UpdateFileTexts() {
        for (int i = 0; i < 4; i++) {
            if (fileTexts != null && i < fileTexts.Length && fileTexts[i] != null) {
                int slot = i + 1;
                if (GameManager.HasSaveData(slot)) {
                    fileTexts[i].Text = $"ファイル {slot}\n(つづきから)";
                } else {
                    fileTexts[i].Text = $"ファイル {slot}\n(あたらしくはじめる)";
                }
            }
        }
    }

    private void ExecuteMainMenu() {
        inputCooldown = 0.2f;

        // どこで止まったか確認するためのログ
        GD.Print($"[ExecuteMainMenu] 開始。 currentIndex: {currentIndex}");

        if (currentIndex >= 0 && currentIndex <= 3) {
            selectedSlot = currentIndex + 1;

            GD.Print($"[ExecuteMainMenu] selectedSlot: {selectedSlot}");

            // GameManager の生存確認
            if (GameManager.Instance == null) {
                GD.PrintErr("[ExecuteMainMenu] エラー：GameManager.Instance が Null です！Autoloadの設定を確認してください。");
                return;
            }

            if (GameManager.HasSaveData(selectedSlot)) {
                GD.Print($"[ExecuteMainMenu] スロット {selectedSlot} のセーブデータあり。FileMenuへ遷移します。");
                ChangeState(TitleState.FileMenu);
            } else {
                GD.Print($"[ExecuteMainMenu] スロット {selectedSlot} のセーブデータなし。新規作成します。");
                GameManager.Instance.currentSaveSlot = selectedSlot;

                GD.Print("[ExecuteMainMenu] ResetData 実行前");
                GameManager.Instance.ResetData();

                GD.Print("[ExecuteMainMenu] SaveGame 実行前");
                GameManager.Instance.SaveGame();

                GD.Print("[ExecuteMainMenu] セーブ完了、Openingシーンへ遷移します（現在はコメントアウト中）");
                // GetTree().ChangeSceneToFile("res://Scene/Opening.tscn");
            }
        } else if (currentIndex == 4) {
            GD.Print("[ExecuteMainMenu] オプションへ遷移");
            ChangeState(TitleState.Options);
        } else if (currentIndex == 5) {
            GD.Print("[ExecuteMainMenu] クレジットへ遷移");
            ChangeState(TitleState.Credits);
        }
    }

    private void ChangeState(TitleState newState) {
        currentState = newState;
        if (pressAnyKeyPanel != null) pressAnyKeyPanel.Visible = false;
        if (mainPanel != null && newState != TitleState.FileMenu) mainPanel.Visible = false;
        if (fileMenuPanel != null) fileMenuPanel.Visible = false;
        if (optionsPanel != null) optionsPanel.Visible = false;
        if (creditsPanel != null) creditsPanel.Visible = false;
        if (cursorImage != null) cursorImage.Visible = false;

        switch (currentState) {
            case TitleState.PressAnyKey:
                if (pressAnyKeyPanel != null) pressAnyKeyPanel.Visible = true;
                break;
            case TitleState.MainMenu:
                if (mainPanel != null) mainPanel.Visible = true;
                if (cursorImage != null) cursorImage.Visible = true;
                UpdateFileTexts();
                UpdateCursorPosition();
                break;
            case TitleState.FileMenu:
                if (mainPanel != null) mainPanel.Visible = true;
                if (fileMenuPanel != null) fileMenuPanel.Visible = true;
                subMenuIndex = 0;
                UpdateSubMenuCursorPosition();
                break;
            case TitleState.Options:
                if (optionsPanel != null) optionsPanel.Visible = true;
                break;
            case TitleState.Credits:
                if (creditsPanel != null) creditsPanel.Visible = true;
                break;
        }
    }
}