using Magician.ExtensionFramework;
using System;
using System.ComponentModel.Composition;

namespace Magician.ExtensionFramework
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TrickDescriptionAttribute : ExportAttribute, ITrickData
    {
        public string Name { get; set; }

        public string Description { get; set; }

        /*Important that the base is invoked with typeof the actual export.
          This way we can avoid needing another Export or InheritedExport. */
        public TrickDescriptionAttribute(string name, string description)
            : base(typeof(ITrick))
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Trick requires a name.", "name");
            }

            Name = name;
            Description = description;
        }
    }
}
