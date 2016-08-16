// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.IO;

namespace VizAccess
{
    public class PhotoList : ObservableCollection<ImageFile>
    {
        private DirectoryInfo _directory;

        public PhotoList()
        {
        }

        public PhotoList(string path) : this(new DirectoryInfo(path))
        {
        }

        public PhotoList(DirectoryInfo directory)
        {
            _directory = directory;
            Update();
        }

        public string Path
        {
            set
            {
                _directory = new DirectoryInfo(value);
                Update();
            }
            get { return _directory.FullName; }
        }

        public DirectoryInfo Directory
        {
            set
            {
                _directory = value;
                Update();
            }
            get { return _directory; }
        }

        private void Update()
        {
            foreach (var f in _directory.GetFiles("*.png"))
            {
                Add(new ImageFile(f.FullName));
            }
        }
    }
}