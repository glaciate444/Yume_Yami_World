using Godot;
using System;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D, IDamageable {
    [Export] public int Hp = 3;
    [Export] public float KnockbackTime = 0.2f;
    [Export] public bool IsInvincible = false;
    [Export] public bool CanBeKnockbacked = true;

    //[Header("ドロップ・演出設定")]
    [Export] public PackedScene ItemPrefab;
    [Export(PropertyHint.Range, "0,100")] public int DropChance = 50;
    [Export] public PackedScene ExplosionEffectPrefab;
    [Export] public PackedScene IceBlockPrefab;
    [Export] public PackedScene IceBreakEffectPrefab;

    //[Header("コミカル撃破設定（落下＆回転）")]
    [Export] public float DeathJumpForce = 350.0f;
    [Export] public float DeathSpinSpeed = 1000.0f; // 1秒間の回転角度

    //[Header("内部ノード参照")]
    [Export] private Node2D _graphics;
    [Export] private AnimatedSprite2D _animatedSprite;
    [Export] private CollisionShape2D _bodyCollision;
    [Export] public TouchDamage TouchDamageArea; // PlayerStompから参照するためpublic

    public bool IsDead { get; private set; } = false;

    // 子クラス（EnemyJumperなど）がダメージ硬直中かどうかを知るため
    protected bool IsInKnockback => _knockbackTimer > 0.0f;

    private List<EnemyMovement> _movements = new List<EnemyMovement>();
    private float _knockbackTimer = 0.0f;
    private float _deathTimer = 0.0f;
    private float _hitCooldown = 0.0f;
    private float _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _Ready() {
        // 子ノードから EnemyMovement を継承したスクリプトをすべて自動取得
        foreach (Node child in GetChildren()) {
            if (child is EnemyMovement movement) {
                _movements.Add(movement);
            }
        }

        // エクスポートが未割り当てでも動くよう、ノード名から補完する
        _graphics ??= GetNodeOrNull<Node2D>("Graphics");
        _animatedSprite ??= GetNodeOrNull<AnimatedSprite2D>("Graphics/AnimatedSprite2D");
        _bodyCollision ??= GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        TouchDamageArea ??= GetNodeOrNull<TouchDamage>("TouchDamage");
    }

    public override void _PhysicsProcess(double delta) {
        float dt = (float)delta;

        // 共通タイマー（子クラスでも必ず毎フレーム進む）
        if (_hitCooldown > 0.0f) _hitCooldown -= dt;

        // 保険：インスペクターなどで Hp を直接0にされた場合も死亡処理へ
        if (!IsDead && !IsInvincible && Hp <= 0) {
            Die();
        }

        // 死亡時（コミカル撃破の回転＆落下処理）
        if (IsDead) {
            ProcessDead(dt);
            return;
        }

        // 生きている時の処理（子クラスで差し替え可能）
        ProcessAlive(dt);
    }

    // 生きている時の標準処理：重力 → ノックバック管理 → 移動
    protected virtual void ProcessAlive(float dt) {
        Vector2 velocity = Velocity;
        if (!IsOnFloor()) {
            velocity.Y += _gravity * dt;
        }
        Velocity = velocity;

        // ノックバックする敵だけ摩擦をかける
        UpdateKnockbackTimer(dt, CanBeKnockbacked);

        MoveAndSlide();
    }

    // ノックバックタイマーの更新（子クラスからも呼べる）
    protected void UpdateKnockbackTimer(float dt, bool applyFriction) {
        if (_knockbackTimer <= 0.0f) return;

        _knockbackTimer -= dt;

        if (applyFriction) {
            Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0, 500.0f * dt), Velocity.Y);
        }

        if (_knockbackTimer <= 0.0f) {
            foreach (var m in _movements) m.PauseMovement(false);
            PlayDefaultAnimation();
        }
    }

    private void ProcessDead(float dt) {
        Velocity = new Vector2(0, Velocity.Y + (_gravity * 2.5f * dt));
        GlobalPosition += Velocity * dt; // 地形をすり抜けて画面下へ落とすため直接座標を更新

        if (_graphics != null) {
            _graphics.RotationDegrees += DeathSpinSpeed * dt;
        }

        // 他のスクリプトに接触判定を復活させられても、死亡中は無効のままにする
        if (TouchDamageArea != null && (TouchDamageArea.Monitoring || TouchDamageArea.Monitorable)) {
            DisableTouchDamage();
        }

        _deathTimer -= dt;
        if (_deathTimer <= 0.0f) {
            QueueFree();
        }
    }

    public void TakeDamage(int damage, Vector2 knockbackDirection, bool isIceAttack = false) {
        if (IsInvincible || IsDead || _hitCooldown > 0.0f) return;
        _hitCooldown = 0.15f;

        Hp -= damage;

        PlayDamageAnimation();

        // 「CanBeKnockbacked が true の時だけ」物理的に吹き飛ばす
        if (CanBeKnockbacked) {
            foreach (var m in _movements) m.PauseMovement(true);
            Velocity = new Vector2(knockbackDirection.X * 150.0f, -100.0f);
        } else {
            // ノックバックしない敵（固定砲台など）の場合、速度をゼロにする
            Velocity = Vector2.Zero;
        }

        if (Hp <= 0) {
            if (isIceAttack && IceBlockPrefab != null) {
                DieAsIce();
            } else {
                Die();
            }
        } else {
            // ノックバックタイマーは、アニメーションの復帰などにも使われるため作動させておく
            _knockbackTimer = KnockbackTime;
        }
    }

    private void DieAsIce() {
        IsDead = true;
        SpawnPrefab(IceBlockPrefab);
        TryDropItem();
        if (IceBreakEffectPrefab != null) {
            Node2D effect = (Node2D)IceBreakEffectPrefab.Instantiate();
            effect.GlobalPosition = GlobalPosition;
            GetParent().AddChild(effect);
        }

        QueueFree(); // 氷自身を消す
    }

    private void Die() {
        IsDead = true;
        _deathTimer = 3.0f; // 3秒間回転落下してから消滅

        // 移動スクリプト（巡回など）を止める
        foreach (var m in _movements) m.PauseMovement(true);

        SpawnPrefab(ExplosionEffectPrefab);
        TryDropItem();

        // 当たり判定と接触ダメージを完全に無効化
        if (_bodyCollision != null) _bodyCollision.SetDeferred("disabled", true);
        DisableTouchDamage();

        // アニメーションをダメージ画像で固定し、上へ跳ね上げる
        PlayDamageAnimation();
        if (_animatedSprite != null) _animatedSprite.Pause();
        Velocity = new Vector2(0, -DeathJumpForce);
    }

    private void DisableTouchDamage() {
        if (TouchDamageArea == null) return;
        TouchDamageArea.SetDeferred("monitoring", false);
        TouchDamageArea.SetDeferred("monitorable", false);
    }

    private void PlayDamageAnimation() {
        if (_animatedSprite == null || _animatedSprite.SpriteFrames == null) return;
        if (_animatedSprite.SpriteFrames.HasAnimation("damage")) {
            _animatedSprite.Play("damage");
        }
    }

    private void TryDropItem() {
        if (ItemPrefab != null && GD.RandRange(0, 99) < DropChance) {
            SpawnPrefab(ItemPrefab);
        }
    }

    private void SpawnPrefab(PackedScene prefab) {
        if (prefab == null) return;
        Node2D instance = prefab.Instantiate<Node2D>();
        instance.GlobalPosition = GlobalPosition;
        GetParent().CallDeferred(Node.MethodName.AddChild, instance);
    }

    private void PlayDefaultAnimation() {
        if (_animatedSprite == null || _animatedSprite.SpriteFrames == null) return;
        var frames = _animatedSprite.SpriteFrames;
        if (frames.HasAnimation("walk")) _animatedSprite.Play("walk");
        else if (frames.HasAnimation("idle")) _animatedSprite.Play("idle");
    }
}