using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class TrickAttribute : Attribute
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public TrickAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
