using System;
using System.Collections.Generic;
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
using WebEye.Controls.Wpf;
using WebEye.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Controls;
using System.ComponentModel;
using Microsoft.ProjectOxford.Face.Contract;
using Newtonsoft.Json;

namespace VizAccess
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FaceVerify : Window
    {


        public PhotoList Photos;
        public string FullResult;
        public string faceName;

        string cameraPic;
        //subscription key
        string subscriptionKey = "c3c69602aecd442987f68ba9447a7be0";

        public FaceVerify()
        {

            InitializeComponent();
            InitializeComboBox();
   

        }

        private ObservableCollection<Face> _foundFaceCollection = new ObservableCollection<Face>();
        public ObservableCollection<Face> FoundFaceCollection
        {
            get
            {
                return _foundFaceCollection;
            }
        }


        private void InitializeComboBox()
        {
            comboBox.ItemsSource = webCameraControl.GetVideoCaptureDevices();

            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedItem = comboBox.Items[0];
            }
        }

        private void StartCamera()
        {
            var cameraId = (WebCameraId)comboBox.SelectedItem;
            webCameraControl.StartCapture(cameraId);

        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {

            StartCamera();


        }




        private async void Verification_Click(object sender, RoutedEventArgs e)
        {
            string root = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var imagePath = System.IO.Path.Combine(root, "../../Assets/");
            cameraPic = imagePath + "picture.bmp";
            webCameraControl.GetCurrentImage().Save(cameraPic);

            await IdentifyUserInGroup(cameraPic);
            await CheckForFace(cameraPic);


        }

        private async Task IdentifyUserInGroup(string imageToVerify)
        {
            string testImageFile = imageToVerify;

            using (Stream s = File.OpenRead(testImageFile))
            {
                string personGroupId = "31bb6099-d409-4bdc-b35f-8ab9bf011668";
                //initialize service
                var faceServiceClient = new FaceServiceClient(subscriptionKey);
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                var results = await faceServiceClient.IdentifyAsync(personGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        Log(string.Format("Not Authorized to enter"));
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceServiceClient.GetPersonAsync(personGroupId, candidateId);
                        faceName = person.Name;
                        Log(string.Format("Identified as {0} Acess Granted to enter", person.Name));
                    }
                }
            }


        }

        private async Task CheckForFace(string pickedImagePath)
        {
            // Create a filestream to read faces in images
            using (var fileStream = File.OpenRead(pickedImagePath))
            {
                try
                {
                    //initialize service
                    var faceServiceClient = new FaceServiceClient(subscriptionKey);

                   

                    var requiredFaceAttributes = new FaceAttributeType[] {
                FaceAttributeType.Age,
                FaceAttributeType.Gender,
                FaceAttributeType.Smile,
                FaceAttributeType.FacialHair,
                FaceAttributeType.HeadPose,
                FaceAttributeType.Glasses
            };

                    //detect faces ( could be more than one ) 
                    var faces = await faceServiceClient.DetectAsync(fileStream, returnFaceId: true, returnFaceLandmarks: true, returnFaceAttributes: requiredFaceAttributes);

                    // If it does not find any faces
                    // right now program crashes if no faces
                    if (faces == null)
                    {
                        Log("No Faces were found in picture");
                        return;
                    }

                    //Write to screen
                    //Log(String.Format("Response: Success. Detected {0} face(s) in {1}", faces.Length, pickedImagePath));

                    // calc rectange for face
                    foreach (Microsoft.ProjectOxford.Face.Contract.Face face in faces)
                    {
                        // Add faces to face collection
                        FoundFaceCollection.Add(face);

                        string queueMessage = JsonConvert.SerializeObject(face);
                        AzureUtil.SendMessage(queueMessage);

                        var rect = face.FaceRectangle;
                        var landmarks = face.FaceLandmarks;
                        double noseX = landmarks.NoseTip.X;
                        double noseY = landmarks.NoseTip.Y;

                        double leftPupilX = landmarks.PupilLeft.X;
                        double leftPupilY = landmarks.PupilLeft.Y;

                        double rightPupilX = landmarks.PupilRight.X;
                        double rightPupilY = landmarks.PupilRight.Y;

                        string heshe;
                        string smiling = "is smiling";
                        string hasBeard = "has a beard";
                        string hasMustache = "has a mustache";
                        string hasSideburns = "has sideburns";
                        string hasGlases = "has no glasses";
                        var id = face.FaceId;
                        var attributes = face.FaceAttributes;
                        var age = attributes.Age;
                        var gender = attributes.Gender;
                        if (gender == "male"){ heshe = "He"; } else { heshe = "She"; }

                        var smile = attributes.Smile;
                        if (smile <= .5) { smiling = "is not smiling"; }

                        var facialHair = attributes.FacialHair;
                        var beard = attributes.FacialHair.Beard;
                        if (beard <= .5) { hasBeard = "does not have a beard"; } 
                        var mustache = attributes.FacialHair.Moustache;
                        if (mustache <= .5) { hasMustache = "does not have a mustache"; } 
                        var sideburns = attributes.FacialHair.Sideburns;
                        if (sideburns <= .5) { hasSideburns = "does not have sideburns"; } 

                        var headPose = attributes.HeadPose;
                        var glasses = attributes.Glasses.ToString();
                        if (glasses != "NoGlasses") { hasGlases = attributes.Glasses.ToString(); }

                        Log(String.Format("{7}'s attributes: {0} is {1} years old, {0}, {2}, {3}, {4}, {6}, and is {5} ", heshe,age,hasBeard,hasMustache,hasSideburns,smiling,hasGlases, faceName));

                        //use information to calculate other information


                        var upperLipBottom = landmarks.UpperLipBottom;
                        var underLipTop = landmarks.UnderLipTop;

                        var centerOfMouth = new Point(
                            (upperLipBottom.X + underLipTop.X) / 2,
                            (upperLipBottom.Y + underLipTop.Y) / 2);

                        var eyeLeftInner = landmarks.EyeLeftInner;
                        var eyeRightInner = landmarks.EyeRightInner;

                        var centerOfTwoEyes = new Point(
                            (eyeLeftInner.X + eyeRightInner.X) / 2,
                            (eyeLeftInner.Y + eyeRightInner.Y) / 2);

                        Vector faceDirection = new Vector(
                            centerOfTwoEyes.X - centerOfMouth.X,
                            centerOfTwoEyes.Y - centerOfMouth.Y);
                    }
                }
                catch (FaceAPIException ex)
                {
                    //Write to screen
                    Log(String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage));

                    return;
                }
            }
        }

        private async Task CompareFaces(Guid faceId1, Guid faceId2)
        {
            Log(String.Format("Request: Verifying face {0} and {1}", faceId1, faceId2));

            // Call verify passing in faceIDs
            try
            {
                var faceServiceClient = new FaceServiceClient(subscriptionKey);
                var res = await faceServiceClient.VerifyAsync(faceId1, faceId2);

                if (res.IsIdentical)
                {
                  //what do we want to do her
                }
                // Verification result contains IsIdentical (true or false) and Confidence (in range 0.0 ~ 1.0),
                FullResult = string.Format("Response: Success. Face {0} and {1} {2} to the same person", faceId1, faceId2, res.IsIdentical ? "belong" : "not belong");
                Log(FullResult);
                decimal confidence = Convert.ToDecimal(res.Confidence) * 100;

                Log("Confidence " + String.Format("{0:0.00}", confidence) + "%");


            }
            catch (FaceAPIException ex)
            {
                Log(String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage));

                return;
            }
        }


        public void Log(string logMessage)
        {
            if (String.IsNullOrEmpty(logMessage) || logMessage == "\n")
            {
                _logTextBox.Text += "\n";
            }
            else
            {
                //string timeStr = DateTime.Now.ToString("HH:mm:ss.ffffff");
                //string messaage = "[" + timeStr + "]: " + logMessage + "\n";
                string messaage = logMessage + "\n";
                _logTextBox.Text += messaage;
            }
            _logTextBox.ScrollToEnd();
        }

        public void ClearLog()
        {
            _logTextBox.Text = "";
        }

        public int MaxImageSize
        {
            get
            {
                return 300;
            }
        }

        private void manageButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FaceId();
            dlg.ShowDialog();
        }
    }


}