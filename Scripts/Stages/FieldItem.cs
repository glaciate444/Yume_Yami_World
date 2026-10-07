using Godot;
using System;

public partial class FieldItem : Area2D{
    public enum ItemType{
        HealHp,
        HealSp,
        Coin
    }

    [Export] public ItemType Type = ItemType.HealHp;
    [Export] public int Amount = 1;

    // ▼ 追加：アイテムの画像をインスペクターから設定できるようにする
    [Export] public Texture2D ItemIcon;

    public override void _Ready(){
        BodyEntered += OnBodyEntered;

        // ▼ 追加：ゲーム開始時に、設定された画像に切り替える
        if (ItemIcon != null){
            GetNode<Sprite2D>("Sprite2D").Texture = ItemIcon;
        }
    }

    private void OnBodyEntered(Node2D body){
        if (body is Player player){
            switch (Type){
                case ItemType.HealHp:
                    player.RecoverHealth(Amount);
                    break;
                case ItemType.HealSp:
                    player.RecoverSp(Amount);
                    break;
                case ItemType.Coin:
                    player.AddCoin(Amount);
                    break;
            }
            QueueFree(); // 取得したら消滅
        }
    }
}