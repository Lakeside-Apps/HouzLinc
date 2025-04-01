using Android.App;
using Android.OS;
using Android.Views;
using Microsoft.Identity.Client;

namespace UnoApp.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustResize | SoftInput.StateVisible
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
    }

    protected override void OnCreate(Bundle bundle)
    {
        base.OnCreate(bundle);
        
        // Add Android specific code that needs to execute on startup here
    }
}
