using Microsoft.ProjectOxford.Face;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VizAccess
{
    /// <summary>
    /// Interaction logic for FaceId.xaml
    /// </summary>
    public partial class FaceId : Window
    {
        readonly string subscriptionKey = "c3c69602aecd442987f68ba9447a7be0";
        readonly string GroupName = "SummerCamp2016";
        public FaceId()
        {
            InitializeComponent();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            var groupExists = false;
            var faceServiceClient = new FaceServiceClient(subscriptionKey);

            string root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var imagePath = System.IO.Path.Combine(root, "../../Photos/");
            var files = Directory.GetFiles(imagePath);
            foreach(var photoFile in files)
            {
               // var faceId = await CheckForFace(photoFile);

            }

            try
            {
                Log("Request: Group {0} will be used for build person database. Checking whether group exists.", GroupName);

                await faceServiceClient.GetPersonGroupAsync(GroupName);
                groupExists = true;
                Log("Response: Group {0} exists.", GroupName);
            }
            catch (FaceAPIException ex)
            {
                if (ex.ErrorCode != "PersonGroupNotFound")
                {
                    Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                    return;
                }
                else
                {
                    Log("Response: Group {0} does not exist before.", GroupName);
                }
            }

        }




        public void Log(string logMessage, params string[] args)
        {
            Log(string.Format(logMessage, args));
        }
        public void Log(string logMessage)
        {
            if (String.IsNullOrEmpty(logMessage) || logMessage == "\n")
            {
                _logTextBox.Text += "\n";
            }
            else
            {
                string timeStr = DateTime.Now.ToString("HH:mm:ss.ffffff");
                string messaage = "[" + timeStr + "]: " + logMessage + "\n";
                _logTextBox.Text += messaage;
            }
            _logTextBox.ScrollToEnd();
        }

    }
}
