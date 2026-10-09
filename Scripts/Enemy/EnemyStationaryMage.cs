using Godot;
using System;

public partial class EnemyStationaryMage : Node2D {
    [ExportCategory("Attack Settings")]
    [Export] public float AttackInterval = 3.0f;
    [Export] public float ShootDelay = 0.2f;

    [ExportCategory("Direction Settings")]
    [Export] public bool IsFacingLeft = false;
    [Export] public bool AutoTurnToPlayer = true;

    private Enemy _enemyBase;
    private AnimatedSprite2D _animatedSprite;
    private Node2D _graphics;
    private EnemyTurret _turret;
    private Node2D _player;

    private float _attackTimer = 0.0f;
    private bool _isAttacking = false;

    public override void _Ready() {
        _enemyBase = GetParent<Enemy>();
        _graphics = _enemyBase.GetNode<Node2D>("Graphics");
        _animatedSprite = _enemyBase.GetNode<AnimatedSprite2D>("Graphics/AnimatedSprite2D");
        _turret = _enemyBase.GetNodeOrNull<EnemyTurret>("EnemyTurret");

        UpdateFacingDirection(IsFacingLeft);
        _animatedSprite?.Play("idle");
        FindPlayer();
        GD.Print($"[Mage] player={_player != null}, graphics={_graphics != null}, turret={_turret != null}");
    }

    private void FindPlayer() {
        var players = GetTree().GetNodesInGroup("Player");
        if (players.Count > 0 && players[0] is Node2D p) {
            _player = p;
        }
    }

    public override void _Process(double delta) {
        if (_enemyBase == null || _enemyBase.IsDead) return;

        // ▼追加：ダメージ中（ノックバック中）は攻撃タイマーを進めない
        if (_animatedSprite != null && _animatedSprite.Animation == "damage") return;

        if (_player == null) {
            FindPlayer();
            if (_player == null) return;
        }

        if (AutoTurnToPlayer) {
            bool shouldFaceLeft = (_player.GlobalPosition.X - _enemyBase.GlobalPosition.X) < 0;
            UpdateFacingDirection(shouldFaceLeft);
        }

        _attackTimer += (float)delta;
        if (_attackTimer >= AttackInterval) {
            _attackTimer = 0.0f;
            StartAttack();
        }
    }

    private void UpdateFacingDirection(bool faceLeft) {
        float sign = faceLeft ? -1.0f : 1.0f;
        if (_graphics != null) _graphics.Scale = new Vector2(sign, 1);
        if (_turret != null) _turret.Scale = new Vector2(sign, 1);
    }

    private async void StartAttack() {
        _isAttacking = true;

        if (_animatedSprite != null) {
            _animatedSprite.Play("attack");
            _animatedSprite.Frame = 0; // ▼追加：必ず最初のフレームから再生する
        }

        await ToSignal(GetTree().CreateTimer(ShootDelay), SceneTreeTimer.SignalName.Timeout);

        // ▼追加：待機中にダメージを受けていたら、攻撃を中止する
        if (_enemyBase.IsDead || (_animatedSprite != null && _animatedSprite.Animation == "damage")) {
            _isAttacking = false;
            return;
        }

        if (_turret != null) _turret.Shoot();

        await ToSignal(GetTree().CreateTimer(0.4f), SceneTreeTimer.SignalName.Timeout);

        // ▼追加：撃った後の余韻中にダメージを受けていた場合も中止する
        if (_enemyBase.IsDead || (_animatedSprite != null && _animatedSprite.Animation == "damage")) {
            _isAttacking = false;
            return;
        }

        if (_animatedSprite != null && !_enemyBase.IsDead) _animatedSprite.Play("idle");

        _isAttacking = false;
    }
}