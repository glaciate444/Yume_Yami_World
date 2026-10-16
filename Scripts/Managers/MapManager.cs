using Godot;
using System;
using System.Collections.Generic;

public partial class MapManager : Control {
    [ExportCategory("マップ設定")]
    [Export] public Control PlayerIcon;
    [Export] public MapNode CurrentNode;
    [Export] public float MoveSpeed = 600f;

    [ExportCategory("道（ライン）の設定")]
    [Export] public Control LineContainer;
    [Export] public float LineWidth = 15f;
    [Export] public Color LockedLineColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
    [Export] public Color UnlockedLineColor = new Color(0.8f, 0.6f, 0.9f, 1.0f);

    [ExportCategory("ワールドマップへ戻る設定")]
    [Export(PropertyHint.File, "*.tscn")]
    public string WorldMapScenePath = "res://Scene/world_map_scene.tscn";

    [ExportCategory("UI表示設定")]
    [Export] public Label StageNameText;
    [Export] public TextureRect[] MedalImages = Array.Empty<TextureRect>();
    [Export] public Color GotMedalColor = Colors.White;
    [Export] public Color NotGotMedalColor = new Color(0f, 0f, 0f, 0.5f);

    [ExportCategory("キャラクター選択UI（後ほど実装用）")]
    [Export] public Control CharacterSelectUI;

    [ExportCategory("データ")]
    [Export] public WorldData MyWorldData;

    private bool _isMoving = false;
    private MapNode _targetNode;
    private bool _isStartingCourse = false;
    private float _inputCooldown = 0f;

    public override void _Ready() {
        PlayStageBGM();

        // シーン内のすべての MapNode を取得してセットアップ
        List<MapNode> allNodes = new List<MapNode>();
        FindAllMapNodes(this, allNodes);

        foreach (var node in allNodes) {
            node.SetupNode();
        }

        // ノード間の道（ライン）を自動描画
        DrawAllPaths(allNodes);

        // 前回いたステージ番号があればそこにカーソルを合わせる
        if (GameManager.Instance != null) {
            int savedNodeNum = GameManager.Instance.currentMapNodeNumber;
            foreach (var node in allNodes) {
                if (node.MyLevelData != null && node.MyLevelData.StageNumber == savedNodeNum) {
                    CurrentNode = node;
                    break;
                }
            }
        }

        if (CurrentNode != null && PlayerIcon != null) {
            PlayerIcon.GlobalPosition = GetNodeCenter(CurrentNode);
        }

        UpdateMapUI();
    }

    private void FindAllMapNodes(Node parent, List<MapNode> list) {
        foreach (Node child in parent.GetChildren()) {
            if (child is MapNode mn) list.Add(mn);
            FindAllMapNodes(child, list);
        }
    }

    // マスの中心座標を取得するヘルパー
    private Vector2 GetNodeCenter(Control node) {
        return node.GlobalPosition + (node.Size * 0.5f) - (PlayerIcon != null ? PlayerIcon.Size * 0.5f : Vector2.Zero);
    }

    private void DrawAllPaths(List<MapNode> allNodes) {
        if (LineContainer == null) return;

        // 既存のラインがあればクリア
        foreach (Node child in LineContainer.GetChildren()) {
            child.QueueFree();
        }

        foreach (var node in allNodes) {
            DrawLineBetween(node, node.UpNode);
            DrawLineBetween(node, node.DownNode);
            DrawLineBetween(node, node.LeftNode);
            DrawLineBetween(node, node.RightNode);
        }
    }

    private void DrawLineBetween(MapNode fromNode, MapNode toNode) {
        if (fromNode == null || toNode == null) return;
        // 二重描画防止
        if (fromNode.GetInstanceId() > toNode.GetInstanceId()) return;

        Line2D line = new Line2D();
        line.Width = LineWidth;
        line.DefaultColor = (fromNode.IsUnlocked && toNode.IsUnlocked) ? UnlockedLineColor : LockedLineColor;

        // 各ノードの中心同士を繋ぐ
        Vector2 startPos = fromNode.GlobalPosition + (fromNode.Size * 0.5f) - LineContainer.GlobalPosition;
        Vector2 endPos = toNode.GlobalPosition + (toNode.Size * 0.5f) - LineContainer.GlobalPosition;

        line.AddPoint(startPos);
        line.AddPoint(endPos);

        LineContainer.AddChild(line);
    }

