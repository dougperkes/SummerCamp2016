﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Microsoft.ProjectOxford.Face.Controls
{
    /// <summary>
    /// UI helper functions
    /// </summary>
    internal static class UIHelper
    {
        #region Methods

        /// <summary>
        /// Calculate the rendering face rectangle
        /// </summary>
        /// <param name="faces">Detected face from service</param>
        /// <param name="maxSize">Image rendering size</param>
        /// <param name="imageInfo">Image width and height</param>
        /// <returns>Face structure for rendering</returns>
        public static IEnumerable<Face> CalculateFaceRectangleForRendering(IEnumerable<Microsoft.ProjectOxford.Face.Contract.Face> faces, int maxSize, Tuple<int, int> imageInfo)
        {
            var imageWidth = imageInfo.Item1;
            var imageHeight = imageInfo.Item2;
            float ratio = (float)imageWidth / imageHeight;
            int uiWidth = 0;
            int uiHeight = 0;
            if (ratio > 1.0)
            {
                uiWidth = maxSize;
                uiHeight = (int)(maxSize / ratio);
            }
            else
            {
                uiHeight = maxSize;
                uiWidth = (int)(ratio * uiHeight);
            }

            int uiXOffset = (maxSize - uiWidth) / 2;
            int uiYOffset = (maxSize - uiHeight) / 2;
            float scale = (float)uiWidth / imageWidth;

            foreach (var face in faces)
            {
                yield return new Face()
                {
                    FaceId = face.FaceId.ToString(),
                    Left = (int)((face.FaceRectangle.Left * scale) + uiXOffset),
                    Top = (int)((face.FaceRectangle.Top * scale) + uiYOffset),
                    Height = (int)(face.FaceRectangle.Height * scale),
                    Width = (int)(face.FaceRectangle.Width * scale),
                };
            }
        }

        /// <summary>
        /// Get image basic information for further rendering usage
        /// </summary>
        /// <param name="imageFilePath">Path to the image file</param>
        /// <returns>Image width and height</returns>
        public static Tuple<int, int> GetImageInfoForRendering(string imageFilePath)
        {
            try
            {
                using (var s = File.OpenRead(imageFilePath))
                {
                    JpegBitmapDecoder decoder = new JpegBitmapDecoder(s, BitmapCreateOptions.None, BitmapCacheOption.None);
                    var frame = decoder.Frames.First();

                    // Store image width and height for following rendering
                    return new Tuple<int, int>(frame.PixelWidth, frame.PixelHeight);
                }
            }
            catch
            {
                return new Tuple<int, int>(0, 0);
            }
        }

        /// <summary>
        /// Append detected face to UI binding collection
        /// </summary>
        /// <param name="collections">UI binding collection</param>
        /// <param name="path">Original image path, used for rendering face region</param>
        /// <param name="face">Face structure returned from service</param>
        public static void UpdateFace(ObservableCollection<Face> collections, string path, Microsoft.ProjectOxford.Face.Contract.AddPersistedFaceResult face)
        {
            collections.Add(new Face()
            {
                ImagePath = path,
                FaceId = face.PersistedFaceId.ToString(),
            });
        }

        /// <summary>
        /// Append detected face to UI binding collection
        /// </summary>
        /// <param name="collections">UI binding collection</param>
        /// <param name="path">Original image path, used for rendering face region</param>
        /// <param name="face">Face structure returned from service</param>
        public static void UpdateFace(ObservableCollection<Face> collections, string path, Microsoft.ProjectOxford.Face.Contract.Face face)
        {
            collections.Add(new Face()
            {
                ImagePath = path,
                Left = face.FaceRectangle.Left,
                Top = face.FaceRectangle.Top,
                Width = face.FaceRectangle.Width,
                Height = face.FaceRectangle.Height,
                FaceId = face.FaceId.ToString(),
            });
        }

        /// <summary>
        /// Logging helper function
        /// </summary>
        /// <param name="log">log output instance</param>
        /// <param name="newMessage">message to append</param>
        /// <returns>log string</returns>
        public static string AppendLine(this string log, string newMessage)
        {
            return string.Format("{0}[{3}]: {2}{1}", log, Environment.NewLine, newMessage, DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
        }

        #endregion Methods
    }
}