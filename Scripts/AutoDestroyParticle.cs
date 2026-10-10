using Godot;
using System;

public partial class AutoDestroyParticle : CpuParticles2D {
    public override void _Ready() {
        // 念のため、生成時にエミットを開始する
        Emitting = true;
    }

    public override void _Process(double delta) {
        // パーティクルの放出が終わったら自分自身を削除する
        if (!Emitting) {
            QueueFree();
        }
    }
}