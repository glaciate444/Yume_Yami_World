using Godot;
using System;

public partial class HUD : CanvasLayer {
    // ▼ プレイヤーをインスペクターからアタッチする（Find型を廃止）
    [Export] private Player _player;

    [Export] private TextureProgressBar _hpBar;
    [Export] private Label _hpLabel;

    [Export] private TextureProgressBar _spBar;
    [Export] private Label _spLabel;

    [Export] private HBoxContainer _apContainer;
    [Export] private Texture2D _apOnIcon;
    [Export] private Texture2D _apOffIcon;

    // ▼ 追加：コイン用のテキスト（Label）
    [Export] private Label _coinLabel;

    public override void _Ready() {
        // GetNodeによる文字パス指定を削除！
        // インスペクターから直接 _player が割り当てられているか確認します
        if (_player != null) {
            OnHealthChanged(_player.CurrentHealth, _player.MaxHealth);
            _player.HealthChanged += OnHealthChanged;

            OnSpChanged(_player.CurrentSp, _player.MaxSp);
            _player.SpChanged += OnSpChanged;

            OnApChanged(_player.CurrentAp, _player.MaxAp);
            _player.ApChanged += OnApChanged;

            // ▼ 追加：コインの初期化とシグナル接続
            OnCoinChanged(_player.Coins);
            _player.CoinChanged += OnCoinChanged;

            GD.Print("HUD: HP、SP、AP、コインの初期化を完了しました！");
        } else {
            GD.PrintErr("HUDエラー: インスペクターに 'Player' が割り当てられていません！");
        }
    }

    private void OnHealthChanged(int currentHp, int maxHp) {
        if (_hpBar != null) _hpBar.MaxValue = maxHp;
        if (_hpBar != null) _hpBar.Value = currentHp;
        if (_hpLabel != null) _hpLabel.Text = currentHp.ToString();
    }

    private void OnSpChanged(int currentSp, int maxSp) {
        if (_spBar != null) _spBar.MaxValue = maxSp;
        if (_spBar != null) _spBar.Value = currentSp;
        if (_spLabel != null) _spLabel.Text = currentSp.ToString();
    }

    private void OnApChanged(int currentAp, int maxAp) {
        if (_apContainer == null) return;

        for (int i = 0; i < _apContainer.GetChildCount(); i++) {
            var icon = _apContainer.GetChild<TextureRect>(i);
            if (i < currentAp) {
                icon.Texture = _apOnIcon;
            } else {
                icon.Texture = _apOffIcon;
            }
        }
    }

    // ▼ 追加：コインが変動した時に呼ばれる処理
    private void OnCoinChanged(int currentCoins) {
        if (_coinLabel != null) {
            // "00", "01", "100" "999" のように常に3桁で表示する
            _coinLabel.Text = currentCoins.ToString("D3");
        }
    }
}