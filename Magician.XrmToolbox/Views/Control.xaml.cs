using Magician.Connect;
using Magician.Controls;
using Magician.ExtensionFramework;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Forms.Integration;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using FormsUserControl = System.Windows.Forms.UserControl;

namespace Magician.XrmToolbox.Views
{
    [TrickDescription("Xrm Toolbox", "Run Xrm Toolbox utilities in our compatibility mode.")]
    public partial class Control : Trick
    {
        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<Lazy<IXrmToolBoxPlugin, IPluginMetadata>> Plugins { get; set; }

        private WindowsFormsHost _formsHost;

        private string _pluginPath;

        private IXrmToolBoxPlugin _activePlugin;
        private IXrmToolBoxPluginControl _activeControl;

        private Connector _connector;
        private OrganizationServiceProxy _service;

        public Control()
        {
            InitializeComponent();

            Unloaded += OnCloseTool;
            _pluginPath = Path.Combine(Assembly.GetEntryAssembly().Location, "..\\..\\..\\..\\XrmToolbox\\");

            InitializePlugins();
        }

        private void InitializePlugins()
        {
            var directoryCatalog = new DirectoryCatalog(_pluginPath);

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(directoryCatalog);
            var container = new CompositionContainer(catalog);
            container.ComposeParts(this);

            Tools.DisplayMemberPath = "Metadata.Name";
            var sortedPlugins = Plugins.OrderBy(p => p.Metadata.Name);
            Tools.ItemsSource = sortedPlugins;
        }

        public void LoadControl(IXrmToolBoxPlugin plugin)
        {
            if (_formsHost == null)
            {
                _formsHost = new WindowsFormsHost();
            }

            _activePlugin = plugin;

            _activeControl = plugin.GetControl();
            _activeControl.OnCloseTool += OnCloseTool;
            _activeControl.OnRequestConnection += OnRequestConnection;

            var userControl = (FormsUserControl)_activeControl;
            userControl.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;

            _formsHost.Child = userControl;

            Host.Children.Add(_formsHost);
        }

        private void OnRequestConnection(object sender, EventArgs e)
        {
            var rcArgs = e as RequestConnectionEventArgs;

            string actionName = null;
            object parameter = null;

            if (rcArgs != null)
            {
                actionName = rcArgs.ActionName;
                parameter = rcArgs.Parameter;
            }

            _activeControl.UpdateConnection(_service, new ConnectionDetail
            {
                ServerName = _connector.OrganizationFriendlyName,
                Organization = _connector.OrganizationFriendlyName,
                OrganizationFriendlyName = _connector.OrganizationFriendlyName
            }, actionName, parameter);
        }

        private void Connect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_connector == null)
            {
                _connector = new Connector();
            }

            if (!_connector.Connect())
            {
                MessageBox.Show("Please click Connect to try connecting to Dynamics CRM again. A valid connection is required.");
                return;
            }

            _service = _connector.OrganizationServiceProxy;

            ConnectText.Text = "Reconnect";

            if (_activeControl != null)
            {
                _activeControl.UpdateConnection(_service, new ConnectionDetail
                {
                    ServerName = _connector.OrganizationFriendlyName,
                    Organization = _connector.OrganizationFriendlyName,
                    OrganizationFriendlyName = _connector.OrganizationFriendlyName
                });
            }
        }

        private void OnCloseTool(object sender, EventArgs e)
        {
            if (CloseActivePlugin() == true)
            {
                Tools.SelectedItem = null;
            }
        }

        public bool CloseActivePlugin()
        {
            if (Host.Children.Count < 1)
            {
                return true;
            }

            var info = new PluginCloseInfo
            {
                Cancel = false,
                FormReason = System.Windows.Forms.CloseReason.None,
                ToolBoxReason = XrmToolBox.Extensibility.ToolBoxCloseReason.PluginRequest
            };

            _activeControl.ClosingPlugin(info);

            if (info.Cancel)
            {
                return false;
            }

            Host.Children.RemoveAt(0);

            return true;
        }

        private void Tools_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count < 1)
            {
                return;
            }

            if (CloseActivePlugin() == false && e.RemovedItems.Count == 1)
            {
                Tools.SelectedItem = e.RemovedItems[0];
                return;
            }

            var plugin = (Lazy<IXrmToolBoxPlugin, IPluginMetadata>)e.AddedItems[0];

            LoadControl(plugin.Value);
        }
    }
}
