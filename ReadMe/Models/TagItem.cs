using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReadMe.Models
{
    public class TagItem : INotifyPropertyChanged
    {
        private string _name;
        private readonly HashSet<int> _associatedBookIds = new();

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public int AssociatedBooksCount => _associatedBookIds.Count;

        public TagItem(string name)
        {
            _name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetAssociatedBooks(IEnumerable<Book> books)
        {
            _associatedBookIds.Clear();

            foreach (var book in books)
            {
                _associatedBookIds.Add(book.Id);
            }

            OnPropertyChanged(nameof(AssociatedBooksCount));
        }

        public bool ContainsBook(int bookId) => _associatedBookIds.Contains(bookId);

        public void AddBook(Book book)
        {
            if (_associatedBookIds.Add(book.Id))
            {
                OnPropertyChanged(nameof(AssociatedBooksCount));
            }
        }

        public void RemoveBook(Book book)
        {
            if (_associatedBookIds.Remove(book.Id))
            {
                OnPropertyChanged(nameof(AssociatedBooksCount));
            }
        }

        public IReadOnlyCollection<int> GetAssociatedBookIds() => _associatedBookIds;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
