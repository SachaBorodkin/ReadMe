using ReadMe.Models;

namespace ReadMe;

public partial class BookDetailPage : ContentPage
{
    public BookDetailPage(Book selectedBook)
    {
        InitializeComponent();

        Title = selectedBook.Title;

        BindingContext = selectedBook;
    }
    private async void OnNextPageClicked(object sender, EventArgs e)
    {
        var book = (Book)BindingContext;
        book.LastPageOpened++;

    }
}
