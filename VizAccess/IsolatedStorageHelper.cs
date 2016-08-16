
using System.IO;
using System.IO.IsolatedStorage;

namespace VizAccess
{
    /// <summary>
    /// A storage helper that's used to persist values to files to be used across pages
    /// </summary>
    internal class IsolatedStorageHelper
    {
        private static IsolatedStorageHelper s_helper;

        private IsolatedStorageHelper()
        {
        }
        /// <summary>
        /// Creates an Instance of the storage helper
        /// </summary>
        /// <returns></returns>
        public static IsolatedStorageHelper getInstance()
        {
            if (s_helper == null)
                s_helper = new IsolatedStorageHelper();
            return s_helper;
        }
        /// <summary>
        /// Reads a value from a given file
        /// </summary>
        /// <param name="filename">The file to read the value from</param>
        /// <returns>String representation of the value</returns>
        public string readValue(string filename)
        {
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                try
                {
                    using (var iStream = new IsolatedStorageFileStream(filename, FileMode.Open, isoStore))
                    {
                        using (var reader = new StreamReader(iStream))
                        {
                            return reader.ReadLine();
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// Writes a value to a given file
        /// </summary>
        /// <param name="fileName">The file to write the value to</param>
        /// <param name="value">The value to be written</param>
        public void writeValue(string fileName, string value)
        {
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                using (var oStream = new IsolatedStorageFileStream(fileName, FileMode.Create, isoStore))
                {
                    using (var writer = new StreamWriter(oStream))
                    {
                        writer.WriteLine(value);
                    }
                }
            }
        }
    }
}

