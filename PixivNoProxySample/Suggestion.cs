using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace PixivNoProxySample;

public sealed partial class WebViewControl
{
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








}
