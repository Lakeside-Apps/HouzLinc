using Windows.Foundation;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using ViewModel.Settings;
using ViewModel.Console;
using System.Runtime.InteropServices;

namespace UnoApp;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        // Check runtime dependencies before initialization
        CheckRuntimeDependencies();
        
        this.InitializeComponent();
        InitializeLogging();
    }

    private static void CheckRuntimeDependencies()
    {
#if WINDOWS
        try
        {
            // Try to access a Windows App SDK API to verify it's available
            // This will throw if Windows App SDK is not installed
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            
            // Additional check: Try to create a basic WinUI component
            var testWindow = new Microsoft.UI.Xaml.Window();
            testWindow = null; // Just testing availability
        }
        catch (System.IO.FileNotFoundException ex) when (ex.Message.Contains("WinRT"))
        {
            ShowRuntimeError("Windows App SDK", 
                "Windows App SDK runtime is not installed.", ex);
            Environment.Exit(1);
        }
        catch (System.DllNotFoundException ex)
        {
            ShowRuntimeError("Windows App SDK", 
                "Required Windows App SDK libraries are missing.", ex);
            Environment.Exit(1);
        }
        catch (Exception ex) when (ex.HResult == unchecked((int)0x80040154)) // REGDB_E_CLASSNOTREG
        {
            ShowRuntimeError("Windows App SDK", 
                "Windows App SDK components are not properly registered.", ex);
            Environment.Exit(1);
        }

        // Check .NET Desktop Runtime
        try
        {
            var netVersion = Environment.Version;
            if (true) // netVersion.Major < 9
            {
                ShowRuntimeError(".NET Runtime", 
                    $"This application requires .NET 9 or later. Found: .NET {netVersion}", null);
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            ShowRuntimeError(".NET Runtime", 
                ".NET Desktop Runtime check failed.", ex);
            Environment.Exit(1);
        }
#endif
    }

    private static void ShowRuntimeError(string component, string message, Exception? ex)
    {
        var fullMessage = $"HouzLinc cannot start because {component} is not available.\n\n" +
                         $"{message}\n\n" +
                         $"Please install the required components:\n" +
                         $"• .NET 9 Desktop Runtime: https://dotnet.microsoft.com/download/dotnet/9.0\n" +
                         $"• Windows App SDK: https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads\n\n";

        if (ex != null)
        {
            fullMessage += $"Technical details: {ex.Message}";
        }

        // Use native Windows MessageBox to avoid WinUI dependencies
        try
        {
            MessageBox(IntPtr.Zero, fullMessage, "HouzLinc - Missing Runtime", 0x10); // MB_ICONERROR
        }
        catch
        {
            // Fallback to console if MessageBox fails
            Console.WriteLine(fullMessage);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    // Native Windows MessageBox for error display
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    public static Window MainWindow { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Use the configuration service that reads from appsettings.json
        // to get the MSAL configuration
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => {
                host.UseConfiguration(configure: configBuilder =>
                    configBuilder
                        // Load configuration information from appsettings.json
                        .EmbeddedSource<App>()
                        .Section<Msal>()
                        .Section<FeatureFlags>()
                );
                // Register a service to get the configuration
                host.ConfigureServices((context, services) => 
                    // Register view model
                    services
                        .AddTransient<MsalConfiguration>()
                        .AddTransient<FeatureFlagsConfiguration>()
                );
            });

        var host = appBuilder.Build();

        // Get MSAL configuration using the service registered above
        var msalConfiguration = host.Services.GetRequiredService<MsalConfiguration>();
        if (msalConfiguration != null)
        {
            OneDrive.Instance.SetMsalConfiguration(msalConfiguration);
        }

        // Get the feature flags configuration using the service registered above
        var featureFlagsConfiguration = host.Services.GetRequiredService<FeatureFlagsConfiguration>();
        if (featureFlagsConfiguration != null)
        {
            SettingsViewModel.Instance.SetFeatureFlagsConfiguration(featureFlagsConfiguration);
        }

        MainWindow = appBuilder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif

#if WINDOWS
        // Set window size from user preference
        // TODO: figure a way to make this work on other platforms
        Size? windowSize = SettingsStore.ReadLastUsedValue("WindowSize") as Size?;
        if (windowSize == null)
        {
            // Use default size
            windowSize = new Size(1600, 1000);
        }

        Windows.Graphics.SizeInt32 sizeInt32 = new Windows.Graphics.SizeInt32(
            Convert.ToInt32(windowSize?.Width),
            Convert.ToInt32(windowSize?.Height)
        );
        appWindow.Resize(sizeInt32);

        // Make sure to record window size changes
        MainWindow.SizeChanged += (s, e) =>
        {
            // To avoid calling the SettingStore too frequently while the user is resizing the window,
            // we record the size change after a delay of 500ms using a UI thread timer
            if (timer == null)
            {
                timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500);
                timer.IsRepeating = false;
                timer.Tick += (s, e) =>
                {
                    var size = new Size(appWindow.Size.Width, appWindow.Size.Height);
                    SettingsStore.WriteLastUsedValue("WindowSize", size);
                };
            }
            timer.Start();
        };

        // Set the window icon using AppWindow (Uno 6.2+ approach)
        try 
        {
            appWindow.SetIcon("Assets/appicon.ico"); // Update path to your icon file
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the app startup
            System.Diagnostics.Debug.WriteLine($"Failed to set window icon: {ex.Message}");
        }
#endif

        // TODO: Uno Single Project: This was the original code
        AppShell? shell = MainWindow.Content as AppShell;

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (shell == null)
        {
            // Create a AppShell to act as the navigation context and navigate to the first page
            shell = new AppShell();

            // Set the default language
            //shell.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

            // Set-up custom title bar
            shell.SetTitleBar();

            // Place our app shell in the current Window
            MainWindow.Content = shell;
        }

        // Ensure the current window is active
        MainWindow.Activate();
    }


#if WINDOWS
    private static DispatcherQueueTimer? timer = null;

    // Helper to get the Win32 window
    private AppWindow appWindow
    {
        get
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(windowId);
        }
    }
#endif


    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#elif WINDOWS
            // For the Visual Studio Debug Output we use the Microsoft provider
            // but keeping a custom implementation in DebugLofferProvider for reference.
            //builder.AddProvider(new DebugLoggerProvider());
            builder.AddDebug();
#else
            //builder.AddProvider(new DebugLoggerProvider());
            builder.AddConsole();
#endif
            builder.AddProvider(new StatusTextLoggerProvider());
            builder.AddProvider(new ConsoleLoggerProvider());

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Warning);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

#if DEBUG
            builder.AddFilter("HouzLinc", LogLevel.Debug);
#else   
            // To see our own log in status bar and Insteon Console
            builder.AddFilter("HouzLinc", LogLevel.Information);
#endif

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
    }
}
