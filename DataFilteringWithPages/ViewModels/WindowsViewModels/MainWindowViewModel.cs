using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataFilteringWithPages.Models;

namespace DataFilteringWithPages.ViewModels.WindowsViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private string _searchQuery = "";
        private int _currentPage = 1;
        private int _currentPageFirstItemIndex;
        private int _pagesCount;
        private int _totalItemsCount;
        private int _onePageItemsCount = 10;
        private int _filteredItemsCount;
        private int _currentPageLastItemIndex;
        private string _onePageItemsCountSelectedItem;

        /// <summary>
        /// Все пользователи.
        /// </summary>
        private List<User> Users { get; set; }

        /// <summary>
        /// Отфильтрованные пользователи (соответствующие поисковому запросу).
        /// </summary>
        private List<User> FilteredUsers { get; set; }

        /// <summary>
        /// Пользователи, отображаемые на текущей странице.
        /// </summary>
        public ObservableCollection<User> CurrentPageUsers { get; set; } = new();

        /// <summary>
        /// Поисковый запрос по фамилии и имени.
        /// </summary>
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                UpdateData();
            }
        }

        public MainWindowViewModel()
        {
            OnePageItemsCountSelectedItem = OnePageItemsCountsList.Last();
            LoadData();
        }

        /// <summary>
        /// Номер текущей страницы (начиная с 1).
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (value == _currentPage) return;
                _currentPage = value;
                OnPropertyChanged();

            }
        }

        /// <summary>
        /// Количество страниц. Вычисляется, исходя из количества отфильтрованных записей.
        /// </summary>
        public int PagesCount
        {
            get => _pagesCount;
            set
            {
                if (value == _pagesCount) return;
                _pagesCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Индекс первой записи на текущей странице (начиная с 1).
        /// </summary>
        public int CurrentPageFirstItemIndex
        {
            get => _currentPageFirstItemIndex;
            set
            {
                if (value == _currentPageFirstItemIndex) return;
                _currentPageFirstItemIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PagesString));
            }
        }

        /// <summary>
        /// Индекс последней записи на текущей странице.
        /// </summary>
        public int CurrentPageLastItemIndex
        {
            get => _currentPageLastItemIndex;
            set
            {
                if (value == _currentPageLastItemIndex) return;
                _currentPageLastItemIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PagesString));
            }
        }

        /// <summary>
        /// Количество записей на одной странице. Равно -1, если нужно отображать все записи.
        /// </summary>
        public int OnePageItemsCount
        {
            get => _onePageItemsCount;
            set
            {
                if (value == _onePageItemsCount) return;
                _onePageItemsCount = value;
                OnPropertyChanged();
                UpdatePageValues();
                UpdateData();
            }
        }

        /// <summary>
        /// Количество всех записей в БД.
        /// </summary>
        public int TotalItemsCount
        {
            get => _totalItemsCount;
            set
            {
                if (value == _totalItemsCount) return;
                _totalItemsCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Количество отфильтрованных записей.
        /// </summary>
        public int FilteredItemsCount
        {
            get => _filteredItemsCount;
            set
            {
                if (value == _filteredItemsCount) return;
                _filteredItemsCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Индекс первой и последней записи на странице. Используется для отображения.
        /// </summary>
        public string PagesString
        {
            get
            {
                var firstPage = CurrentPageFirstItemIndex;
                var lastPage = CurrentPageLastItemIndex;
                return lastPage == 0 
                    ? "0" 
                    : firstPage == lastPage
                        ? firstPage.ToString()
                        : $"{firstPage}-{lastPage}";
            }
        }

        /// <summary>
        /// Команда перехода на предыдущую страницу.
        /// </summary>
        public Command PreviousPageCommand => new(o =>
        {
            ChangePage(-1);
        }, o => CurrentPage > 1);

        /// <summary>
        /// Команда перехода на следующую страницу.
        /// </summary>
        public Command NextPageCommand => new(o =>
        {
            ChangePage(1);
        }, o => CurrentPage < PagesCount);

        /// <summary>
        /// Смена страницы.
        /// </summary>
        /// <param name="increment">Значение, на которое изменяется номер страницы.</param>
        private void ChangePage(int increment)
        {
            CurrentPage += increment;
            UpdatePageValues();
            UpdateData();
        }

        /// <summary>
        /// Отображаемые варианты количества записей на одной странице.
        /// </summary>
        public List<string> OnePageItemsCountsList { get; set; } = new()
        {
            "10",
            "50",
            "200",
            "Все"
        };

        /// <summary>
        /// Выбранный элемент коллекции OnePageItemsCountsList.
        /// </summary>
        public string OnePageItemsCountSelectedItem
        {
            get => _onePageItemsCountSelectedItem;
            set
            {
                if (value == _onePageItemsCountSelectedItem) return;
                _onePageItemsCountSelectedItem = value;
                if (int.TryParse(value, out var count))
                {
                    OnePageItemsCount = count;
                }
                else
                {
                    OnePageItemsCount = -1;
                }
            }
        }

        /// <summary>
        /// Загрузка данных из БД.
        /// </summary>
        private async void LoadData()
        {
            using var db = new AppContext();
            Users = await db.Users.ToListAsync();
            FilteredUsers = Users;

            UpdateData();
        }

        /// <summary>
        /// Обновление отображаемых данных.
        /// </summary>
        private async void UpdateData()
        {
            if (FilteredUsers == null) return;

            //Нужен для выполнения операций в главном потоке (?).
            var synchronizationContext = SynchronizationContext.Current;

            //Фильтрация пользователей.
            var users = Users.Where(user =>
            {
                var query = SearchQuery == null 
                    ? ""
                    : SearchQuery.ToLower().Trim();
                var fullNameMatch = string.IsNullOrWhiteSpace(SearchQuery) ||
                                    user.FullName.ToLower().Contains(query);
                return fullNameMatch;
            }).ToList();

            FilteredUsers = users;

            //Выбор записей, отображаемых на текущей странице.
            List<User> pageUsers;
            if (OnePageItemsCount == -1)
            {
                pageUsers = users;
            }
            else
            {
                pageUsers = users
                    .Skip(CurrentPageFirstItemIndex - 1)
                    .Take(OnePageItemsCount)
                    .ToList();
            }

            CurrentPageUsers.Clear();
            //Асинхронное заполнение отображаемой коллекции CurrentPageUsers.
            foreach (var user in pageUsers)
            {
                await Task.Run(() =>
                {
                    synchronizationContext.Send(o =>
                    {
                        CurrentPageUsers.Add(user);
                    }, null);
                });
            }

            UpdatePageValues();
        }

        /// <summary>
        /// Обновляет значения, связанные со страницами.
        /// </summary>
        private void UpdatePageValues()
        {
            if (FilteredUsers == null) return;
            FilteredItemsCount = FilteredUsers.Count;
            TotalItemsCount = Users.Count;
            PagesCount = OnePageItemsCount == -1
                ? 1
                : (int) Math.Ceiling((double) FilteredItemsCount / OnePageItemsCount);
            if (PagesCount == 0)
            {
                PagesCount = 1;
            }
            if (CurrentPage > PagesCount)
            {
                CurrentPage = PagesCount;
            }
            CurrentPageFirstItemIndex = OnePageItemsCount == -1 
                ? FilteredItemsCount
                : ((CurrentPage - 1) * OnePageItemsCount) + 1;
            var index = CurrentPageFirstItemIndex + OnePageItemsCount - 1;
            var min = new List<int> { index, FilteredItemsCount }.Min();
            CurrentPageLastItemIndex = OnePageItemsCount == -1 
                ? CurrentPageFirstItemIndex 
                : min;
        }
    }
}
