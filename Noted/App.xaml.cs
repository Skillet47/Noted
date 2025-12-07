namespace Noted
{
    /// <summary>
    /// The main application class for the Noted MAUI app.
    /// Initializes the application and creates the main window.
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates the main application window with the Blazor WebView content.
        /// </summary>
        /// <param name="activationState">The activation state from the platform.</param>
        /// <returns>The main application window.</returns>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Noted" };
        }
    }
}
