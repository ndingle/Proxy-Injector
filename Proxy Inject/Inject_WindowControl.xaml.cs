namespace Proxy_Inject
{
    using System.Windows;
    using System.Windows.Controls;
    using System.IO;
    using System;
    using Microsoft.VisualStudio.Shell;
    using EnvDTE;
    using System.Windows.Media;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio;
    using IniParser.Model;
    using IniParser;

    /// <summary>
    /// Interaction logic for Inject_WindowControl.
    /// </summary>
    public partial class Inject_WindowControl : UserControl, IVsSolutionEvents3
    {

        //Helped with access the DTE through a User Control window (I'm sure this is hacky)
        //https://stackoverflow.com/questions/7825489/how-do-i-subscribe-to-solution-and-project-events-from-a-vspackage


        private DTE _dte;
        private IVsSolution solution = null;
        private uint _hSolutionEvents = uint.MaxValue;

        string globalConfigFile = $@"C:\Users\{Environment.UserName}\.gitconfig";


        /// <summary>
        /// Initializes a new instance of the <see cref="Inject_WindowControl"/> class.
        /// </summary>
        public Inject_WindowControl()
        {
            this.InitializeComponent();
            this._dte = GetCurrentDTE();
            AdviseSolutionEvents();
            ReadProxySettings();
        }

        #region "Event attachment"
        private void AdviseSolutionEvents()
        {
            UnadviseSolutionEvents();

            solution = GetCurrentSolution();

            if (solution != null)
            {
                solution.AdviseSolutionEvents(this, out _hSolutionEvents);
            }
        }

        private void UnadviseSolutionEvents()
        {
            if (solution != null)
            {
                if (_hSolutionEvents != uint.MaxValue)
                {
                    solution.UnadviseSolutionEvents(_hSolutionEvents);
                    _hSolutionEvents = uint.MaxValue;
                }

                solution = null;
            }
        }
        #endregion

        #region "Get Solution Functions"
        /*
         * https://stackoverflow.com/questions/2336818/how-do-you-get-the-current-solution-directory-from-a-vspackage
         * */
        public static DTE GetCurrentDTE(IServiceProvider provider)
        {
            /*ENVDTE. */
            DTE vs = (DTE)provider.GetService(typeof(DTE));
            if (vs == null) throw new InvalidOperationException("DTE not found.");
            return vs;
        }


        public static DTE GetCurrentDTE()
        {
            //TODO: Test if this works with multiple instances, with different projects open
            return GetCurrentDTE(/* Microsoft.VisualStudio.Shell. */ServiceProvider.GlobalProvider);
        }

        public static IVsSolution GetCurrentSolution()
        {

            IServiceProvider provider = ServiceProvider.GlobalProvider;
            IVsSolution vs = provider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (vs == null) throw new InvalidOperationException("DTE not found.");
            return vs;
        }

        #endregion

        #region "Placeholder Text Events"
        private void txtProxyAddress_GotFocus(object sender, RoutedEventArgs e)
        {

            TextBox txt = (TextBox)sender;

            //If the tag matches the text, then we take away the placeholder
            if (txt.Tag.ToString() == txt.Text)
            {
                txt.Clear();
                txt.Foreground = Brushes.Black;
            }

        }


        private void txtProxyAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox txt = (TextBox)sender;

            //If they leave the box empty, go back to placeholder
            if (txt.Text.Length == 0 || txt.Text == txt.Tag.ToString())
            {
                txt.Text = txt.Tag.ToString();
                txt.Foreground = Brushes.LightGray;
            }
        }


        private void txtProxyPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            PasswordBox pass = (PasswordBox)sender;

            if (pass.Password.Length == 0 || pass.Password == pass.Tag.ToString())
            {
                pass.Password = pass.Tag.ToString();
                pass.Foreground = Brushes.LightGray;
            }
        }

        private void txtProxyPassword_GotFocus(object sender, RoutedEventArgs e)
        {

            PasswordBox pass = (PasswordBox)sender;

            //If the tag matches the text, then we take away the placeholder
            if (pass.Tag.ToString() == pass.Password)
            {
                pass.Clear();
                pass.Foreground = Brushes.Black;
            }

        }
        #endregion

        #region "Button Events"
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            txbStatus.Text = "";
            AddProxyDetails();
        }


        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            txbStatus.Text = "";
            RemoveProxyDetails();
            RemoveGitHubDetails();
        }
        #endregion

        #region "Input Validation"
        bool ValidInputString(TextBox txt)
        {

            //If the string hasn't changed, is empty or is just spaces, no go
            if (txt.Text == txt.Tag.ToString() ||
                txt.Text == "" ||
                txt.Text.Trim().Length == 0)
            {
                return false;
            }

            return true;

        }

        bool ValidInputString(PasswordBox txt)
        {

            //If the string hasn't changed, is empty or is just spaces, no go
            if (txt.Password == txt.Tag.ToString() ||
                txt.Password == "" ||
                txt.Password.Trim().Length == 0)
            {
                return false;
            }

            return true;

        }
        #endregion

        #region "File Functions"
        bool RemoveProxyDetails()
        {

            if (!File.Exists(globalConfigFile))
                return false;

            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(globalConfigFile);

            if (!data["http"].ContainsKey("proxy"))
                return false;

            data["http"].RemoveKey("proxy");
            data["https"].RemoveKey("proxy");

            //Reset
            txtProxyAddress.Text = txtProxyAddress.Tag.ToString();
            txtProxyAddress.Foreground = Brushes.LightGray;
            txtProxyUsername.Text = txtProxyUsername.Tag.ToString();
            txtProxyUsername.Foreground = Brushes.LightGray;
            txtProxyPassword.Password = txtProxyPassword.Tag.ToString();
            txtProxyPassword.Foreground = Brushes.LightGray;

            parser.WriteFile(globalConfigFile, data);

            return true;

        }


        bool RemoveGitHubDetails()
        {

            //Ensure a solution is open
            if (GetCurrentDTE().Solution.Count == 0)
                return false;

            //Read the project's path and thus the github folder
            string githubConfigFile = Path.GetDirectoryName(GetCurrentDTE().Solution.Properties.Item("Path").Value.ToString()) + "\\.git\\config";

            //Ensure there is a file
            if (!File.Exists(githubConfigFile))
                return false;

            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(githubConfigFile);

            //Do we have the key
            if (!data["remote \"origin\""].ContainsKey("url"))
                return false;

            //Read
            string url = data["remote \"origin\""]["url"];

            if (url.IndexOf("@") == -1)
                return false;

            //Remove the username and password
            url = url.Replace(GetSubStringBetweenTwoCharacters(url, "://", "@") + "@", "");
            data["remote \"origin\""]["url"] = url;
            parser.WriteFile(githubConfigFile, data);

            //Reset
            txtGitHubUsername.Text = txtGitHubUsername.Tag.ToString();
            txtGitHubUsername.Foreground = Brushes.LightGray;
            txtGitHubPassword.Password = txtGitHubPassword.Tag.ToString();
            txtGitHubPassword.Foreground = Brushes.LightGray;

            return true;

        }


        string GetSubStringBetweenTwoCharacters(string source, string leftDelimiter, string rightDelimiter)
        {
            //TODO: Test and use this function
            int leftIndex = source.IndexOf(leftDelimiter) + leftDelimiter.Length;
            int rightIndex = source.IndexOf(rightDelimiter);

            if (leftIndex >= 0 &&
                rightIndex >= 0)
                return source.Substring(leftIndex, rightIndex - leftIndex);
            else
                return "";

        }

        bool ReadProxySettings()
        {

            //Time to read the config file
            if (!File.Exists(globalConfigFile))
                return false;

            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(globalConfigFile);

            string proxyAddress = data["http"]["proxy"];
            if (proxyAddress == null)
                return false;

            //Get the http index
            int httpIndex = proxyAddress.IndexOf("://") + 3;

            //Does it have a username and password
            if (proxyAddress.IndexOf("@") >= 0)
            {

                //Get the username and password
                string userPassString = GetSubStringBetweenTwoCharacters(proxyAddress, "://", "@");
                string username = userPassString.Substring(0, userPassString.IndexOf(":"));
                string password = userPassString.Substring(userPassString.IndexOf(":") + 1);

                //Enter the data into the text boxes
                txtProxyUsername.Text = Uri.UnescapeDataString(username);
                txtProxyUsername.Foreground = Brushes.Black;
                txtProxyPassword.Password = Uri.UnescapeDataString(password);
                txtProxyPassword.Foreground = Brushes.Black;

                //Address time
                txtProxyAddress.Text = proxyAddress.Substring(0, httpIndex) +
                                        proxyAddress.Substring(proxyAddress.IndexOf("@") + 1);
                txtProxyAddress.Foreground = Brushes.Black;

            }
            else
            {
                txtProxyAddress.Text = proxyAddress;
                txtProxyAddress.Foreground = Brushes.Black;
            }

            return true;

        }


        bool ReadGitHubSettings()
        {

            //Ensure a solution is open
            if (GetCurrentDTE().Solution.Count == 0)
                return false;

            //Read the project's path and thus the github folder
            string githubConfigFile = Path.GetDirectoryName(GetCurrentDTE().Solution.Properties.Item("Path").Value.ToString()) + "\\.git\\config";


            //Check if the config file exists
            if (!File.Exists(githubConfigFile))
                return false;

            //Read the file
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(githubConfigFile);

            //Get the setting we need
            string url = data["remote \"origin\""]["url"];
            if (url == null)
                return false;

            //Check we have a value and username and password exist
            if (url != "" && url.IndexOf("@") >= 0)
            {
                //Cut up string to just the username and password
                string userPassString = GetSubStringBetweenTwoCharacters(url, "://", "@");
                string githubUsername = userPassString.Substring(0, userPassString.IndexOf(":"));
                string githubPassword = userPassString.Substring(userPassString.IndexOf(":") + 1);

                txtGitHubUsername.Text = Uri.UnescapeDataString(githubUsername);
                txtGitHubUsername.Foreground = Brushes.Black;
                txtGitHubPassword.Password = Uri.UnescapeDataString(githubPassword);
                txtGitHubPassword.Foreground = Brushes.Black;

            }
            else
                return false;

            //We won!
            return true;

        }

        bool AddProxyDetails()
        {

            //If either function fails, let me know
            if (!UpdateProxySettings()) return false;
            if (!UpdateGithubSettings()) return false;

            return true;
        }


        bool UpdateProxySettings()
        {

            //Fix global config by adding proxy
            if (!File.Exists(globalConfigFile))
            {
                txbStatus.Text += "No config file found.";
                return false;
            }

            //We only collect valid input strings
            string proxy = "";
            string proxyUsername = "";
            string proxyPassword = "";

            //TODO: Add regex validation for proxy address
            if (ValidInputString(txtProxyAddress))
                proxy = txtProxyAddress.Text.ToLower();

            //Check for filled in fields
            if (proxy.Length == 0)
            {
                txbStatus.Text += "Please provide at least a proxy address.";
                return false;
            }

            if (ValidInputString(txtProxyUsername))
                proxyUsername = Uri.EscapeDataString(txtProxyUsername.Text);

            if (ValidInputString(txtProxyPassword))
                proxyPassword = Uri.EscapeDataString(txtProxyPassword.Password);


            //Time to build a proxy string
            //Find the index of the :// from http:// or https://
            int httpIndex = proxy.IndexOf("://");

            //Check if there is a username and password
            if (proxyUsername != "" && proxyPassword != "")
            {
                //If there is no http(s), then we add http:// by default, otherwise add the username and password in place
                if (httpIndex == -1) proxy = $"http://{proxyUsername}:{proxyPassword}@{proxy}";
                else proxy = proxy.Insert(httpIndex + 3, $"{proxyUsername}:{proxyPassword}@");
            }
            else
            {
                //If there is no http(s), then we add http:// by default
                if (httpIndex == -1) proxy = $"http://{proxy}";
            }

            //Get the ini file
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(globalConfigFile);

            //string configText = File.ReadAllText(globalConfigFile);
            if (data["http"].ContainsKey("proxy"))
            {
                data["http"]["proxy"] = proxy;
                data["https"]["proxy"] = proxy;
                txbStatus.Text += "Proxy details updated.\n";
            }
            else
            {
                data["http"].AddKey("proxy", proxy);
                data["https"].AddKey("proxy", proxy);
                txbStatus.Text += "Proxy server settings added.\n";
            }

            //Update the file
            parser.WriteFile(globalConfigFile, data);
            return true;

        }


        bool UpdateGithubSettings()
        {

            //Ensure a solution is open
            if (GetCurrentDTE().Solution.Count == 0)
            {
                txbStatus.Text += "Unable to add github information, no solution open";
                return false;
            }

            //Read the project's path and thus the github folder
            string githubUsername = "";
            string githubPassword = "";
            string githubConfigFile = Path.GetDirectoryName(GetCurrentDTE().Solution.Properties.Item("Path").Value.ToString()) + "\\.git\\config";

            //Check if the config file exists
            if (!File.Exists(githubConfigFile))
            {
                txbStatus.Text += "No git in solution.";
                return false;
            }

            //Get the github settings
            if (ValidInputString(txtGitHubUsername))
                githubUsername = Uri.EscapeDataString(txtGitHubUsername.Text);

            if (ValidInputString(txtGitHubPassword))
                githubPassword = Uri.EscapeDataString(txtGitHubPassword.Password);


            //Ensure there are details
            if (githubUsername.Length == 0 ||
                githubPassword.Length == 0)
            {
                txbStatus.Text += "No github details provided.";
                return false;
            }


            //Read the file
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(githubConfigFile);

            string url = data["remote \"origin\""]["url"];
            if (url == null)
            {
                txbStatus.Text += "Unable to find remote github settings in config.\n";
                return false;
            }

            if (url.Contains("@"))
            {
                //Update username and password
                url = url.Replace(GetSubStringBetweenTwoCharacters(url, "://", "@") + "@", "");
                url = url.Replace("https://github.com/", $"https://{githubUsername}:{githubPassword}@github.com/");
            }
            else
            {
                //Update original string
                url = url.Replace("https://github.com/", $"https://{githubUsername}:{githubPassword}@github.com/");
            }

            data["remote \"origin\""]["url"] = url;
            parser.WriteFile(githubConfigFile, data);
            txbStatus.Text += "Github settings updated.\n";

            return true;

        }
        #endregion

        #region "Solution Events"
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            //Read the github settings
            ReadGitHubSettings();
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            //Reset the github boxes
            txtGitHubUsername.Text = txtGitHubUsername.Tag.ToString();
            txtGitHubUsername.Foreground = Brushes.LightGray;
            txtGitHubPassword.Password = txtGitHubPassword.Tag.ToString();
            txtGitHubPassword.Foreground = Brushes.LightGray;
            return VSConstants.S_OK;
        }

        public int OnAfterMergeSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpeningChildren(IVsHierarchy pHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeClosingChildren(IVsHierarchy pHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterClosingChildren(IVsHierarchy pHierarchy)
        {
            return VSConstants.S_OK;
        }
        #endregion

    }
}