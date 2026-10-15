using Godot;
using System;

public partial class GoalPoint : Area2D {
    [ExportCategory("遷移先設定")]
    [Export(PropertyHint.File, "*.tscn")]
    public string NextScenePath = "res://Scene/MiniGameScene.tscn";

    [ExportCategory("ステージ進行設定（フラグ式）")]
    [Export] public int ClearedStageNumber = 1;

    [ExportCategory("ワールド進行設定（ボス専用）")]
    [Export] public bool UnlocksNewWorld = false;
    [Export] public int UnlockWorldReward = 2;

    [ExportCategory("サウンド・演出設定")]
    [Export] public AudioStream ResultBGM;
    [Export] public float WaitTime = 2.0f;

    private bool _isGoal = false;

    public override void _Ready() {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body) {
        if (_isGoal) return;

        if (body is Player player) {
            TriggerGoal(player);
        }
    }

    public async void TriggerGoal(Player player) {
        if (_isGoal || player == null) return;

        _isGoal = true;
        GD.Print("ゴール処理開始！");

        // 1. ステージ内の全敵を停止
        StopAllEnemies(GetTree().CurrentScene);

        // 2. BGMを止めてファンファーレ（ジングル）を鳴らす
        if (SoundManager.Instance != null) {
            SoundManager.Instance.StopBGM();
            if (ResultBGM != null) {
                SoundManager.Instance.PlaySE(ResultBGM);
            }
        }

        // 3. プレイヤーのゴール演出（停止処理）
        player.PlayGoalAction();

        // 4. 進行フラグとセーブデータの更新
        if (GameManager.Instance != null) {
            // ※もしPlayer.cs側の変数（例: player.Coins や player.currentCoins）から
            // ゴール時にまとめて渡す方式の場合は、下のコメントを外して変数名を合わせてください。
            // GameManager.Instance.stageCoins = player.Coins;

            // このステージをクリア済みにする
            GameManager.Instance.MarkStageAsCleared(ClearedStageNumber);

            // ボス撃破などで新ワールドが解放される場合
            if (UnlocksNewWorld) {
                string worldFlag = "Unlocked_World_" + UnlockWorldReward;
                GameManager.Instance.AddEventFlag(worldFlag);
                GD.Print($"新ワールド解放フラグ【{worldFlag}】が立ちました！");
            }
        } else {
            //GD.PrintWarning("【テストモード】GameManagerがいないため、フラグ保存はスキップされます。");
        }

        // 5. 指定秒数（WaitTime）待機してから次のシーン（ボーナスステージ等）へ遷移
        await ToSignal(GetTree().CreateTimer(WaitTime), SceneTreeTimer.SignalName.Timeout);

        if (!string.IsNullOrEmpty(NextScenePath)) {
            GetTree().ChangeSceneToFile(NextScenePath);
        }
        // GoalPoint.cs の TriggerGoal 末尾：
        IrisTransitionManager iris = GetTree().CurrentScene.FindChild("IrisTransitionManager", true, false) as IrisTransitionManager;
        if (iris != null) {
            iris.StartIrisOut(player, NextScenePath);
        } else if (!string.IsNullOrEmpty(NextScenePath)) {
            GetTree().ChangeSceneToFile(NextScenePath);
        }
    }

    private void StopAllEnemies(Node parent) {
        foreach (Node child in parent.GetChildren()) {
            if (child is Enemy enemy) {
                enemy.SetPhysicsProcess(false);
                enemy.SetProcess(false);
            }
            StopAllEnemies(child);
        }
    }

}