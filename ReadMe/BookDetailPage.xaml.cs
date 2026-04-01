using ReadMe.Models;

namespace ReadMe;

public partial class BookDetailPage : ContentPage
{
    public BookDetailPage(Book selectedBook)
    {
        InitializeComponent();

        // Setting the Title of the page to the book name
        Title = selectedBook.Title;

        // Now this page has access to all the book data
        BindingContext = selectedBook;
    }
    private async void OnNextPageClicked(object sender, EventArgs e)
    {
        var book = (Book)BindingContext;
        book.LastPageOpened++;

        // Logic to save to SQLite would go here so it remembers where you left off
        // await _dbService.SaveBookAsync(book);
    }
}