using Godot;
using System;

public partial class Spring : Area2D{
    //[Header("跳ね返る力")]
    [Export] public float BounceForce = 900.0f; // 通常ジャンプ(500)より大きい値

    //[Header("グラフィック設定")]
    [Export] private Sprite2D _sprite;          // インスペクターからSprite2Dを割り当て
    [Export] public Texture2D ActiveTexture;    // 踏まれた瞬間の画像
    [Export] public float ResetTime = 0.2f;     // 元に戻すまでの秒数

    private Texture2D _defaultTexture;
    private float _resetTimer = 0.0f;

    public override void _Ready(){
        if (_sprite != null){
            _defaultTexture = _sprite.Texture;
        }
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta){
        // 画像が切り替わっている間だけタイマーを減らし、0になったら元の画像に戻す
        if (_resetTimer > 0.0f){
            _resetTimer -= (float)delta;
            if (_resetTimer <= 0.0f && _sprite != null){
                _sprite.Texture = _defaultTexture;
            }
        }
    }

    private void OnBodyEntered(Node2D body){
        if (body is Player player){
            // ▼ 横からの誤爆防止：
            // 「プレイヤーがバネの中心より上にいる」または「落下中(Velocity.Y >= 0)」の時だけ発動
            if (player.GlobalPosition.Y <= GlobalPosition.Y && player.Velocity.Y >= -10.0f){
                player.Bounce(BounceForce);

                // 画像を切り替えてタイマーをセット
                if (_sprite != null && ActiveTexture != null){
                    _sprite.Texture = ActiveTexture;
                    _resetTimer = ResetTime;
                }
            }
        }
    }
}