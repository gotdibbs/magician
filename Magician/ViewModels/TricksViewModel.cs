using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Magician.Attributes;
using Magician.Controls;
using Magician.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Magician.ViewModels
{
    internal class TricksViewModel : ViewModelBase
    {
        public ObservableCollection<TrickViewModel> Tricks { get; set; }

        public TricksViewModel()
        {
            LoadTricks();
        }

        public void LoadTricks()
        {
            if (!Directory.Exists("Tricks"))
            {
                DisplayNoTricksFound();
                return;
            }

            var libraries = Directory.EnumerateFiles("Tricks", "*.dll").ToList();

            if (libraries.Count == 0)
            {
                DisplayNoTricksFound();
                return;
            }

            var tricks = new List<TrickViewModel>();

            var root = GetAssemblyPath();

            foreach (var path in libraries)
            {
                var absolutePath = Path.Combine(root, path);

                var library = Assembly.LoadFrom(absolutePath);

                var validTypes = from type in library.GetTypes()
                                 where Attribute.IsDefined(type, typeof(TrickAttribute))
                                 select type;

                foreach (var type in validTypes)
                {
                    var myInterfaceType = typeof(Trick);
                    if (type != myInterfaceType && myInterfaceType.IsAssignableFrom(type))
                    {
                        var attribute = (TrickAttribute)Attribute.GetCustomAttribute(type, typeof(TrickAttribute));

                        var Trick = new TrickViewModel
                        {
                            Name = attribute.Name,
                            Description = attribute.Description,
                            PathToAssembly = absolutePath,
                            TypeName = type.FullName
                        };

                        tricks.Add(Trick);
                    }
                }
            }

            tricks.Sort(delegate (TrickViewModel t1, TrickViewModel t2) { return t1.Name.CompareTo(t2.Name); });

            Tricks = new ObservableCollection<TrickViewModel>(tricks);
        }

        private void DisplayNoTricksFound()
        {
            MessageBox.Show("No Tricks found, please install a Trick.");
        }

        private string GetAssemblyPath()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}
