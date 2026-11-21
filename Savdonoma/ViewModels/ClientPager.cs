using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Savdonoma.ViewModels
{
    // Xotiradagi ro'yxatni sahifalarga bo'lib ko'rsatadi (masalan, Dashboard jadvallari uchun)
    public partial class ClientPager<T> : ObservableObject
    {
        private List<T> _all = new();

        public ClientPager(int pageSize)
        {
            PageSize = Math.Max(1, pageSize);
        }

        public int PageSize { get; }

        [ObservableProperty] private ObservableCollection<T> items = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageText), nameof(CanGoPrevious), nameof(CanGoNext))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand), nameof(NextPageCommand))]
        private int page = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageText), nameof(CanGoPrevious), nameof(CanGoNext), nameof(HasMultiplePages))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand), nameof(NextPageCommand))]
        private int pageCount = 1;

        public int TotalCount => _all.Count;
        public bool HasMultiplePages => PageCount > 1;
        public bool CanGoPrevious => Page > 1;
        public bool CanGoNext => Page < PageCount;
        public string PageText => $"{Page} / {PageCount}";

        // Yangi ma'lumot kelganda birinchi sahifaga qaytadi
        public void SetSource(IEnumerable<T> source)
        {
            _all = source.ToList();
            PageCount = Math.Max(1, (int)Math.Ceiling(_all.Count / (double)PageSize));
            OnPropertyChanged(nameof(TotalCount));
            if (Page == 1)
                Refresh();
            else
                Page = 1;
        }

        partial void OnPageChanged(int value) => Refresh();

        private void Refresh()
        {
            Items = new ObservableCollection<T>(_all.Skip((Page - 1) * PageSize).Take(PageSize));
        }

        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private void PreviousPage() => Page--;

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private void NextPage() => Page++;
    }
}
