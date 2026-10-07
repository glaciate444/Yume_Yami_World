using Godot;
using System;

public partial class MovingPlatform : AnimatableBody2D{
    public enum MoveMode{
        AlwaysMove, // 常に動いている（標準）
        MoveOnRide  // プレイヤーが乗ったら動き出す
    }

    //[Header("移動設定")]
    [Export] public MoveMode Mode = MoveMode.AlwaysMove;
    [Export] private Node2D[] _waypoints;      // 目標地点（Marker2Dなど）のリスト
    [Export] public float Speed = 150.0f;      // 移動スピード（ピクセル/秒）
    [Export] public float WaitTime = 1.0f;     // 到着時の待機時間

    //[Header("スイッチ式用の乗降判定")]
    [Export] private Area2D _rideDetector;     // 床の上面に配置したArea2D

    private int _currentPointIndex = 0;
    private float _waitTimer = 0.0f;
    private bool _isMoving = false;

    public override void _Ready(){
        // コード（_PhysicsProcess）から直接座標を動かすため、SyncToPhysicsはオフにする（ガクつき防止）
        SyncToPhysics = false;
        _waitTimer = WaitTime;

        if (Mode == MoveMode.AlwaysMove){
            _isMoving = true;
        }

        if (_rideDetector != null){
            _rideDetector.BodyEntered += OnRideDetectorBodyEntered;
        }
    }

    public override void _PhysicsProcess(double delta){
        if (!_isMoving || _waypoints == null || _waypoints.Length == 0) return;

        Node2D targetPoint = _waypoints[_currentPointIndex];
        if (targetPoint == null) return;

        Vector2 currentPos = GlobalPosition;
        Vector2 targetPos = targetPoint.GlobalPosition;

        // 目的地に向かって移動
        GlobalPosition = currentPos.MoveToward(targetPos, Speed * (float)delta);

        // 目的地にほぼ到着したかどうかの判定
        if (GlobalPosition.DistanceTo(targetPos) < 0.5f){
            GlobalPosition = targetPos; // 座標をピタッと合わせる
            _waitTimer -= (float)delta;

            if (_waitTimer <= 0.0f){
                _currentPointIndex = (_currentPointIndex + 1) % _waypoints.Length;
                _waitTimer = WaitTime;
            }
        }
    }

    private void OnRideDetectorBodyEntered(Node2D body){
        if (body is Player player){
            // スイッチ式モードでプレイヤーが上に乗ったら起動
            if (Mode == MoveMode.MoveOnRide && !_isMoving && player.Velocity.Y >= 0){
                _isMoving = true;
            }
        }
    }
}