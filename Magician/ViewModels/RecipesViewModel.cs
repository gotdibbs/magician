using GalaSoft.MvvmLight;
using Magician.Models;
using System.Collections.ObjectModel;

namespace Magician.ViewModels
{
    public class RecipesViewModel : ViewModelBase
    {
        public ObservableCollection<Recipe> Recipes { get; set; }

        public RecipesViewModel()
        {
            Recipes = new ObservableCollection<Recipe>
            {
                new Recipe
                {
                    Name = "Migrate Records",
                    Description = "Push records from one environemnt to another"
                }
            };
        }
    }
}
