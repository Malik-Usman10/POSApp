using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace POSApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProductDialog : ContentDialog
    {
        public Product Product { get; private set; }
        private readonly bool _isEditMode;

        public ProductDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            SetupDialog();
        }

        public ProductDialog(Product product) : this()
        {
            _isEditMode = true;
            Product = product;
            this.Loaded += ProductDialog_Loaded;
            SetupDialog();
        }

        private void SetupDialog()
        {
            // Configure dialog properties
            this.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            this.Title = _isEditMode ? "Edit Product" : "New Product";
            this.PrimaryButtonText = "Save";
            this.CloseButtonText = "Cancel";
            this.DefaultButton = ContentDialogButton.Primary;

            // Register event handlers
            this.PrimaryButtonClick += ProductDialog_PrimaryButtonClick;
            this.Closing += ProductDialog_Closing;
        }

        private void ProductDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= ProductDialog_Loaded;
            LoadProductData();
        }

        private async void LoadProductData()
        {
            try
            {
                if (Product != null)
                {
                    ProductNameTextBox.Text = Product.Name ?? string.Empty;
                    ProductPriceNumberBox.Value = (double)Product.Price;
                    CategoryComboBox.SelectedItem = CategoryComboBox.Items
                        .OfType<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content.ToString() == Product.Category);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Dialog Error", $"An error occurred: {ex.Message}");
            }
        }

        private void ProductDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text))
            {
                args.Cancel = true;
                ShowValidationError("Please enter a product name.");
                return;
            }

            if (ProductPriceNumberBox.Value <= 0)
            {
                args.Cancel = true;
                ShowValidationError("Please enter a valid price greater than 0.");
                return;
            }

            if (CategoryComboBox.SelectedItem == null)
            {
                args.Cancel = true;
                ShowValidationError("Please Select a product category.");
                return;
            }

            // Create or update product
            if (_isEditMode && Product != null)
            {
                Product.Name = ProductNameTextBox.Text.Trim();
                Product.Price = (decimal)ProductPriceNumberBox.Value;
                Product.Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            }
            else
            {
                Product = new Product
                {
                    Name = ProductNameTextBox.Text.Trim(),
                    Price = (decimal)ProductPriceNumberBox.Value,
                    Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                };
            }
        }

        private void ProductDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
        }
        private async void ShowValidationError(string message)
        {
            await ShowErrorDialog("Validation Error", message);
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
