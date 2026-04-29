using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReadMe.Models
{
    public class TagSummary : INotifyPropertyChanged
    {
        private string _name;
        private int _bookCount;

        public TagSummary(string name)
        {
            _name = name;
        }

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

        public int BookCount
        {
            get => _bookCount;
            set
            {
                if (_bookCount != value)
                {
                    _bookCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
