using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bounce.Models
{
    static class StorageSettings
    {
        static Windows.Storage.ApplicationDataContainer localSettings =
                Windows.Storage.ApplicationData.Current.LocalSettings;

        public static string FaceAPIKey
        {
            get
            {
                Object value = localSettings.Values["FaceAPIKey"];
                return value as string;
            }
            set
            {
                localSettings.Values["FaceAPIKey"] = value;
            }
        }
        public static string StorageAccountKey
        {
            get
            {
                Object value = localSettings.Values["StorageAccountKey"];
                return value as string;
            }
            set
            {
                localSettings.Values["StorageAccountKey"] = value;
            }
        }

        public static string StorageAccountName
        {
            get
            {
                Object value = localSettings.Values["StorageAccountName"];
                return value as string;
            }
            set
            {
                localSettings.Values["StorageAccountName"] = value;
            }
        }

        public static string StorageAccountQueue
        {
            get
            {
                Object value = localSettings.Values["StorageAccountQueue"];
                return value as string;
            }
            set
            {
                localSettings.Values["StorageAccountQueue"] = value;
            }
        }

    }

}
