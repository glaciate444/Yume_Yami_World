using Godot;
using System;

public partial class HUD : CanvasLayer{
    // ▼ [Export] をつけると、Unityのようにインスペクターから直接ノードを割り当てられます
    [Export] private TextureProgressBar _hpBar;
    [Export] private Label _hpLabel;

    [Export] private TextureProgressBar _spBar;
    [Export] private Label _spLabel;

    [Export] private HBoxContainer _apContainer; // アイコンをまとめているコンテナ
    [Export] private Texture2D _apOnIcon;        // ON状態の画像
    [Export] private Texture2D _apOffIcon;       // OFF状態の空っぽの画像
    public override void _Ready(){
        // GetNodeによる文字パス指定は削除しました！
        // これで名前や階層が変わってもサイレントクラッシュしません。

        var player = GetNodeOrNull<Player>("../Player");
        if (player != null){
            OnHealthChanged(player.CurrentHealth, player.MaxHealth);
            player.HealthChanged += OnHealthChanged;

            OnSpChanged(player.CurrentSp, player.MaxSp);
            player.SpChanged += OnSpChanged;

            // ▼ 追加：APの初期化とシグナル接続
            OnApChanged(player.CurrentAp, player.MaxAp);
            player.ApChanged += OnApChanged;

            GD.Print("HUD: HPとSPの初期化を通過しました！");
        }else{
            GD.PrintErr("HUDエラー: 'Player'ノードが見つかりません！");
        }
    }

    private void OnHealthChanged(int currentHp, int maxHp){
        _hpBar.MaxValue = maxHp;
        _hpBar.Value = currentHp;
        _hpLabel.Text = currentHp.ToString();
    }

    private void OnSpChanged(int currentSp, int maxSp){
        _spBar.MaxValue = maxSp;
        _spBar.Value = currentSp;
        _spLabel.Text = currentSp.ToString();
    }
    // ▼ 追加：APが変動した時に呼ばれる処理
    private void OnApChanged(int currentAp, int maxAp){
        if (_apContainer == null) return;

        // コンテナの中にある3つのアイコン（TextureRect）を順番にチェックする
        for (int i = 0; i < _apContainer.GetChildCount(); i++){
            // 子ノードを取得
            var icon = _apContainer.GetChild<TextureRect>(i);

            // i番目のアイコンが現在のAPより小さければON画像、それ以上ならOFF画像にする
            // (例: APが2なら、i=0とi=1はON、i=2はOFFになる)
            if (i < currentAp){
                icon.Texture = _apOnIcon;
            }else{
                icon.Texture = _apOffIcon;
            }
        }
    }
}