using Godot;
using System;

public partial class HUD : CanvasLayer{
    private TextureProgressBar _hpBar;
    private Label _hpLabel;

    public override void _Ready(){
        _hpBar = GetNode<TextureProgressBar>("Margin/TextureProgressBar");
        // ▼ ノード階層に合わせてパスを修正してください（TextureRect2の子なら "Margin/TextureRect2/Label"）
        _hpLabel = GetNode<Label>("Margin/TextureRect2/Label");

        var player = GetTree().GetFirstNodeInGroup("Player") as Player;
        if (player != null){
            // ▼ 変更点1：ゲーム開始直後に、HUDが自らプレイヤーの数値を引っ張ってきてUIに反映させる！
            OnHealthChanged(player.CurrentHealth, player.MaxHealth);

            // ダメージ時用にシグナルを接続
            player.HealthChanged += OnHealthChanged;

            // 成功したか出力タブで確認するためのログ
            GD.Print("HUD: プレイヤーとの接続に成功しました！");
        }else{
            GD.PrintErr("HUDエラー: 'Player'グループを持つノードが見つかりません！");
        }
    }

    // ▼ プレイヤーから通知を受け取ってUIを書き換える処理
    private void OnHealthChanged(int currentHp, int maxHp){
        _hpBar.MaxValue = maxHp;
        _hpBar.Value = currentHp;

        // Labelのテキストを現在のHPの数字に書き換える
        _hpLabel.Text = currentHp.ToString();
    }
}