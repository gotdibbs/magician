# Basic Steps Required for Creating your own Trick

1. Create a new WPF User Control Library project in Visual Studio 2015.
2. Add a reference to Magician most importantly, but also to Magician.Connect if you're planning on connecting to Dynamics CRM.
3. Add the Microsoft.CrmSdk.CoreAssemblies nuget package and (as a suggestion) MvvmLightLibs.
4. Update your user control XAML to use Magician.Cotnrols.Trick as the base class.
5. Use Magician.RoleCompare or Magician.UsersByRole as an example to build your Trick.
6. To deploy and test, ensure that the compiled user control library and any dependencies that are not already included in Magician (the CRM SDK assemblies are already included for example) are copied to the Tricks folder which sits alongside Magician.exe.