using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace POSApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OrdersHistory : Page
    {
        private DatabaseService _databaseService;
        private readonly ObservableCollection<Order> _paidOrders = new();
        private readonly ObservableCollection<Order> _unpaidOrders = new();
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.Now;


        public OrdersHistory()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            OrdersDatePicker.Date = DateTimeOffset.Now;
            LoadOrdersForSelectedDate(DateTime.Now);

        }

        private async void LoadOrdersForSelectedDate(DateTime selectedDate)
        {
            try
            {
                // Clear old data
                _paidOrders.Clear();
                _unpaidOrders.Clear();

                // Get all orders for the selected date
                var allOrders = await _databaseService.LoadOrdersByDateAsync(selectedDate);

                // Separate into paid/unpaid lists
                foreach (var order in allOrders)
                {
                    if (order.IsPaid)
                        _paidOrders.Add(order);
                    else
                        _unpaidOrders.Add(order);
                }

                // Bind only once after filling collections
                PaidOrdersListView.ItemsSource = _paidOrders;
                UnpaidOrdersListView.ItemsSource = _unpaidOrders;

                // Update totals
                PaidTotalRun.Text = _paidOrders.Sum(o => o.TotalAmount).ToString("F2");
                UnpaidTotalRun.Text = _unpaidOrders.Sum(o => o.TotalAmount).ToString("F2");
            }
            catch (Exception ex)
            {
                // Optional: show an error dialog or log
                Console.WriteLine($"Error loading orders: {ex.Message}");
            }
        }

        private async void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {
            if (!(sender.DataContext is Order order))
                return;

            // If already loaded, do nothing
            if (order.Items != null && order.Items.Count > 0)
                return;

            // Optionally show a small loading indicator here

            // Fetch items from DB
            var items = await _databaseService.GetOrderItemsAsync(order.Id);

            // If Items was null for some reason, create it once
            if (order.Items == null)
                order.Items = new ObservableCollection<OrderItem>();

            // Add to the existing collection (this notifies the UI)
            foreach (var it in items)
                order.Items.Add(it);

            // Optional: debug info
            System.Diagnostics.Debug.WriteLine($"Loaded {items.Count} items for order {order.Id}");
        }

        private void OrdersDatePicker_SelectedDateChanged(DatePicker sender, DatePickerSelectedValueChangedEventArgs args)
        {
            if (args.NewDate != null)
            {
                OrderDate = args.NewDate.Value.Date;
                LoadOrdersForSelectedDate(OrderDate.Date); // Or whatever your logic is
            }
        }
    }
}
