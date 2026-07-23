using System.Drawing;

namespace AjazzBattery.App.UI.Theme;

public sealed record ThemePalette(
    Color Background,
    Color Surface,
    Color SurfaceElevated,
    Color Border,
    Color PrimaryText,
    Color SecondaryText,
    Color MutedText,
    Color Accent,
    Color Success,
    Color Warning,
    Color Danger,
    Color Critical
)
{
    public static ThemePalette Dark { get; } = new(
        Background: ColorTranslator.FromHtml("#0F1117"),
        Surface: ColorTranslator.FromHtml("#171A23"),
        SurfaceElevated: ColorTranslator.FromHtml("#1D212C"),
        Border: ColorTranslator.FromHtml("#2A303C"),
        PrimaryText: ColorTranslator.FromHtml("#F4F6FA"),
        SecondaryText: ColorTranslator.FromHtml("#AAB2C0"),
        MutedText: ColorTranslator.FromHtml("#788293"),
        Accent: ColorTranslator.FromHtml("#7C8CFF"),
        Success: ColorTranslator.FromHtml("#48D17A"),
        Warning: ColorTranslator.FromHtml("#FFB84D"),
        Danger: ColorTranslator.FromHtml("#FF5D6C"),
        Critical: ColorTranslator.FromHtml("#E5394F")
    );

    public static ThemePalette Light { get; } = new(
        Background: ColorTranslator.FromHtml("#F4F6FA"),
        Surface: ColorTranslator.FromHtml("#FFFFFF"),
        SurfaceElevated: ColorTranslator.FromHtml("#F8F9FC"),
        Border: ColorTranslator.FromHtml("#DCE1E8"),
        PrimaryText: ColorTranslator.FromHtml("#171A23"),
        SecondaryText: ColorTranslator.FromHtml("#596273"),
        MutedText: ColorTranslator.FromHtml("#808A9A"),
        Accent: ColorTranslator.FromHtml("#5869E8"),
        Success: ColorTranslator.FromHtml("#209B55"),
        Warning: ColorTranslator.FromHtml("#D98600"),
        Danger: ColorTranslator.FromHtml("#D93D50"),
        Critical: ColorTranslator.FromHtml("#BE1E35")
    );
}

public enum AppThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}
