using Godot;
using System;
using System.Threading.Tasks;

public partial class IrisTransitionManager : Node {
    [ExportCategory("演出設定")]
    [Export] public float IrisDuration = 1.5f; // 閉じるまでにかかる時間[cite: 24]

    public async void StartIrisOut(Node2D targetNode, string nextScenePath) {
        await IrisOutRoutine(targetNode, nextScenePath);
    }

    private async Task IrisOutRoutine(Node2D target, string nextScenePath) {
        // 1. 画面最前面に描画するためのCanvasLayerとColorRectを自動生成
        CanvasLayer canvasLayer = new CanvasLayer();
        canvasLayer.Layer = 100; // HUDよりも手前に表示
        AddChild(canvasLayer);

        ColorRect blackRect = new ColorRect();
        blackRect.Color = Colors.Black;
        blackRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        canvasLayer.AddChild(blackRect);

        // 2. プレイヤーの位置を丸くくり抜くシェーダーをプログラムで生成
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
                    COLOR = vec4(0.0, 0.0, 0.0, 0.0); // 円の内側は透明
                } else {
                    COLOR = vec4(0.0, 0.0, 0.0, 1.0); // 円の外側は黒[cite: 24]
                }
            }";
        ShaderMaterial mat = new ShaderMaterial();
        mat.Shader = shader;
        blackRect.Material = mat;

        // 3. アニメーション処理（穴を徐々に小さくしていく）[cite: 24]
        float timer = 0f;
        while (timer < IrisDuration) {
            timer += (float)GetProcessDeltaTime();
            float progress = Mathf.Clamp(timer / IrisDuration, 0f, 1f);
            float ease = progress * progress * progress; // 3乗イージング[cite: 24]

            float currentRadius = Mathf.Lerp(1.2f, 0.0f, ease);
            mat.SetShaderParameter("radius", currentRadius);

            Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
            mat.SetShaderParameter("screen_size", viewportSize);

            if (IsInstanceValid(target)) {
                // プレイヤーの画面上の座標（0.0〜1.0）を取得して円の中心にする
                Vector2 screenPos = target.GetGlobalTransformWithCanvas().Origin;
                Vector2 normalizedPos = screenPos / viewportSize;
                mat.SetShaderParameter("center", normalizedPos);
            }

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        mat.SetShaderParameter("radius", 0.0f);

        // 0.2秒待ってからシーン切り替え[cite: 24]
        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);

        canvasLayer.QueueFree();

        if (!string.IsNullOrEmpty(nextScenePath)) {
            GetTree().ChangeSceneToFile(nextScenePath);
        }
    }
}