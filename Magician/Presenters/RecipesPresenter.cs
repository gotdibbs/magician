using Magician.ViewModels;
using Magician.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.Presenters
{
    internal class RecipesPresenter
    {
        internal RecipesTab View { get; private set; }

        internal RecipesViewModel ViewModel { get; private set; }

        internal RecipesPresenter(RecipesTab view)
        {
            View = view;
            ViewModel = new RecipesViewModel();

            view.DataContext = ViewModel;
        }
    }
}
