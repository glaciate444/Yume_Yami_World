using Godot;
using System;

public partial class Player : CharacterBody2D{
	[Export] public float Speed = 300.0f;
	[Export] public float JumpVelocity = -400.0f;

	// ▼ ダッシュ用の変数を追加
	[Export] public float DashSpeed = 800.0f;
	[Export] public float DashDuration = 0.4f;

	private bool _isDashing = false;
	private float _dashTimer = 0.0f;
	private float _facingDirection = 1.0f; // 1(右) か -1(左)

	private bool _isAttacking = false;

	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	// アニメーション操作用の変数を追加
	private AnimatedSprite2D _animatedSprite;
	// プレイヤーの変数宣言部分に追加
	private AnimationPlayer _animationPlayer;
    // まとめて反転させるためのノード
    private Node2D _graphics; 

    public override void _Ready(){
        // 階層が変わったのでパスを修正して取得
        _graphics = GetNode<Node2D>("Graphics");
        _animatedSprite = GetNode<AnimatedSprite2D>("Graphics/AnimatedSprite2D");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // ▼ 鞭の当たり判定を取得し、「何かに触れた時」のシグナル（イベント）を登録する
        Area2D whipHitbox = GetNode<Area2D>("Graphics/TipPoint/WhipHitbox");
        whipHitbox.BodyEntered += OnWhipHitboxBodyEntered;
    }

	public override void _PhysicsProcess(double delta){
		Vector2 velocity = Velocity;

		Vector2 direction = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");
		if (direction.X != 0){
			_facingDirection = Mathf.Sign(direction.X);
		}

		// ▼ ダッシュの開始処理
		if (Input.IsActionJustPressed("dash") && !_isDashing){
			_isDashing = true;
			_dashTimer = DashDuration;
		}

		// ▼ 状態ごとの移動処理
		if (_isDashing){
			// ダッシュ中：重力を無視してX軸に高速移動
			_dashTimer -= (float)delta;
			velocity.Y = 0; // 重力落下をキャンセル[cite: 16]
			velocity.X = _facingDirection * DashSpeed;

			if (_dashTimer <= 0){
				_isDashing = false;
			}
		}else{
			// 通常時：重力の適用
			if (!IsOnFloor()){
				velocity.Y += gravity * (float)delta;
			}

			// 通常時：ジャンプ処理
			if (Input.IsActionJustPressed("jump") && IsOnFloor()){
				velocity.Y = JumpVelocity;
			}

			// 通常時：左右の移動処理
			if (direction != Vector2.Zero){
				velocity.X = direction.X * Speed;
			}else{
				velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			}
		}

		Velocity = velocity;
		MoveAndSlide();

		// ダッシュ中以外でアニメーションを更新
		// ▼ 変更後（攻撃中もUpdateAnimationを呼ばないようにする）
		if (!_isDashing && !_isAttacking){
			UpdateAnimation(direction.X);
		}

		if (Input.IsActionJustPressed("attack") && !_isAttacking){
			StartAttack();
		}
	}

// アニメーション切り替えと左右反転のメソッド
	private void UpdateAnimation(float directionX){
        // ▼ 変更箇所：FlipH ではなく、Graphics全体のスケールを反転させる！
        // これにより、手元（HandPoint）や当たり判定（TipPoint）も一緒に反対側へ移動します。
        if (directionX != 0){
            _graphics.Scale = new Vector2(directionX < 0 ? -1 : 1, 1);
        }

        // 次に再生すべきアニメーションの名前を決定する
        string nextAnim = "";

        if (IsOnFloor()){
			if (directionX == 0){
				nextAnim = "idle"; // 止まっている時
			}else{
				nextAnim = "walk"; // 歩いている時
			}
		}else{
			if (Velocity.Y < 0){
				nextAnim = "jump"; // 上昇中
			}else{
				nextAnim = "fall"; // 下降中
			}
		}

		// 【重要】現在設定されているアニメーションと違う場合だけPlayを呼ぶ
		if (_animatedSprite.Animation != nextAnim){
			_animatedSprite.Play(nextAnim);
		}
	}

	private void StartAttack(){
		_isAttacking = true;

		// AnimatedSprite2DのPlayではなく、AnimationPlayerを再生する
		_animationPlayer.Play("attack_whip");

		// Unityの「Invoke」の代わりに、Timerを使って一定時間後に攻撃状態を解除
		GetTree().CreateTimer(0.6f).Timeout += ResetAttackState;
	}

	private void ResetAttackState(){
		_isAttacking = false;
	}

    // ▼ 新規追加：鞭の当たり判定に何かのボディ（箱や敵など）が重なった時に自動で呼ばれる処理
    private void OnWhipHitboxBodyEntered(Node2D body){
        // 1. 何かに触れたら絶対に出力する
        GD.Print($"ムチが {body.Name} に当たりました！");

        // 触れた相手が IDamageable インターフェースを持っているか（壊せる箱や敵か）をチェック
        if (body is IDamageable damageable){
            // 相手を吹き飛ばす方向（ノックバック）を計算
            // Graphics.Scale.X を見ることで、右向きなら 1、左向きなら -1 の方向になります
            Vector2 knockback = new Vector2(_graphics.Scale.X, 0);

            // Unity時代と同じメソッドを呼び出し、ダメージ1を与える！
            damageable.TakeDamage(1, knockback);
        }
    }
}
