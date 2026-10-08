using Godot;

// 敵や箱など、ダメージを受けるすべてのオブジェクトに実装するインターフェース
public interface IDamageable{
    void TakeDamage(int damage, Vector2 knockback, bool isIceAttack = false);
}