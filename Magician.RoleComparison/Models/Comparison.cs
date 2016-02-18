using System;
using System.Windows.Media;

namespace Magician.RoleCompare.Models
{
    public class Comparison
    {
        public Guid PrivilegeId { get; set; }

        public string Name { get; set; }

        public string AccessRight { get; set; }

        public string EntityName { get; set; }

        public string Depth1 { get; set; }

        public string Depth2 { get; set; }

        public bool IsMatch
        {
            get
            {
                return Depth1 == Depth2;
            }
        }

        public string IsMatchYesNo
        {
            get
            {
                return IsMatch ? "Yes" : "No";
            }
        }

        public Brush Background
        {
            get
            {
                return IsMatch ?
                    new SolidColorBrush(Color.FromRgb(185, 246, 202)) :
                    new SolidColorBrush(Color.FromRgb(229, 115, 115));
            }
        }
    }
}
