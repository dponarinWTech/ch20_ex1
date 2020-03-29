using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace KofaxIndexRecon_OnBase
{
    public partial class ConfigurationForm : Form
    {
        private readonly String configFile;
        private string appName; // Name of the application; we need it because it appears in the setting names

        public ConfigurationForm(String configFile)
        {
            InitializeComponent();
            this.configFile = configFile;

            appName = Path.GetFileName(configFile);
            var tokens = appName.Split('.');
            if (tokens.Length > 0) { appName = tokens[0]; }
            else { appName = "KofaxIndexRecon_OnBase"; }
        }

        private void ConfigurationForm_Load(object sender, EventArgs e)
        {
            if (!File.Exists(configFile))
            {
                string msg = $"Config file '{configFile}' not found. Installation aborted. " + Environment.NewLine + Environment.NewLine + "Please notify BSD of this problem.";
                MessageBox.Show(msg, "Configure Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Abort;
                Close();
            }

            PopulateForm(configFile);

            // We don't want text in tbKofaxConn to be highlighted when the form opens
            tbKofaxConn.SelectionStart = tbKofaxConn.Text.Length;
            tbKofaxConn.DeselectAll();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("The installation is not yet complete. Are you sure you want to exit?", "Configure Settings", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                // Verify that there are no empty fields.        
                var emptyTextBox = Controls.OfType<TextBox>().FirstOrDefault(t => string.IsNullOrWhiteSpace(t.Text));
                if (emptyTextBox != null)
                {
                    MessageBox.Show(new Form { TopMost = true },
                        @"All field values are required" + Environment.NewLine + Environment.NewLine +
                        "for a successful install");
                    emptyTextBox.Focus();
                    return;
                }

                UpdateConfig(configFile);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception thrown: \n" + ex);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }



        public void PopulateForm(String configFile)
        {
            try
            {
                tbKofaxConn.Text = GetConnStringValue(configFile, $"{appName}.Properties.Settings.KofaxConnString");
                tbOnBaseConn.Text = GetConnStringValue(configFile, $"{appName}.Properties.Settings.OnBaseConnString");

                tbLogFolder.Text = GetLogFolderValue(configFile);

                List<XElement> settings = GetAllSettings(configFile);
                tbEmailFrom.Text = FetchSettingValue(settings, "EmailFrom")?.Trim();
                tbEmailTo.Text = FetchSettingValue(settings, "EmailTo")?.Trim();
            }
            catch (Exception ex)
            {
                String msg = "Installation aborted. Exception caught when populating Settings Configuration Form:  " + ex.ToString() +
                             Environment.NewLine + Environment.NewLine + "Please notify BSD of this problem.";
                MessageBox.Show(msg, "Configure Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShutDown();
            }
        }

        public void UpdateConfig(String configFile)
        {
            try
            {
                XDocument doc = XDocument.Load(configFile);

                XAttribute kofaxConnString = GetConnStringAttribute(doc, $"{appName}.Properties.Settings.KofaxConnString");
                if (kofaxConnString != null) kofaxConnString.Value = tbKofaxConn.Text.Trim();
                else { ShowError("connection string", $"{appName}.Properties.Settings.KofaxConnString"); }

                XAttribute obConnString = GetConnStringAttribute(doc, $"{appName}.Properties.Settings.OnBaseConnString");
                if (obConnString != null) obConnString.Value = tbOnBaseConn.Text.Trim();
                else { ShowError("connection string", $"{appName}.Properties.Settings.OnBaseConnString"); }

                XAttribute logFile = GetLogFileValueAttribute(doc);
                if (logFile != null) logFile.Value = Path.Combine(tbLogFolder.Text.Trim(), $"{appName}.log");
                else { ShowError("log file name in element 'LogFileAppender', sub-element", "File"); }

                List<XElement> settings = GetAllSettings(doc); // remember 'settings' XElement list to avoid multiple reading of config file

                XElement emailFrom = GetSettingElement(settings, "EmailFrom");
                if (emailFrom != null) emailFrom.Value = tbEmailFrom.Text.Trim();
                else { ShowError("setting", $"EmailFrom"); }

                XElement emailTo = GetSettingElement(settings, "EmailTo");
                if (emailTo != null) emailTo.Value = tbEmailTo.Text.Trim();
                else { ShowError("setting", "EmailTo"); }

                doc.Save(configFile);
            }
            catch (Exception ex)
            {
                String msg = "Installation aborted. Exception caught when saving configuration settings:  " + ex.ToString() + 
                    Environment.NewLine + Environment.NewLine + "Please notify BSD of this problem.";
                MessageBox.Show(msg, "Configuration Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ShutDown();
            }
        }

        private XElement GetSettingElement(List<XElement> settings, string settingName)
        {
            if (settings == null || settings.Count == 0 || string.IsNullOrWhiteSpace(settingName)) return null;
            try
            {
                return settings.FirstOrDefault(x => x.Attribute("name")?.Value?.ToLower() == settingName?.ToLower())?.Element("value");
            }
            catch { return null; }
        }

        private string GetConnStringValue(string configFile, string connStringName)
        {
            try
            {
                XDocument doc = XDocument.Load(configFile);
                return GetConnStringAttribute(doc, connStringName)?.Value?.Trim();
            }
            catch { return string.Empty; }
        }

        private string FetchSettingValue(List<XElement> settings, string settingName)
        {
            if (settings == null || settings.Count == 0 || string.IsNullOrWhiteSpace(settingName)) return string.Empty;

            try
            {
                return settings.FirstOrDefault(x => x.Attribute("name")?.Value.ToLower() == settingName?.ToLower())?.Element("value")?.Value;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Exception in FetchSettingValue() for setting [{settingName}]: " + ex); 
                return string.Empty;
            }
        }

        private List<XElement> GetAllSettings(string configFile)
        {
            XDocument doc = XDocument.Load(configFile);
            return GetAllSettings(doc);
        }

        private List<XElement> GetAllSettings(XDocument doc)
        {
            try
            {
                return doc.Element("configuration")?.Element("applicationSettings")
                    ?.Element($"{appName}.Properties.Settings")?.Elements("setting").ToList();
            }
            catch { return new List<XElement>(); }
        }

        private XAttribute GetConnStringAttribute(XDocument doc, string connStringName)
        {
            try
            {
                var conStrings = doc.Element("configuration")?.Element("connectionStrings")?.Elements("add");
                return conStrings?.FirstOrDefault(x => x.Attribute("name")?.Value == connStringName)
                    ?.Attribute("connectionString");
            }
            catch { return null; }
        }

        private string GetLogFolderValue(string configFile)
        {
            try
            {
                XDocument doc = XDocument.Load(configFile);
                return Path.GetDirectoryName(GetLogFileValueAttribute(doc)?.Value);
            }
            catch { return string.Empty; }
        }

        private XAttribute GetLogFileValueAttribute(XDocument doc)
        {
            try
            {
                XElement fileAppender = doc.Element("configuration")?.Element("log4net")?.Elements("appender")
                    ?.Where(x => x.Attribute("name")?.Value == "LogFileAppender").First();
                return fileAppender?.Elements("param")?.FirstOrDefault(x => x.Attribute("name")?.Value == "File")?.Attribute("value");
            }
            catch { return null; }
        }

        public void ShowError(string settingKind, string settingName)
        {
            string msg = $"Installer failed to update {settingKind} '{settingName}' in the config file. "
                         + Environment.NewLine + "Please update this setting manually.";
            MessageBox.Show(msg, "Installer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShutDown()
        {
            Cursor.Current = Cursors.Default;
            DialogResult = DialogResult.Abort;
            Close();
        }

    }
}
