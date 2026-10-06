using Godot;
using System;

public partial class Player : CharacterBody2D, IDamageable{
    // ▼ UIへ数値を送るためのシグナル（Unityの HUDManager.Instance.Update... の代わり）
    [Signal] public delegate void HealthChangedEventHandler(int currentHp, int maxHp);
    [Signal] public delegate void SpChangedEventHandler(int currentSp, int maxSp);

    //[Header("HP設定")]
    [Export] public int MaxHealth = 12;
    public int CurrentHealth;

    //[Header("SP設定")]
    [Export] public int MaxSp = 6;
    public int CurrentSp;


    [Export] public float Speed = 300.0f;
	[Export] public float JumpVelocity = -500.0f;

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

    // ノックバック用の変数
    private bool _isKnockback = false;
    private float _knockbackDuration = 0.2f; // 操作不能になる時間

    // ▼ 新規追加：無敵用の変数
    private bool _isInvincible = false;
    [Export] public float InvincibilityDuration = 1.0f; // 無敵時間（秒）

    public override void _Ready(){
        // 階層が変わったのでパスを修正して取得
        _graphics = GetNode<Node2D>("Graphics");
        _animatedSprite = GetNode<AnimatedSprite2D>("Graphics/AnimatedSprite2D");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // ▼ 鞭の当たり判定を取得し、「何かに触れた時」のシグナル（イベント）を登録する
        Area2D whipHitbox = GetNode<Area2D>("Graphics/TipPoint/WhipHitbox");
        whipHitbox.BodyEntered += OnWhipHitboxBodyEntered;

        // ゲーム開始時にステータスを最大値にして、UIに送信
        CurrentHealth = MaxHealth;
        CurrentSp = MaxSp;

        // 確実なタイミングでUIを初期化するため CallDeferred を使う
        CallDeferred(nameof(EmitHealthChanged));
        CallDeferred(nameof(EmitSpChanged));
    }
    // シグナル送信用の補助メソッド
    private void EmitHealthChanged() => EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    private void EmitSpChanged() => EmitSignal(SignalName.SpChanged, CurrentSp, MaxSp);
    
    public override void _PhysicsProcess(double delta){
		Vector2 velocity = Velocity;

        // ▼ 修正1：if文の外で direction をあらかじめ準備（宣言）しておく
        Vector2 direction = Vector2.Zero;

        // 1. 重力はノックバック中も常に計算する（空中に弾き飛ばされて落ちてくるため）
        if (!IsOnFloor()){
            velocity += GetGravity() * (float)delta;
        }

        // 2. ノックバック「ではない」時だけ、通常のキー操作を受け付ける
        if (!_isKnockback){
            direction = Input.GetVector("move_left", "move_right", "ui_up", "ui_down");
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
        }
		Velocity = velocity;
		MoveAndSlide();

        // ダッシュ中以外でアニメーションを更新
        // ▼ 変更後（攻撃中もUpdateAnimationを呼ばないようにする）
        // ダッシュ中・攻撃中・ノックバック中 以外でアニメーションを更新
        if (!_isDashing && !_isAttacking && !_isKnockback){
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
        if (body == this) return; // 自分自身は無視
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
    // ==========================================
    // ▼ HPの処理 (元 PlayerHealth.cs の役割)
    // ==========================================
    public async void TakeDamage(int damage, Vector2 knockbackDirection, bool isIceAttack = false){
        if (_isInvincible) return; // 無敵中は処理しない

        _isKnockback = true;
        _isInvincible = true;

        // ダメージ計算（0未満にならないようにする）
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        // ★HPが減ったのでUIにシグナルを送る
        EmitHealthChanged();

        if (CurrentHealth <= 0){
            GD.Print("プレイヤー死亡処理"); // 元の Die() 相当
        }

        _animatedSprite.Play("knockback");

        // ▼ 私が消してしまっていた物理ノックバック処理を復活！
        float knockbackForceX = knockbackDirection.X * 300f;
        float knockbackForceY = -300f;
        Velocity = new Vector2(knockbackForceX, knockbackForceY);

        DamageEffect(); // 1秒間の点滅エフェクト

        // ノックバック終了待ち
        await ToSignal(GetTree().CreateTimer(_knockbackDuration), SceneTreeTimer.SignalName.Timeout);
        _isKnockback = false;
    }
    // ▼ 新規追加：無敵時間の点滅エフェクト
    private async void DamageEffect(){
        // 0.2秒を1回の点滅として計算
        int blinkCount = Mathf.RoundToInt(InvincibilityDuration / 0.2f);

        for (int i = 0; i < blinkCount; i++){
            // 透明にする (Alpha = 0)
            _animatedSprite.Modulate = new Color(1, 1, 1, 0);
            await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);

            // 不透明に戻す (Alpha = 1)
            _animatedSprite.Modulate = new Color(1, 1, 1, 1);
            await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
        }

        // 念のため確実に不透明に戻す
        _animatedSprite.Modulate = new Color(1, 1, 1, 1);
        _isInvincible = false; // 無敵終了
    }
    // ==========================================
    // ▼ SPの処理 (元 PlayerShoot.cs の役割)
    // ==========================================
    private void Shoot(){
        int cost = 1; // 本来は currentSpecialEquip.spCost[cite: 19]

        if (CurrentSp >= cost){
            CurrentSp -= cost; // SPを消費[cite: 19]
            EmitSpChanged();   // ★SPが減ったのでUIにシグナルを送る

            // 弾の発射処理など...[cite: 19]
            GD.Print("弾を発射しました！");
            _animationPlayer.Play("attack_whip"); // 仮のアニメーション
        }
    }

    public void RecoverSp(int amount){
        CurrentSp += amount;
        CurrentSp = Mathf.Clamp(CurrentSp, 0, MaxSp); // 最大値を超えないように制限[cite: 19]
        EmitSpChanged(); // ★SPが回復したのでUIにシグナルを送る
    }
}
