using Godot;
using System;

public partial class RewardPopup : Control {
    [ExportCategory("UIパーツ")]
    [Export] public TextureRect IconImage;
    [Export] public Label AmountText;

    [ExportCategory("アニメーション設定")]
    [Export] public float MoveSpeed = 50f; // 上に昇るスピード[cite: 23]
    [Export] public float LifeTime = 1.5f; // 何秒で消えるか[cite: 23]

    private float _elapsed = 0f;

    public void Setup(Texture2D icon, int amount) {
        if (IconImage != null) {
            IconImage.Texture = icon;
        }

        if (AmountText != null) {
            // プラスの時は「+50」、マイナスの時は「-20」と表示[cite: 23]
            AmountText.Text = (amount >= 0 ? "+" : "") + amount.ToString();
            // プラスは黄色、マイナスは赤色[cite: 23]
            AmountText.Modulate = amount >= 0 ? Colors.Yellow : Colors.Red;
        }
    }

    public override void _Process(double delta) {
        float dt = (float)delta;
        _elapsed += dt;

        // 上に移動させる（Godotの2D/UIはYマイナスが上方向）[cite: 23]
        Position += new Vector2(0, -MoveSpeed * dt);

        // 徐々に透明にする[cite: 23]
        Color c = Modulate;
        c.A = Mathf.Clamp(1.0f - (_elapsed / LifeTime), 0f, 1f);
        Modulate = c;

        // 指定秒数後に消滅させる[cite: 23]
        if (_elapsed >= LifeTime) {
            QueueFree();
        }
    }
}