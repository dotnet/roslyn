// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public static  class SolutionTestUtilities
    {
        public static byte[] GetResourceBytes(string fileName)
        {
            var fullName = @"Microsoft.CodeAnalysis.UnitTests.TestFiles." + fileName;
            var resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName);
            if (resourceStream != null)
            {
                using (resourceStream)
                {
                    var bytes = new byte[resourceStream.Length];
                    resourceStream.Read(bytes, 0, (int)resourceStream.Length);
                    return bytes;
                }
            }

            return null;
        }
    }
}
