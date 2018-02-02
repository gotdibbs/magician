using System;

namespace Magician.RoleCompare.Models
{
    public class RoleExport
    {
        public Guid PrivilegeId { get; set; }

        public string RoleName { get; set; }

        public string PrivilegeName { get; set; }

        public string AccessRight { get; set; }

        public string EntityName { get; set; }

        public string Depth { get; set; }
    }
}
