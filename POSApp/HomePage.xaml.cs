using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Printing;
using Windows.Foundation;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.UI;

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
        private Button _selectedCategoryButton;
        public HomePage()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            _products = new ObservableCollection<Product>();
            _filteredProducts = new ObservableCollection<Product>();
            _orderItems = new ObservableCollection<OrderItem>();


            OrderItemsListView.ItemsSource = _orderItems;
            ProductsListView.ItemsSource = _filteredProducts;

            Loaded += HomePage_Loaded;
        }
        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProducts();
            CategoryButton_Click(KarahiButton, null);
        }

        private async Task LoadProducts()
        {
            try
            {
                var products = await _databaseService.GetProductsAsync();
                _products.Clear();
                _filteredProducts.Clear();

                foreach (var product in products)
                    _products.Add(product);

                foreach (var product in products.Where(p => p.Category == "Karahi"))
                    _filteredProducts.Add(product);
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

        // Search Product on TextChange in ProductSearchBox if text field is Null or Empty then show only products of selected category
        private void ProductSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = ProductSearchBox.Text.ToLower();
            _filteredProducts.Clear();
            if (string.IsNullOrEmpty(searchText))
            {
                // If search text is empty, show products of the selected category
                foreach (var product in _products.Where(p => p.Category == _selectedCategoryButton?.Tag as string))
                {
                    _filteredProducts.Add(product);
                }
            }
            else
            {
                // Filter products based on search text
                foreach (var product in _products.Where(p => p.Name.ToLower().Contains(searchText)))
                {
                    _filteredProducts.Add(product);
                }
            }
        }

        // Filter Product based on Category Buttons Click
        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string category)
            {
                if (_selectedCategoryButton != null)
                {
                    _selectedCategoryButton.ClearValue(Button.BackgroundProperty);
                    _selectedCategoryButton.ClearValue(Button.ForegroundProperty);
                    _selectedCategoryButton.ClearValue(Button.BorderBrushProperty);
                }

                // Highlight current
                button.Background = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
                button.Foreground = new SolidColorBrush(Colors.White);
                button.BorderBrush = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);

                _selectedCategoryButton = button;
                FilterProductsByCategory(category);
            }
        }
        private void FilterProductsByCategory(string category)
        {
            _filteredProducts.Clear();
            foreach (var product in _products.Where(p => p.Category == category))
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
                var newItem = new OrderItem { Product = product, Quantity = 1 };

                // Find the index where the product should be inserted (high to low unit price)
                int insertIndex = 0;
                while (insertIndex < _orderItems.Count && _orderItems[insertIndex].Product.Price >= product.Price)
                {
                    insertIndex++;
                }

                _orderItems.Insert(insertIndex, newItem);

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

            // Prepare the Order
            var order = new Order
            {
                OrderDate = DateTime.Now,
                IsPaid = PaidCheckbox.IsChecked == true,
                TotalAmount = _orderItems.Sum(item => item.Total),
                Items = new ObservableCollection<OrderItem>() // make sure it's initialized
            };

            foreach (var item in _orderItems)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                });
            }

            if (!order.IsPaid)
            {
                TextBox nameTextBox = new TextBox
                {
                    PlaceholderText = "Enter Customer Name",
                };

                var namedialog = new ContentDialog
                {
                    Title = "Customer Name",
                    Content = nameTextBox,
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };
                var result = await namedialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    string customerName = nameTextBox.Text.Trim();

                    if (string.IsNullOrEmpty(customerName))
                    {
                        var warningDialog = new ContentDialog
                        {
                            Title = "Invalid Name",
                            Content = "Please enter a valid customer name.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await warningDialog.ShowAsync();
                        return;
                    }
                    order.Name = customerName;
                }
                else
                {
                    return; // User cancelled, do not proceed with printing
                }
            }

            //Try to Print the Order Slip
            bool printSuccess = await PrintOrderSlip();


            if (printSuccess)
            {
                await _databaseService.InsertOrderAsync(order);
                _orderItems.Clear(); // Clear the order items after printing
                UpdateSubtotal(); // Reset subtotal display
                PaidCheckbox.IsChecked = true; // Reset paid checkbox

            }
        }

        private async Task<bool> PrintOrderSlip()
        {
            try
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
                    return false;
                }

                RawPrinterHelper.SendStringToPrinter(printerName, receipt);

                return true;
            }
            catch (Exception ex)
            {
                // Handle printing error
                var dialog = new ContentDialog
                {
                    Title = "Printing Error",
                    Content = $"Failed to print receipt: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return false;
            }
        }
        private string GenerateReceiptContent()
        {
            var receipt = new StringBuilder();

            // Header
            receipt.AppendLine("      WAQAS BARBEQUE & TIKKA HOUSE      ");
            receipt.AppendLine("         Mobile No: 03024700067       ");
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
                string totalLine = $"{"",20}     PKR {itemTotal,8:F2}";

                receipt.AppendLine(itemLine);
                receipt.AppendLine(totalLine);
                receipt.AppendLine();
            }

            // Summary
            receipt.AppendLine("----------------------------------------");
            receipt.AppendLine("               BILL SUMMARY             ");
            receipt.AppendLine("----------------------------------------");
            receipt.AppendLine($"Subtotal:                    PKR {subtotal,8:F2}");

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
