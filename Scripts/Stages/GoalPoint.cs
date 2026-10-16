using Godot;
using System;
using System.Threading.Tasks;

public partial class GoalPoint : Area2D {
    [ExportCategory("遷移先設定")]
    [Export(PropertyHint.File, "*.tscn")]
    public string NextScenePath = "res://Scene/mini_game_scene.tscn";

    [ExportCategory("ステージ進行設定（フラグ式）")]
    [Export] public int ClearedStageNumber = 1;

    [ExportCategory("ワールド進行設定（ボス専用）")]
    [Export] public bool UnlocksNewWorld = false;
    [Export] public int UnlockWorldReward = 2;

    [ExportCategory("サウンド・演出設定")]
    [Export] public AudioStream ResultBGM;
    [Export] public float WaitTime = 2.0f;
    [Export] public float IrisDuration = 1.5f; // アイリスアウトが閉じる時間

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

        // 2. BGMを止めてファンファーレを鳴らす
        if (SoundManager.Instance != null) {
            SoundManager.Instance.StopBGM();
            if (ResultBGM != null) {
                SoundManager.Instance.PlaySE(ResultBGM);
            }
        }

        // 3. プレイヤーのゴール演出（停止＆待機アニメ化）
        player.PlayGoalAction();

        // 4. 進行フラグの更新
        if (GameManager.Instance != null) {
            GameManager.Instance.MarkStageAsCleared(ClearedStageNumber);

            if (UnlocksNewWorld) {
                string worldFlag = "Unlocked_World_" + UnlockWorldReward;
                GameManager.Instance.AddEventFlag(worldFlag);
            }
        }

        // 5. 指定秒数（WaitTime）待機
        await ToSignal(GetTree().CreateTimer(WaitTime), SceneTreeTimer.SignalName.Timeout);

        // 6. プレイヤーを中心にアイリスアウトを実行してから次シーンへ遷移
        await PlayIrisOut(player);

        if (!string.IsNullOrEmpty(NextScenePath)) {
            GetTree().ChangeSceneToFile(NextScenePath);
        }
    }

    // アイリスアウト演出（GoalPoint単体で確実に動作します）
    private async Task PlayIrisOut(Node2D target) {
        CanvasLayer canvasLayer = new CanvasLayer();
        canvasLayer.Layer = 100;
        AddChild(canvasLayer);

        ColorRect blackRect = new ColorRect();
        blackRect.Color = Colors.Black;
        blackRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvasLayer.AddChild(blackRect);

        Shader shader = new Shader();
        shader.Code = @"
            shader_type canvas_item;
            uniform vec2 center = vec2(0.5, 0.5);
            uniform float radius = 1.5;
            uniform vec2 screen_size = vec2(1920.0, 1080.0);

            void fragment() {
                vec2 uv = UV * screen_size;
                vec2 c = center * screen_size;
                float dist = distance(uv, c);
                float max_dist = length(screen_size);
                if (dist < radius * max_dist) {
                    COLOR = vec4(0.0, 0.0, 0.0, 0.0);
                } else {
                    COLOR = vec4(0.0, 0.0, 0.0, 1.0);
                }
            }";
        ShaderMaterial mat = new ShaderMaterial();
        mat.Shader = shader;
        blackRect.Material = mat;

        float timer = 0f;
        while (timer < IrisDuration) {
            timer += (float)GetProcessDeltaTime();
            float progress = Mathf.Clamp(timer / IrisDuration, 0f, 1f);
            float ease = progress * progress * progress;

            mat.SetShaderParameter("radius", Mathf.Lerp(1.2f, 0.0f, ease));

            Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
            mat.SetShaderParameter("screen_size", viewportSize);

            if (IsInstanceValid(target)) {
                // 画面上のプレイヤーの位置（0.0〜1.0）を計算して円の中心にする
                Vector2 screenPos = target.GetGlobalTransformWithCanvas().Origin;
                mat.SetShaderParameter("center", screenPos / viewportSize);
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        mat.SetShaderParameter("radius", 0.0f);
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
        canvasLayer.QueueFree();
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