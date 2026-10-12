using Godot;
using System;

public partial class EnemyJumper : Enemy {
    [ExportCategory("Jump Settings")]
    [Export] public float JumpForceX = 150.0f;
    [Export] public float JumpForceY = -400.0f;
    [Export] public float JumpInterval = 2.0f;

    private float _jumpTimer;
    private AnimatedSprite2D _sprite;
    private float _jumpGravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _Ready() {
        base._Ready();
        _sprite = GetNodeOrNull<AnimatedSprite2D>("Graphics/AnimatedSprite2D");
        _jumpTimer = JumpInterval;
    }

    // _PhysicsProcess は上書きしない。
    // タイマー更新・死亡処理は親（Enemy.cs）が毎フレーム行い、
    // 生きている時の動きだけをここで差し替える。
    protected override void ProcessAlive(float dt) {
        // ダメージ硬直タイマーを進める（ジャンプ敵は摩擦なし）
        UpdateKnockbackTimer(dt, false);

        Vector2 velocity = Velocity;

        // 1. 重力
        if (!IsOnFloor()) {
            velocity.Y += _jumpGravity * dt;
        }

        // 2. 接地中の処理
        if (IsOnFloor()) {
            _jumpTimer -= dt;

            // 着地後のブレーキ
            if (_jumpTimer < JumpInterval - 0.1f) {
                velocity.X = Mathf.MoveToward(velocity.X, 0, 300 * dt);
            }

            // ジャンプ実行（プレイヤーがいて、ダメージ硬直中でない時）
            bool hasPlayer = GameManager.Instance?.currentPlayer != null;
            if (_jumpTimer <= 0.0f && hasPlayer && !IsInKnockback) {
                velocity = JumpTowardsPlayer();
                _jumpTimer = JumpInterval;
            }
        }

        Velocity = velocity;
        MoveAndSlide();

        UpdateAnimation();
    }

    private Vector2 JumpTowardsPlayer() {
        Player player = GameManager.Instance.currentPlayer;

        // プレイヤーの位置に向かって飛ぶ方向を計算
        float direction = Mathf.Sign(player.GlobalPosition.X - GlobalPosition.X);

        if (_sprite != null) {
            // 元画像が「右向き」：プレイヤーが左側にいる時だけ左を向かせる
            _sprite.FlipH = (direction < 0);
        }

        return new Vector2(direction * JumpForceX, JumpForceY);
    }

    private void UpdateAnimation() {
        if (_sprite == null) return;

        // ダメージ硬直中は damage アニメを上書きしない
        if (IsInKnockback) return;

        if (IsOnFloor()) {
            _sprite.Play("idle");
        } else {
            if (Velocity.Y < 0)
                _sprite.Play("jump");
            else
                _sprite.Play("fall");
        }
    }
}