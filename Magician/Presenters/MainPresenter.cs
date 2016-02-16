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
    internal class MainPresenter
    {
        internal MainWindow View { get; private set; }

        internal MainViewModel ViewModel { get; private set; }

        internal MainPresenter(MainWindow view)
        {
            View = view;
            ViewModel = new MainViewModel();
            
            view.DataContext = ViewModel;
        }
    }
}
