using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReadMe.Models
{
    public class BookSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public Book Book { get; }

        public string Title => Book.Title;

        public string Author => Book.Author;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public BookSelectionItem(Book book)
        {
            Book = book;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
