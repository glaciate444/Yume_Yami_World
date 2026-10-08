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
        if (body is Enemy enemy) {
            TryStomp(enemy);
        }
    }

    private void OnAreaEntered(Area2D area) {
        if (area is TouchDamage td && td.GetParent() is Enemy enemy) {
            TryStomp(enemy);
        }
    }

    private void TryStomp(Enemy enemy) {
        if (_player == null || enemy.IsDead) return;

        // ヒップドロップ中、または地上・ジャンプ上昇中は踏まない
        if (_player.IsHipDropping || _player.IsOnFloor() || _player.Velocity.Y <= 0.0f) return;

        if (enemy.TouchDamageArea != null && !enemy.TouchDamageArea.CanBeStomped) {
            return;
        }

        // 敵の中心より上から踏んだ時のみ発動
        if (_player.GlobalPosition.Y < enemy.GlobalPosition.Y) {
            if (enemy.TouchDamageArea != null) {
                enemy.TouchDamageArea.DisableTemporarily(0.5f);
            }

            enemy.TakeDamage(StompDamage, Vector2.Down);
            _player.Bounce(BounceForce);
        }
    }
}