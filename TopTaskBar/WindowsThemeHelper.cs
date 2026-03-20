using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace TopTaskBar;

internal static class WindowsThemeHelper
{
    public static WindowsThemePalette GetPalette()
    {
        var accent = GetAccentColor();
        var isLightTheme = IsLightTheme();

        var baseStart = isLightTheme ? Color.FromArgb(230, 245, 247, 250) : Color.FromArgb(230, 25, 31, 39);
        var baseMiddle = isLightTheme ? Color.FromArgb(220, 237, 241, 246) : Color.FromArgb(217, 35, 42, 51);
        var baseEnd = isLightTheme ? Color.FromArgb(214, 229, 235, 241) : Color.FromArgb(209, 28, 33, 41);

        var barStart = Blend(baseStart, accent, isLightTheme ? 0.10 : 0.18);
        var barMiddle = Blend(baseMiddle, accent, isLightTheme ? 0.12 : 0.20);
        var barEnd = Blend(baseEnd, accent, isLightTheme ? 0.10 : 0.16);

        var primaryForeground = isLightTheme ? Color.FromRgb(33, 37, 41) : Color.FromRgb(245, 247, 250);
        var mutedForeground = isLightTheme ? Color.FromRgb(90, 103, 115) : Color.FromRgb(184, 199, 217);
        var chipBackground = isLightTheme
            ? Color.FromArgb(120, 255, 255, 255)
            : Blend(Color.FromArgb(80, 44, 55, 68), accent, 0.18);
        var chipBorder = Blend(accent, isLightTheme ? Colors.White : Colors.Black, isLightTheme ? 0.55 : 0.45);
        var appStripBackground = isLightTheme
            ? Color.FromArgb(120, 255, 255, 255)
            : Color.FromArgb(40, 243, 246, 251);
        var appStripBorder = Blend(accent, Colors.White, isLightTheme ? 0.55 : 0.32);
        var accentSurface = Color.FromArgb(isLightTheme ? (byte)90 : (byte)70, accent.R, accent.G, accent.B);
        var accentBorder = Color.FromArgb(isLightTheme ? (byte)150 : (byte)110, accent.R, accent.G, accent.B);
        var accentForeground = isLightTheme ? Colors.White : Color.FromRgb(243, 255, 251);

        return new WindowsThemePalette
        {
            BarBackgroundBrush = CreateGradientBrush(barStart, barMiddle, barEnd),
            BarBorderBrush = CreateBrush(Color.FromArgb(102, accent.R, accent.G, accent.B)),
            PrimaryForegroundBrush = CreateBrush(primaryForeground),
            MutedForegroundBrush = CreateBrush(mutedForeground),
            AccentBackgroundBrush = CreateBrush(accentSurface),
            AccentBorderBrush = CreateBrush(accentBorder),
            AccentForegroundBrush = CreateBrush(accentForeground),
            AppStripBackgroundBrush = CreateBrush(appStripBackground),
            AppStripBorderBrush = CreateBrush(appStripBorder),
            ChipBackgroundBrush = CreateBrush(chipBackground),
            ChipBorderBrush = CreateBrush(Color.FromArgb(80, chipBorder.R, chipBorder.G, chipBorder.B))
        };
    }

    private static bool IsLightTheme()
    {
        const string personalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        using var personalizeKey = Registry.CurrentUser.OpenSubKey(personalizeKeyPath);
        var value = personalizeKey?.GetValue("AppsUseLightTheme");
        return value is int intValue && intValue != 0;
    }

    private static Color GetAccentColor()
    {
        if (!TryGetColorizationColor(out var colorizationColor))
        {
            return Color.FromRgb(103, 232, 249);
        }

        var a = (byte)((colorizationColor >> 24) & 0xff);
        var r = (byte)((colorizationColor >> 16) & 0xff);
        var g = (byte)((colorizationColor >> 8) & 0xff);
        var b = (byte)(colorizationColor & 0xff);

        return Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
    }

    private static LinearGradientBrush CreateGradientBrush(Color start, Color middle, Color end)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1)
        };

        brush.GradientStops.Add(new GradientStop(start, 0));
        brush.GradientStops.Add(new GradientStop(middle, 0.55));
        brush.GradientStops.Add(new GradientStop(end, 1));
        brush.Freeze();
        return brush;
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color Blend(Color baseColor, Color accentColor, double accentWeight)
    {
        var baseWeight = 1 - accentWeight;
        return Color.FromArgb(
            (byte)Math.Clamp((baseColor.A * baseWeight) + (accentColor.A * accentWeight), 0, 255),
            (byte)Math.Clamp((baseColor.R * baseWeight) + (accentColor.R * accentWeight), 0, 255),
            (byte)Math.Clamp((baseColor.G * baseWeight) + (accentColor.G * accentWeight), 0, 255),
            (byte)Math.Clamp((baseColor.B * baseWeight) + (accentColor.B * accentWeight), 0, 255));
    }

    private static bool TryGetColorizationColor(out uint colorizationColor)
    {
        try
        {
            _ = DwmGetColorizationColor(out colorizationColor, out _);
            return true;
        }
        catch
        {
            colorizationColor = 0;
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);
}
