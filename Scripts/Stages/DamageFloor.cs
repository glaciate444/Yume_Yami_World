using Godot;

public partial class DamageFloor : Area2D{
    [Export] public int DamageAmount = 1;

    // シグナルから呼び出されるメソッド
    private void OnBodyEntered(Node2D body){
        if (body is IDamageable damageable){
            // プレイヤーがトゲの中心より「右」にいれば 1、「左」にいれば -1 を計算
            float pushDirection = Mathf.Sign(body.GlobalPosition.X - GlobalPosition.X);

            // 弾き飛ばす方向を決定（Y軸の跳ね上がりはPlayer側で処理済み）
            Vector2 knockback = new Vector2(pushDirection, 0);

            // 相手（プレイヤー）の TakeDamage を呼び出す！
            damageable.TakeDamage(DamageAmount, knockback);
        }
    }
}