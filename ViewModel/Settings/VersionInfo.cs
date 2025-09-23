using System.Diagnostics;
using System.Reflection;

#if WINDOWS
using Windows.ApplicationModel;
#endif
#if ANDROID
using Android.Content.PM;
#endif
#if IOS || MACCATALYST
using Foundation;
#endif

namespace ViewModel.Settings;

internal static class VersionInfo
{
    public static string Current
    {
        get
        {
#if WINDOWS
            // Works only for packaged apps; will throw for unpackaged
            try
            {
                var v = Package.Current.Id.Version;
                return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
            }
            catch { /* fall through */ }
#endif
#if ANDROID
            try
            {
                var ctx = global::Android.App.Application.Context;
#if ANDROID33_0_OR_GREATER
                var pi = ctx.PackageManager?.GetPackageInfo(ctx.PackageName!, PackageManager.PackageInfoFlags.Of(0.0));
#else
                var pi = ctx.PackageManager?.GetPackageInfo(ctx.PackageName!, PackageInfoFlags.MatchAll);
#endif
                var ver = pi?.VersionName;
                if (!string.IsNullOrWhiteSpace(ver)) return ver!;
            }
            catch { /* fall through */ }
#endif
#if IOS || MACCATALYST
            try
            {
                var dict = NSBundle.MainBundle.InfoDictionary;
                var shortV = dict?["CFBundleShortVersionString"]?.ToString();
                var build = dict?["CFBundleVersion"]?.ToString();
                if (!string.IsNullOrWhiteSpace(shortV))
                    return string.IsNullOrWhiteSpace(build) ? shortV! : $"{shortV}.{build}";
            }
            catch { /* fall through */ }
#endif
            return FallbackFromAssembly();
        }
    }

    public static string Current3Part
    {
        get
        {
            var full = Current;
            var parts = full.Split('.');
            return parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : full;
        }
    }

    private static string FallbackFromAssembly()
    {
        try
        {
            var asm = typeof(VersionInfo).Assembly;
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info)) return info!;

            // File version is often set even when AssemblyVersion isn’t
            var fvi = FileVersionInfo.GetVersionInfo(asm.Location).FileVersion;
            if (!string.IsNullOrWhiteSpace(fvi)) return fvi!;

            return asm.GetName().Version?.ToString() ?? "0.0.0.0";
        }
        catch
        {
            return "0.0.0.0";
        }
    }
}
