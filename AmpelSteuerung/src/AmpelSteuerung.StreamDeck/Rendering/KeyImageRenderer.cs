using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace AmpelSteuerung.StreamDeck.Rendering;

/// <summary>
/// Generates 144×144 PNG key images for Stream Deck buttons.
/// Uses System.Drawing to render text, shapes, and color-coded backgrounds.
/// </summary>
public class KeyImageRenderer
{
    private const int Size = 144;
    private const int Padding = 6;

    // Color palette matching the WPF app theme
    private static readonly Color BgDark = ColorFromHex("#0D0D1A");
    private static readonly Color BgCard = ColorFromHex("#161625");
    private static readonly Color TextPrimary = ColorFromHex("#F0F0F5");
    private static readonly Color TextSecondary = ColorFromHex("#8888AA");
    private static readonly Color TextMuted = ColorFromHex("#555570");
    private static readonly Color AccentGreen = ColorFromHex("#2ECC71");
    private static readonly Color AccentRed = ColorFromHex("#E74C3C");
    private static readonly Color AccentOrange = ColorFromHex("#E8A030");
    private static readonly Color AccentBlue = ColorFromHex("#5B6BF5");
    private static readonly Color AccentPurple = ColorFromHex("#8E6FD8");

    #region Timer Display

    /// <summary>
    /// Renders the main timer display key with color background, countdown, group, and end info.
    /// </summary>
    public string RenderTimerDisplay(int timeRemaining, string color, string group, string end, 
        string phase, string status, bool isFinalMode)
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = CreateGraphics(bmp);

        // Background: ampel color
        var bgColor = AmpelColorFromString(color);
        
        // Dim background when stopped/idle
        if (status == "stopped" && phase == "Idle")
            bgColor = DimColor(bgColor, 0.3f);

        // Rounded rect background
        FillRoundedRect(g, new Rectangle(0, 0, Size, Size), 12, bgColor);

        // Dark overlay for contrast
        using var overlayBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        FillRoundedRect(g, new Rectangle(0, 0, Size, Size), 12, overlayBrush);

        // Phase indicator bar at top
        var phaseColor = phase switch
        {
            "PreparationGroup1" or "PreparationGroup2" => AccentOrange,
            "ShootingGroup1" or "ShootingGroup2" => AccentGreen,
            "EndCompleted" => AccentBlue,
            "EmergencyStopped" => AccentRed,
            _ => Color.Transparent
        };
        if (phaseColor != Color.Transparent)
        {
            using var phaseBrush = new SolidBrush(phaseColor);
            g.FillRectangle(phaseBrush, 0, 0, Size, 4);
        }

        // End info at top
        using var endFont = new Font("Segoe UI", 11f, FontStyle.Regular);
        DrawCenteredText(g, end, endFont, TextPrimary, 14);

        // Timer text (main)
        var timeStr = $"{timeRemaining / 60:D2}:{timeRemaining % 60:D2}";
        using var timerFont = new Font("Segoe UI", 34f, FontStyle.Bold);
        DrawCenteredText(g, timeStr, timerFont, Color.White, 40);

        // Group at bottom
        using var groupFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        DrawCenteredText(g, group, groupFont, TextPrimary, 100);

        // Phase label at very bottom
        var phaseLabel = phase switch
        {
            "Idle" => "",
            "PreparationGroup1" => "VORB.",
            "ShootingGroup1" => "SCHIESSEN",
            "PreparationGroup2" => "VORB. G2",
            "ShootingGroup2" => "SCHIESSEN G2",
            "EndCompleted" => "FERTIG",
            "EmergencyStopped" => "NOTFALL",
            _ => ""
        };
        if (!string.IsNullOrEmpty(phaseLabel))
        {
            using var phaseFont = new Font("Segoe UI", 9f, FontStyle.Regular);
            DrawCenteredText(g, phaseLabel, phaseFont, Color.FromArgb(200, 255, 255, 255), 120);
        }

        // Status indicator dot
        if (status == "running")
        {
            using var dotBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
            g.FillEllipse(dotBrush, Size - 16, 6, 8, 8);
        }
        else if (status == "paused")
        {
            using var dotBrush = new SolidBrush(AccentOrange);
            g.FillEllipse(dotBrush, Size - 16, 6, 8, 8);
        }

