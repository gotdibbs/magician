using GalaSoft.MvvmLight;
using Magician.Controls;
using Magician.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Magician.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ObservableCollection<TabItem> Tabs
        {
            get
            {
                return new ObservableCollection<TabItem>
                {
                    new RecipesTab
                    {
                        Header = "Recipes",
                        IsSelected = true
                    }
                };
            }
        }
    }
}
