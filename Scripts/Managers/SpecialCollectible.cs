using Godot;
using System;

// プレイヤーが触れたら拾える Area2D にアタッチする想定
public partial class SpecialCollectible : Area2D {
    [ExportCategory("Collectible Settings")]
    [Export] public int StageId = 1;      // ステージ番号
    [Export] public int CollectibleId = 0; // このステージ内での連番（0,1,2...）

    [Export] public AudioStream CollectSE;

    // UIを更新するためのシグナル（Eventのようなもの）を定義
    [Signal]
    public delegate void OnCollectedEventHandler(int stageId, int collectibleId);

    // セーブデータ保存用のキー文字列を生成
    private string GetSaveKey() {
        return $"Stage_{StageId}_SpecialItem_{CollectibleId}";
    }

    public override void _Ready() {
        // 過去に取得したことがあるか確認（SaveManagerがある前提。なければ自作のグローバル変数等で代用）
        if (SaveManager.Instance != null) {
            // すでに取得済み（1）なら、シーンから消す
            if (SaveManager.Instance.GetCustomDataInt(GetSaveKey(), 0) == 1) {
                QueueFree();
                return;
            }
        }

        // BodyEntered シグナルをスクリプトで接続（インスペクターで繋いでもOKです）
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body) {
        // 触れたのがプレイヤー(Playerクラス)なら
        if (body is Player) {
            // 1. セーブデータに「1（取得済み）」を保存
            if (SaveManager.Instance != null) {
                SaveManager.Instance.SetCustomDataInt(GetSaveKey(), 1);
            }

            // 2. HUDなどのUIに「メダル取ったよ！」とシグナルで一斉に知らせる
            EmitSignal(SignalName.OnCollected, StageId, CollectibleId);

            // 3. エフェクトやSEの処理（必要に応じて追加）
            SoundManager.Instance?.PlaySE(CollectSE);

            // 4. 消滅する
            QueueFree();
        }
    }
}