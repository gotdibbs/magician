using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Magician.ExtensionFramework;
using Magician.Models;
using Magician.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Windows.Controls;

namespace Magician.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        private CompositionContainer _container;

        [ImportMany]
        private List<Lazy<ITrick, ITrickData>> _tricks;

        private ObservableCollection<TabItem> _tabs;
        public ObservableCollection<TabItem> Tabs
        {
            get { return _tabs; }
            set { Set(ref _tabs, value); }
        }

        public MainViewModel()
        {
            // Initialize MEF
            var catalog = new DirectoryCatalog("Tricks");
            _container = new CompositionContainer(catalog);

            this._container.ComposeParts(this);

            var tricks = new List<TrickViewModel>();

            foreach (var trick in _tricks)
            {
                var Trick = new TrickViewModel
                {
                    Name = trick.Metadata.Name,
                    Description = trick.Metadata.Description
                };

                tricks.Add(Trick);
            }

            Tabs = new ObservableCollection<TabItem>
            {
                new TabItem
                {
                    Header = "All Tricks",
                    IsSelected = true,
                    Content = new TricksTab(tricks)
                }
            };

            Messenger.Default.Register<LoadMessage>(this, (m) => OnLoadTab(m.Trick));
        }

        public void OnLoadTab(TrickViewModel selectedTrick)
        {
            var trick = _tricks
                .Where(t => t.Metadata.Name == selectedTrick.Name)
                .FirstOrDefault();

            var tab = new TabItem();

            tab.Header = selectedTrick.Name;
            tab.IsSelected = true;

            tab.Content = trick.Value;

            AddTab(tab);
        }

        private void AddTab(TabItem tabWrappedControl)
        {
            Tabs.Add(tabWrappedControl);
            RaisePropertyChanged(() => Tabs);
        }
    }
}
