using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReadMe.Models
{
    public class TagSelectionItem : INotifyPropertyChanged
    {
        public TagItem Tag { get; }
        private bool _isSelected;

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

        public string Name => Tag.Name;

        public TagSelectionItem(TagItem tag)
        {
            Tag = tag;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
