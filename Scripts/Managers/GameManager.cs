using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node {
    public static GameManager Instance { get; private set; }

    // ▼ Godot用のセーブデータ管理ファイル
    private const string SaveFilePath = "user://savedata.cfg";

    [ExportGroup("セーブデータ管理")]
    [Export] public int currentSaveSlot = 1;

    [ExportGroup("プレイヤーのデータ（セーブ対象）")]
    [Export] public int currentMaxHp = 12;
    [Export] public int currentMaxSp = 6;
    [Export] public int unlockedStageLevel = 1;
    [Export] public int totalCoins = 0;
    [Export] public int currentMapNodeNumber = 1;

    [ExportGroup("ステージ一時保存用")]
    [Export] public int stageCoins = 0;

    [ExportGroup("残機システム")]
    [Export] public int currentLives = 3;
    [Export] public int currentLifePieces = 0;
    [Export] public AudioStream oneUpSE; // AudioClip -> AudioStreamに変更

    [ExportGroup("ワールド進行データ")]
    [Export] public int unlockedWorldLevel = 1;
    [Export] public int currentWorldNodeNumber = 1;

    [ExportGroup("マップ遷移用")]
    [Export] public string returnMapSceneName = "";

    [System.Serializable]
    public class OwnedItemData {
        public int itemId;
        public int itemLevel;
    }

    // ※ CharacterData や ItemInventoryData はResource等で定義されている前提とします
    // [ExportGroup("現在のキャラクター")]
    // [Export] public CharacterData currentCharacter;

    // [ExportGroup("現在の装備データ")]
    // [Export] public ItemInventoryData currentEquipSubAction;
    // [Export] public ItemInventoryData currentEquipSpecial;
    // [Export] public ItemInventoryData currentEquipPassiveA;
    // [Export] public ItemInventoryData currentEquipPassiveB;

    [ExportGroup("インベントリデータ")]
    public List<OwnedItemData> ownedItems = new List<OwnedItemData>();

    [ExportGroup("ショップでの購入回数")]
    [Export] public int hpUpPurchaseCount = 0;
    [Export] public int spUpPurchaseCount = 0;

    [ExportGroup("進行度（フラグ管理）")]
    public List<int> clearedStageNumbers = new List<int>();
    public List<string> eventFlags = new List<string>();

    public override void _Ready() {
        if (Instance == null) {
            Instance = this;
            // GodotのAutoloadを使用する場合はDontDestroyOnLoadは不要です
        } else {
            QueueFree(); // 重複した場合は破棄
        }
    }

    public void AddLifePiece(int amount) {
        currentLifePieces += amount;
        if (currentLifePieces >= 100) {
            currentLifePieces -= 100;
            currentLives++;
            // if (SoundManager.Instance != null && oneUpSE != null) SoundManager.Instance.PlaySE(oneUpSE);
        }

        // UIの更新 (グループ "LifeHUD" を持つノードを探して更新メソッドを呼ぶなどの代替案)
        // var hud = GetTree().GetFirstNodeInGroup("LifeHUD");
        // if (hud != null && hud.HasMethod("UpdateHUD")) hud.Call("UpdateHUD");
    }

    // ==========================================
    // セーブ・ロード・リセット機能
    // ==========================================
    public void SaveGame() {
        ConfigFile config = new ConfigFile();
        config.Load(SaveFilePath); // 既存データがあれば読み込む

        string section = "Slot_" + currentSaveSlot;

        config.SetValue(section, "IsSaved", 1);
        config.SetValue(section, "MaxHp", currentMaxHp);
        config.SetValue(section, "MaxSp", currentMaxSp);
        config.SetValue(section, "UnlockedStageLevel", unlockedStageLevel);
        config.SetValue(section, "TotalCoins", totalCoins);
        config.SetValue(section, "CurrentLives", currentLives);
        config.SetValue(section, "CurrentLifePieces", currentLifePieces);
        config.SetValue(section, "CurrentMapNodeNumber", currentMapNodeNumber);
        config.SetValue(section, "UnlockedWorldLevel", unlockedWorldLevel);
        config.SetValue(section, "CurrentWorldNodeNumber", currentWorldNodeNumber);
        config.SetValue(section, "HpUpPurchaseCount", hpUpPurchaseCount);
        config.SetValue(section, "SpUpPurchaseCount", spUpPurchaseCount);

        List<string> itemSaveList = new List<string>();
        foreach (var item in ownedItems) {
            itemSaveList.Add(item.itemId + ":" + item.itemLevel);
        }
        config.SetValue(section, "OwnedItemIds", string.Join(",", itemSaveList));

        config.SetValue(section, "ClearedStageNumbers", string.Join(",", clearedStageNumbers));
        config.SetValue(section, "EventFlags", string.Join(",", eventFlags));

        config.Save(SaveFilePath);
    }

    public void LoadGame() {
        ConfigFile config = new ConfigFile();
        if (config.Load(SaveFilePath) != Error.Ok) return; // セーブデータがなければ終了

        string section = "Slot_" + currentSaveSlot;
        if (!config.HasSection(section)) return;

        currentMaxHp = (int)config.GetValue(section, "MaxHp", currentMaxHp);
        currentMaxSp = (int)config.GetValue(section, "MaxSp", currentMaxSp);
        unlockedStageLevel = (int)config.GetValue(section, "UnlockedStageLevel", unlockedStageLevel);
        totalCoins = (int)config.GetValue(section, "TotalCoins", totalCoins);
        currentLives = (int)config.GetValue(section, "CurrentLives", currentLives);
        currentLifePieces = (int)config.GetValue(section, "CurrentLifePieces", currentLifePieces);
        currentMapNodeNumber = (int)config.GetValue(section, "CurrentMapNodeNumber", currentMapNodeNumber);
        unlockedWorldLevel = (int)config.GetValue(section, "UnlockedWorldLevel", unlockedWorldLevel);
        currentWorldNodeNumber = (int)config.GetValue(section, "CurrentWorldNodeNumber", currentWorldNodeNumber);
        hpUpPurchaseCount = (int)config.GetValue(section, "HpUpPurchaseCount", hpUpPurchaseCount);
        spUpPurchaseCount = (int)config.GetValue(section, "SpUpPurchaseCount", spUpPurchaseCount);

        string idsStr = (string)config.GetValue(section, "OwnedItemIds", "");
        ownedItems.Clear();
        if (!string.IsNullOrEmpty(idsStr)) {
            string[] itemArray = idsStr.Split(',');
            foreach (string itemStr in itemArray) {
                string[] parts = itemStr.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int id) && int.TryParse(parts[1], out int lv)) {
                    ownedItems.Add(new OwnedItemData { itemId = id, itemLevel = lv });
                }
            }
        }

        string stagesStr = (string)config.GetValue(section, "ClearedStageNumbers", "");
        clearedStageNumbers.Clear();
        if (!string.IsNullOrEmpty(stagesStr)) {
            string[] idArray = stagesStr.Split(',');
            foreach (string idStr in idArray) {
                if (int.TryParse(idStr, out int val)) clearedStageNumbers.Add(val);
            }
        }

        string flagsStr = (string)config.GetValue(section, "EventFlags", "");
        eventFlags.Clear();
        if (!string.IsNullOrEmpty(flagsStr)) {
            eventFlags.AddRange(flagsStr.Split(','));
        }
    }

    public void ResetData() {
        currentMaxHp = 12;
        currentMaxSp = 6;
        unlockedStageLevel = 1;
        totalCoins = 0;
        currentLives = 3;
        currentLifePieces = 0;
        unlockedWorldLevel = 1;
        currentWorldNodeNumber = 1;
        currentMapNodeNumber = 1;
        hpUpPurchaseCount = 0;
        spUpPurchaseCount = 0;

        ownedItems.Clear();
        ownedItems.Add(new OwnedItemData { itemId = 0, itemLevel = 1 });
        ownedItems.Add(new OwnedItemData { itemId = 1, itemLevel = 1 });
        ownedItems.Add(new OwnedItemData { itemId = 2, itemLevel = 1 });
        ownedItems.Add(new OwnedItemData { itemId = 21, itemLevel = 1 });

        clearedStageNumbers.Clear();
        eventFlags.Clear();
    }

    public void DeleteSaveData(int slot) {
        ConfigFile config = new ConfigFile();
        if (config.Load(SaveFilePath) == Error.Ok) {
            string section = "Slot_" + slot;
            if (config.HasSection(section)) {
                config.EraseSection(section);
                config.Save(SaveFilePath);
            }
        }
    }

    public static bool HasSaveData(int slot) {
        ConfigFile config = new ConfigFile();
        if (config.Load(SaveFilePath) == Error.Ok) {
            return config.HasSectionKey("Slot_" + slot, "IsSaved");
        }
        return false;
    }

    // その他フラグ管理メソッド等はロジックが同じため省略せずに使えます（GetTotalSpecialMedals等はPlayerPrefsからConfigFileアクセスに変更が必要です）
}