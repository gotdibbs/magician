﻿using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Magician.Models;
using System.Windows.Input;

namespace Magician.ViewModels
{
    internal class RecipeViewModel
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string PathToAssembly { get; set; }

        public string TypeName { get; set; }

        public ICommand LoadCommand { get; private set; }

        public RecipeViewModel()
        {
            LoadCommand = new RelayCommand(() => Load());
        }

        public void Load()
        {
            Messenger.Default.Send(new LoadMessage
            {
                Recipe = this
            });
        }
    }
}