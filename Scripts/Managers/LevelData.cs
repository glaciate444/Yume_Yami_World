using Godot;
using System;

[GlobalClass]
public partial class LevelData : Resource {
    [ExportCategory("ステージ基本情報")]
    // システム管理用のID（11, 12など。クリア判定やメダル保存キーに使います）
    [Export] public int StageNumber = 11;

    // トランジション画面で表示するCourse No.（1-1なら1など）
    [Export] public int DisplayCourseNumber = 1;

    [Export] public string LevelName = "1-1: AAAAA";

    [Export(PropertyHint.File, "*.tscn")]
    public string ScenePath = "";

    [ExportCategory("解放条件（フラグ式）")]
    // 初期解放ステージ（1-1）は要素数 0（空）にします
    [Export] public int[] RequiredClearedStageNumbers = Array.Empty<int>();
    [Export] public string[] RequiredEventFlags = Array.Empty<string>();

    [ExportCategory("収集アイテム・サウンド設定")]
    [Export] public int MaxMedals = 3;
    [Export] public AudioStream StageBGM;
}