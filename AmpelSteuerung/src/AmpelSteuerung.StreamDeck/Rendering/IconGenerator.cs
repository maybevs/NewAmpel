using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace AmpelSteuerung.StreamDeck.Rendering;

/// <summary>
/// Generates the static icon PNG files required by the Stream Deck manifest.
/// Run <see cref="GenerateAll"/> once during first-time setup or build.
/// </summary>
public static class IconGenerator
{
    private static readonly Color BgDark = Color.FromArgb(13, 13, 26);
    private static readonly Color Green = Color.FromArgb(46, 204, 113);
    private static readonly Color Red = Color.FromArgb(231, 76, 60);
    private static readonly Color Orange = Color.FromArgb(232, 160, 48);
    private static readonly Color Blue = Color.FromArgb(91, 107, 245);
    private static readonly Color Purple = Color.FromArgb(142, 111, 216);
    private static readonly Color Gray = Color.FromArgb(127, 140, 141);

    /// <summary>
    /// Generate all required icon PNGs into the given plugin directory.
    /// </summary>
    public static void GenerateAll(string pluginDir)
    {
        var imgsDir = Path.Combine(pluginDir, "imgs");
        var actionsDir = Path.Combine(imgsDir, "actions");
        Directory.CreateDirectory(actionsDir);

        // Plugin-level icons (28x28 for category, 72x72 for plugin icon)
        SaveIcon(Path.Combine(imgsDir, "plugin-icon.png"), 72, 72, (g, w, h) =>
        {
            FillRounded(g, 0, 0, w, h, 8, Green);
            DrawCentered(g, "A", "Segoe UI", 32, FontStyle.Bold, Color.White, w, h);
        });
        SaveIcon(Path.Combine(imgsDir, "plugin-icon@2x.png"), 144, 144, (g, w, h) =>
        {
            FillRounded(g, 0, 0, w, h, 16, Green);
            DrawCentered(g, "A", "Segoe UI", 64, FontStyle.Bold, Color.White, w, h);
        });
        SaveIcon(Path.Combine(imgsDir, "category-icon.png"), 28, 28, (g, w, h) =>
        {
            FillRounded(g, 0, 0, w, h, 4, Green);
            DrawCentered(g, "A", "Segoe UI", 16, FontStyle.Bold, Color.White, w, h);
        });
        SaveIcon(Path.Combine(imgsDir, "category-icon@2x.png"), 56, 56, (g, w, h) =>
        {
            FillRounded(g, 0, 0, w, h, 8, Green);
            DrawCentered(g, "A", "Segoe UI", 32, FontStyle.Bold, Color.White, w, h);
        });

        // Action icons (20x20 action list, 72x72 key default)
        GenerateActionIcons(actionsDir, "timer-display", Color.FromArgb(204, 0, 0), "T");
        GenerateActionIcons(actionsDir, "start-pause", Green, "\u25B6");
        GenerateActionIcons(actionsDir, "stop", Red, "\u25A0");
        GenerateActionIcons(actionsDir, "reset", Gray, "\u21BA");
        GenerateActionIcons(actionsDir, "skip", Orange, "\u23ED");
        GenerateActionIcons(actionsDir, "emergency-stop", Color.FromArgb(220, 30, 30), "!");
        GenerateActionIcons(actionsDir, "next-end", Blue, "\u25B6");
        GenerateActionIcons(actionsDir, "prev-end", Blue, "\u25C0");
        GenerateActionIcons(actionsDir, "group-ab", Purple, "AB");
        GenerateActionIcons(actionsDir, "group-cd", Purple, "CD");
        GenerateActionIcons(actionsDir, "preset", Purple, "\u2699");
        GenerateActionIcons(actionsDir, "start-side-left", Blue, "\u25C0");
        GenerateActionIcons(actionsDir, "start-side-right", Red, "\u25B6");
        GenerateActionIcons(actionsDir, "switch-side", Blue, "\u2194");
    }

    private static void GenerateActionIcons(string dir, string name, Color color, string symbol)
    {
        // Action list icon (20x20)
        SaveIcon(Path.Combine(dir, name + ".png"), 20, 20, (g, w, h) =>
        {
            FillRounded(g, 0, 0, w, h, 3, color);
            DrawCentered(g, symbol, "Segoe UI", symbol.Length > 1 ? 7 : 11, FontStyle.Bold, Color.White, w, h);
        });
        SaveIcon(Path.Combine(dir, name + "@2x.png"), 40, 40, (g, w, h) =>
        {
            FillRounded(g, 0, 0, w, h, 6, color);
            DrawCentered(g, symbol, "Segoe UI", symbol.Length > 1 ? 14 : 22, FontStyle.Bold, Color.White, w, h);
        });

        // Key default image (72x72)
        SaveIcon(Path.Combine(dir, name + "-key.png"), 72, 72, (g, w, h) =>
        {
            FillRounded(g, 2, 2, w - 4, h - 4, 8, BgDark);
            FillRounded(g, 6, 6, w - 12, h - 12, 6, color);
            DrawCentered(g, symbol, "Segoe UI", symbol.Length > 1 ? 18 : 28, FontStyle.Bold, Color.White, w, h);
        });
        SaveIcon(Path.Combine(dir, name + "-key@2x.png"), 144, 144, (g, w, h) =>
        {
            FillRounded(g, 4, 4, w - 8, h - 8, 16, BgDark);
            FillRounded(g, 12, 12, w - 24, h - 24, 12, color);
            DrawCentered(g, symbol, "Segoe UI", symbol.Length > 1 ? 36 : 56, FontStyle.Bold, Color.White, w, h);
        });
    }

    private static void SaveIcon(string path, int w, int h, Action<Graphics, int, int> draw)
    {
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);
        draw(g, w, h);
        bmp.Save(path, ImageFormat.Png);
    }

    private static void FillRounded(Graphics g, int x, int y, int w, int h, int r, Color color)
    {
        using var brush = new SolidBrush(color);
        using var path = new GraphicsPath();
        var d = r * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    private static void DrawCentered(Graphics g, string text, string fontFamily, float size, FontStyle style, Color color, int w, int h)
    {
        using var font = new Font(fontFamily, size, style);
        using var brush = new SolidBrush(color);
        var measure = g.MeasureString(text, font);
        g.DrawString(text, font, brush, (w - measure.Width) / 2, (h - measure.Height) / 2);
    }
}
