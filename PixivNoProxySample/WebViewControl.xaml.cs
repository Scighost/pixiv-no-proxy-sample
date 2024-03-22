using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PixivNoProxySample;

public sealed partial class WebViewControl : UserControl, INotifyPropertyChanged
{


    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }



    public WebViewControl()
    {
        this.InitializeComponent();
    }




    private string _Title = "正在加载";
    public string Title
    {
        get { return _Title; }
        set
        {
            _Title = value;
            OnPropertyChanged();
        }
    }

    public string InitialUrl { get; set; }


    private bool initialized;


    private CoreWebView2 coreWebView2;

    private HttpClient pixivClient { get; init; } = PixivSocketsHttpClient.Create("www.pixivision.net");

    private HttpClient imgClient { get; init; } = PixivSocketsHttpClient.Create("s.pximg.net");


    private async void webview_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (initialized)
            {
                return;
            }
            string folder = Path.Join(Path.GetDirectoryName(Environment.ProcessPath), "powerfulpixivdownloader");
            var registration = new CoreWebView2CustomSchemeRegistration("pixiv");
            registration.AllowedOrigins.Add("*");
            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = $"--embedded-browser-webview-enable-extension --load-extension=\"{folder}\" ",
                CustomSchemeRegistrations = [registration],
            };
            await webview.EnsureCoreWebView2Async(await CoreWebView2Environment.CreateWithOptionsAsync(null, null, options));
            coreWebView2 = webview.CoreWebView2;
            coreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            coreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            coreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
            coreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            coreWebView2.LaunchingExternalUriScheme += CoreWebView2_LaunchingExternalUriScheme;
            coreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            if (Uri.TryCreate(InitialUrl, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                coreWebView2.Navigate(uri.ToString());
            }
            else
            {
                coreWebView2.Navigate("https://www.pixiv.net");
            }
            webview.Focus(FocusState.Programmatic);
            initialized = true;
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "初始化失败",
                Content = ex.Message,
                PrimaryButtonText = "关闭",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            await dialog.ShowAsync();
        }
    }



    private void CoreWebView2_HistoryChanged(CoreWebView2 sender, object args)
    {
        try
        {
            AutoSuggestBox_SearchAndUrl.Text = sender.Source;
            Button_GoBack.IsEnabled = sender.CanGoBack;
            Button_GoForward.IsEnabled = sender.CanGoForward;
        }
        catch { }
    }


    private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args)
    {
        try
        {
            Title = sender.DocumentTitle;
        }
        catch { }
    }

    private void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        try
        {
            args.Handled = true;
            MainWindow.Current.AddTabView(args.Uri);
        }
        catch { }
    }


    private async void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri? uri))
        {
            if (uri.Scheme is "pixiv")
            {
                await ShowSchemeDialogAsync(args.Request.Uri);
                return;
            }
            if (uri.Host.Contains("pixiv.net") || uri.Host.Contains("fanbox.cc") || uri.Host.Contains("i.pximg.net"))
            {
                var defer = args.GetDeferral();
                try
                {
                    var request = new HttpRequestMessage(new HttpMethod(args.Request.Method), args.Request.Uri.Replace("https://", "http://"))
                    {
                        Version = HttpVersion.Version20,
                        VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
                    };
                    foreach (KeyValuePair<string, string> item in args.Request.Headers)
                    {
                        request.Headers.TryAddWithoutValidation(item.Key, item.Value);
                    }
                    if (args.Request.Content is not null)
                    {
                        request.Content = new StreamContent(args.Request.Content.AsStream());
                        request.Content.Headers.Clear();
                        foreach (KeyValuePair<string, string> item in args.Request.Headers)
                        {
                            if (string.Equals(item.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                            {
                                request.Content.Headers.TryAddWithoutValidation(item.Key, item.Value);
                            }
                        }
                    }

                    HttpResponseMessage? response = null;
                    if (uri.Host.Contains("pixiv.net") || uri.Host.Contains("fanbox.cc"))
                    {
                        response = await pixivClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    }
                    else if (uri.Host.Contains("i.pximg.net"))
                    {
                        response = await imgClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    }
                    if (response is null)
                    {
                        return;
                    }

                    var ms = new MemoryStream();
                    await response.Content.CopyToAsync(ms);
                    ms.Position = 0;

                    var sb = new StringBuilder();
                    foreach (var cookies in response.Headers)
                    {
                        foreach (var item in cookies.Value)
                        {
                            sb.AppendLine(cookies.Key + ": " + item);
                        }
                    }
                    foreach (var cookies in response.Content.Headers)
                    {
                        foreach (var item in cookies.Value)
                        {
                            sb.AppendLine(cookies.Key + ": " + item);
                        }
                    }
                    if (uri.Host.Contains("i.pximg.net"))
                    {
                        sb.AppendLine("Access-Control-Allow-Origin: *");
                    }
                    args.Response = sender.Environment.CreateWebResourceResponse(ms.AsRandomAccessStream(), (int)response.StatusCode, response.ReasonPhrase, sb.ToString());
                }
                finally
                {
                    defer.Complete();
                }
            }
        }

    }


    private async void CoreWebView2_LaunchingExternalUriScheme(CoreWebView2 sender, CoreWebView2LaunchingExternalUriSchemeEventArgs args)
    {
        args.Cancel = true;
        await ShowSchemeDialogAsync(args.Uri);
    }



    private async Task ShowSchemeDialogAsync(string uri)
    {
        try
        {
            var dialog = new ContentDialog()
            {
                Title = "网页请求打开以下链接",
                Content = new TextBlock
                {
                    Text = uri,
                    IsTextSelectionEnabled = true,
                    TextWrapping = TextWrapping.Wrap,
                },
                PrimaryButtonText = "打开",
                SecondaryButtonText = "关闭",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                await Launcher.LaunchUriAsync(new Uri(uri));
            }
        }
        catch { }
    }



    public ObservableCollection<Suggestion> Suggestions { get; set; } = new();



    private void AutoSuggestBox_SearchAndUrl_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        try
        {
            if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string text = sender.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(text))
                {
                    Suggestions.Clear();
                    sender.IsSuggestionListOpen = false;
                    return;
                }
                if (Uri.TryCreate(text, UriKind.Absolute, out Uri? uri))
                {
                    AddOrUpdate("网页", uri.ToString());
                    Remove("搜索");
                }
                else
                {
                    AddOrUpdate("搜索", text);
                    Remove("网页");
                }
                if (long.TryParse(text, out long id))
                {
                    AddOrUpdate("用户", id.ToString());
                    AddOrUpdate("插画", id.ToString());
                    AddOrUpdate("小说", id.ToString());
                }
                else
                {
                    Remove("用户");
                    Remove("插画");
                    Remove("小说");
                }
                sender.IsSuggestionListOpen = true;
            }
        }
        catch { }
    }


    private void AddOrUpdate(string title, string content)
    {
        try
        {
            if (Suggestions.FirstOrDefault(x => x.Title == title) is Suggestion suggestion)
            {
                suggestion.Content = content;
            }
            else
            {

                Suggestions.Add(new Suggestion(title, content));
            }
        }
        catch { }
    }


    private void Remove(string title)
    {
        try
        {
            if (Suggestions.FirstOrDefault(x => x.Title == title) is Suggestion suggestion)
            {
                Suggestions.Remove(suggestion);
            }
        }
        catch { }
    }



    private void AutoSuggestBox_SearchAndUrl_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        try
        {
            string? url;
            if (args.ChosenSuggestion is Suggestion suggestion)
            {
                string content = suggestion.Content;
                url = suggestion.Title switch
                {
                    "网页" => suggestion.Content,
                    "搜索" => $"https://www.pixiv.net/tags/{Uri.EscapeDataString(content)}",
                    "用户" => $"https://www.pixiv.net/users/{content}",
                    "插画" => $"https://www.pixiv.net/artworks/{content}",
                    "小说" => $"https://www.pixiv.net/novel/show.php?id={content}",
                    _ => null,
                };
            }
            else
            {
                string text = args.QueryText?.Trim() ?? "";
                if (Uri.TryCreate(text, UriKind.Absolute, out Uri? uri))
                {
                    url = uri.ToString();
                }
                else
                {
                    url = $"https://www.pixiv.net/tags/{Uri.EscapeDataString(text)}";
                }
            }
            if (!string.IsNullOrWhiteSpace(url))
            {
                coreWebView2.Navigate(url);
            }
        }
        catch { }
    }


    private void Button_GoBack_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (coreWebView2.CanGoBack)
            {
                coreWebView2.GoBack();
            }
        }
        catch { }
    }

    private void Button_GoForward_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (coreWebView2.CanGoForward)
            {
                coreWebView2.GoForward();
            }
        }
        catch { }
    }

    private void Button_Reload_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            coreWebView2.Reload();
        }
        catch { }
    }


}
