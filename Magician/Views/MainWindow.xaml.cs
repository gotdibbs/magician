using Magician.Presenters;
using System.Windows;
using System.Windows.Threading;

namespace Magician.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : AppWindowBase
    {
        internal MainWindow()
        {
            Application.Current.DispatcherUnhandledException += OnUnhandledException;

            InitializeComponent();

            new MainPresenter(this);
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            ErrorControl.Visibility = Visibility.Visible;
            ErrorControl.Message = e.Exception.Message;
        }
    }
}
