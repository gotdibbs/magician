using Magician.ExtensionFramework.Controls;
using Magician.Presenters;
using Magician.ViewModels;
using System.Collections.Generic;

namespace Magician.Views
{
    /// <summary>
    /// Interaction logic for Tricks.xaml
    /// </summary>
    internal partial class TricksTab : Trick
    {
        internal TricksTab(List<TrickViewModel> tricks)
        {
            InitializeComponent();

            new TricksPresenter(this, tricks);
        }
    }
}
