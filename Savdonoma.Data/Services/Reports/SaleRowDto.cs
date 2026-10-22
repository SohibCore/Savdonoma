using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Reflection;
using Savdonoma.Core.Enums;
using System.Runtime.CompilerServices;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
