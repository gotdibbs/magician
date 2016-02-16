using Magician.Presenters;
using System.Windows;

namespace Magician.Connect.Views
{
    /// <summary>
    /// Interaction logic for ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        public ConnectPresenter Presenter { get; private set; }

        public ConnectWindow()
        {
            InitializeComponent();

            Presenter = new ConnectPresenter(this);
        }
    }
}
