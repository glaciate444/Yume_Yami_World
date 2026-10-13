using Godot;
using System;

// 足場として上に乗れるようにするため、StaticBody2Dを継承します
public partial class BreakableBlock : StaticBody2D, IDamageable {
    [ExportGroup("耐久度・破壊設定")]
    [Export] public bool IsIndestructible = false; // 鉄の箱フラグ
    [Export] public bool CanBreakByHazard = false; // 大玉ギミックフラグ[cite: 24]

    [ExportGroup("ドロップ＆エフェクト")]
    [Export] public PackedScene DropItemPrefab; // GameObjectの代わり[cite: 24]
    [Export] public PackedScene BreakParticlePrefab;

    public void TakeDamage(int damage, Vector2 knockback, bool isIceAttack = false) {
        // 1. 壊れない設定（鉄の箱）の時の判定[cite: 24]
        if (IsIndestructible) {
            if (CanBreakByHazard && damage >= 9999) {
                // ガードを突破して下の破壊処理へ進む[cite: 24]
            } else {
                return; // 通常の攻撃ならここで処理を止める[cite: 24]
            }
        }

        // 2. パーティクルを生成
        if (BreakParticlePrefab != null) {
            Node2D particle = BreakParticlePrefab.Instantiate<Node2D>();

            // ▼ AddChildする前に、先に座標を箱と同じ位置にする[cite: 24]
            particle.GlobalPosition = GlobalPosition;
            GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, particle);

            // ▼ その後にシーンに追加する（正しい位置で_Readyが実行され、爆発する）[cite: 24]
            GetTree().CurrentScene.AddChild(particle);
        }

        // 3. アイテムをドロップ[cite: 24]
        if (DropItemPrefab != null) {
            // UnityのRandom.Range(int, int)は最大値を含まない(1〜5)ため、GD.RandRangeで調整[cite: 24]
            int randomInt = GD.RandRange(1, 5);
            for (int i = 0; i < randomInt; i++) {
                Node2D drop = DropItemPrefab.Instantiate<Node2D>();

                // ピクセル単位になるため、オフセットの数値を20前後に拡大[cite: 24]
                float offsetX = (float)GD.RandRange(-20.0, 20.0);
                float offsetY = (float)GD.RandRange(-20.0, 0.0); // 上方向にばらけさせる[cite: 24]

                drop.GlobalPosition = GlobalPosition + new Vector2(offsetX, offsetY);
                
                // ▼ 統合部分：もし生成したアイテムが BouncingItem（バウンドするアイテム）なら、上向きの初速を与える
                if (drop is BouncingItem bounceItem) {
                    float randomVelocityX = (float)GD.RandRange(-150.0, 150.0);
                    float randomVelocityY = (float)GD.RandRange(-250.0, -400.0);
                    bounceItem.Velocity = new Vector2(randomVelocityX, randomVelocityY);
                }

                GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, drop);
            }
        }

        // 4. 自分自身を消去（UnityのDestroyに相当）[cite: 24]
        QueueFree();
    }
}