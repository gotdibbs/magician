namespace Magician.RoleCompare.Models
{
    enum PrivilegeDepthMask
    {
        // User
        Basic = 1,
        // Business Unit
        Local = 2,
        // Parent: Child
        Deep = 4,
        // Organization
        Global = 8
    }
}
