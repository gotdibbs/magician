using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magician.DeployTeams.Models
{
    public class Team : ObservableObject
    {
        private bool _isSelected;
        [JsonIgnore]
        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        public Guid TeamId { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public bool IsInRootBusinessUnit { get; set; }

        public string BusinessUnit { get; set; }

        [JsonIgnore]
        public Guid DestinationBusinessUnitId { get; set; }

        [JsonIgnore]
        public string BusinessUnitDisplayName
        {
            get
            {
                return IsInRootBusinessUnit ?
                    BusinessUnit + " (Root)" : BusinessUnit;
            }
        }

        public string AdministratorDomainName { get; set; }

        public string AdministratorFullName { get; set; }

        [JsonIgnore]
        public Guid DestinationAdministratorId { get; set; }

        public string Currency { get; set; }

        [JsonIgnore]
        public Guid DestinationCurrencyId { get; set; }

        public Dictionary<string, object> Attributes { get; set; }

        public List<string> SecurityRoles { get; set; }

        [JsonIgnore]
        public int SecurityRolesCount
        {
            get
            {
                if (SecurityRoles == null)
                {
                    return 0;
                }

                return SecurityRoles.Count;
            }
        }

        public Team()
        {
            Attributes = new Dictionary<string, object>();
            SecurityRoles = new List<string>();
        }
    }
}
