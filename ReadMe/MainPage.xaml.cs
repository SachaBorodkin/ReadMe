using ReadMe.Models;
using ReadMe.ViewModels;
using ReadMe.Models;
namespace ReadMe
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            System.Diagnostics.Debug.WriteLine($"DATABASE PATH: {Path.Combine(FileSystem.AppDataDirectory, "api.db")}");
        }

        private async void OnBookTapped(object sender, TappedEventArgs e)
        {

            var selectedBook = e.Parameter as Book;

            if (selectedBook != null)
            {

                await Navigation.PushAsync(new ReaderPage(selectedBook));
            }
        }

        private void OnMenuClicked(object sender, EventArgs e)
        {
            if (Shell.Current != null)
            {
                Shell.Current.FlyoutIsPresented = true;
            }
        }

        private void OnBooksViewClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.SetBooksView();
            }
        }

        private void OnTagsViewClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.SetTagsView();
            }
        }

        private void OnToggleFiltersClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.ToggleFilters();
            }
        }

        private void OnOpenTagPickerClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TagItem tag && BindingContext is MainViewModel vm)
            {
                vm.OpenTagPicker(tag);
            }
        }

        private void OnCloseTagPickerClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.CloseTagPicker();
            }
        }

        private void OnConfirmTagPickerClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.ConfirmTagPickerSelection();
            }
        }

        private void OnTagTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is TagItem tag && BindingContext is MainViewModel vm)
            {
                vm.ViewBooksForTag(tag);
            }
        }

        private void OnClearTagFilterClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.ClearTagFilter();
                vm.SetTagsView();
            }
        }

        private void OnAddBookOpenClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.OpenAddBook();
            }
        }

        private void OnAddBookCloseClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.CloseAddBook();
            }
        }

        private async void OnAddBookPickEpubClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                await vm.PickEpubAsync();
            }
        }

        private async void OnFetchApiBooksClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                await vm.FetchBooksFromApiAsync();
            }
        }

        private async void OnAddBookConfirmClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                await vm.ConfirmAddBookAsync();
            }
        }

        private async void OnDeleteBookClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is Book book && BindingContext is MainViewModel vm)
            {
                bool confirm = await DisplayAlert("Confirmer la suppression", 
                    $"Êtes-vous sûr de vouloir supprimer \"{book.Title}\" ?", 
                    "Oui", 
                    "Non");

                if (confirm)
                {
                    await vm.DeleteBookAsync(book);
                }
            }
        }

        private void OnOpenCreateTagClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.OpenCreateTag();
            }
        }

        private void OnCloseCreateTagClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.CloseCreateTag();
            }
        }

        private async void OnConfirmCreateTagClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                await vm.ConfirmCreateTagAsync();
            }
        }

        private void OnOpenBookTagPickerClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is Book book && BindingContext is MainViewModel vm)
            {
                vm.OpenBookTagPicker(book);
            }
        }

        private void OnCloseBookTagPickerClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.CloseBookTagPicker();
            }
        }

        private void OnConfirmBookTagPickerClicked(object sender, EventArgs e)
        {
            if (BindingContext is MainViewModel vm)
            {
                vm.ConfirmBookTagPickerSelection();
            }
        }

        private void OnSelectionItemTapped(object sender, TappedEventArgs e)
        {
            var context = (sender as BindableObject)?.BindingContext ?? e.Parameter;
            
            if (context is TagSelectionItem tagItem)
            {
                tagItem.IsSelected = !tagItem.IsSelected;
            }
            else if (context is BookSelectionItem bookItem)
            {
                bookItem.IsSelected = !bookItem.IsSelected;
            }
        }

        private async void OnDeleteTagClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is TagItem tag && BindingContext is MainViewModel vm)
            {
                bool confirm = await DisplayAlert("Confirmer", $"Voulez-vous vraiment supprimer le tag '{tag.Name}' ?", "Oui", "Non");
                if (confirm)
                {
                    await vm.DeleteTagAsync(tag);
                }
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is MainViewModel vm)
            {
                await vm.LoadBooksAsync();
            }
        }

    }
}
