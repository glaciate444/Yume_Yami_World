using Godot;
using System;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D, IDamageable{
    [Export] public int Hp = 3;
    [Export] public float KnockbackTime = 0.2f;
    [Export] public bool IsInvincible = false;

    //[Header("ドロップ・演出設定")]
    [Export] public PackedScene ItemPrefab;
    [Export(PropertyHint.Range, "0,100")] public int DropChance = 50;
    [Export] public PackedScene ExplosionEffectPrefab;
    [Export] public PackedScene IceBlockPrefab;

    //[Header("コミカル撃破設定（落下＆回転）")]
    [Export] public float DeathJumpForce = 350.0f;
    [Export] public float DeathSpinSpeed = 1000.0f; // 1秒間の回転角度

    //[Header("内部ノード参照")]
    [Export] private Node2D _graphics;
    [Export] private AnimatedSprite2D _animatedSprite;
    [Export] private CollisionShape2D _bodyCollision;
    [Export] public TouchDamage TouchDamageArea; // PlayerStompから参照するためpublic

    public bool IsDead { get; private set; } = false;

    private List<EnemyMovement> _movements = new List<EnemyMovement>();
    private float _knockbackTimer = 0.0f;
    private float _deathTimer = 0.0f;
    private float _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _Ready(){
        // 子ノードから EnemyMovement を継承したスクリプトをすべて自動取得
        foreach (Node child in GetChildren()){
            if (child is EnemyMovement movement){
                _movements.Add(movement);
            }
        }
    }

    public override void _PhysicsProcess(double delta){
        float dt = (float)delta;

        // 1. 死亡時（コミカル撃破の回転＆落下処理）
        if (IsDead){
            Velocity = new Vector2(0, Velocity.Y + (_gravity * 2.5f * dt));
            GlobalPosition += Velocity * dt; // 地形をすり抜けて画面下へ落とすため直接座標を更新

            if (_graphics != null){
                _graphics.RotationDegrees += DeathSpinSpeed * dt;
            }

            _deathTimer -= dt;
            if (_deathTimer <= 0.0f){
                QueueFree();
            }
            return;
        }

        // 2. 通常時の重力処理
        Vector2 velocity = Velocity;
        if (!IsOnFloor()){
            velocity.Y += _gravity * dt;
        }

        // 3. ノックバックタイマー管理
        if (_knockbackTimer > 0.0f){
            _knockbackTimer -= dt;
            velocity.X = Mathf.MoveToward(velocity.X, 0, 500.0f * dt);
            if (_knockbackTimer <= 0.0f){
                foreach (var m in _movements) m.PauseMovement(false);
                if (_animatedSprite != null) _animatedSprite.Play("walk");
            }
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public void TakeDamage(int damage, Vector2 knockbackDirection, bool isIceAttack = false){
        if (IsInvincible || IsDead) return;

        Hp -= damage;

        // 移動スクリプトを一時停止してノックバック速度を与える
        foreach (var m in _movements) m.PauseMovement(true);
        Velocity = new Vector2(knockbackDirection.X * 150.0f, -100.0f);

        if (_animatedSprite != null && _animatedSprite.SpriteFrames.HasAnimation("damage")){
            _animatedSprite.Play("damage");
        }

        if (Hp <= 0){
            if (isIceAttack && IceBlockPrefab != null){
                DieAsIce();
            }else{
                Die();
            }
        }else{
            _knockbackTimer = KnockbackTime;
        }
    }

    private void DieAsIce(){
        IsDead = true;
        SpawnPrefab(IceBlockPrefab);
        TryDropItem();
        QueueFree();
    }

    private void Die(){
        IsDead = true;
        _deathTimer = 3.0f; // 3秒間回転落下してから消滅

        SpawnPrefab(ExplosionEffectPrefab);
        TryDropItem();

        // 当たり判定と接触ダメージを完全に無効化
        if (_bodyCollision != null) _bodyCollision.SetDeferred("disabled", true);
        if (TouchDamageArea != null){
            TouchDamageArea.SetDeferred("monitoring", false);
            TouchDamageArea.SetDeferred("monitorable", false);
        }

        // アニメーションをダメージ画像で固定し、上へ跳ね上げる
        if (_animatedSprite != null) _animatedSprite.Pause();
        Velocity = new Vector2(0, -DeathJumpForce);
    }

    private void TryDropItem(){
        if (ItemPrefab != null && GD.RandRange(0, 99) < DropChance){
            SpawnPrefab(ItemPrefab);
        }
    }

    private void SpawnPrefab(PackedScene prefab){
        if (prefab == null) return;
        Node2D instance = prefab.Instantiate<Node2D>();
        instance.GlobalPosition = GlobalPosition;
        GetParent().CallDeferred(Node.MethodName.AddChild, instance);
    }
}