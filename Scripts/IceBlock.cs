using Godot;
using System;

public partial class IceBlock : CharacterBody2D, IDamageable {
    [Export] public float LifeTime = 5.0f;
    [Export] public float SlideSpeed = 400.0f;
    [Export] public int AttackDamage = 5;
    [Export] public PackedScene BreakParticlePrefab;

    [Export] private Sprite2D _sprite;

    private bool _isSliding = false;
    private float _slideDirection = 0.0f;
    private float _lifeTimer = 0.0f;
    private float _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _PhysicsProcess(double delta) {
        float dt = (float)delta;
        Vector2 velocity = Velocity;

        // 1. 重力
        if (!IsOnFloor()) {
            velocity.Y += _gravity * dt;
        }

        // 2. 滑走処理[cite: 21]
        if (_isSliding) {
            velocity.X = _slideDirection * SlideSpeed;
        } else {
            velocity.X = 0; // 止まっている時は動かない

            // 寿命タイマーと点滅処理[cite: 21]
            _lifeTimer += dt;
            if (_lifeTimer >= LifeTime - 2.0f && _sprite != null) {
                // 残り2秒で半透明点滅させる
                float alpha = Mathf.Sin(_lifeTimer * 20.0f) > 0 ? 1.0f : 0.4f;
                _sprite.Modulate = new Color(1, 1, 1, alpha);
            }
            if (_lifeTimer >= LifeTime) {
                BreakIce();
            }
        }

        Velocity = velocity;
        MoveAndSlide();

        // 3. 滑走中の衝突判定[cite: 21]
        if (_isSliding) {
            for (int i = 0; i < GetSlideCollisionCount(); i++) {
                var collision = GetSlideCollision(i);

                // 床や坂道は無視。進行方向と正面から向き合う面だけ判定する
                if (collision.GetNormal().X * _slideDirection > -0.8f) continue;

                var collider = collision.GetCollider();
                if (collider is Player) continue;

                if (collider is IDamageable target) {
                    target.TakeDamage(AttackDamage, new Vector2(_slideDirection * 150.0f, -100.0f));
                }
                BreakIce();
                break;
            }
        }
    }

    // プレイヤーがムチで叩いた・踏んだ時に呼ばれる[cite: 21]
    public void TakeDamage(int damage, Vector2 knockbackDirection, bool isIceAttack = false) {
        if (!_isSliding) {
            // 叩かれた方向へ滑り出す[cite: 21]
            float dir = knockbackDirection.X >= 0 ? 1.0f : -1.0f;
            StartSliding(dir);
        } else {
            BreakIce(); // すでに滑っている時に叩かれたら砕ける[cite: 21]
        }
    }

    private void StartSliding(float dir) {
        _isSliding = true;
        _slideDirection = dir;
        _lifeTimer = 0.0f; // 寿命タイマーをキャンセルして消えなくする[cite: 21]
        if (_sprite != null) _sprite.Modulate = new Color(1, 1, 1, 1);
    }

    private void BreakIce() {
        if (BreakParticlePrefab != null) {
            var effect = BreakParticlePrefab.Instantiate<Node2D>();
            effect.GlobalPosition = GlobalPosition;
            GetParent().CallDeferred(Node.MethodName.AddChild, effect);
        }
        QueueFree();
    }
    // プレイヤーが体で押した時に呼ばれる
    public void Push(float dir) {
        if (_isSliding) return;
        StartSliding(dir);
    }
}