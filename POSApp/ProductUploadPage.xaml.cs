using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace POSApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProductUploadPage : Page
    {
        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _filteredProducts;
        private DatabaseService _databaseService;
        public ProductUploadPage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _products = new ObservableCollection<Product>();
            _filteredProducts = new ObservableCollection<Product>();

            ProductsListView.ItemsSource = _filteredProducts;

            Loaded += ProductUploadPage_Loaded;
        }
        private async void ProductUploadPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProducts();
        }

        private async Task LoadProducts()
        {
            try
            {
                var products = await _databaseService.GetProductsAsync();
                _products.Clear();
                _filteredProducts.Clear();

                foreach (var product in products)
                {
                    _products.Add(product);
                    _filteredProducts.Add(product);
                }
            }
            catch (Exception ex)
            {
                // Handle error
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to load products: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = ProductSearchBox.Text.ToLower();
            _filteredProducts.Clear();

            var filtered = string.IsNullOrEmpty(searchText)
                ? _products
                : _products.Where(p => p.Name.ToLower().Contains(searchText));

            foreach (var product in filtered)
            {
                _filteredProducts.Add(product);
            }
        }

        private async void CreateProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ProductDialog();
                dialog.XamlRoot = this.XamlRoot;

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && dialog.Product != null)
                {
                    try
                    {
                        await _databaseService.InsertProductAsync(dialog.Product);
                        await LoadProducts();
                    }
                    catch (Exception ex)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = $"Failed to create product: {ex.Message}",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Unexpected Error",
                    Content = $"An unexpected error occurred: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Product product)
                {
                    var dialog = new ProductDialog(product);
                    dialog.XamlRoot = this.XamlRoot;

                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary && dialog.Product != null)
                    {
                        try
                        {
                            await _databaseService.UpdateProductAsync(dialog.Product);
                            await LoadProducts();
                        }
                        catch (Exception ex)
                        {
                            var errorDialog = new ContentDialog
                            {
                                Title = "Error",
                                Content = $"Failed to update product: {ex.Message}",
                                CloseButtonText = "OK",
                                XamlRoot = this.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Unexpected Error",
                    Content = $"An unexpected error occurred: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Product product)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Confirm Delete",
                    Content = $"Are you sure you want to delete '{product.Name}'?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        await _databaseService.DeleteProductAsync(product.Id);
                        await LoadProducts();
                    }
                    catch (Exception ex)
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = $"Failed to delete product: {ex.Message}",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await errorDialog.ShowAsync();
                    }
                }
            }
        }
    }
}
