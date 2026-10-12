using Godot;
using System;

[Tool] // エディタ上でも画像を即座に反映させるため
public partial class FieldItem : Area2D {
    // 効果の種類だけを定義（名前は汎用的に）
    public enum ItemEffect { HealHp, HealSp, AddCoin }

    [Export] public ItemEffect Effect = ItemEffect.HealHp;
    [Export] public int Amount = 1;

    private Texture2D _itemTexture;

    // ▼ ここにインスペクターから画像をドラッグ＆ドロップできるようにする
    [Export]
    public Texture2D ItemTexture {
        get => _itemTexture;
        set {
            _itemTexture = value;
            UpdateIcon(); // インスペクターで画像をセットした瞬間に呼ばれる
        }
    }

    public override void _Ready() {
        // エディタ上ではなく、実際のゲームプレイ時のみ当たり判定をオンにする
        if (!Engine.IsEditorHint()) {
            BodyEntered += OnBodyEntered;
        }
    }

    private void UpdateIcon() {
        Sprite2D sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null) {
            sprite.Texture = _itemTexture;
        }
    }

    private void OnBodyEntered(Node2D body) {
        if (body is Player player) {
            // 効果に応じてプレイヤーのメソッドを呼ぶ
            if (Effect == ItemEffect.HealHp) {
                player.RecoverHealth(Amount);
            } else if (Effect == ItemEffect.HealSp) {
                player.RecoverSp(Amount);
            } else if (Effect == ItemEffect.AddCoin) {
                player.AddCoin(Amount);
            }

            QueueFree(); // 取得したら消滅させる
        }
    }
}