using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PixivNoProxySample;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。


/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{


    public MainWindow()
    {
        this.InitializeComponent();
        if (MicaController.IsSupported())
        {
            this.SystemBackdrop = new MicaBackdrop();
        }
        Title = "Pixiv No Proxy Sample";
        SetIcon();
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        AdaptTitleBarButtonColorToActuallTheme();
    }



    private CoreWebView2 coreWebView2;

    private HttpClient pixivClient { get; init; } = PixivSocketsHttpClient.Create("www.pixivision.net");

    private HttpClient imgClient { get; init; } = PixivSocketsHttpClient.Create("s.pximg.net");


    private async void webview_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await webview.EnsureCoreWebView2Async();
            coreWebView2 = webview.CoreWebView2;
            coreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            coreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            coreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            coreWebView2.LaunchingExternalUriScheme += CoreWebView2_LaunchingExternalUriScheme;
            coreWebView2.Navigate("https://www.pixiv.net");
            webview.Focus(FocusState.Programmatic);
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "初始化失败",
                Content = ex.Message,
                PrimaryButtonText = "关闭",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Grid_TitleBar.XamlRoot,
            };
            await dialog.ShowAsync();
        }
    }


    private void CoreWebView2_HistoryChanged(CoreWebView2 sender, object args)
    {
        try
        {
            AutoSuggestBox_SearchAndUrl.Text = sender.Source;
        }
        catch { }
    }


    private async void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out Uri? uri))
        {
            if (uri.Host.Contains("pixiv") || uri.Host.Contains("i.pximg.net"))
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
                    if (uri.Host.Contains("pixiv"))
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
                    args.Response = sender.Environment.CreateWebResourceResponse(ms.AsRandomAccessStream(), (int)response.StatusCode, response.ReasonPhrase, sb.ToString());
                }
                catch (Exception ex)
                {

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
        try
        {
            args.Cancel = true;
            var dialog = new ContentDialog()
            {
                Title = "接收到了协议请求",
                Content = new TextBlock
                {
                    Text = args.Uri.ToString(),
                    IsTextSelectionEnabled = true,
                    TextWrapping = TextWrapping.Wrap,
                },
                PrimaryButtonText = "打开",
                SecondaryButtonText = "关闭",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = Grid_TitleBar.XamlRoot,
            };
            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                await Launcher.LaunchUriAsync(new Uri(args.Uri));
            }
        }
        catch { }
    }


    private void Button_Home_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            coreWebView2.Navigate("https://www.pixiv.net");
        }
        catch { }
    }



    #region Title Bar



    private void Grid_TitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var scale = GetDpiForWindow((IntPtr)AppWindow.Id.Value) / 96d;
        var point = AutoSuggestBox_SearchAndUrl.TransformToVisual(Grid_TitleBar).TransformPoint(new Point());
        var width = AutoSuggestBox_SearchAndUrl.ActualWidth;
        //var height = AutoSuggestBox_SearchAndUrl.ActualHeight;
        int len = (int)(48 * scale);
        var rect1 = new RectInt32(len, 0, (int)((point.X - 48) * scale), len);
        var rect2 = new RectInt32((int)((point.X + width) * scale), 0, (int)((point.X + width) * scale), len);
        AppWindow.TitleBar.SetDragRectangles([rect1, rect2]);
    }





    private void Grid_TitleBar_ActualThemeChanged(FrameworkElement sender, object args)
    {
        AdaptTitleBarButtonColorToActuallTheme();
    }


    private void AdaptTitleBarButtonColorToActuallTheme()
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar == true)
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            if (Content is FrameworkElement element)
            {
                switch (element.ActualTheme)
                {
                    case ElementTheme.Default:
                        break;
                    case ElementTheme.Light:
                        titleBar.ButtonForegroundColor = Colors.Black;
                        titleBar.ButtonHoverForegroundColor = Colors.Black;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0x00, 0x00, 0x00);
                        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                        break;
                    case ElementTheme.Dark:
                        titleBar.ButtonForegroundColor = Colors.White;
                        titleBar.ButtonHoverForegroundColor = Colors.White;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
                        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                        break;
                    default:
                        break;
                }
            }
        }
    }


    private void SetIcon()
    {
        nint hInstance = GetModuleHandle(null);
        nint hIcon = LoadIcon(hInstance, "#32512");
        AppWindow.SetIcon(Win32Interop.GetIconIdFromIcon(hIcon));
    }


    [DllImport("User32.dll")]
    private static extern int GetDpiForWindow(IntPtr hWnd);

    [DllImport("User32.dll")]
    private static extern nint LoadIcon(nint hInstance, string lpIconName);


    [DllImport("Kernel32.dll")]
    private static extern nint GetModuleHandle(string? lpModuleName = null);


    #endregion




    #region Search




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




    public class Suggestion : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Suggestion(string title, string content)
        {
            Title = title;
            Content = content;
        }
        private string _Title;
        public string Title
        {
            get { return _Title; }
            set
            {
                _Title = value;
                OnPropertyChanged();
            }
        }

        private string _Content;
        public string Content
        {
            get { return _Content; }
            set
            {
                _Content = value;
                OnPropertyChanged();
            }
        }

    }






    #endregion


}
