using Godot;
using System;

public partial class MapNode : Control {
    [ExportCategory("ステージ設定")]
    [Export] public LevelData MyLevelData;

    [ExportCategory("特殊ノード設定（ショップ用）")]
    [Export] public bool IsShopNode = false;
    [Export(PropertyHint.File, "*.tscn")]
    public string ShopScenePath = "res://Scene/ShopScene.tscn";

    [ExportCategory("UI設定（ベース枠）")]
    [Export] public TextureRect NodeImage;
    [Export] public Texture2D LockedSprite;
    [Export] public Texture2D UnlockedSprite;
    [Export] public Texture2D ClearedSprite;

    [ExportCategory("UI設定（数字アイコン）")]
    [Export] public TextureRect NumberImage;
    [Export] public Texture2D LockedNumSprite;
    [Export] public Texture2D UnlockedNumSprite;
    [Export] public Texture2D ClearedNumSprite;

    [ExportCategory("UI設定（クラウン）")]
    [Export] public TextureRect CrownImage;
    [Export] public Texture2D SilverCrownSprite;
    [Export] public Texture2D GoldCrownSprite;

    [ExportCategory("隣接ノード")]
    [Export] public MapNode UpNode;
    [Export] public MapNode DownNode;
    [Export] public MapNode LeftNode;
    [Export] public MapNode RightNode;

    public bool IsUnlocked { get; private set; }
    public bool IsCleared { get; private set; }

    public void SetupNode() {
        // ショップマスの場合は常時解放・王冠なし
        if (IsShopNode) {
            IsUnlocked = true;
            IsCleared = false;
            UpdateVisuals();
            if (CrownImage != null) CrownImage.Visible = false;
            return;
        }

        if (MyLevelData == null) return;

        // テストモード（GameManager不在時）
        if (GameManager.Instance == null) {
            GD.Print($"【テストモード】GameManagerがいないため、{MyLevelData.LevelName} を強制解放します！");
            IsUnlocked = true;
            IsCleared = true;
            UpdateVisuals();
            UpdateCrown();
            return;
        }

        // クリア済みかどうかの判定
        IsCleared = CheckStageCleared(MyLevelData.StageNumber);
        IsUnlocked = true;

        // 条件1：必須クリアステージのチェック
        if (MyLevelData.RequiredClearedStageNumbers != null) {
            foreach (int reqStageNum in MyLevelData.RequiredClearedStageNumbers) {
                if (!CheckStageCleared(reqStageNum)) {
                    IsUnlocked = false;
                    break;
                }
            }
        }

        // 条件2：必須イベントフラグのチェック
        if (IsUnlocked && MyLevelData.RequiredEventFlags != null) {
            foreach (string reqFlag in MyLevelData.RequiredEventFlags) {
                if (string.IsNullOrEmpty(reqFlag)) continue;
                if (!GameManager.Instance.eventFlags.Contains(reqFlag)) {
                    IsUnlocked = false;
                    break;
                }
            }
        }

        UpdateVisuals();
        UpdateCrown();
    }

    private bool CheckStageCleared(int stageNum) {
        if (GameManager.Instance != null && GameManager.Instance.clearedStageNumbers.Contains(stageNum))
            return true;
        if (SaveManager.Instance != null && SaveManager.Instance.GetCustomDataInt($"Stage_{stageNum}_Cleared", 0) == 1)
            return true;
        return false;
    }

    // メダルが取得済みか判定（ステージ内の保存形式とスロット付き形式の両方に対応）
    public static bool CheckMedalGot(int stageNum, int medalIndex) {
        if (SaveManager.Instance == null) return false;

        int slot = GameManager.Instance != null ? GameManager.Instance.currentSaveSlot : 1;
        string keyWithSlot = $"Stage_{stageNum}_SpecialItem_{medalIndex}_{slot}";
        string keySimple = $"Stage_{stageNum}_SpecialItem_{medalIndex}";

        return SaveManager.Instance.GetCustomDataInt(keyWithSlot, 0) == 1 ||
               SaveManager.Instance.GetCustomDataInt(keySimple, 0) == 1;
    }

    private void UpdateVisuals() {
        if (NodeImage != null) NodeImage.Modulate = Colors.White;
        if (NumberImage != null) NumberImage.Modulate = Colors.White;

        if (IsCleared) {
            if (NodeImage != null && ClearedSprite != null) NodeImage.Texture = ClearedSprite;
            if (NumberImage != null && ClearedNumSprite != null) NumberImage.Texture = ClearedNumSprite;
        } else if (IsUnlocked) {
            if (NodeImage != null && UnlockedSprite != null) NodeImage.Texture = UnlockedSprite;
            if (NumberImage != null && UnlockedNumSprite != null) NumberImage.Texture = UnlockedNumSprite;
        } else {
            if (NodeImage != null && LockedSprite != null) NodeImage.Texture = LockedSprite;
            if (NumberImage != null && LockedNumSprite != null) NumberImage.Texture = LockedNumSprite;
        }
    }

    private void UpdateCrown() {
        if (CrownImage == null || MyLevelData == null) return;

        int maxMedals = MyLevelData.MaxMedals;
        if (maxMedals <= 0) {
            CrownImage.Visible = false;
            return;
        }

        int collectedMedals = 0;
        for (int i = 0; i < maxMedals; i++) {
            if (CheckMedalGot(MyLevelData.StageNumber, i)) {
                collectedMedals++;
            }
        }

        if (collectedMedals == 0) {
            // 0枚なら王冠を非表示
            CrownImage.Visible = false;
        } else if (collectedMedals < maxMedals) {
            // 1枚以上〜最大未満なら銀の王冠
            CrownImage.Visible = true;
            if (SilverCrownSprite != null) CrownImage.Texture = SilverCrownSprite;
        } else {
            // 全回収（コンプリート）なら金の王冠
            CrownImage.Visible = true;
            if (GoldCrownSprite != null) CrownImage.Texture = GoldCrownSprite;
        }
    }
}