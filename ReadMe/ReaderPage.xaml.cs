using ReadMe.Models;
using ReadMe.ViewModels;
using ReadMe.Services;

namespace ReadMe;

public partial class ReaderPage : ContentPage
{
    private ReaderViewModel _viewModel;

    public ReaderPage(Book book)
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services != null)
        {
            var databaseService = services.GetService<DatabaseService>();
            var epubReaderService = services.GetService<EpubReaderService>();

            _viewModel = new ReaderViewModel(databaseService, epubReaderService);
            _viewModel.CurrentBook = book;
            BindingContext = _viewModel;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel?.CurrentBook != null)
        {
            await _viewModel.LoadBookAsync(_viewModel.CurrentBook);
            UpdateWebView();

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ReaderViewModel.CurrentChapterContent))
                    {
                        UpdateWebView();
                    }
                };
            }
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        await _viewModel.SaveProgressAsync();
    }

    private void UpdateWebView()
    {
        try
        {
            if (_viewModel?.CurrentChapterContent == null)
            {
                System.Diagnostics.Debug.WriteLine("[ReaderPage] No chapter content to display");
                return;
            }

            var htmlContent = _viewModel.CurrentChapterContent;

            var styledHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', sans-serif;
            font-size: 16px;
            line-height: 1.6;
            margin: 10px;
            padding: 10px;
            color: #333;
            background-color: #fff;
        }}
        h1, h2, h3, h4, h5, h6 {{
            color: #0F172A;
            margin-top: 1em;
            margin-bottom: 0.5em;
        }}
        p {{
            margin-bottom: 1em;
        }}
        img {{
            max-width: 100%;
            height: auto;
        }}
    </style>
</head>
<body>
{htmlContent}
</body>
</html>";

            ContentWebView.Source = new HtmlWebViewSource
            {
                Html = styledHtml
            };

            System.Diagnostics.Debug.WriteLine("[ReaderPage] WebView updated successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReaderPage] Error updating WebView: {ex.Message}");
        }
    }

    private void OnNextClicked(object sender, EventArgs e)
    {
        _viewModel.NextChapter();
        UpdateWebView();
    }

    private void OnPreviousClicked(object sender, EventArgs e)
    {
        _viewModel.PreviousChapter();
        UpdateWebView();
    }

    private void OnMenuClicked(object sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            Shell.Current.FlyoutIsPresented = true;
        }
    }
}
