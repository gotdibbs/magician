using GalaSoft.MvvmLight;
using Magician.ViewModels;
using Magician.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.Presenters
{
    public class MainPresenter
    {
        public MainWindow View { get; private set; }

        public MainViewModel ViewModel { get; private set; }

        public MainPresenter(MainWindow view)
        {
            View = view;
            ViewModel = new MainViewModel();
            
            view.DataContext = ViewModel;
        }
    }
}
