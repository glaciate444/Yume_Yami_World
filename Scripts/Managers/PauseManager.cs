using Godot;
using System;

public partial class PauseManager : CanvasLayer {
    public static PauseManager Instance { get; private set; }
    // 上部などの変数宣言エリアに追加

    [ExportGroup("UI参照")]
    [Export] public Control menuPanel;

    [ExportGroup("コース退出確認用")]
    [Export] public Control exitDialogPanel;

    [ExportGroup("ステータスUI参照")]
    // TextMeshProUGUI は Godot の Label に置き換えます
    [Export] public Label hpCurrentText; // HPの現在値
    [Export] public Label hpMaxText;     // HPの最大値
    [Export] public Label spCurrentText; // SPの現在値
    [Export] public Label spMaxText;     // SPの最大値
    [Export] public Label apNormalText;  // 普通の攻撃力(Zキー)
    [Export] public Label apStompText;   // 踏みつけ攻撃力
    [Export] public Label jpText;        // ジャンプ力
    [Export] public Label dpText;        // 移動速度

    private bool isPaused = false;

    public override void _Ready() {
        if (Instance == null) {
            Instance = this;
        } else {
            QueueFree();
            return;
        }

        ProcessMode = ProcessModeEnum.Always;

        if (menuPanel != null) menuPanel.Visible = false;
        if (exitDialogPanel != null) exitDialogPanel.Visible = false;

        // ▼【追加】UIノードとして自分自身（または子ノード）に強制的にフォーカスを当てる
        // これをしないと、キー入力（ui_cancel等）を拾ってくれない場合があります。
        var controlNode = GetNodeOrNull<Control>("Control"); // ルートの直下にあるControlノード
        if (controlNode != null) {
            controlNode.GrabFocus();
        }
    }

    public override void _Input(InputEvent @event) {
        // "ui_cancel" (EscキーやコントローラーのStart/Optionボタンなど) が押されたらポーズ切り替え
        if (@event.IsActionPressed("ui_cancel")) {
            TogglePause();
        }
    }

    public void TogglePause() {
        isPaused = !isPaused;

        if (menuPanel != null) {
            menuPanel.Visible = isPaused;
        }

        if (!isPaused && exitDialogPanel != null) {
            exitDialogPanel.Visible = false;
        }

        if (isPaused) {
            UpdatePersonalData();
        }

        // Godotでの時間停止（ポーズ）処理
        GetTree().Paused = isPaused;
    }

    // ===============================================
    // ▼▼▼ コース退出用のメソッド ▼▼▼
    // ===============================================
    public void OpenExitDialog() {
        if (exitDialogPanel != null) exitDialogPanel.Visible = true;
    }

    public void CloseExitDialog() {
        if (exitDialogPanel != null) exitDialogPanel.Visible = false;
    }

    public void ConfirmExitCourse() {
        // 1. 時間停止（ポーズ）を解除する
        GetTree().Paused = false;
        isPaused = false;

        // 2. GameManagerから「元いたマップのシーン名」を取得してロードする
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.returnMapSceneName)) {
            // ※ SceneTransitionManager を実装済みの場合はそちらに置き換えてください
            GetTree().ChangeSceneToFile(GameManager.Instance.returnMapSceneName);
        } else {
            GD.PrintErr("マップのシーン名が記録されていません。処理を中断します。");
            // GetTree().ChangeSceneToFile("res://Scene/MapScene.tscn"); // テスト用
        }
    }

    public void UpdatePersonalData() {
        // Godotでのプレイヤー検索（グループ名 "Player" が設定されている前提）
        Node playerNode = GetTree().GetFirstNodeInGroup("Player");
        if (playerNode == null) {
            GD.PrintErr("グループ 'Player' を持つノードが見つかりませんでした。");
            return;
        }

        // プレイヤーにアタッチされている Player.cs スクリプトを取得
        if (playerNode is Player player) {
            // 【HP】
            if (hpCurrentText != null) hpCurrentText.Text = player.CurrentHealth.ToString("D2");
            if (hpMaxText != null) hpMaxText.Text = player.MaxHealth.ToString("D2");

            // 【SP】
            if (spCurrentText != null) spCurrentText.Text = player.CurrentSp.ToString("D2");
            if (spMaxText != null) spMaxText.Text = player.MaxSp.ToString("D2");

            // 【AP（Attack Power：攻撃力）】
            int normalAp = player.BaseAttackPower + player.PassiveAttackBonus;
            int stompAp = player.BaseStompPower + player.PassiveAttackBonus;

            if (apNormalText != null) apNormalText.Text = normalAp.ToString("D2");
            if (apStompText != null) apStompText.Text = stompAp.ToString("D2");

            // 【JP / DP（ジャンプ力 / スピード）】
            // GodotのJumpVelocityはマイナスの値（-500など）が入っているため、
            // 表示用に絶対値(Mathf.Abs)にしてプラスの数値にしています。
            if (jpText != null) jpText.Text = Mathf.Abs(player.JumpVelocity).ToString("F0");
            if (dpText != null) dpText.Text = player.Speed.ToString("F0");
        } else {
            GD.PrintErr("取得したノードに Player.cs がアタッチされていません。");
        }
    }
    public override void _ExitTree() {
        // シーン破棄時にポーズを解除（Time.timeScale = 1f の代わり）
        GetTree().Paused = false;
    }
}