using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace POSApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _filteredProducts;
        private ObservableCollection<OrderItem> _orderItems;
        private DatabaseService _databaseService;
        public HomePage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _products = new ObservableCollection<Product>();
            _filteredProducts = new ObservableCollection<Product>();
            _orderItems = new ObservableCollection<OrderItem>();

            OrderItemsListView.ItemsSource = _orderItems;
            ProductsGridView.ItemsSource = _filteredProducts;

            Loaded += HomePage_Loaded;
        }
        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
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

        private void ProductsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Product product)
            {
                AddToOrder(product);
            }
        }

        private void AddToOrder(Product product)
        {
            var existingItem = _orderItems.FirstOrDefault(item => item.Product.Id == product.Id);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                _orderItems.Add(new OrderItem { Product = product, Quantity = 1 });
            }

            UpdateSubtotal();
        }

        private void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OrderItem orderItem)
            {
                orderItem.Quantity++;
                UpdateSubtotal();
            }
        }

        private void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OrderItem orderItem)
            {
                if (orderItem.Quantity > 1)
                {
                    orderItem.Quantity--;
                }
                else
                {
                    _orderItems.Remove(orderItem);
                }
                UpdateSubtotal();
            }
        }

        private void RemoveOrderItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OrderItem orderItem)
            {
                _orderItems.Remove(orderItem);
                UpdateSubtotal();
            }
        }

        private void UpdateSubtotal()
        {
            var subtotal = _orderItems.Sum(item => item.Total);
            SubtotalRun.Text = subtotal.ToString("F2");
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_orderItems.Any())
            {
                var dialog = new ContentDialog
                {
                    Title = "No Items",
                    Content = "Please add items to the order before printing.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            await PrintOrderSlip();
        }

        private async Task PrintOrderSlip()
        {
            var receipt = GenerateReceiptContent();
            string printerName = Setting.LoadSavedPrinter();

            if (string.IsNullOrEmpty(printerName))
            {
                // Optional: show dialog if no printer selected
                var dialog = new ContentDialog
                {
                    Title = "No Printer Selected",
                    Content = "Please select a printer in Settings before printing.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            RawPrinterHelper.SendStringToPrinter(printerName, receipt);
        }
        private string GenerateReceiptContent()
        {
            var receipt = new StringBuilder();

            // Header
            receipt.AppendLine("      WAQAS BARBEQUE & TIKKA HOUSE      ");
            receipt.AppendLine("       --------------------------       ");
            receipt.AppendLine($"Date: {DateTime.Now:dd MMM yyyy}    Time: {DateTime.Now:hh:mm tt}");
            receipt.AppendLine("       --------------------------      ");
            receipt.AppendLine("               ORDER RECEIPT            ");
            receipt.AppendLine("----------------------------------------");

            // Items
            decimal subtotal = 0;
            foreach (var item in _orderItems)
            {
                var itemTotal = item.Total;
                subtotal += itemTotal;

                var productName = item.Product.Name.Length > 25
                    ? item.Product.Name.Substring(0, 22) + "..."
                    : item.Product.Name;

                string itemLine = $"{productName,-25} {item.Quantity} x {item.Product.Price,6:F2}";
                string totalLine = $"{"",25}     PKR {itemTotal,8:F2}";

                receipt.AppendLine(itemLine);
                receipt.AppendLine(totalLine);
                receipt.AppendLine();
            }

            // Summary
            receipt.AppendLine("----------------------------------------");
            receipt.AppendLine("               BILL SUMMARY             ");
            receipt.AppendLine("----------------------------------------");
            receipt.AppendLine($"Subtotal:                    PKR {subtotal,8:F2}");

            // You can add tax, discount, or total if needed
            // receipt.AppendLine($"Tax (5%):                    PKR {tax,8:F2}");
            // receipt.AppendLine($"Total:                       PKR {total,8:F2}");

            receipt.AppendLine();

            return receipt.ToString();

        }

        private async void PreviewReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_orderItems.Any())
            {
                var dialogTwo = new ContentDialog
                {
                    Title = "No Items",
                    Content = "Please add items to the order before previewing.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialogTwo.ShowAsync();
                return;
            }

            var receiptContent = GenerateReceiptContent();
            var contentPanel = new StackPanel();

            var scrollViewer = new ScrollViewer
            {
                Content = new TextBlock
                {
                    Text = receiptContent,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    FontSize = 14,
                    TextWrapping = TextWrapping.NoWrap,
                    Margin = new Thickness(10)
                },
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = 400,
                MaxWidth = 500
            };
            contentPanel.Children.Add(scrollViewer);

            var dialog = new ContentDialog
            {
                Title = "Receipt Preview",
                Content = contentPanel,
                PrimaryButtonText = "Print",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await PrintOrderSlip();
            }
        }
    }
}
