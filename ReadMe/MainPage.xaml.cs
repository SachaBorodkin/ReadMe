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

    }
}
