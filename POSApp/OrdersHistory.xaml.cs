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
            _paidOrders.Clear();
            _unpaidOrders.Clear();

            var allOrders = await _databaseService.LoadOrdersByDateAsync(selectedDate);

            foreach (var order in allOrders)
            {
                if (order.IsPaid)
                    _paidOrders.Add(order);
                else
                    _unpaidOrders.Add(order);
            }

            PaidOrdersListView.ItemsSource = _paidOrders;
            UnpaidOrdersListView.ItemsSource = _unpaidOrders;

            var paidTotal = _paidOrders.Sum(o => o.TotalAmount);
            var unpaidTotal = _unpaidOrders.Sum(o => o.TotalAmount);

            PaidTotalRun.Text = paidTotal.ToString("F2");
            UnpaidTotalRun.Text = unpaidTotal.ToString("F2");
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
