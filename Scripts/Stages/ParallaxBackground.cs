using Godot;
using System;

public partial class ParallaxBackground : Node {
    // ▼ 変数の宣言を追加します
    [Export] public float AutoScrollSpeed = 50.0f;
    private Vector2 ScrollOffset;

    public override void _Process(double delta) {
        ScrollOffset += new Vector2(AutoScrollSpeed * (float)delta, 0);
    }
}