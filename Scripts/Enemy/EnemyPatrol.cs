using Godot;
using System;

public partial class EnemyPatrol : EnemyMovement {
    [Export] public float MoveSpeed = 100.0f;
    [Export] public bool MovingRight = false;
    [Export] public bool ReverseSprite = false;

    [Export] private CharacterBody2D _enemyBody;
    [Export] private AnimatedSprite2D _animatedSprite;
    [Export] private RayCast2D _wallCheck;
    [Export] private RayCast2D _edgeCheck;

    private float _flipCooldown = 0.0f;
    private float _wallTargetX = 30.0f;
    private float _edgeTargetX = 30.0f;
    private float _edgePosX = 0.0f;

    public override void _Ready() {
        if (_enemyBody == null) _enemyBody = GetParentOrNull<CharacterBody2D>();
        var graphics = _enemyBody?.GetNodeOrNull<Node2D>("Graphics");
        if (_animatedSprite == null && graphics != null) _animatedSprite = graphics.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (_wallCheck == null && graphics != null) _wallCheck = graphics.GetNodeOrNull<RayCast2D>("WallCheck");
        if (_edgeCheck == null && graphics != null) _edgeCheck = graphics.GetNodeOrNull<RayCast2D>("EdgeCheck");

        if (_wallCheck != null) _wallTargetX = Mathf.Abs(_wallCheck.TargetPosition.X);
        if (_edgeCheck != null) {
            _edgeTargetX = Mathf.Abs(_edgeCheck.TargetPosition.X);
            _edgePosX = Mathf.Abs(_edgeCheck.Position.X);
        }

        UpdateDirection();
    }

    public override void _PhysicsProcess(double delta) {
        if (IsPaused || _enemyBody == null) return;

        if (_flipCooldown > 0.0f) {
            _flipCooldown -= (float)delta;
        }

        float currentSpeed = MovingRight ? MoveSpeed : -MoveSpeed;
        _enemyBody.Velocity = new Vector2(currentSpeed, _enemyBody.Velocity.Y);

        UpdateSpriteFlip();

        if (_flipCooldown <= 0.0f) {
            bool isHittingWall = false;
            if (_wallCheck != null && _wallCheck.IsColliding()) {
                if (_wallCheck.GetCollider() is not Player) {
                    isHittingWall = true;
                }
            }

            bool isGroundAhead = _edgeCheck == null || _edgeCheck.IsColliding();

            if (isHittingWall || (_enemyBody.IsOnFloor() && !isGroundAhead)) {
                Flip();
            }
        }
    }

    private void Flip() {
        MovingRight = !MovingRight;
        _flipCooldown = 0.15f;
        UpdateDirection();
    }

    private void UpdateSpriteFlip() {
        if (_animatedSprite != null) {
            bool flip = !MovingRight;
            if (ReverseSprite) flip = !flip;
            _animatedSprite.FlipH = flip;
        }
    }

    private void UpdateDirection() {
        UpdateSpriteFlip();

        float dir = MovingRight ? 1.0f : -1.0f;

        if (_wallCheck != null) {
            _wallCheck.TargetPosition = new Vector2(_wallTargetX * dir, _wallCheck.TargetPosition.Y);
            _wallCheck.ForceRaycastUpdate();
        }
        if (_edgeCheck != null) {
            _edgeCheck.Position = new Vector2(_edgePosX * dir, _edgeCheck.Position.Y);
            _edgeCheck.TargetPosition = new Vector2(_edgeTargetX * dir, _edgeCheck.TargetPosition.Y);
            _edgeCheck.ForceRaycastUpdate();
        }
    }
}