        return ToBase64DataUri(bmp);
    }

    #endregion

    #region Action Button

    /// <summary>
    /// Renders a standard action button with icon, label, and state-dependent colors.
    /// </summary>
    public string RenderActionButton(string label, string bgColorHex, string icon, bool enabled)
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = CreateGraphics(bmp);

        var bgColor = enabled ? ColorFromHex(bgColorHex) : DimColor(ColorFromHex(bgColorHex), 0.35f);
        var textColor = enabled ? TextPrimary : TextMuted;

        // Background
        FillRoundedRect(g, new Rectangle(2, 2, Size - 4, Size - 4), 16, BgDark);
        FillRoundedRect(g, new Rectangle(6, 6, Size - 12, Size - 12), 12, bgColor);

        // Icon
        DrawIcon(g, icon, enabled);

        // Label
        using var labelFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        DrawCenteredText(g, label, labelFont, textColor, 108);

        return ToBase64DataUri(bmp);
    }

    #endregion

    #region Emergency Button

    /// <summary>
    /// Renders the emergency stop button with prominent warning appearance.
    /// </summary>
    public string RenderEmergencyButton(bool isActive)
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = CreateGraphics(bmp);

        // Bright red background, brighter when active
        var bgColor = isActive ? Color.FromArgb(255, 220, 30, 30) : Color.FromArgb(255, 180, 20, 20);

        FillRoundedRect(g, new Rectangle(2, 2, Size - 4, Size - 4), 16, BgDark);
        FillRoundedRect(g, new Rectangle(6, 6, Size - 12, Size - 12), 12, bgColor);

        // Warning border when active
        if (isActive)
        {
            using var borderPen = new Pen(Color.Yellow, 3);
            DrawRoundedRect(g, new Rectangle(6, 6, Size - 12, Size - 12), 12, borderPen);
        }

        // Warning triangle
        var cx = Size / 2;
        var trianglePoints = new PointF[]
        {
            new(cx, 22),
            new(cx - 30, 72),
            new(cx + 30, 72)
        };
        using var trianglePen = new Pen(Color.White, 3) { LineJoin = LineJoin.Round };
        g.DrawPolygon(trianglePen, trianglePoints);

        // Exclamation mark
        using var excFont = new Font("Segoe UI", 22f, FontStyle.Bold);
        DrawCenteredText(g, "!", excFont, Color.White, 38);

        // Label
        using var labelFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        var label = isActive ? "NOTFALL!" : "NOT-STOP";
        DrawCenteredText(g, label, labelFont, Color.White, 86);

        if (isActive)
        {
            using var activeFont = new Font("Segoe UI", 9f, FontStyle.Regular);
            DrawCenteredText(g, "AKTIV", activeFont, Color.Yellow, 108);
        }

        return ToBase64DataUri(bmp);
    }

    #endregion

    #region End Navigation Button

    /// <summary>
    /// Renders an end navigation button (next/previous) with current end display.
    /// </summary>
    public string RenderEndNavigationButton(string direction, string currentEnd, bool enabled)
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = CreateGraphics(bmp);

        var bgColor = enabled ? BgCard : DimColor(BgCard, 0.5f);
        var accentColor = enabled ? AccentBlue : TextMuted;

        FillRoundedRect(g, new Rectangle(2, 2, Size - 4, Size - 4), 16, BgDark);
        FillRoundedRect(g, new Rectangle(6, 6, Size - 12, Size - 12), 12, bgColor);

        // Direction arrow
        var cx = Size / 2;
        if (direction == "next")
        {
            // Right-pointing arrow
            var arrowPoints = new PointF[]
            {
                new(cx - 15, 30),
                new(cx + 15, 50),
                new(cx - 15, 70)
            };
            using var arrowPen = new Pen(accentColor, 4) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLines(arrowPen, arrowPoints);
        }
        else
        {
            // Left-pointing arrow
            var arrowPoints = new PointF[]
            {
                new(cx + 15, 30),
                new(cx - 15, 50),
                new(cx + 15, 70)
            };
            using var arrowPen = new Pen(accentColor, 4) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLines(arrowPen, arrowPoints);
        }

        // End counter
        using var endFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        DrawCenteredText(g, currentEnd, endFont, enabled ? TextPrimary : TextMuted, 82);

        // Label
        var label = direction == "next" ? "Nächste" : "Vorherige";
        using var labelFont = new Font("Segoe UI", 10f, FontStyle.Regular);
        DrawCenteredText(g, label, labelFont, enabled ? TextSecondary : TextMuted, 108);

        return ToBase64DataUri(bmp);
    }

    #endregion

    #region Group Button

    /// <summary>
    /// Renders a group selection button (AB or CD) with active highlight.
    /// </summary>
    public string RenderGroupButton(string group, bool isActive)
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = CreateGraphics(bmp);

        var bgColor = isActive ? AccentPurple : BgCard;

        FillRoundedRect(g, new Rectangle(2, 2, Size - 4, Size - 4), 16, BgDark);
        FillRoundedRect(g, new Rectangle(6, 6, Size - 12, Size - 12), 12, bgColor);

        if (isActive)
        {
            using var borderPen = new Pen(Color.FromArgb(150, 255, 255, 255), 2);
            DrawRoundedRect(g, new Rectangle(8, 8, Size - 16, Size - 16), 10, borderPen);
        }

        // Group name large
        using var groupFont = new Font("Segoe UI", 36f, FontStyle.Bold);
        DrawCenteredText(g, group, groupFont, isActive ? Color.White : TextSecondary, 36);

        // Label
        using var labelFont = new Font("Segoe UI", 11f, FontStyle.Regular);
        DrawCenteredText(g, "Gruppe", labelFont, isActive ? Color.FromArgb(200, 255, 255, 255) : TextMuted, 108);

        return ToBase64DataUri(bmp);
    }

    #endregion

    #region Preset Button

    /// <summary>
    /// Renders a preset selection button showing the preset name.
    /// </summary>
    public string RenderPresetButton(string presetName)
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = CreateGraphics(bmp);

        FillRoundedRect(g, new Rectangle(2, 2, Size - 4, Size - 4), 16, BgDark);
        FillRoundedRect(g, new Rectangle(6, 6, Size - 12, Size - 12), 12, AccentPurple);

        // Preset icon (gear/config)
        using var iconFont = new Font("Segoe UI", 20f, FontStyle.Regular);
        DrawCenteredText(g, "\u2699", iconFont, Color.White, 20); // ⚙

        // Preset name (truncated)
        var displayName = presetName.Length > 12 ? presetName[..11] + "." : presetName;
        using var nameFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        DrawCenteredText(g, displayName, nameFont, Color.White, 65);

        // Label
        using var labelFont = new Font("Segoe UI", 10f, FontStyle.Regular);
        DrawCenteredText(g, "Preset", labelFont, Color.FromArgb(180, 255, 255, 255), 108);

        return ToBase64DataUri(bmp);
    }

    #endregion

    #region Start Side Button

    /// <summary>
    /// Renders a start side button (Links/Rechts) for Final mode.
    /// Highlighted when the side is currently active.
    /// </summary>
    public string RenderStartSideButton(string side, bool isActive, bool isFinalMode)
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = CreateGraphics(bmp);

        var isLeft = side == "left";
        var label = isLeft ? "Links" : "Rechts";
        var sideColor = isLeft ? Color.FromArgb(52, 152, 219) : Color.FromArgb(231, 76, 60); // Blue / Red
        var bgColor = !isFinalMode ? DimColor(BgCard, 0.5f)
                    : isActive ? sideColor : BgCard;

        FillRoundedRect(g, new Rectangle(2, 2, Size - 4, Size - 4), 16, BgDark);
        FillRoundedRect(g, new Rectangle(6, 6, Size - 12, Size - 12), 12, bgColor);

        if (isActive && isFinalMode)
        {
            using var borderPen = new Pen(Color.FromArgb(180, 255, 255, 255), 2);
            DrawRoundedRect(g, new Rectangle(8, 8, Size - 16, Size - 16), 10, borderPen);
        }

        // Arrow icon
        var cx = Size / 2;
        var arrowColor = (isActive && isFinalMode) ? Color.White : (isFinalMode ? TextSecondary : TextMuted);
        using var arrowPen = new Pen(arrowColor, 4) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

        if (isLeft)
        {
            var pts = new PointF[] { new(cx + 12, 28), new(cx - 12, 48), new(cx + 12, 68) };
            g.DrawLines(arrowPen, pts);
        }
        else
        {
            var pts = new PointF[] { new(cx - 12, 28), new(cx + 12, 48), new(cx - 12, 68) };
            g.DrawLines(arrowPen, pts);
        }

        // Label
        var textColor = (isActive && isFinalMode) ? Color.White : (isFinalMode ? TextSecondary : TextMuted);
        using var labelFont = new Font("Segoe UI", 13f, FontStyle.Bold);
        DrawCenteredText(g, label, labelFont, textColor, 82);

        // Sub-label
        using var subFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        DrawCenteredText(g, "Starten", subFont, Color.FromArgb(isActive && isFinalMode ? 200 : 100, 255, 255, 255), 110);

        return ToBase64DataUri(bmp);
    }

    #endregion

    #region Disconnected State

    /// <summary>
    /// Renders a "disconnected" state for any key when the API is unreachable.
    /// </summary>
    public string RenderDisconnected()
    {
        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = CreateGraphics(bmp);

        FillRoundedRect(g, new Rectangle(0, 0, Size, Size), 12, Color.FromArgb(30, 30, 30));

        using var iconFont = new Font("Segoe UI", 28f, FontStyle.Regular);
        DrawCenteredText(g, "?", iconFont, TextMuted, 35);

        using var labelFont = new Font("Segoe UI", 10f, FontStyle.Regular);
        DrawCenteredText(g, "Getrennt", labelFont, TextMuted, 95);

        return ToBase64DataUri(bmp);
    }

    #endregion

    #region Drawing Helpers

    private static Graphics CreateGraphics(Bitmap bmp)
    {
        var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        return g;
    }

    private static void DrawCenteredText(Graphics g, string text, Font font, Color color, float y)
    {
        using var brush = new SolidBrush(color);
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, (Size - size.Width) / 2, y);
    }

    private static void DrawIcon(Graphics g, string icon, bool enabled)
    {
        var color = enabled ? Color.White : Color.FromArgb(100, 255, 255, 255);
        var cx = Size / 2;
        var cy = 55;

        using var pen = new Pen(color, 3) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var brush = new SolidBrush(color);

        switch (icon)
        {
            case "play":
                // Triangle pointing right
                var playPoints = new PointF[]
                {
                    new(cx - 12, cy - 16),
                    new(cx + 16, cy),
                    new(cx - 12, cy + 16)
                };
                g.FillPolygon(brush, playPoints);
                break;

            case "pause":
                // Two vertical bars
                g.FillRectangle(brush, cx - 12, cy - 14, 8, 28);
                g.FillRectangle(brush, cx + 4, cy - 14, 8, 28);
                break;

            case "stop":
                // Square
                g.FillRectangle(brush, cx - 13, cy - 13, 26, 26);
                break;

            case "reset":
                // Circular arrow
                using (var arcPen = new Pen(color, 3))
                {
                    g.DrawArc(arcPen, cx - 14, cy - 14, 28, 28, -60, 300);
                    // Arrow tip
                    var tipPoints = new PointF[]
                    {
                        new(cx + 10, cy - 18),
                        new(cx + 14, cy - 8),
                        new(cx + 4, cy - 10)
                    };
                    g.FillPolygon(brush, tipPoints);
                }
                break;

            case "skip":
                // Double triangle + bar
                var skip1 = new PointF[]
                {
                    new(cx - 18, cy - 14),
                    new(cx - 2, cy),
                    new(cx - 18, cy + 14)
                };
                g.FillPolygon(brush, skip1);
                var skip2 = new PointF[]
                {
                    new(cx, cy - 14),
                    new(cx + 16, cy),
                    new(cx, cy + 14)
                };
                g.FillPolygon(brush, skip2);
                g.FillRectangle(brush, cx + 17, cy - 14, 4, 28);
                break;

            case "next":
                // Play triangle with "+" 
                var nextPoints = new PointF[]
                {
                    new(cx - 12, cy - 14),
                    new(cx + 10, cy),
                    new(cx - 12, cy + 14)
                };
                g.FillPolygon(brush, nextPoints);
                g.FillRectangle(brush, cx + 14, cy - 8, 3, 16);
                break;

            case "blocked":
                // Circle with line through it
                using (var blockPen = new Pen(color, 3))
                {
                    g.DrawEllipse(blockPen, cx - 14, cy - 14, 28, 28);
                    g.DrawLine(blockPen, cx - 10, cy + 10, cx + 10, cy - 10);
                }
                break;

            case "switch":
                // Double-headed horizontal arrow (↔)
                using (var swPen = new Pen(color, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(swPen, cx - 18, cy, cx + 18, cy);
                    // Left arrowhead
                    g.DrawLine(swPen, cx - 18, cy, cx - 10, cy - 8);
                    g.DrawLine(swPen, cx - 18, cy, cx - 10, cy + 8);
                    // Right arrowhead
                    g.DrawLine(swPen, cx + 18, cy, cx + 10, cy - 8);
                    g.DrawLine(swPen, cx + 18, cy, cx + 10, cy + 8);
                }
                break;
        }
    }

    private static void FillRoundedRect(Graphics g, Rectangle rect, int radius, Color color)
    {
        using var brush = new SolidBrush(color);
        FillRoundedRect(g, rect, radius, brush);
    }

    private static void FillRoundedRect(Graphics g, Rectangle rect, int radius, Brush brush)
    {
        using var path = CreateRoundedRectPath(rect, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRect(Graphics g, Rectangle rect, int radius, Pen pen)
    {
        using var path = CreateRoundedRectPath(rect, radius);
        g.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color AmpelColorFromString(string color) => color switch
    {
        "red" => Color.FromArgb(204, 0, 0),
        "green" => Color.FromArgb(0, 170, 0),
        "yellow" => Color.FromArgb(221, 170, 0),
        _ => Color.FromArgb(204, 0, 0)
    };

    private static Color DimColor(Color c, float factor) =>
        Color.FromArgb(c.A, (int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));

    private static Color ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return Color.FromArgb(
            Convert.ToInt32(hex[..2], 16),
            Convert.ToInt32(hex[2..4], 16),
            Convert.ToInt32(hex[4..6], 16));
    }

    private static string ToBase64DataUri(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
    }

    #endregion
}
