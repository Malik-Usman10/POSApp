using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace POSApp
{
    public class Product : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private decimal _price;
        private string _imagePath;
        private string _category;

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged();
            }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProductImageSource));
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged();
            }
        }

        public BitmapImage ProductImageSource
        {
            get
            {
                if (!string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
                {
                    try
                    {
                        return new BitmapImage(new Uri(_imagePath));
                    }
                    catch { }
                }
                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class OrderItem : INotifyPropertyChanged
    {
        public int OrderId { get; set; }   // FK to Orders table
        public int ProductId { get; set; } // FK to Products table

        public int Id { get; set; }
        private decimal _unitPrice;
        private Product _product;
        private int _quantity;

        public Product Product
        {
            get => _product;
            set
            {
                _product = value;
                if (_product != null)
                {
                    ProductId = _product.Id;
                    UnitPrice = _product.Price;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
            }
        }
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                _unitPrice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Total));
            }
        }
        public decimal Total => Quantity * UnitPrice;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Model for the Orders
    public class Order : INotifyPropertyChanged
    {
        private bool _isPaid;
        private int _id;
        private DateTime _orderDate;
        private decimal _totalAmount;
        private String? _name;
        private ObservableCollection<OrderItem> _items = new();


        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public String? Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        public DateTime OrderDate
        {
            get => _orderDate;

            set
            {
                _orderDate = value;
                OnPropertyChanged();
            }
        }
        public bool IsPaid
        {
            get => _isPaid;
            set
            {
                _isPaid = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                _totalAmount = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<OrderItem> Items
        {
            get => _items;
            set
            {
                _items = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemCount));
            }
        }

        //Convert Date into dd/MM/yyyy format for display
        public string OrderDateFormatted => _orderDate.ToString("dd/MM/yyyy");


        public int ItemCount => Items?.Count ?? 0;
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
