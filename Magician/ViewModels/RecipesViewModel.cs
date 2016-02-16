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
    internal class RecipesViewModel : ViewModelBase
    {
        public ObservableCollection<RecipeViewModel> Recipes { get; set; }

        public RecipesViewModel()
        {
            LoadRecipes();
        }

        public void LoadRecipes()
        {
            if (!Directory.Exists("Plugins"))
            {
                DisplayNoRecipesFound();
                return;
            }

            var libraries = Directory.EnumerateFiles("Plugins", "*.dll").ToList();

            if (libraries.Count == 0)
            {
                DisplayNoRecipesFound();
                return;
            }

            var recipes = new List<RecipeViewModel>();

            var root = GetAssemblyPath();

            foreach (var path in libraries)
            {
                var absolutePath = Path.Combine(root, path);

                var library = Assembly.LoadFrom(absolutePath);

                var validTypes = from type in library.GetTypes()
                                 where Attribute.IsDefined(type, typeof(RecipeAttribute))
                                 select type;

                foreach (var type in validTypes)
                {
                    var myInterfaceType = typeof(RecipeBase);
                    if (type != myInterfaceType && myInterfaceType.IsAssignableFrom(type))
                    {
                        var attribute = (RecipeAttribute)Attribute.GetCustomAttribute(type, typeof(RecipeAttribute));

                        var recipe = new RecipeViewModel
                        {
                            Name = attribute.Name,
                            Description = attribute.Description,
                            PathToAssembly = absolutePath,
                            TypeName = type.FullName
                        };

                        recipes.Add(recipe);
                    }
                }
            }

            Recipes = new ObservableCollection<RecipeViewModel>(recipes);
        }

        private void DisplayNoRecipesFound()
        {
            MessageBox.Show("No recipes found, please install a recipe.");
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
