using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.WebResourceLiveSync.Model
{
    public class Solution
    {
        public string Name { get; set; }

        public Guid SolutionId { get; set; }

        public Guid PublisherId { get; set; }

        public string CustomizationPrefix { get; set; }
    }
}
