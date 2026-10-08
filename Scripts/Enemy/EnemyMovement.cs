using Godot;

public abstract partial class EnemyMovement : Node{
    protected bool IsPaused = false;

    // ダメージを受けた時や死亡時に Enemy.cs から呼ばれる一時停止命令
    public virtual void PauseMovement(bool isPaused){
        IsPaused = isPaused;
        SetPhysicsProcess(!isPaused);
    }
}