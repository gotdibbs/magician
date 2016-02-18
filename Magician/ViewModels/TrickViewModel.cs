using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.ExtensionFramework;
using Magician.Models;
using System;
using System.Windows.Input;

namespace Magician.ViewModels
{
    internal class TrickViewModel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public ICommand LoadCommand { get; private set; }

        public TrickViewModel()
        {
            LoadCommand = new RelayCommand(() => Load());
        }

        public void Load()
        {
            Messenger.Default.Send(new LoadMessage
            {
                Trick = this
            });
        }
    }
}
