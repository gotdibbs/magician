using Magician.ViewModels;
using Magician.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.Presenters
{
    public class RecipesPresenter
    {
        public RecipesTab View { get; private set; }

        public RecipesViewModel ViewModel { get; private set; }

        public RecipesPresenter(RecipesTab view)
        {
            View = view;
            ViewModel = new RecipesViewModel();

            view.DataContext = ViewModel;
        }
    }
}
