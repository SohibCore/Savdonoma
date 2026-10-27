using System.Reflection;
using Savdonoma.Core.Enums;
using System.ComponentModel;
using Savdonoma.Data.Services.Sales;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

namespace Savdonoma.Data.Services.Reports
{
    public class SaleRowDto : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";
        public DateTime Time { get; set; }          
        public PaymentMethod PaymentMethod { get; set; }
        public long TotalAmount { get; set; }
        public SaleStatus Status { get; set; }
        public int? UserId { get; set; }
        public string StatusDisplay => typeof(SaleStatus)
            .GetField(Status.ToString())?
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName() ?? Status.ToString();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value)
                    return;

                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        private SaleDetailDto? _detail;
        public SaleDetailDto? Detail
        {
            get => _detail;
            set
            {
                if (ReferenceEquals(_detail, value))
                    return;

                _detail = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoadingDetail;
        public bool IsLoadingDetail
        {
            get => _isLoadingDetail;
            set
            {
                if (_isLoadingDetail == value)
                    return;

                _isLoadingDetail = value;
                OnPropertyChanged();
            }
        }

        private string _detailError = "";
        public string DetailError
        {
            get => _detailError;
            set
            {
                if (_detailError == value)
                    return;

                _detailError = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
