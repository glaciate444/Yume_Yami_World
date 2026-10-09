using Godot;
using System;

public partial class Bullet : Area2D {
    [Export] public float Speed = 400.0f;
    [Export] public int Damage = 2;
    [Export] public float Impact = 150.0f;
    [Export] public float LifeTime = 2.0f;
    [Export] public bool IsPiercing = false;
    [Export] public bool IsIceAttack = true; // 氷の弾ならオンにする
    [Export] public PackedScene HitEffectPrefab;

    // 自機弾か敵弾かの識別（インスペクターまたは生成時に設定可能）
    [Export] public bool IsPlayerBullet = true;

    private Vector2 _direction;
    private float _timer = 0.0f;
    private Node2D _shooter; // 発射元（誤爆防止用）

    /// <summary>
    /// 弾の初期化。方向・陣営・発射元ノードを渡す
    /// </summary>
    public void Initialize(Vector2 direction, bool isPlayerBullet = true, Node2D shooter = null) {
        _direction = direction.Normalized();
        IsPlayerBullet = isPlayerBullet;
        _shooter = shooter;

        // 弾の向きを進行方向に合わせる（右向き画像基準）
        Rotation = _direction.Angle();
    }

    public override void _Ready() {
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta) {
        float dt = (float)delta;

        // まっすぐ飛ばす
        GlobalPosition += _direction * Speed * dt;

        // 寿命タイマー
        _timer += dt;
        if (_timer >= LifeTime) {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node2D body) {
        // 画面外判定用カメラバウンダリなどは無視する
        if (body.Name == "CameraBounds") return;

        // 発射した本人自身には当たらないようにする
        if (body == _shooter) return;

        // 陣営チェック（グループ判定による同士討ち防止）
        // ※ プレイヤー側ノードに "Player" グループ、敵側ノードに "Enemy" グループを設定している前提
        if (IsPlayerBullet && body.IsInGroup("Player")) return;
        if (!IsPlayerBullet && body.IsInGroup("Enemy")) return;

        bool hitSomething = false;

        // IDamageable（敵、プレイヤー、破壊可能な箱など）へのダメージ処理
        if (body is IDamageable target) {
            Vector2 knockback = _direction * Impact;
            target.TakeDamage(Damage, knockback, IsIceAttack);
            hitSomething = true;
        }
        // 地形（TileMapLayerなど）に当たった場合
        else if (body is TileMapLayer || body is StaticBody2D) {
            hitSomething = true;
        }

        if (hitSomething) {
            if (HitEffectPrefab != null) {
                var effect = HitEffectPrefab.Instantiate<Node2D>();
                effect.GlobalPosition = GlobalPosition;
                GetParent().CallDeferred(Node.MethodName.AddChild, effect);
            }

            if (!IsPiercing) {
                QueueFree(); // 貫通しないなら消滅
            }
        }
    }
}