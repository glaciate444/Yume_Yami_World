using Godot;
using System;

public partial class StageController : Node2D {
    [ExportCategory("Stage Info")]
    [Export] public int StageId = 1; // ステージ番号（Level1-Stage1なら1）
    [Export] public string BgmPath = "res://Audio/BGM/Stage1.ogg";

    // ▼ ここを追加：このステージに置かれているメダルの総数（3枚のステージなら3にする）
    [Export] public int TotalMedals = 3;

    [ExportCategory("Camera Limits")]
    [Export] public int LimitLeft = 0;
    [Export] public int LimitTop = -500;
    [Export] public int LimitRight = 3000;
    [Export] public int LimitBottom = 1000;

    public override void _Ready() {
        CallDeferred(nameof(ApplyCameraLimits));
        CallDeferred(nameof(PlayStageBGM));

        // ▼ ここを追加：HUDを見つけて、このステージの情報を送りつける
        CallDeferred(nameof(SetupHUD));
    }

    private void SetupHUD() {
        // 画面内にある SpecialCollectibleHUD (メダルのUIスクリプト) を探し出す
        // （GetNodeなどのパス指定ではなく、型で直接探す確実な方法です）
        SpecialCollectibleHUD medalHud = GetNodeOrNull<SpecialCollectibleHUD>("/root/Level1-Stage1/HUD/Margin/SpecialMedalContainer");

        // もしパスが違って見つからなかった時のための、さらに確実な検索方法
        if (medalHud == null) {
            // シーン全体から SpecialCollectibleHUD クラスを探す
            medalHud = FindFirstNodeOfType<SpecialCollectibleHUD>(GetTree().Root);
        }

        if (medalHud != null) {
            // HUD側に、このステージのIDとメダル枚数を強制的に教え込む！
            medalHud.CurrentStageId = StageId;
            medalHud.TotalCollectiblesInStage = TotalMedals;

            // 設定を書き換えたので、HUDの表示を更新させる
            medalHud.UpdateHUD();
        }
    }

    // シーンツリー内から特定の型のノードを探し出す便利メソッド
    private T FindFirstNodeOfType<T>(Node parent) where T : Node {
        if (parent is T node) return node;
        foreach (Node child in parent.GetChildren()) {
            T result = FindFirstNodeOfType<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void ApplyCameraLimits() {
        Player player = GameManager.Instance?.currentPlayer;
        if (player != null) {
            Camera2D camera = player.GetNodeOrNull<Camera2D>("Camera2D");
            if (camera != null) {
                camera.LimitLeft = LimitLeft;
                camera.LimitTop = LimitTop;
                camera.LimitRight = LimitRight;
                camera.LimitBottom = LimitBottom;
            }
        }
    }

    private void PlayStageBGM() {
        SoundManager.Instance?.PlayBGM(BgmPath);
    }
}