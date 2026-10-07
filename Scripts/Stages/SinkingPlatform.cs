using Godot;
using System;

public partial class SinkingPlatform : AnimatableBody2D{
    //[Header("沈み込み設定")]
    [Export] public float SinkDistance = 16.0f; // どのくらい下がるか（ピクセル）
    [Export] public float SinkSpeed = 60.0f;    // 沈むときの速さ（ピクセル/秒）
    [Export] public float ReturnSpeed = 30.0f;  // 元に戻るときの速さ（ピクセル/秒）

    //[Tooltip("チェックを入れると、プレイヤーが降りた際に元の高さに戻ります")]
    [Export] public bool AutoReturn = true;

    //[Header("乗降検知")]
    [Export] private Area2D _rideDetector;

    private Vector2 _initialPosition;
    private Vector2 _targetPosition;
    private bool _isPlayerOn = false;
    private bool _hasBeenSteppedOn = false; // AutoReturnがオフの時に一度でも乗ったかを記憶

    public override void _Ready(){
        SyncToPhysics = false;

        // 初期位置と沈み込んだ後の位置を記憶（Godotは下方向がYプラス）
        _initialPosition = GlobalPosition;
        _targetPosition = _initialPosition + new Vector2(0, SinkDistance);

        if (_rideDetector != null){
            _rideDetector.BodyEntered += OnRideDetectorBodyEntered;
            _rideDetector.BodyExited += OnRideDetectorBodyExited;
        }
    }

    public override void _PhysicsProcess(double delta){
        Vector2 currentTarget = _initialPosition;

        if (_isPlayerOn){
            currentTarget = _targetPosition;
        }else if (!AutoReturn && _hasBeenSteppedOn){
            // AutoReturnがオフで、一度でも乗ったことがあるなら沈んだ位置を維持
            currentTarget = _targetPosition;
        }

        float currentSpeed = _isPlayerOn ? SinkSpeed : ReturnSpeed;

        if (GlobalPosition != currentTarget){
            GlobalPosition = GlobalPosition.MoveToward(currentTarget, currentSpeed * (float)delta);
        }
    }

    private void OnRideDetectorBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            // 上から乗った時のみ反応
            if (player.Velocity.Y >= -10.0f && player.GlobalPosition.Y <= GlobalPosition.Y)
            {
                _isPlayerOn = true;
                _hasBeenSteppedOn = true;
            }
        }
    }

    private void OnRideDetectorBodyExited(Node2D body)
    {
        if (body is Player)
        {
            _isPlayerOn = false;
        }
    }
}