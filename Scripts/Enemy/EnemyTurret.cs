using Godot;
using System;

public partial class EnemyTurret : Node2D {
    public enum AimType {
        Forward,
        AimAtPlayer,
        RandomDirection,
        Up,
        Down,
        ParabolaForward,
        ParabolaAtPlayer
    }

    [ExportCategory("Basic Settings")]
    [Export] public int TurretID = 0;
    [Export] public bool AutoFire = true;   // 魔法使いの砲台はオフにする
    [Export] public PackedScene EnemyBulletPrefab;
    [Export] public float FireInterval = 2.0f;
    [Export] public bool NotRotateAngle = false;

    [ExportCategory("① Way Settings")]
    [Export] public AimType CurrentAimType = AimType.AimAtPlayer;
    [Export(PropertyHint.Range, "1,10,1")] public int BulletCount = 1; // 同時発射数
    [Export] public float SpreadAngle = 15.0f;                         // 弾の広がり角度

    [ExportCategory("② Burst Settings")]
    [Export(PropertyHint.Range, "1,10,1")] public int BurstCount = 1;  // 連射数
    [Export] public float BurstInterval = 0.2f;                        // 連射間隔

    [ExportCategory("Parabola & Randomness Settings")]
    [Export] public float ParabolaUpForce = 1.0f;
    [Export] public float AngleRandomness = 0.0f;
    [Export] public Vector2 PositionOffset = Vector2.Zero;
    [Export] public Vector2 PositionRandomness = Vector2.Zero;
    [Export] public Vector2 TargetOffset = new Vector2(0f, -20f); // プレイヤーの足元を狙わないための補正

    private Node2D _player;
    private double _timer = 0.0;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready() {
        _rng.Randomize();
        FindPlayer();
    }

    public override void _Process(double delta) {
        if (!AutoFire) return;
        _timer += delta;
        if (_timer >= FireInterval) {
            _timer = 0.0;
            Shoot();
        }
    }

    // Find系メソッドを毎フレーム呼ばないよう_Readyで一度取得しキャッシュする
    private void FindPlayer() {
        var players = GetTree().GetNodesInGroup("Player");
        if (players.Count > 0 && players[0] is Node2D p) {
            _player = p;
        }
    }

    public void Shoot() {
        ShootRoutine();
    }

    public void ShootByID(int id) {
        if (TurretID == id) {
            ShootRoutine();
        }
    }

    // UnityのIEnumerator（コルーチン）をGodotの非同期メソッドに変換
    private async void ShootRoutine() {
        if (EnemyBulletPrefab == null) return;

        // 親ノード（敵本体）を弾の発射元（誤爆防止用）として取得
        Node2D shooter = GetParent() as Node2D;

        for (int b = 0; b < BurstCount; b++) {
            // グローバルスケールから現在の向きを取得
            float facingDirection = Mathf.Sign(GlobalScale.X);

            Vector2 baseDir = Vector2.Right;
            if (CurrentAimType == AimType.AimAtPlayer && _player != null) {
                Vector2 targetPos = _player.GlobalPosition + TargetOffset;
                baseDir = (targetPos - GlobalPosition).Normalized();
            } else if (CurrentAimType == AimType.Forward) {
                baseDir = new Vector2(facingDirection, 0).Normalized();
            } else if (CurrentAimType == AimType.RandomDirection) {
                float randomAngle = _rng.RandfRange(0f, Mathf.Pi * 2);
                baseDir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            } else if (CurrentAimType == AimType.Up) {
                baseDir = Vector2.Up;
            } else if (CurrentAimType == AimType.Down) {
                baseDir = Vector2.Down;
            } else if (CurrentAimType == AimType.ParabolaForward) {
                baseDir = new Vector2(facingDirection, -ParabolaUpForce).Normalized(); // GodotはY軸下向きがプラス
            } else if (CurrentAimType == AimType.ParabolaAtPlayer && _player != null) {
                float dirX = Mathf.Sign(_player.GlobalPosition.X - GlobalPosition.X);
                baseDir = new Vector2(dirX, -ParabolaUpForce).Normalized();
            }

            for (int i = 0; i < BulletCount; i++) {
                float offsetAngle = 0f;
                if (BulletCount > 1) {
                    offsetAngle = (i - (BulletCount - 1) / 2.0f) * SpreadAngle;
                }
                offsetAngle += _rng.RandfRange(-AngleRandomness, AngleRandomness);

                // Godotの組み込みRotatedを利用（ラジアン変換）
                Vector2 finalDir = baseDir.Rotated(Mathf.DegToRad(offsetAngle));

                Vector2 randomPosOffset = new Vector2(
                    _rng.RandfRange(-PositionRandomness.X, PositionRandomness.X),
                    _rng.RandfRange(-PositionRandomness.Y, PositionRandomness.Y)
                );

                Vector2 finalPos = GlobalPosition
                                 + new Vector2(PositionOffset.X * facingDirection, PositionOffset.Y)
                                 + randomPosOffset;

                // 弾の生成と発射
                var bullet = EnemyBulletPrefab.Instantiate<Bullet>();
                bullet.GlobalPosition = finalPos;

                if (!NotRotateAngle) {
                    bullet.Rotation = finalDir.Angle();
                }

                // isPlayerBullet: false として共通の Bullet.cs を初期化。親ノードを渡して誤爆防止。
                bullet.Initialize(finalDir, isPlayerBullet: false, shooter: shooter);
                bullet.IsIceAttack = false; // 必要に応じて敵の属性を設定

                // メインシーンへ追加
                GetTree().CurrentScene.AddChild(bullet);
            }

            // バースト間隔の待機（Unityの yield return new WaitForSeconds に相当）
            if (BurstCount > 1 && BurstInterval > 0f) {
                await ToSignal(GetTree().CreateTimer(BurstInterval), SceneTreeTimer.SignalName.Timeout);
            }
        }
    }
}