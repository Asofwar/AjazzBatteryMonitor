using System.Drawing;
using System.Reflection;

namespace AjazzBattery.App;

/// <summary>
/// Provides access to embedded application resources.
/// </summary>
public static class AppResources
{
    private static Icon? _applicationIcon;

    /// <summary>
    /// Gets the application icon loaded from embedded resources.
    /// Falls back to SystemIcons.Application if the resource is not found.
    /// </summary>
    public static Icon ApplicationIcon
    {
        get
        {
            if (_applicationIcon != null) return _applicationIcon;

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("AjazzBattery.App.Resources.AppIcon.ico");
                if (stream != null)
                {
                    _applicationIcon = new Icon(stream);
                    return _applicationIcon;
                }
            }
            catch (Exception ex)
            {
                Core.Logger.Log("ICON", $"Failed to load embedded AppIcon.ico: {ex.Message}");
            }

            _applicationIcon = SystemIcons.Application;
            return _applicationIcon;
        }
    }
}
