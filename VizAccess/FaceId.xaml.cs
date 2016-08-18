using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using ClientContract = Microsoft.ProjectOxford.Face.Contract;


namespace VizAccess
{
    /// <summary>
    /// Interaction logic for FaceId.xaml
    /// </summary>
    public partial class FaceId : Window
    {
        readonly string subscriptionKey = "c3c69602aecd442987f68ba9447a7be0";
        readonly string GroupName = "SummerCamp2016";
        readonly Guid GroupId = new Guid("31bb6099-d409-4bdc-b35f-8ab9bf011668");
        public FaceId()
        {
            InitializeComponent();
        }

        private static ObservableCollection<Person> _persons = new ObservableCollection<Person>();

        /// <summary>
        /// Gets person database
        /// </summary>
        public ObservableCollection<Person> Persons
        {
            get
            {
                return _persons;
            }
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            var groupExists = false;
            var faceServiceClient = new FaceServiceClient(subscriptionKey);
            var SuggestionCount = 15;
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

                await faceServiceClient.GetPersonGroupAsync(GroupId.ToString());
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


            if (groupExists)
            {
                var cleanGroup = System.Windows.MessageBox.Show(string.Format("Requires a clean up for group \"{0}\" before setup new person database. Click OK to proceed, group \"{0}\" will be fully cleaned up.", GroupName), "Warning", MessageBoxButton.OKCancel);
                if (cleanGroup == MessageBoxResult.OK)
                {
                    await faceServiceClient.DeletePersonGroupAsync(GroupId.ToString());
                }
                else
                {
                    return;
                }
            }

            //Persons.Clear();
            //TargetFaces.Clear();
            //SelectedFile = null;

            // Call create person group REST API
            // Create person group API call will failed if group with the same name already exists
            Log("Request: Creating group \"{0}\"", GroupName);
            try
            {
                await faceServiceClient.CreatePersonGroupAsync(GroupId.ToString(), GroupName);
                Log("Response: Success. Group \"{0}\" created", GroupName);
            }
            catch (FaceAPIException ex)
            {
                Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                return;
            }

            int processCount = 0;
            bool forceContinue = false;

            Log("Request: Preparing faces for identification, detecting faces in chosen folder.");

            // Enumerate top level directories, each directory contains one person's images
            foreach (var img in System.IO.Directory.EnumerateFiles(imagePath, "*.jpg"))
            {
                var tasks = new List<Task>();
                var tag = System.IO.Path.GetFileNameWithoutExtension(img);
                Person p = new Person();
                p.PersonName = tag;

                var faces = new ObservableCollection<Face>();
                p.Faces = faces;

                // Call create person REST API, the new create person id will be returned
                Log("Request: Creating person \"{0}\"", p.PersonName);
                p.PersonId = (await faceServiceClient.CreatePersonAsync(GroupId.ToString(), p.PersonName)).PersonId.ToString();
                Log("Response: Success. Person \"{0}\" (PersonID:{1}) created", p.PersonName, p.PersonId);

                // Enumerate images under the person folder, call detection
                //foreach (var img in System.IO.Directory.EnumerateFiles(dir, "*.jpg", System.IO.SearchOption.AllDirectories))
                //{
                    tasks.Add(Task.Factory.StartNew(
                        async (obj) =>
                        {
                            var imgPath = obj as string;

                            using (var fStream = File.OpenRead(imgPath))
                            {
                                try
                                {
                                        // Update person faces on server side
                                        var persistFace = await faceServiceClient.AddPersonFaceAsync(GroupId.ToString(), Guid.Parse(p.PersonId), fStream, imgPath);
                                    return new Tuple<string, ClientContract.AddPersistedFaceResult>(imgPath, persistFace);
                                }
                                catch (FaceAPIException)
                                {
                                        // Here we simply ignore all detection failure in this sample
                                        // You may handle these exceptions by check the Error.Error.Code and Error.Message property for ClientException object
                                        return new Tuple<string, ClientContract.AddPersistedFaceResult>(imgPath, null);
                                }
                            }
                        },
                        img).Unwrap().ContinueWith((detectTask) =>
                        {
                                // Update detected faces for rendering
                                var detectionResult = detectTask.Result;
                            if (detectionResult == null || detectionResult.Item2 == null)
                            {
                                return;
                            }

                            //this.Dispatcher.Invoke(
                            //    new Action<ObservableCollection<Face>, string, ClientContract.AddPersistedFaceResult>(UIHelper.UpdateFace),
                            //    faces,
                            //    detectionResult.Item1,
                            //    detectionResult.Item2);
                        }));
                    if (processCount >= SuggestionCount && !forceContinue)
                    {
                        //var continueProcess = System.Windows.Forms.MessageBox.Show("The images loaded have reached the recommended count, may take long time if proceed. Would you like to continue to load images?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo);
                        //if (continueProcess == System.Windows.Forms.DialogResult.Yes)
                        //{
                        //    forceContinue = true;
                        //}
                        //else
                        //{
                            break;
                        //}
                    }
                //}

                Persons.Add(p);

                await Task.WhenAll(tasks);
            }

            Log("Response: Success. Total {0} faces are detected.", Persons.Sum(p => p.Faces.Count));

            try
            {
                // Start train person group
                Log("Request: Training group \"{0}\"", GroupName);
                await faceServiceClient.TrainPersonGroupAsync(GroupId.ToString());

                // Wait until train completed
                while (true)
                {
                    await Task.Delay(1000);
                    var status = await faceServiceClient.GetPersonGroupTrainingStatusAsync(GroupId.ToString());
                    Log("Response: {0}. Group \"{1}\" training process is {2}", "Success", GroupName, status.Status);
                    if (status.Status != Status.Running)
                    {
                        break;
                    }
                }
            }
            catch (FaceAPIException ex)
            {
                Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
            }

        }




        public void Log(string logMessage, params object[] args)
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

        /// <summary>
        /// Person structure for UI binding
        /// </summary>
        public class Person : INotifyPropertyChanged
        {
            #region Fields

            /// <summary>
            /// Person's faces from database
            /// </summary>
            private ObservableCollection<Face> _faces = new ObservableCollection<Face>();

            /// <summary>
            /// Person's id
            /// </summary>
            private string _personId;

            /// <summary>
            /// Person's name
            /// </summary>
            private string _personName;

            #endregion Fields

            #region Events

            /// <summary>
            /// Implement INotifyPropertyChanged interface
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            #endregion Events

            #region Properties

            /// <summary>
            /// Gets or sets person's faces from database
            /// </summary>
            public ObservableCollection<Face> Faces
            {
                get
                {
                    return _faces;
                }

                set
                {
                    _faces = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Faces"));
                    }
                }
            }

            /// <summary>
            /// Gets or sets person's id
            /// </summary>
            public string PersonId
            {
                get
                {
                    return _personId;
                }

                set
                {
                    _personId = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("PersonId"));
                    }
                }
            }

            /// <summary>
            /// Gets or sets person's name
            /// </summary>
            public string PersonName
            {
                get
                {
                    return _personName;
                }

                set
                {
                    _personName = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("PersonName"));
                    }
                }
            }

            #endregion Properties
        }

    }
}
