using Magician.Attributes;
using Magician.Controls;

namespace Magician.TestRecipe.Views
{
    /// <summary>
    /// Interaction logic for TestRecipe.xaml
    /// </summary>
    [Recipe("Test Recipe", "Super awesome description about this test recipe")]
    public partial class TestRecipe : RecipeBase
    {
        public TestRecipe()
        {
            InitializeComponent();

            DataContext = new ViewModels.TestRecipeViewModel();
        }
    }
}
