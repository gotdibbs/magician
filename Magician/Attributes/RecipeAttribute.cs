using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class RecipeAttribute : Attribute
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public RecipeAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
