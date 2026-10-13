using Godot;
using System;

[Tool] // エディタ上でも画像を即座に反映させるため
public partial class FieldItem : Area2D {
    public enum ItemEffect { HealHp, HealSp, AddCoin }

    [Export] public ItemEffect Effect = ItemEffect.HealHp;
    [Export] public int Amount = 1;

    private Texture2D _itemTexture;
    [Export]
    public Texture2D ItemTexture {
        get => _itemTexture;
        set {
            _itemTexture = value;
            UpdateIcon();
        }
    }

    // 2重取得防止用のフラグ
    private bool _isCollected = false;

    public override void _Ready() {
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
        if (_isCollected) return;

        if (body is Player player) {
            _isCollected = true;
            SetDeferred(Area2D.PropertyName.Monitoring, false); // 取得後は検知を止める

            if (Effect == ItemEffect.HealHp) player.RecoverHealth(Amount);
            else if (Effect == ItemEffect.HealSp) player.RecoverSp(Amount);
            else if (Effect == ItemEffect.AddCoin) player.AddCoin(Amount);

            // 祖先をたどって BouncingItem を探し、見つからなければ自分だけ消す
            Node target = this;
            for (Node n = GetParent(); n != null; n = n.GetParent()) {
                if (n is BouncingItem) { target = n; break; }
            }
            target.QueueFree();
        }
    }
}