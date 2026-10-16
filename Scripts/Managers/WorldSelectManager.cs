using Godot;
using System;
using System.Collections.Generic;

public partial class WorldSelectManager : Control {
    [ExportCategory("設定")]
    [Export] public Control PlayerIcon;
    [Export] public WorldNode CurrentNode;
    [Export] public float MoveSpeed = 500f;

    [ExportCategory("サウンド設定")]
    [Export] public string MapBgmPath = "";

    [ExportCategory("UI連携（後ほど実装用）")]
    [Export] public Control MedalRewardUI;

    private bool _isMoving = false;
    private WorldNode _targetNode;
    private bool _isStartingWorld = false;
    private float _inputCooldown = 0f;

    public override void _Ready() {
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(MapBgmPath)) {
            SoundManager.Instance.PlayBGM(MapBgmPath);
        }

        // シーン内のすべての WorldNode を取得して初期化
        List<WorldNode> allNodes = new List<WorldNode>();
        FindAllWorldNodes(this, allNodes);

        foreach (var node in allNodes) {
            node.SetupNode();
        }

        // セーブされている現在地（currentWorldNodeNumber）があればそこに合わせる
        if (GameManager.Instance != null) {
            int savedNodeNum = GameManager.Instance.currentWorldNodeNumber;
            foreach (var node in allNodes) {
                if (node.MyWorldData != null && node.MyWorldData.WorldNumber == savedNodeNum) {
                    CurrentNode = node;
                    break;
                }
            }
        }

        // プレイヤーアイコンを現在のノードの位置に配置
        if (CurrentNode != null && PlayerIcon != null) {
            PlayerIcon.GlobalPosition = CurrentNode.GlobalPosition;
        }
    }

    private void FindAllWorldNodes(Node parent, List<WorldNode> list) {
        foreach (Node child in parent.GetChildren()) {
            if (child is WorldNode wn) list.Add(wn);
            FindAllWorldNodes(child, list);
        }
    }

    public override void _Process(double delta) {
        // メダル報酬UIが開いている間はロック（後ほど実装した際に自動で機能します）
        if (MedalRewardUI != null && MedalRewardUI.Visible) return;

        if (_isStartingWorld) return;

        float dt = (float)delta;
        if (_inputCooldown > 0f) {
            _inputCooldown -= dt;
        }

        if (_isMoving) {
            MovePlayerIcon(dt);
            return;
        }

        if (CurrentNode == null) return;

        // 上下左右の入力判定（先日追加した move_up / move_down にも対応）
        bool isUp = false, isDown = false, isLeft = false, isRight = false;
        if (_inputCooldown <= 0f) {
            if (Input.IsActionPressed("ui_up") || Input.IsActionPressed("move_up")) isUp = true;
            else if (Input.IsActionPressed("ui_down") || Input.IsActionPressed("move_down")) isDown = true;
            else if (Input.IsActionPressed("ui_left") || Input.IsActionPressed("move_left")) isLeft = true;
            else if (Input.IsActionPressed("ui_right") || Input.IsActionPressed("move_right")) isRight = true;
        }

        WorldNode nextNode = null;
        if (isUp) nextNode = CurrentNode.UpNode;
        else if (isDown) nextNode = CurrentNode.DownNode;
        else if (isLeft) nextNode = CurrentNode.LeftNode;
        else if (isRight) nextNode = CurrentNode.RightNode;

        // 移動先が存在し、かつ解放済み（IsUnlocked）なら移動開始
        if (nextNode != null && nextNode.IsUnlocked) {
            _targetNode = nextNode;
            _isMoving = true;
            _inputCooldown = 0.2f;
        }

        // 決定ボタンでワールドに入る
        bool isSubmit = Input.IsActionJustPressed("ui_accept") ||
                        Input.IsActionJustPressed("jump") ||
                        Input.IsActionJustPressed("attack");

        if (isSubmit) {
            if (CurrentNode.MyWorldData != null && CurrentNode.IsUnlocked) {
                _isStartingWorld = true;

                WorldData targetWorld = CurrentNode.MyWorldData;
                string storyFlag = "StoryWatched_World_" + targetWorld.WorldNumber;
                bool isStoryWatched = false;

                if (GameManager.Instance != null) {
                    isStoryWatched = GameManager.Instance.eventFlags.Contains(storyFlag);
                }

                string sceneToLoad = targetWorld.ScenePath;

                // ストーリー未視聴かつストーリーシーンが設定されていればそちらを優先
                if (!isStoryWatched && !string.IsNullOrEmpty(targetWorld.StoryScenePath)) {
                    sceneToLoad = targetWorld.StoryScenePath;
                }

                if (SceneTransitionManager.Instance != null) {
                    SceneTransitionManager.Instance.LoadScene(sceneToLoad, TransitionType.Fade);
                } else if (!string.IsNullOrEmpty(sceneToLoad)) {
                    GetTree().ChangeSceneToFile(sceneToLoad);
                }
            }
        }
    }

    private void MovePlayerIcon(float dt) {
        if (PlayerIcon == null || _targetNode == null) return;

        PlayerIcon.GlobalPosition = PlayerIcon.GlobalPosition.MoveToward(_targetNode.GlobalPosition, MoveSpeed * dt);

        if (PlayerIcon.GlobalPosition.DistanceTo(_targetNode.GlobalPosition) < 1.0f) {
            PlayerIcon.GlobalPosition = _targetNode.GlobalPosition;
            CurrentNode = _targetNode;
            _isMoving = false;

            if (GameManager.Instance != null && CurrentNode.MyWorldData != null) {
                GameManager.Instance.currentWorldNodeNumber = CurrentNode.MyWorldData.WorldNumber;
                GameManager.Instance.SaveGame();
            }
        }
    }
}