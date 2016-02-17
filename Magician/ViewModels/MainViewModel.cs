﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Magician.Controls;
using Magician.Models;
using Magician.Views;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Controls;

namespace Magician.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        private ObservableCollection<TabItem> _tabs;
        public ObservableCollection<TabItem> Tabs
        {
            get { return _tabs; }
            set { Set(ref _tabs, value); }
        }

        public MainViewModel()
        {
            Tabs = new ObservableCollection<TabItem>
            {
                new TabItem
                {
                    Header = "All Recipes",
                    IsSelected = true,
                    Content = new RecipesTab()
                }
            };

            Messenger.Default.Register<LoadMessage>(this, (m) => OnLoadTab(m.Recipe));
        }

        public void OnLoadTab(RecipeViewModel recipe)
        {
            var assembly = Assembly.LoadFrom(recipe.PathToAssembly);

            var type = assembly.GetType(recipe.TypeName);

            var control = (RecipeBase)Activator.CreateInstance(type);

            var tab = new TabItem();

            tab.Header = recipe.Name;
            tab.IsSelected = true;

            tab.Content = control;

            AddTab(tab);
        }

        private void AddTab(TabItem tabWrappedControl)
        {
            Tabs.Add(tabWrappedControl);
            RaisePropertyChanged(() => Tabs);
        }
    }
}
