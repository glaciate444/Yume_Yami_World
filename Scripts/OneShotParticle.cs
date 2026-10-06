using Godot;

// ▼ CPUParticles2D ではなく CpuParticles2D に修正
public partial class OneShotParticle : CpuParticles2D{
    public override void _Ready()
    {
        Emitting = true;
        Finished += QueueFree;
    }
}