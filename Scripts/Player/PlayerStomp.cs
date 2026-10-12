using Godot;
using System;

public partial class PlayerStomp : Area2D {
    [Export] private Player _player;
    [Export] public int StompDamage = 1;
    [Export] public float BounceForce = 550.0f;

    public override void _Ready() {
        CollisionMask = 1 | 2 | 4;
        BodyEntered += OnBodyEntered;
        AreaEntered += OnAreaEntered;
    }

    private void OnBodyEntered(Node2D body) {
        if (body is Enemy enemy) CallDeferred(nameof(TryStomp), enemy);
    }

    private void OnAreaEntered(Area2D area) {
        if (area is TouchDamage td && td.GetParent() is Enemy enemy)
            CallDeferred(nameof(TryStomp), enemy);
    }

    private void TryStomp(Enemy enemy) {
        if (!IsInstanceValid(enemy) || _player == null || enemy.IsDead) return;
        if (_player.IsHipDropping || _player.IsOnFloor() || _player.Velocity.Y <= 0.0f) return;
        if (enemy.TouchDamageArea != null && !enemy.TouchDamageArea.CanBeStomped) return;

        // 敵の「上端」より上から踏んだ時だけ有効（頭上の空振り防止）
        // ※ 敵の半径に合わせて調整してください
        if (_player.GlobalPosition.Y < enemy.GlobalPosition.Y - 10.0f) {
            enemy.TouchDamageArea?.DisableTemporarily(0.5f);
            enemy.TakeDamage(StompDamage, Vector2.Zero);
            _player.Bounce(BounceForce);
        }
    }
}