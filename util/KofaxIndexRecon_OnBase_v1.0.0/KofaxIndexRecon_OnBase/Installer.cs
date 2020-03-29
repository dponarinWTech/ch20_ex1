using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Windows.Forms;

namespace KofaxIndexRecon_OnBase
{
    [RunInstaller(true)]
    public partial class Installer : System.Configuration.Install.Installer
    {
        public Installer()
        {
            InitializeComponent();
        }

        public override void Uninstall(IDictionary savedState)
        {
            if (savedState != null)
            {
                base.Uninstall(savedState);
            }
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            String configPath = Context.Parameters["assemblypath"] + ".config"; // fully qualifies name of config file 
            if (!File.Exists(configPath))
            {
                MessageBox.Show($"Config file '{configPath}' does not exists. Installation failed.", "Installation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw new InstallException("Config file not found"); // throwing unhandled exception will automatically call Rollback
            }

            ConfigurationForm confForm = new ConfigurationForm(configPath);
            confForm.TopLevel = true;

            DialogResult dialog = confForm.ShowDialog();
            if (dialog != DialogResult.OK)
            {
                MessageBox.Show("User cancelled installation.", "KofaxIndexRecon Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                throw new InstallException("User cancelled installation.");
            }
        }

    }
}
