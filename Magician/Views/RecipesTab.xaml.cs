using Magician.Controls;
using Magician.Presenters;

namespace Magician.Views
{
    /// <summary>
    /// Interaction logic for Recipes.xaml
    /// </summary>
    internal partial class RecipesTab : RecipeBase
    {
        internal RecipesTab()
        {
            InitializeComponent();

            new RecipesPresenter(this);
        }
    }
}
