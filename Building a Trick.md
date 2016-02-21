# Basic Steps Required for Creating your own Trick

1. Create a new WPF User Control Library project in Visual Studio 2015.
2. Add a reference to Magician most importantly, but also to Magician.Connect if you're planning on connecting to Dynamics CRM.
3. Add a reference to System.ComponentModel.Composition.
4. Add the Microsoft.CrmSdk.CoreAssemblies nuget package and (as a suggestion) MvvmLightLibs.
5. Open your user control XAML code behind (.xaml.cs file).
6. Update the base class to be Magician.Controls.Trick.
6. Add a Magician.ExtensionFramework.TrickDescriptionAttribute to the base class, specifying both the name and description of your new trick as you want it displayed in Magician.
6. Use Magician.RoleCompare or Magician.BulkWorkflowExecutor as an example to continue building out your trick.
7. To deploy and test, ensure that the compiled user control library and any dependencies that are not already included in Magician (the CRM SDK assemblies are already included for example) are copied to the Tricks folder which sits alongside Magician.exe.