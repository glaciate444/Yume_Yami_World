using Godot;
using System;

public partial class SoundManager : Node {
    public static SoundManager Instance { get; private set; }

    private AudioStreamPlayer _bgmPlayer;
    private AudioStreamPlayer[] _sePlayers;
    private int _sePlayerIndex = 0;

    public override void _Ready() {
        if (Instance == null) {
            Instance = this;

            // ▼ BGM用の再生機を1つ自動生成する
            _bgmPlayer = new AudioStreamPlayer();
            AddChild(_bgmPlayer);

            // ▼ SE用の再生機を自動生成する（音が重なっても途切れないように5つ用意）
            _sePlayers = new AudioStreamPlayer[5];
            for (int i = 0; i < _sePlayers.Length; i++) {
                _sePlayers[i] = new AudioStreamPlayer();
                AddChild(_sePlayers[i]);
            }
        } else {
            QueueFree();
        }
    }

    // BGMの再生（ファイルパスから読み込む）
    public void PlayBGM(string path) {
        if (string.IsNullOrEmpty(path)) return;

        // 同じBGMがすでに流れている場合は、最初から再生し直さない
        if (_bgmPlayer.Stream != null && _bgmPlayer.Stream.ResourcePath == path && _bgmPlayer.Playing) return;

        AudioStream bgm = GD.Load<AudioStream>(path);
        if (bgm != null) {
            _bgmPlayer.Stream = bgm;
            _bgmPlayer.Play();
        }
    }

    // SEの再生（インスペクターでセットしたオーディオファイルを直接渡す）
    public void PlaySE(AudioStream seClip) {
        if (seClip == null) return;

        // 5つある再生機のうち、順番に空いているものを使って音を鳴らす
        _sePlayers[_sePlayerIndex].Stream = seClip;
        _sePlayers[_sePlayerIndex].Play();

        // 次に鳴らす再生機の番号をズラす
        _sePlayerIndex = (_sePlayerIndex + 1) % _sePlayers.Length;
    }
    // BGMを停止する
    public void StopBGM() {
        if (_bgmPlayer != null && _bgmPlayer.Playing) {
            _bgmPlayer.Stop();
        }
    }
}