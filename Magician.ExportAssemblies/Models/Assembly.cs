using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.ExportAssemblies.Models
{
    public class Assembly : ViewModelBase
    {
        public Guid AssemblyId { get; set; }

        private bool _export;
        public bool Export
        {
            get { return _export; }
            set { Set(ref _export, value); }
        }

        public string Name { get; set; }

        public int RegisteredStepCount { get; set; }
    }
}
