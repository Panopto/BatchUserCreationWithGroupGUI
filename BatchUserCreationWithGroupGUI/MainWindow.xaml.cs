using System;
using System.ComponentModel;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Timers;
using System.Windows;
using System.Windows.Forms;

namespace BatchUserCreationWithGroupGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool selfSigned = true; // Target server is a self-signed server
        private static bool hasBeenInitialized = false;
        private static System.Timers.Timer timer;

        public MainWindow()
        {
            InitializeComponent();

            if (selfSigned)
            {
                // For self-signed servers
                EnsureCertificateValidation();
            }
        }

        /// <summary>
        /// Opens a file chooser and allows the user to pick which file to upload
        /// </summary>
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.DefaultExt = ".csv";
            dlg.Filter = "Comma Separated Values File (*.csv)|*.csv";
            dlg.Multiselect = false;

            DialogResult result = dlg.ShowDialog();

            if (result.Equals(System.Windows.Forms.DialogResult.OK))
            {
                FileInput.Text = System.IO.Path.GetFullPath(dlg.FileName);
            }
        }

        /// <summary>
        /// Creates a timer that calls method to create user after short interval to
        /// allow for UI changes
        /// </summary>
        /// <param name="sender">object that calls this method</param>
        /// <param name="e">arguments</param>
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            LockAllFields();
            Status.Content = "Creating...";

            ErrorLog.Text = "";

            timer = new System.Timers.Timer(1500);
            timer.Elapsed += DispatchUpload;

            timer.Start();
        }

        /// <summary>
        /// Disabling all fields
        /// </summary>
        private void LockAllFields()
        {
            UserID.IsEnabled = false;
            UserPassword.IsEnabled = false;
            Browse.IsEnabled = false;
            Create.IsEnabled = false;
        }

        /// <summary>
        /// Enabling all fields that are originally enabled
        /// </summary>
        private void FreeAllFields()
        {
            UserID.IsEnabled = true;
            UserPassword.IsEnabled = true;
            Browse.IsEnabled = true;
            Create.IsEnabled = true;
        }

        /// <summary>
        /// Calls the create user method
        /// </summary>
        private void DispatchUpload(Object source, ElapsedEventArgs e)
        {
            timer.Stop();

            Action uploadMethod = ProcessBackground;
            Dispatcher.BeginInvoke(uploadMethod);
        }

        /// <summary>
        /// Sets up BackgroundWorker for user creation in background and starts BackgroundWorker
        /// </summary>
        private void ProcessBackground()
        {
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += new DoWorkEventHandler(ProcessCreation);
            bgw.ProgressChanged += new ProgressChangedEventHandler(CreationStatus);
            bgw.WorkerReportsProgress = true;
            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(CreationComplete);

            string filePaths = FileInput.Text;

            object[] args = new object[] { UserID.Text, UserPassword.Password, filePaths };

            bgw.RunWorkerAsync(args);
        }

        /// <summary>
        /// Methods to creates users
        /// </summary>
        /// <param name="sender">BackgroundWorker Object</param>
        /// <param name="e">Arguments necessary to create user</param>
        private void ProcessCreation(object sender, DoWorkEventArgs e)
        {
            string errorMessage = "Error Log:";
            bool openFileSuccess = true;
            bool hasError = false;

            BackgroundWorker bgw = sender as BackgroundWorker;

            // Variables needed to create user
            object[] args = e.Argument as object[];
            string filePath = args[2] as string;
            string userName = args[0] as string;
            string password = args[1] as string;

            // Parse the CSV file given
            string[] userInfos = null;
            try
            {
                userInfos = System.IO.File.ReadAllLines(filePath);
            }
            catch (Exception ex)
            {
                hasError = true;
                openFileSuccess = false;
                errorMessage = "Failed to open file: " + ex.Message;
            }

            // Create each user in the CSV file
            if (openFileSuccess)
            {
                foreach (string singleUser in userInfos)
                {
                    string singleError = ManagementWrapper.FullyCreateUser(userName, password, singleUser);
                    if (singleError != null)
                    {
                        hasError = true;
                        errorMessage += singleError;
                    }
                }
            }

            // Handle overall status
            if (hasError)
            {
                if (!openFileSuccess)
                {
                    bgw.ReportProgress(0, -1 + "~" + errorMessage);
                }
                else
                {
                    bgw.ReportProgress(0, 0 + "~" + errorMessage);
                }
            }
            else
            {
                bgw.ReportProgress(100, 1 + "~" + "Creation Successful");
            }
        }

        /// <summary>
        /// Updates status of user creation to UI
        /// </summary>
        /// <param name="sender">BackgroundWorker Object</param>
        /// <param name="e">Arguments used to update status</param>
        private void CreationStatus(object sender, ProgressChangedEventArgs e)
        {
            int errorCode = Convert.ToInt16((e.UserState as string).Split('~')[0]);
            string msg = (e.UserState as string).Split('~')[1];

            if (errorCode == -1)
            {
                Status.Content = msg;
            }
            else if (errorCode == 0)
            {
                Status.Content = "Creation process experienced error";
                ErrorLog.Text = msg;
            }
            else
            {
                Status.Content = msg;
            }
        }

        /// <summary>
        /// Free fields in UI
        /// </summary>
        /// <param name="sender">BackgoundWorker Object</param>
        /// <param name="e">Arguments to be used</param>
        private void CreationComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            FreeAllFields();
        }

        //========================= Needed to use self-signed servers

        /// <summary>
        /// Ensures that our custom certificate validation has been applied
        /// </summary>
        public static void EnsureCertificateValidation()
        {
            if (!hasBeenInitialized)
            {
                ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(CustomCertificateValidation);
                hasBeenInitialized = true;
            }
        }

        /// <summary>
        /// Ensures that server certificate is authenticated
        /// </summary>
        private static bool CustomCertificateValidation(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }
    }
}
