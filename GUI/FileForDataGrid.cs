using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GUI
{
    public class FileForDataGrid
    {
        public string FullFilePath { get; set; }
        public string FileNameWithExtension { get; set; }

        public FileForDataGrid(string fullFilePath)
        {
            FullFilePath = fullFilePath;
            FileNameWithExtension = Path.GetFileName(fullFilePath);
        }

        public override string ToString()
        {
            return FileNameWithExtension;
        }
    }
}
