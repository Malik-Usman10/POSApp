using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace POSApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static MainWindow? MainWindow { get; private set; }
        private DatabaseService? _databaseService;
        public Window Window { get; private set; }
        public List<Window> Windows { get; } = new List<Window>();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ShowErrorDialog(e.Exception);
        }

        private async void ShowErrorDialog(Exception ex)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Application Error",
                    Content = $"An error occurred: {ex.Message}\n\nPlease restart the application.",
                    CloseButtonText = "Close",
                    XamlRoot = MainWindow?.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch
            {
                // If showing dialog fails, we can't do much more
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // Initialize database first
                _databaseService = new DatabaseService();

                // Create and activate main window
                MainWindow = new MainWindow();
                Window = MainWindow;
                Windows.Add(Window);
                Window.Activate();
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
            }
        }
    }
}
