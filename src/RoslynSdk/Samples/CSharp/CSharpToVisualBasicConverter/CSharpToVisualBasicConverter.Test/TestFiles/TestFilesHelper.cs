// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;

namespace CSharpToVisualBasicConverter.UnitTests.TestFiles
{
    internal class TestFilesHelper
    {
        public static string GetFile(string fileName)
        {
            string fullName = "CSharpToVisualBasicConverter.Test.TestFiles." + fileName;
            Stream resourceStream = Assembly.GetAssembly(typeof(TestFilesHelper)).GetManifestResourceStream(fullName);
            using (StreamReader streamReader = new StreamReader(resourceStream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }
}
