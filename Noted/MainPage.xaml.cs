namespace Noted
{
    /// <summary>
    /// The main page that hosts the Blazor WebView component.
    /// This page serves as the container for all Blazor-rendered UI content.
    /// </summary>
    /// <remarks>
    /// The actual UI is defined in Blazor components under the Components folder.
    /// This MAUI page simply provides the hosting infrastructure.
    /// </remarks>
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
    }
}
