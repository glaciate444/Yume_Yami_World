using Godot;
using System;

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
    }

    public override void _Process(double delta) {
        if (inputCooldown > 0f) {
            inputCooldown -= (float)delta; // unscaledDeltaTime の代わりに通常のdeltaを使用
        }

        // GodotのInput Mapを使用 (プロジェクト設定で "ui_up", "ui_accept" などを設定済みとする)
        bool isUp = Input.IsActionJustPressed("ui_up") || (inputCooldown <= 0f && Input.GetActionStrength("ui_up") > 0.5f);
        bool isDown = Input.IsActionJustPressed("ui_down") || (inputCooldown <= 0f && Input.GetActionStrength("ui_down") > 0.5f);
        bool isLeft = Input.IsActionJustPressed("ui_left") || (inputCooldown <= 0f && Input.GetActionStrength("ui_left") > 0.5f);
        bool isRight = Input.IsActionJustPressed("ui_right") || (inputCooldown <= 0f && Input.GetActionStrength("ui_right") > 0.5f);

        // "ui_accept" は決定キー(ZやSpace)、 "ui_cancel" はキャンセルキー(XやEsc)
        bool isSubmit = Input.IsActionJustPressed("ui_accept");
        bool isCancel = Input.IsActionJustPressed("ui_cancel");

        bool isAnyAction = isUp || isDown || isLeft || isRight || isSubmit || isCancel || Input.IsAnythingPressed();

        switch (currentState) {
            case TitleState.PressAnyKey:
                if (isAnyAction && inputCooldown <= 0f) {
                    ChangeState(TitleState.MainMenu);
                    inputCooldown = 0.2f;
                }
                break;
            case TitleState.MainMenu:
                HandleMainMenuInput(isUp, isDown, isLeft, isRight, isSubmit);
                break;
            case TitleState.FileMenu:
                HandleFileMenuInput(isUp, isDown, isSubmit, isCancel);
                break;
            case TitleState.Options:
            case TitleState.Credits:
                if (isCancel || (isSubmit && currentState == TitleState.Credits)) {
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
                // ※ SceneTransitionManager はGodotの GetTree().ChangeSceneToFile() などに置き換え推奨
                // GetTree().ChangeSceneToFile("res://Scene/WorldMap.tscn");
            } else {
                GameManager.Instance.DeleteSaveData(selectedSlot);
                ChangeState(TitleState.MainMenu);
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
        if (currentIndex >= 0 && currentIndex <= 3) {
            selectedSlot = currentIndex + 1;
            if (GameManager.HasSaveData(selectedSlot)) {
                ChangeState(TitleState.FileMenu);
            } else {
                GameManager.Instance.currentSaveSlot = selectedSlot;
                GameManager.Instance.ResetData();
                GameManager.Instance.SaveGame();
                // GetTree().ChangeSceneToFile("res://Scene/Opening.tscn");
            }
        } else if (currentIndex == 4) ChangeState(TitleState.Options);
        else if (currentIndex == 5) ChangeState(TitleState.Credits);
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