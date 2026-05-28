using Avalonia.Media;

namespace PhigrosShellGUI.Controls;

/// <summary>
/// 共享图标路径几何数据。
/// 所有图标从这里统一引用，确保视觉一致性。
/// </summary>
public static class IconGeometries
{
    /// <summary>铅笔编辑图标</summary>
    public const string EditIconPathData = "M3 17.25V21h3.75l11.06-11.06-3.75-3.75L3 17.25z M20.71 7.04c.39-.39.39-1.04 0-1.41l-2.34-2.34c-.37-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z";

    /// <summary>勾确认图标</summary>
    public const string ConfirmIconPathData = "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z";

    public static Geometry EditIcon => Geometry.Parse(EditIconPathData);
    public static Geometry ConfirmIcon => Geometry.Parse(ConfirmIconPathData);
}
