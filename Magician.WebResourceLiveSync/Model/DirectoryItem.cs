using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.WebResourceLiveSync.Model
{
    public class DirectoryItem
    {
        public string Name { get; set; }

        public List<DirectoryItem> Items { get; set; } = new List<DirectoryItem>();
    }
}
