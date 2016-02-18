using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace Magician.ViewModels
{
    internal class TricksViewModel : ViewModelBase
    {
        public ObservableCollection<TrickViewModel> Tricks { get; set; }

        public TricksViewModel(List<TrickViewModel> tricks)
        {
            tricks.Sort(delegate (TrickViewModel t1, TrickViewModel t2) 
            {
                return t1.Name.CompareTo(t2.Name);
            });

            Tricks = new ObservableCollection<TrickViewModel>(tricks);

            if (tricks.Count == 0)
            {
                DisplayNoTricksFound();
            }
        }

        private void DisplayNoTricksFound()
        {
            MessageBox.Show("No Tricks found, please install a Trick.");
        }
    }
}
