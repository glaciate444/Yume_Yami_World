using Godot;
using System;

// 箱から飛び出してバウンドするアイテム
public partial class BouncingItem : CharacterBody2D {
    [Export] public float BounceFactor = 0.5f; // バウンドの強さ（0.5なら半分の高さで跳ねる）
    [Export] public float Friction = 200.0f;   // 地面を滑る時のブレーキ

    private float _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _PhysicsProcess(double delta) {
        float dt = (float)delta;
        Vector2 velocity = Velocity;

        // 重力で落下
        if (!IsOnFloor()) {
            velocity.Y += _gravity * dt;
        } else {
            // 床に着地したら、Yの速度を反転させてバウンドさせる
            // 速度が小さい場合はバウンドを止める
            if (Mathf.Abs(velocity.Y) > 50.0f) {
                velocity.Y = -velocity.Y * BounceFactor;
            } else {
                velocity.Y = 0;
            }

            // X方向の速度（横滑り）にブレーキをかける
            velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * dt);
        }

        Velocity = velocity;
        MoveAndSlide();

        // 天井や壁にぶつかった時の跳ね返り（オプション）
        if (IsOnCeiling()) velocity.Y = Mathf.Abs(velocity.Y) * BounceFactor;
        if (IsOnWall()) velocity.X = -velocity.X * BounceFactor;
    }
}