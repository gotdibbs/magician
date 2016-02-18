using Magician.Controls;
using Magician.Presenters;

namespace Magician.Views
{
    /// <summary>
    /// Interaction logic for Tricks.xaml
    /// </summary>
    internal partial class TricksTab : Trick
    {
        internal TricksTab()
        {
            InitializeComponent();

            new TricksPresenter(this);
        }
    }
}
