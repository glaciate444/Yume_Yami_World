using Godot;
using System;

public partial class FallingPlatform : AnimatableBody2D{
    public enum PlatformType{
        SimpleFall,       // 乗ったら一定時間後に落下
        TimedMoveAndFall, // 乗ったら横移動し、一定時間後に落下
        AutoMove          // 自動で横移動し続ける（乗っても落下しない）
    }

    //[Header("モード設定")]
    [Export] public PlatformType Type = PlatformType.SimpleFall;

    //[Header("共通設定")]
    [Export] public float FallDelay = 1.0f;       // 落下までの猶予時間（秒）
    [Export] public float DestroyDelay = 3.0f;    // 落下開始から消滅するまでの時間（秒）

    //[Header("移動設定（TimedMoveAndFall / AutoMove用）")]
    [Export] public float MoveSpeedX = 150.0f;    // 横移動スピード（ピクセル/秒）

    //[Header("乗降検知")]
    [Export] private Area2D _rideDetector;

    private bool _isTriggered = false;
    private bool _isFalling = false;
    private float _timer = 0.0f;
    private float _destroyTimer = 0.0f;
    private Vector2 _fallVelocity = Vector2.Zero;
    private float _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    public override void _Ready(){
        SyncToPhysics = false;
        _timer = FallDelay;

        if (_rideDetector != null){
            _rideDetector.BodyEntered += OnRideDetectorBodyEntered;
        }
    }

    public override void _PhysicsProcess(double delta){
        float dt = (float)delta;

        // 1. 落下中の処理
        if (_isFalling){
            _fallVelocity.Y += _gravity * dt;
            GlobalPosition += _fallVelocity * dt;

            _destroyTimer -= dt;
            if (_destroyTimer <= 0.0f){
                QueueFree(); // 3秒後に安全に消滅（プレイヤーは道連れになりません）
            }
            return;
        }

        // 2. AutoMoveモード（最初から常に横移動）
        if (Type == PlatformType.AutoMove){
            GlobalPosition += new Vector2(MoveSpeedX * dt, 0);
            return;
        }

        // 3. 乗られて起動した後の処理（SimpleFall / TimedMoveAndFall）
        if (!_isTriggered) return;

        _timer -= dt;

        if (Type == PlatformType.TimedMoveAndFall && _timer > 0.0f){
            GlobalPosition += new Vector2(MoveSpeedX * dt, 0);
        }

        if (_timer <= 0.0f){
            StartFalling();
        }
    }

    private void StartFalling(){
        _isFalling = true;
        _destroyTimer = DestroyDelay;
        // 落下開始時に少し下向きの初速を与え、横移動していた場合はその慣性も残す
        float currentVelX = (Type == PlatformType.TimedMoveAndFall) ? MoveSpeedX : 0.0f;
        _fallVelocity = new Vector2(currentVelX, 60.0f);
    }

    private void OnRideDetectorBodyEntered(Node2D body){
        if (Type == PlatformType.AutoMove || _isTriggered || _isFalling) return;

        if (body is Player player){
            // 下からすり抜け上昇中ではなく、上から着地した時のみ起動
            if (player.Velocity.Y >= -10.0f && player.GlobalPosition.Y <= GlobalPosition.Y){
                _isTriggered = true;
            }
        }
    }
}