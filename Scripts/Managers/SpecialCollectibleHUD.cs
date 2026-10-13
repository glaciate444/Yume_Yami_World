using Godot;
using System;

public partial class SpecialCollectibleHUD : Control {
    [ExportCategory("HUD Settings")]
    [Export] public int CurrentStageId = 1;
    [Export] public int TotalCollectiblesInStage = 3; // この数に合わせて不要な枠を非表示にします

    [ExportCategory("UI References")]
    // ▼ インスペクターで、先ほど作った Medal1 ～ Medal5 の TextureRect をここに5つセットしてください
    [Export] public TextureRect[] CollectibleIcons;

    private Color _uncollectedColor = new Color(1f, 1f, 1f, 0.3f); // 未取得（半透明）
    private Color _collectedColor = new Color(1f, 1f, 1f, 1f);   // 取得済み（くっきり）

    public override void _Ready() {
        // 画面内の全 SpecialCollectible が発するシグナルを受信できるようにする
        var allCollectibles = GetTree().GetNodesInGroup("SpecialItems");
        foreach (var node in allCollectibles) {
            if (node is SpecialCollectible item) {
                item.OnCollected += HandleItemCollected;
            }
        }

        UpdateHUD();
    }

    private void HandleItemCollected(int stageId, int collectibleId) {
        // 取られたメダルがこのステージのものならHUDを更新
        if (stageId == CurrentStageId) {
            UpdateHUD();
        }
    }

    public void UpdateHUD() {
        int collectedCount = 0;

        for (int i = 0; i < CollectibleIcons.Length; i++) {
            if (CollectibleIcons[i] == null) continue;

            // ▼ 重要：設定した「このステージのメダル総数」より後ろの枠は、画面から完全に消す（非表示にする）
            if (i >= TotalCollectiblesInStage) {
                CollectibleIcons[i].Hide();
                continue;
            }

            // 総数以内の枠は表示する
            CollectibleIcons[i].Show();

            // SaveManager から取得状況を確認
            string saveKey = $"Stage_{CurrentStageId}_SpecialItem_{i}";
            int isCollected = 0;

            if (SaveManager.Instance != null) {
                isCollected = SaveManager.Instance.GetCustomDataInt(saveKey, 0);
            }

            // 色の切り替え（取得済みならくっきり、未取得なら半透明）
            if (isCollected == 1) {
                CollectibleIcons[i].Modulate = _collectedColor;
                collectedCount++;
            } else {
                CollectibleIcons[i].Modulate = _uncollectedColor;
            }
        }

        // 全部集めた時の処理（ファンファーレを鳴らすなど）
        if (TotalCollectiblesInStage > 0 && collectedCount == TotalCollectiblesInStage) {
            GD.Print($"ステージ {CurrentStageId} のスペシャルメダルをコンプリート！");
        }
    }
}