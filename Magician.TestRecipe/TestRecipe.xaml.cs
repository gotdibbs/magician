using Magician.Attributes;
using Magician.Controls;
using Microsoft.Crm.Sdk.Messages;
using System.Windows;

namespace Magician.TestRecipe
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
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            var service = Connect();

            if (service == null)
            {
                return;
            }

            var i = (WhoAmIResponse)service.Execute(new WhoAmIRequest());

            MessageBox.Show(i.UserId.ToString());
        }
    }
}
