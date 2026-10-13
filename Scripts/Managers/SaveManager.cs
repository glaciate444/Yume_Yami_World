using Godot;
using System;

// どのシーンからでも SaveManager.Instance でアクセスできるクラス
public partial class SaveManager : Node {
    // シングルトン（ゲーム中に1つだけ存在する実体）
    public static SaveManager Instance { get; private set; }

    // 保存先のファイルパス（user:// は、Godotが自動で用意するセーブデータ用の安全なフォルダです）
    private readonly string _savePath = "user://savedata.cfg";
    private ConfigFile _config = new ConfigFile();

    public override void _Ready() {
        if (Instance == null) {
            Instance = this;
            LoadData(); // 起動時にセーブデータを読み込む
        } else {
            QueueFree(); // 既に存在する場合は自分を消す
        }
    }

    // データの読み込み
    private void LoadData() {
        Error err = _config.Load(_savePath);
        if (err != Error.Ok) {
            GD.Print("セーブデータが見つからないため、新規作成します。");
        }
    }

    // データの保存
    private void SaveData() {
        _config.Save(_savePath);
    }

    // Unityの PlayerPrefs.SetInt と同じ役割
    public void SetCustomDataInt(string key, int value, string section = "GameData") {
        _config.SetValue(section, key, value);
        SaveData(); // セットした瞬間にファイルに保存する
    }

    // Unityの PlayerPrefs.GetInt と同じ役割
    public int GetCustomDataInt(string key, int defaultValue = 0, string section = "GameData") {
        // Variant型で返ってくるので、int型にキャスト(変換)して返す
        return (int)_config.GetValue(section, key, defaultValue);
    }
}