    public override void _Process(double delta) {
        if (CharacterSelectUI != null && CharacterSelectUI.Visible) return;
        if (_isStartingCourse) return;

        // キャラ選択UIを開くボタン（後ほど実装した際に自動で機能します）
        if (Input.IsActionJustPressed("shoot")) {
            if (CharacterSelectUI != null) {
                CharacterSelectUI.Visible = true;
                return;
            }
        }

        float dt = (float)delta;
        if (_inputCooldown > 0f) {
            _inputCooldown -= dt;
        }

        if (_isMoving) {
            MovePlayerIcon(dt);
            return;
        }

        // キャンセル（ダッシュキー / ポーズキー / UIキャンセル）でワールドマップへ戻る
        bool isCancel = Input.IsActionJustPressed("dash") ||
                        Input.IsActionJustPressed("pause") ||
                        Input.IsActionJustPressed("ui_cancel");

        if (isCancel) {
            _isStartingCourse = true;
            if (SceneTransitionManager.Instance != null) {
                SceneTransitionManager.Instance.LoadScene(WorldMapScenePath, TransitionType.Fade);
            } else if (!string.IsNullOrEmpty(WorldMapScenePath)) {
                GetTree().ChangeSceneToFile(WorldMapScenePath);
            }
            return;
        }

        if (CurrentNode == null) return;

        bool isUp = false, isDown = false, isLeft = false, isRight = false;
        if (_inputCooldown <= 0f) {
            if (Input.IsActionPressed("ui_up") || Input.IsActionPressed("move_up")) isUp = true;
            else if (Input.IsActionPressed("ui_down") || Input.IsActionPressed("move_down")) isDown = true;
            else if (Input.IsActionPressed("ui_left") || Input.IsActionPressed("move_left")) isLeft = true;
            else if (Input.IsActionPressed("ui_right") || Input.IsActionPressed("move_right")) isRight = true;
        }

        MapNode nextNode = null;
        if (isUp) nextNode = CurrentNode.UpNode;
        else if (isDown) nextNode = CurrentNode.DownNode;
        else if (isLeft) nextNode = CurrentNode.LeftNode;
        else if (isRight) nextNode = CurrentNode.RightNode;

        if (nextNode != null && nextNode.IsUnlocked) {
            _targetNode = nextNode;
            _isMoving = true;
            _inputCooldown = 0.2f;
        }

        // 決定ボタンでコース（またはショップ）へ入る
        bool isSubmit = Input.IsActionJustPressed("ui_accept") ||
                        Input.IsActionJustPressed("jump") ||
                        Input.IsActionJustPressed("attack");

        if (isSubmit && CurrentNode.IsUnlocked) {
            // クリア後にこのマップへ戻ってこられるよう、現在のシーンパスを記録
            if (GameManager.Instance != null) {
                GameManager.Instance.returnMapSceneName = GetTree().CurrentScene.SceneFilePath;
                // ステージに入る前に今回の獲得コイン一時変数を0にリセット
                GameManager.Instance.stageCoins = 0;
            }

            if (CurrentNode.IsShopNode) {
                _isStartingCourse = true;
                if (SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.LoadScene(CurrentNode.ShopScenePath, TransitionType.Fade);
                else if (!string.IsNullOrEmpty(CurrentNode.ShopScenePath))
                    GetTree().ChangeSceneToFile(CurrentNode.ShopScenePath);
            } else if (CurrentNode.MyLevelData != null) {
                _isStartingCourse = true;
                string targetScene = CurrentNode.MyLevelData.ScenePath;

                if (SceneTransitionManager.Instance != null) {
                    SceneTransitionManager.Instance.LoadCourseByNumber(
                        targetScene,
                        CurrentNode.MyLevelData.DisplayCourseNumber
                    );
                } else if (!string.IsNullOrEmpty(targetScene)) {
                    GetTree().ChangeSceneToFile(targetScene);
                }
            }
        }
    }

    private void MovePlayerIcon(float dt) {
        if (PlayerIcon == null || _targetNode == null) return;

        Vector2 targetPos = GetNodeCenter(_targetNode);
        PlayerIcon.GlobalPosition = PlayerIcon.GlobalPosition.MoveToward(targetPos, MoveSpeed * dt);

        if (PlayerIcon.GlobalPosition.DistanceTo(targetPos) < 1.0f) {
            PlayerIcon.GlobalPosition = targetPos;
            CurrentNode = _targetNode;
            _isMoving = false;

            if (GameManager.Instance != null && CurrentNode.MyLevelData != null) {
                GameManager.Instance.currentMapNodeNumber = CurrentNode.MyLevelData.StageNumber;
                GameManager.Instance.SaveGame();
            }
            UpdateMapUI();
        }
    }

    private void UpdateMapUI() {
        if (CurrentNode != null && CurrentNode.IsShopNode) {
            if (StageNameText != null) StageNameText.Text = "ショップ";
            for (int i = 0; i < MedalImages.Length; i++) {
                if (MedalImages[i] != null) MedalImages[i].Visible = false;
            }
        } else if (CurrentNode != null && CurrentNode.MyLevelData != null) {
            if (StageNameText != null) StageNameText.Text = CurrentNode.MyLevelData.LevelName;

            int maxMedals = CurrentNode.MyLevelData.MaxMedals;
            int stageNum = CurrentNode.MyLevelData.StageNumber;

            for (int i = 0; i < MedalImages.Length; i++) {
                if (MedalImages[i] != null) {
                    if (i < maxMedals) {
                        MedalImages[i].Visible = true;
                        bool isGot = MapNode.CheckMedalGot(stageNum, i);
                        MedalImages[i].Modulate = isGot ? GotMedalColor : NotGotMedalColor;
                    } else {
                        MedalImages[i].Visible = false;
                    }
                }
            }
        } else {
            if (StageNameText != null) StageNameText.Text = "";
            for (int i = 0; i < MedalImages.Length; i++) {
                if (MedalImages[i] != null) MedalImages[i].Visible = false;
            }
        }
    }

    private void PlayStageBGM() {
        if (MyWorldData != null && MyWorldData.WorldBGM != null && SoundManager.Instance != null) {
            // SoundManager側がAudioStream対応であればそのまま再生
        }
    }
}