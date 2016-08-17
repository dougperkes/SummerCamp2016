using System;
using Bounce.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using Windows.Media.Capture;
using Windows.System.Display;
using Microsoft.ProjectOxford.Face.Contract;
using Bounce.Services;
using Newtonsoft.Json;
using Windows.Foundation;
using Microsoft.ProjectOxford.Face;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using System.Linq;
using Windows.Storage.Streams;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using Windows.Storage.FileProperties;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml.Media.Imaging;

namespace Bounce.Views
{
    public sealed partial class MainPage : Page
    {

        MediaCapture _mediaCapture;
        bool _isPreviewing;
        DisplayRequest _displayRequest;

        public MainPage()
        {
            InitializeComponent();
            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            this.Loaded += MainPage_Loaded;
        }


        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await StartPreviewAsync();
        }


        private async Task StartPreviewAsync()
        {
            try
            {

                _mediaCapture = new MediaCapture();
                var cameraDevice = await GetVideoProfileSupportedDeviceIdAsync(Windows.Devices.Enumeration.Panel.Back);
                var mediaInitSettings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice };
                await _mediaCapture.InitializeAsync(mediaInitSettings);

                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;

                _displayRequest.RequestActive();
                //DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                System.Diagnostics.Debug.WriteLine("The app was denied access to the camera");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("MediaCapture initialization failed. {0}", ex.Message);
            }
        }

        public async Task<string> GetVideoProfileSupportedDeviceIdAsync(Windows.Devices.Enumeration.Panel panel)
        {
            string deviceId = string.Empty;

            // Finds all video capture devices
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            foreach (var device in devices)
            {
                // Check if the device on the requested panel supports Video Profile
                if (device.EnclosureLocation.Panel == panel)
                {
                    // We've located a device that supports Video Profiles on expected panel
                    deviceId = device.Id;
                    break;
                }
            }

            return deviceId;
        }

        private async void verifyButton_Click(object sender, RoutedEventArgs e)
        {
            var applicationData = Windows.Storage.ApplicationData.Current;
            var temporaryFolder = applicationData.TemporaryFolder;

            StorageFile file = await temporaryFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);

            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(captureStream);
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                    var properties = new BitmapPropertySet {
                        { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) }
                    };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);

                    await encoder.FlushAsync();
                }
            }

            string vipGroupId = "31bb6099-d409-4bdc-b35f-8ab9bf011668";
            string criminalGroupId = "3ff7668f-623e-48e5-a2f7-a10da867b484";
            bool isFound = false;
            isFound = await IdentifyUserInGroup(file, vipGroupId, "VIP", true);
            if (!isFound)
            {
                isFound = await IdentifyUserInGroup(file, criminalGroupId, "Criminal", false);
            }
            if (!isFound)
            {
                Log("Unknown person. Let 'em in!");
            }
            await CheckForFace(file);
            await ViewModel.UploadFile(file);
        }

        private async Task<bool> IdentifyUserInGroup(StorageFile imageToVerify, string groupId, string groupLabel, bool allowEntrance)
        {
            //string testImageFile = imageToVerify;
            bool isFound = false;
            using (var s = (await imageToVerify.OpenAsync(FileAccessMode.Read)).AsStream())
            {
                //initialize service
                var faceServiceClient = new FaceServiceClient(Models.StorageSettings.FaceAPIKey);
                var faces = await faceServiceClient.DetectAsync(s);
                var faceIds = faces.Select(face => face.FaceId).ToArray();

                IdentifyResult[] results = await faceServiceClient.IdentifyAsync(groupId, faceIds);
                foreach (var identifyResult in results)
                {

                    Log("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        //Log(string.Format("Not Authorized to enter"));
                    }
                    else
                    {
                        isFound = true;
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await faceServiceClient.GetPersonAsync(groupId, candidateId);
                        //faceName = person.Name;
                        if (allowEntrance)
                        {
                            Log(string.Format("Identified as {0} in {1} group Acess Granted to enter", person.Name, groupLabel));
                        } else
                        {
                            Log(string.Format("Identified as {0} in {1} group Acess Denied to enter", person.Name, groupLabel));

                        }
                        var pgresult = new Models.PersonGroupResult
                        {
                            Name = person.Name,
                            Group = groupLabel,
                            DateSeen = DateTime.UtcNow
                        };

                        string queueMessage = JsonConvert.SerializeObject(pgresult);
                        AzureUtil.SendMessage(queueMessage);

                    }
                }

            }
            return isFound;

        }

        private async Task CheckForFace(StorageFile imageToVerify)
        {
            // Create a filestream to read faces in images
            using (var s = (await imageToVerify.OpenStreamForReadAsync()))
            {
                try
                {
                    //initialize service
                    var faceServiceClient = new FaceServiceClient(Models.StorageSettings.FaceAPIKey);



                    var requiredFaceAttributes = new FaceAttributeType[] {
                                    FaceAttributeType.Age,
                                    FaceAttributeType.Gender,
                                    FaceAttributeType.Smile,
                                    FaceAttributeType.FacialHair,
                                    FaceAttributeType.HeadPose,
                                    FaceAttributeType.Glasses
                                };

                    //detect faces ( could be more than one ) 
                    Face[] faces = await faceServiceClient.DetectAsync(s, returnFaceId: true, returnFaceLandmarks: true, returnFaceAttributes: requiredFaceAttributes);

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
                        if (gender == "male") { heshe = "He"; } else { heshe = "She"; }

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
                        var faceName = "[ coming soon ]";
                        Log(String.Format("{7}'s attributes: {0} is {1} years old, {0}, {2}, {3}, {4}, {6}, and is {5} ", heshe, age, hasBeard, hasMustache, hasSideburns, smiling, hasGlases, faceName));

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

                        //var faceDirection = new Vector(
                        //    centerOfTwoEyes.X - centerOfMouth.X,
                        //    centerOfTwoEyes.Y - centerOfMouth.Y);
                    }
                }
                catch (FaceAPIException ex)
                {
                    //Write to screen
                    Log(String.Format("Response: {0}. {1}", ex.ErrorCode, ex.ErrorMessage));


                }
            }
        }

        /*
        private async Task MarkFaces(string pickedImagePath)
        {

            Uri fileUri = new Uri(pickedImagePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();
            using (Stream imageFileStream = File.OpenRead(pickedImagePath))
            {
                //initialize service
                var fileStream = File.OpenRead(pickedImagePath);
                var faceServiceClient = new FaceServiceClient(subscriptionKey);
                var faces = await faceServiceClient.DetectAsync(imageFileStream);
                var faceRects = faces.Select(face => face.FaceRectangle);
                var faces2 = faceRects.ToArray();





                if (faces2.Length > 0)
                {
                    DrawingVisual visual = new DrawingVisual();
                    DrawingContext drawingContext = visual.RenderOpen();
                    drawingContext.DrawImage(bitmapSource,
                        new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                    double dpi = bitmapSource.DpiX;
                    double resizeFactor = 96 / dpi;

                    foreach (var faceRect in faces2)
                    {
                        drawingContext.DrawRectangle(
                            Brushes.Transparent,
                            new Pen(Brushes.Red, 2),
                            new Rect(
                                faceRect.Left * resizeFactor,
                                faceRect.Top * resizeFactor,
                                faceRect.Width * resizeFactor,
                                faceRect.Height * resizeFactor
                                )
                        );
                    }

                    drawingContext.Close();
                    RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                        (int)(bitmapSource.PixelWidth * resizeFactor),
                        (int)(bitmapSource.PixelHeight * resizeFactor),
                        96,
                        96,
                        PixelFormats.Pbgra32);

                    faceWithRectBitmap.Render(visual);
                    shotImage.Source = faceWithRectBitmap;
                }

            }
        }
        */

        private ObservableCollection<Face> _foundFaceCollection = new ObservableCollection<Face>();
        public ObservableCollection<Face> FoundFaceCollection
        {
            get
            {
                return _foundFaceCollection;
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
                //string timeStr = DateTime.Now.ToString("HH:mm:ss.ffffff");
                //string messaage = "[" + timeStr + "]: " + logMessage + "\n";
                string messaage = logMessage + "\n";
                _logTextBox.Text += messaage;
            }
            //_logTextBox.ScrollToEnd();
        }

        public void ClearLog()
        {
            _logTextBox.Text = "";
        }

    }
}
