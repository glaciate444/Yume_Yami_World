using Godot;
using System;

public partial class TouchDamage : Area2D {
    [Export] public int Damage = 1;
    [Export] public float Impact = 1.0f;
    [Export] public bool CanBeStomped = true;

    private float _disableTimer = 0.0f;

    public override void _Ready() {
        CollisionMask = 1 | 2 | 4;
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta) {
        if (_disableTimer > 0.0f) {
            _disableTimer -= (float)delta;
            return;
        }

        // 密着している間も確実にプレイヤーを検知する
        foreach (Node2D body in GetOverlappingBodies()) {
            if (body is Player player) {
                TryDamagePlayer(player);
            }
        }
    }

    public void DisableTemporarily(float duration) {
        _disableTimer = duration;
    }

    private void OnBodyEntered(Node2D body) {
        if (body is Player player) {
            TryDamagePlayer(player);
        }
    }

    private void TryDamagePlayer(Player player) {
        if (_disableTimer > 0.0f || player == null) return;

        // 空中で上から踏みつけようとしている時だけ接触ダメージをスキップ
        if (CanBeStomped) {
            bool isFallingInAir = !player.IsOnFloor() && player.Velocity.Y > 0.0f;
            bool isAboveHead = player.GlobalPosition.Y < GlobalPosition.Y - 25.0f;
            if (isFallingInAir && isAboveHead) {
                return;
            }
        }

        _disableTimer = 0.2f; // 連続ヒット防止

        float dirX = (player.GlobalPosition.X >= GlobalPosition.X) ? 1.0f : -1.0f;
        Vector2 knockbackDir = new Vector2(dirX, -1.0f);
        player.TakeDamage(Damage, knockbackDir * Impact);
    }
}