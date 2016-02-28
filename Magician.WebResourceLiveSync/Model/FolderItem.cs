using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.WebResourceLiveSync.Model
{
    public class FolderItem : DirectoryItem
    {
        public FolderItem()
        {
            IsExpanded = true;
            IsFolder = true;
        }
    }
}
