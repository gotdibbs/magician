using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.UsersByRole.Models
{
    public class User
    {
        public Guid UserId { get; set; }

        public string DomainName { get; set; }

        public string FullName { get; set; }
    }
}
