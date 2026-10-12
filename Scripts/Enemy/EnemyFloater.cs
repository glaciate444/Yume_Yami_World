using Godot;
using System;

public partial class EnemyFloater : Enemy {
    // ▼ インスペクターで動きの種類を選べるようにする列挙型
    public enum MoveMode {
        Horizontal, // 左右往復
        Vertical,   // 上下往復
        Circular    // 円移動
    }

    [ExportCategory("Float Settings")]
    [Export] public MoveMode Mode = MoveMode.Horizontal;
    [Export] public float Amplitude = 50.0f; // 往復の幅、または円の半径
    [Export] public float Speed = 2.0f;      // 移動の速さ

    private Vector2 _startPosition;
    private float _time = 0.0f;
    private AnimatedSprite2D _sprite;

    public override void _Ready() {
        base._Ready(); // 親の初期化を呼ぶ

        _sprite = GetNodeOrNull<AnimatedSprite2D>("Graphics/AnimatedSprite2D");

        // 配置された初期位置を記憶し、ここを基準に往復や円移動を行う
        _startPosition = GlobalPosition;
    }

    // ▼ 親のProcessAliveを上書きし、重力を無効化して独自の空中移動を行う
    protected override void ProcessAlive(float dt) {
        // 鞭で殴られた時などのノックバック（硬直）中の処理
        if (IsInKnockback) {
            UpdateKnockbackTimer(dt, true); // 摩擦をかけて吹っ飛ばす
            MoveAndSlide();                 // 吹っ飛んだ速度で移動
            return;                         // 硬直中は以下の移動処理をしない
        }

        // 時間を進める
        _time += dt;

        // モードに応じた目標位置（Target）の計算
        Vector2 targetPos = _startPosition;

        switch (Mode) {
            case MoveMode.Horizontal:
                targetPos.X += Mathf.Sin(_time * Speed) * Amplitude;
                break;
            case MoveMode.Vertical:
                targetPos.Y += Mathf.Sin(_time * Speed) * Amplitude;
                break;
            case MoveMode.Circular:
                // 円運動（CosとSinを組み合わせて円を描く）
                targetPos.X += Mathf.Cos(_time * Speed) * Amplitude;
                targetPos.Y += Mathf.Sin(_time * Speed) * Amplitude;
                break;
        }

        // 目標位置へ向かう速度を計算して代入（ここで重力は一切足さない）
        Velocity = (targetPos - GlobalPosition) / dt;

        MoveAndSlide();

        UpdateAnimation();
    }

    private void UpdateAnimation() {
        if (_sprite == null) return;

        // 横移動している場合、進行方向を向く（元画像が右向き前提）
        if (Mathf.Abs(Velocity.X) > 0.1f) {
            _sprite.FlipH = (Velocity.X < 0);
        }

        // 飛行中のアニメーション（羽ばたきなど）があれば再生
        if (_sprite.SpriteFrames != null) {
            // 次に再生すべきアニメーション名を決める
            string nextAnim = _sprite.SpriteFrames.HasAnimation("fly") ? "fly" : "idle";

            // ▼ 修正：現在設定されているアニメーションと違う場合、
            // もしくは完全に止まってしまっている場合だけ Play を呼ぶ
            if (_sprite.Animation != nextAnim || !_sprite.IsPlaying()) {
                _sprite.Play(nextAnim);
            }
        }
    }
}