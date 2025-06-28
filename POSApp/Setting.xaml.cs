using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Drawing.Printing;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace POSApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Setting : Page
    {
        private const string PrinterSettingKey = "SelectedPrinter";
        public Setting()
        {
            InitializeComponent();
            LoadPrinters();
        }
        private void LoadPrinters()
        {
            PrinterComboBox.Items.Clear();

            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                PrinterComboBox.Items.Add(printer);
            }

            // Load saved printer if any
            var savedPrinter = LoadSavedPrinter();
            if (!string.IsNullOrEmpty(savedPrinter))
            {
                PrinterComboBox.SelectedItem = savedPrinter;
            }
        }

        private void SavePrinter_Click(object sender, RoutedEventArgs e)
        {
            if (PrinterComboBox.SelectedItem != null)
            {
                var printerName = PrinterComboBox.SelectedItem.ToString();
                SaveSelectedPrinter(printerName);
                StatusText.Text = $"Saved printer: {printerName}";
            }
            else
            {
                StatusText.Text = "Please select a printer.";
            }
        }

        private void SaveSelectedPrinter(string printerName)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[PrinterSettingKey] = printerName;
        }

        public static string LoadSavedPrinter()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            object value = localSettings.Values[PrinterSettingKey];
            return value?.ToString();
        }
    }
}
