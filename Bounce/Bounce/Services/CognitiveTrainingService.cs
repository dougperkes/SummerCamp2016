using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientContract = Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace Bounce.Services
{
    class CognitiveTrainingService
    {

        public static async Task Train()
        {
            string VIPGroupName = "VIP";
            Guid VIPGroupId = new Guid("31bb6099-d409-4bdc-b35f-8ab9bf011668");
            string CriminalGroupName = "Criminal";
            Guid CriminalGroupId = new Guid("3ff7668f-623e-48e5-a2f7-a10da867b484");

            //await TrainGroup(VIPGroupName, VIPGroupId, "VIPs");
            //await Task.Delay(1000);
            await TrainGroup(CriminalGroupName, CriminalGroupId, "Criminals");

        }

        private static async Task TrainGroup(string groupName, Guid groupId, string imagesFolderName)
        {
            var groupExists = false;
            var faceServiceClient = new FaceServiceClient(Models.StorageSettings.FaceAPIKey);
            var imagePath = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync(@"Assets\" + imagesFolderName);

            var files = await imagePath.GetFilesAsync();

            try
            {
                Log("Request: Group {0} will be used for build person database. Checking whether group exists.", groupName);

                await faceServiceClient.GetPersonGroupAsync(groupId.ToString());
                groupExists = true;
                Log("Response: Group {0} exists.", groupName);
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
                    Log("Response: Group {0} does not exist before.", groupName);
                }
            }


            if (groupExists)
            {
                await faceServiceClient.DeletePersonGroupAsync(groupId.ToString());
            }

            // Call create person group REST API
            // Create person group API call will failed if group with the same name already exists
            Log("Request: Creating group \"{0}\"", groupName);
            try
            {
                await faceServiceClient.CreatePersonGroupAsync(groupId.ToString(), groupName);
                Log("Response: Success. Group \"{0}\" created", groupName);
            }
            catch (FaceAPIException ex)
            {
                Log("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage);
                return;
            }

            //int processCount = 0;
            //bool forceContinue = false;

            Log("Request: Preparing faces for identification, detecting faces in chosen folder.");

            // Enumerate top level directories, each directory contains one person's images
            foreach (var photoFile in files)
            {
                var tasks = new List<Task>();
                var tag = photoFile.DisplayName;  //System.IO.Path.GetFileNameWithoutExtension(img);
                Person p = new Person();
                p.PersonName = tag;

                var faces = new ObservableCollection<Face>();
                p.Faces = faces;

                // Call create person REST API, the new create person id will be returned
                Log("Request: Creating person \"{0}\"", p.PersonName);
                p.PersonId = (await faceServiceClient.CreatePersonAsync(groupId.ToString(), p.PersonName)).PersonId.ToString();
                Log("Response: Success. Person \"{0}\" (PersonID:{1}) created", p.PersonName, p.PersonId);

                var photoStream = await photoFile.OpenStreamForReadAsync();
                var persistFace = await
                    faceServiceClient.AddPersonFaceAsync(groupId.ToString(), Guid.Parse(p.PersonId), photoStream, photoFile.Path);

                await Task.Delay(1000);
            }

            //Log("Response: Success. Total {0} faces are detected.", Persons.Sum(p => p.Faces.Count));

            try
            {
                // Start train person group
                Log("Request: Training group \"{0}\"", groupName);
                await faceServiceClient.TrainPersonGroupAsync(groupId.ToString());

                // Wait until train completed
                while (true)
                {
                    await Task.Delay(1000);
                    var status = await faceServiceClient.GetPersonGroupTrainingStatusAsync(groupId.ToString());
                    Log("Response: {0}. Group \"{1}\" training process is {2}", "Success", groupName, status.Status);
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

        public static void Log(string logMessage, params object[] args)
        {
            Log(string.Format(logMessage, args));
        }
        public static void Log(string logMessage)
        {
            if (String.IsNullOrEmpty(logMessage) || logMessage == "\n")
            {
                //_logTextBox.Text += "\n";
            }
            else
            {
                string timeStr = DateTime.Now.ToString("HH:mm:ss.ffffff");
                string messaage = "[" + timeStr + "]: " + logMessage + "\n";
                //_logTextBox.Text += messaage;
            }
            //_logTextBox.ScrollToEnd();
        }
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
