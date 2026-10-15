using Godot;
using System;
using System.Threading.Tasks;

public enum TransitionType {
    Fade,       // 通常の暗転フェード[cite: 25]
    CourseSlide // コース選択時のスライド[cite: 25]
}

public partial class SceneTransitionManager : CanvasLayer {
    public static SceneTransitionManager Instance { get; private set; }

    [ExportCategory("UI連携")]
    [Export] public AnimationPlayer Anim;
    [Export] public Label CourseText;
    [Export] public ColorRect FallbackFadeRect; // AnimationPlayer未設定時用の黒背景（任意）

    [ExportCategory("設定")]
    [Export] public float FadeWaitTime = 1.0f;  // 暗転にかかる時間[cite: 25]
    [Export] public float SlideWaitTime = 1.2f; // スライドにかかる時間[cite: 25]

    public override void _Ready() {
        if (Instance != null) {
            QueueFree();
            return;
        }
        Instance = this;
        Layer = 110; // 最前面に表示
        ProcessMode = ProcessModeEnum.Always; // ポーズ中や時間停止中も動くようにする[cite: 25]
    }

    // ① 文字なし遷移（タイトル→マップ、ミニゲーム→リザルトなど）[cite: 25]
    public async void LoadScene(string scenePath, TransitionType type = TransitionType.Fade) {
        await TransitionRoutine(scenePath, "", type);
    }

    // ② 文字入り遷移[cite: 25]
    public async void LoadCourse(string scenePath, string displayName, TransitionType type = TransitionType.CourseSlide) {
        await TransitionRoutine(scenePath, displayName, type);
    }

    // ③ ステージ番号を渡すと「Course No.00」にしてくれるメソッド[cite: 25]
    public async void LoadCourseByNumber(string scenePath, int stageNumber, TransitionType type = TransitionType.CourseSlide) {
        string formattedName = $"Course No.{stageNumber:D2}";
        await TransitionRoutine(scenePath, formattedName, type);
    }

    private async Task TransitionRoutine(string scenePath, string displayName, TransitionType type) {
        if (CourseText != null) {
            CourseText.Text = displayName;
            CourseText.Visible = !string.IsNullOrEmpty(displayName);
        }

        string inAnim = type == TransitionType.Fade ? "FadeIn" : "CourseIn";
        string outAnim = type == TransitionType.Fade ? "FadeOut" : "CourseOut";
        float waitTime = type == TransitionType.Fade ? FadeWaitTime : SlideWaitTime;

        // トランジションIn 再生[cite: 25]
        if (Anim != null && Anim.HasAnimation(inAnim)) {
            Anim.Play(inAnim);
        }
        await ToSignal(GetTree().CreateTimer(waitTime, true), SceneTreeTimer.SignalName.Timeout);

        // シーン切り替え[cite: 25]
        if (!string.IsNullOrEmpty(scenePath)) {
            GetTree().ChangeSceneToFile(scenePath);
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        // トランジションOut 再生[cite: 25]
        if (Anim != null && Anim.HasAnimation(outAnim)) {
            Anim.Play(outAnim);
        }
    }

    // 同一シーン内のワープ用メソッド[cite: 25]
    public async void WarpInSameScene(Action warpAction, TransitionType type = TransitionType.Fade) {
        await WarpSameSceneRoutine(warpAction, type);
    }

    private async Task WarpSameSceneRoutine(Action warpAction, TransitionType type) {
        if (CourseText != null) {
            CourseText.Visible = false;
        }

        string inAnim = type == TransitionType.Fade ? "FadeIn" : "CourseIn";
        string outAnim = type == TransitionType.Fade ? "FadeOut" : "CourseOut";
        float waitTime = type == TransitionType.Fade ? FadeWaitTime : SlideWaitTime;

        if (Anim != null && Anim.HasAnimation(inAnim)) {
            Anim.Play(inAnim);
        }
        await ToSignal(GetTree().CreateTimer(waitTime, true), SceneTreeTimer.SignalName.Timeout);

        // 暗転した瞬間にワープ処理を実行[cite: 25]
        warpAction?.Invoke();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (Anim != null && Anim.HasAnimation(outAnim)) {
            Anim.Play(outAnim);
        }
    }
}