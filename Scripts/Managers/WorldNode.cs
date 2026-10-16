using Godot;
using System;

public partial class WorldNode : TextureRect {
    [ExportCategory("ワールド設定")]
    [Export] public WorldData MyWorldData;

    [ExportCategory("UI設定")]
    // 空欄の場合はこのノード自身の色を変えます
    [Export] public CanvasItem NodeImage;
    [Export] public Color LockedColor = Colors.Gray;
    [Export] public Color UnlockedColor = Colors.White;

    [ExportCategory("隣接ノード（移動先）")]
    [Export] public WorldNode UpNode;
    [Export] public WorldNode DownNode;
    [Export] public WorldNode LeftNode;
    [Export] public WorldNode RightNode;

    public bool IsUnlocked { get; private set; }

    public void SetupNode() {
        if (MyWorldData == null) return;

        // テストモード（GameManager不在時）の処理
        if (GameManager.Instance == null) {
            GD.Print($"【テストモード】GameManagerがいないため、ワールド {MyWorldData.WorldNumber} を強制解放します！");
            IsUnlocked = true;
            UpdateVisuals();
            return;
        }

        IsUnlocked = true;

        // 必須イベントフラグのチェック
        if (MyWorldData.RequiredEventFlags != null) {
            foreach (string reqFlag in MyWorldData.RequiredEventFlags) {
                if (string.IsNullOrEmpty(reqFlag)) continue;

                // GameManagerのeventFlagsリストに含まれているか判定
                if (!GameManager.Instance.eventFlags.Contains(reqFlag)) {
                    IsUnlocked = false;
                    break;
                }
            }
        }

        UpdateVisuals();
    }

    private void UpdateVisuals() {
        CanvasItem target = NodeImage != null ? NodeImage : this;
        target.SelfModulate = IsUnlocked ? UnlockedColor : LockedColor;
    }
}