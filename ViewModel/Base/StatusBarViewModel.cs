using Common;
using ViewModel.Settings;

namespace ViewModel.Base;
public class StatusBarViewModel : PageViewModel
{
    public static StatusBarViewModel Instance => instance ??= new();
    private static StatusBarViewModel? instance;

    public override void ViewNavigatedTo()
    {
        // For StatusTextLogger to show status text and user action requests on the status bar
        StatusTextLogger.UpdateStatusTextCallback = (text, isUserActionRequest) =>
        {
            StatusText = text;
            IsUserActionRequest = isUserActionRequest;
        };
    }

    public override void ViewNavigatedFrom()
    {
    }

    // Text to show in the status bar
    public string StatusText
    {
        get => statusText;
        set
        {
            statusText = value;
            OnPropertyChanged();
        }
    }
    string statusText = string.Empty;

    // Whether the status text is a user action request
    public bool IsUserActionRequest
    {
        get => isUserActionRequest;
        set
        {
            isUserActionRequest = value;
            OnPropertyChanged();
        }
    }
    bool isUserActionRequest = false;
}
