using Godot;

public partial class OneShotParticle : CpuParticles2D {
    [Export] public int Count = 6;               // 合計で何回出すか
    [Export] public float ScatterRadius = 24f;   // 発生位置のばらつき(px)
    [Export] public float Interval = 0.05f;      // 1回ごとの発生間隔(秒)

    private float _delay = 0f;
    private bool _isClone = false;

    public override void _Ready() {
        Emitting = false;
        Finished += QueueFree;
        CallDeferred(nameof(Begin));
    }

    private void Begin() {
        // 親(Enemy.cs)が位置を代入し終えた後に、ここへ来る
        if (!_isClone) {
            for (int i = 1; i < Count; i++) {
                var clone = (OneShotParticle)Duplicate();
                clone._isClone = true;
                clone._delay = i * Interval;
                GetParent().AddChild(clone);
                clone.GlobalPosition = GlobalPosition;
            }
        }

        float a = GD.Randf() * Mathf.Tau;
        float r = GD.Randf() * ScatterRadius;
        GlobalPosition += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;

        if (_delay <= 0f) {
            Emitting = true;
        } else {
            GetTree().CreateTimer(_delay).Timeout += () => {
                if (IsInstanceValid(this)) Emitting = true;
            };
        }
    }
}