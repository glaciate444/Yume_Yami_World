using Godot;
using System;

// ▼ [GlobalClass] を付けることで、Unityの [CreateAssetMenu] と同じように
// エディタの「新規リソース」一覧に WorldData が表示されるようになります。
[GlobalClass]
public partial class WorldData : Resource {
    [ExportCategory("ワールド基本情報")]
    [Export] public int WorldNumber = 1;
    [Export] public string WorldName = "";

    // 遷移先のステージセレクト（またはステージ）のシーンパス
    [Export(PropertyHint.File, "*.tscn")]
    public string ScenePath = "";

    [ExportCategory("ストーリー演出設定")]
    // 導入ストーリーのシーンパス（空欄なら直接 ScenePath へ遷移）
    [Export(PropertyHint.File, "*.tscn")]
    public string StoryScenePath = "";

    [ExportCategory("サウンド設定")]
    [Export] public AudioStream WorldBGM;

    [ExportCategory("解放条件（フラグ式）")]
    // ここに入れたイベントフラグがすべて立っていれば解放（例: Unlocked_World_2）
    // ※Level 1 は要素数 0（空）にしておけば最初から解放されます
    [Export] public string[] RequiredEventFlags = Array.Empty<string>();
}