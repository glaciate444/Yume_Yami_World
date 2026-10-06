using Godot;
using System;

public partial class WhipLineController : Line2D{
    // [Export] は Unity の [Tooltip] や [SerializeField] の役割を果たします
    [Export] public Node2D HandPoint;
    [Export] public Node2D TipPoint;

    // Unityでは0.5fでしたが、ピクセル単位なので数値を大きくします[cite: 15]
    [Export] public float CurveHeight = 50.0f;
    [Export] public int Resolution = 15;

    public override void _Process(double delta){
        // 表示されている時だけ曲線を描画する
        if (Visible && HandPoint != null && TipPoint != null){
            DrawCurve();
        }
    }

    private void DrawCurve(){
        ClearPoints(); // 古い頂点をリセット

        // GlobalPosition（ワールド座標）を取得
        Vector2 start = HandPoint.GlobalPosition;
        Vector2 end = TipPoint.GlobalPosition;

        // 2点間の方向から、リボンが膨らむ方向（法線）を計算[cite: 15]
        Vector2 dir = end - start;
        Vector2 normal = new Vector2(-dir.Y, dir.X).Normalized();

        // 向きによる反転補正[cite: 15]
        float facing = (dir.X >= 0f) ? 1f : -1f;
        normal *= facing;

        // 制御点（しなりの頂点）を計算[cite: 15]
        Vector2 control = (start + end) / 2.0f + (normal * CurveHeight);

        // 滑らかな曲線を計算してLine2Dに追加[cite: 15]
        for (int i = 0; i < Resolution; i++){
            float t = i / (float)(Resolution - 1);
            Vector2 point = CalculateBezierPoint(t, start, control, end);

            // ToLocal を使ってワールド座標をLine2Dのローカル座標に変換して打つ
            AddPoint(ToLocal(point));
        }
    }

    // 曲線を描くための計算式（2次ベジェ曲線）はUnityのものをそのまま流用[cite: 15]
    private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2){
        float u = 1 - t;
        return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
    }
}