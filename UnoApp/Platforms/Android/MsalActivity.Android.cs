using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Identity.Client;

namespace UnoApp.Droid;

// This class is used by Android to intercept the callback from the browser and call OnActivityResult in the MainActivity.
// However, the redirect URI configured in the Azure portal is different from what the doc above states.
// See https://learn.microsoft.com/en-us/azure/developer/mobile-apps/azure-mobile-apps/quickstarts/uno/authentication
// The redirect URI is of the form "msauth://com.lakesideapps.houzlinc/..."
[Activity(
    NoHistory = true,
    LaunchMode = LaunchMode.SingleTask, 
    Exported = true)]
[IntentFilter(new[] { Intent.ActionView },
   Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
   DataHost = "com.lakesideapps.houzlinc",
   DataScheme = "msauth")]
internal class MsalActivity : BrowserTabActivity
{
}
