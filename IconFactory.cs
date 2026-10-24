using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace ToggleScreensaver;

/// <summary>
/// Icônes dessinées à l'exécution : croissant de lune (veille activée) et soleil
/// (veille désactivée, le PC reste éveillé).
/// </summary>
public static class IconFactory
{
    private static readonly int[] Sizes = { 16, 32, 48, 64 };
    private static readonly Dictionary<bool, byte[]> IcoCache = new();
    private static readonly Dictionary<bool, Drawing.Icon> TrayCache = new();

    /// <summary>Icône de fenêtre / barre des tâches.</summary>
    public static ImageSource GetWindowIcon(bool sleepBlocked) =>
        BitmapFrame.Create(new MemoryStream(GetIco(sleepBlocked)), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

    /// <summary>Icône de zone de notification, à la taille du système.</summary>
    public static Drawing.Icon GetTrayIcon(bool sleepBlocked)
    {
        if (!TrayCache.TryGetValue(sleepBlocked, out var icon))
        {
            using var stream = new MemoryStream(GetIco(sleepBlocked));
            icon = new Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
            TrayCache[sleepBlocked] = icon;
        }
        return icon;
    }

    private static byte[] GetIco(bool sun)
    {
        if (!IcoCache.TryGetValue(sun, out var ico))
        {
            ico = BuildIco(Sizes.Select(s => RenderPng(sun, s)).ToArray(), Sizes);
            IcoCache[sun] = ico;
        }
        return ico;
    }

    // Fichier .ico avec images PNG intégrées
    private static byte[] BuildIco(byte[][] pngs, int[] sizes)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        w.Write((short)0);
        w.Write((short)1);
        w.Write((short)pngs.Length);

        var offset = 6 + 16 * pngs.Length;
        for (var i = 0; i < pngs.Length; i++)
        {
            w.Write((byte)sizes[i]);
            w.Write((byte)sizes[i]);
            w.Write((byte)0);
            w.Write((byte)0);
            w.Write((short)1);
            w.Write((short)32);
            w.Write(pngs[i].Length);
            w.Write(offset);
            offset += pngs[i].Length;
        }
        foreach (var png in pngs) w.Write(png);
        return ms.ToArray();
    }

    private static byte[] RenderPng(bool sun, int size)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(size / 100.0, size / 100.0));
            if (sun) DrawSun(dc); else DrawMoon(dc);
            dc.Pop();
        }

        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    private static Brush Solid(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    private static void DrawMoon(DrawingContext dc)
    {
        dc.DrawEllipse(Solid("#1B2A5C"), null, new Point(50, 50), 48, 48);

        var crescent = new CombinedGeometry(
            GeometryCombineMode.Exclude,
            new EllipseGeometry(new Point(46, 54), 29, 29),
            new EllipseGeometry(new Point(62, 42), 25, 25));
        dc.DrawGeometry(Solid("#FFE082"), null, crescent);
    }

    private static void DrawSun(DrawingContext dc)
    {
        dc.DrawEllipse(Solid("#4FC3F7"), null, new Point(50, 50), 48, 48);

        var yellow = Solid("#FFD600");
        var pen = new Pen(yellow, 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        for (var i = 0; i < 8; i++)
        {
            var angle = i * Math.PI / 4;
            var (sin, cos) = Math.SinCos(angle);
            dc.DrawLine(pen,
                new Point(50 + 28 * cos, 50 + 28 * sin),
                new Point(50 + 38 * cos, 50 + 38 * sin));
        }
        dc.DrawEllipse(yellow, null, new Point(50, 50), 18, 18);
    }
}
