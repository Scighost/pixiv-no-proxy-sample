using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PixivNoProxySample;



/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{


    public static new MainWindow Current { get; private set; }


    public MainWindow()
    {
        Current = this;
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




    private void Grid_ActualThemeChanged(FrameworkElement sender, object args)
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



    private void tabView_Loaded(object sender, RoutedEventArgs e)
    {
        AddTabView();
    }


    private void tabView_AddTabButtonClick(TabView sender, object args)
    {
        AddTabView();
    }


    public void AddTabView(string? url = null)
    {
        try
        {
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri) && uri.Scheme is "http" or "https")
            {
                url = uri.ToString();
            }
            else
            {
                url = "https://www.pixiv.net";
            }
            var web = new WebViewControl { InitialUrl = url };
            var binding = new Binding
            {
                Mode = BindingMode.OneWay,
                Source = web,
                Path = new PropertyPath(nameof(WebViewControl.Title)),
            };
            var item = new TabViewItem
            {
                Content = web,
            };
            BindingOperations.SetBinding(item, TabViewItem.HeaderProperty, binding);
            tabView.TabItems.Add(item);
            tabView.SelectedItem = item;
        }
        catch { }
    }


    private void Border_TabStrip_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            double scale = GetDpiForWindow((IntPtr)AppWindow.Id.Value) / 96d;
            Point point = Border_TabStrip.TransformToVisual(tabView).TransformPoint(new Point());
            int x = (int)(point.X * scale);
            int width = (int)(tabView.ActualWidth * scale);
            int height = (int)(Border_TabStrip.ActualHeight * scale);
            var rect = new RectInt32(x, 0, width, height);
            AppWindow.TitleBar.SetDragRectangles([rect]);
        }
        catch { }
    }



    private void tabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        try
        {
            if (sender.TabItems.Contains(args.Tab))
            {
                sender.TabItems.Remove(args.Tab);
            }
            if (sender.TabItems.Count == 0)
            {
                AddTabView();
            }
        }
        catch { }
    }


}
