using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.WebResourceLiveSync.Model
{
    public class CompareResult
    {
        public Guid ResourceId { get; set; }

        public bool IsMatch { get; set; }

        public DateTime LastModified { get; set; }
    }
}
