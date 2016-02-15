using Magician.Controls;
using Magician.Presenters;

namespace Magician.Views
{
    /// <summary>
    /// Interaction logic for Recipes.xaml
    /// </summary>
    public partial class RecipesTab : RecipeBase
    {
        public RecipesTab()
        {
            InitializeComponent();

            new RecipesPresenter(this);
        }
    }
}
