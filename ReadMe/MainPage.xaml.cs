using ReadMe.Models;
using ReadMe.ViewModels;
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
