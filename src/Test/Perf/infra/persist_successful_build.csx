using System;
using System.IO;
using System.Linq;

string fileToUpdate = Args[0];
string successfulBuildNumber = Args[1];

var directoryName = Path.GetDirectoryName(fileToUpdate);
if (!Directory.Exists(directoryName))
{
    Directory.CreateDirectory(directoryName);
}

File.WriteAllText(fileToUpdate, successfulBuildNumber);