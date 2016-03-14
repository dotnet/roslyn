// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../util/test_util.csx"

using System.IO;

var fakeSign = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".nuget\FakeSign\0.9.2\tools\FakeSign.exe");

if (!File.Exists(fakeSign))
{
    throw new FileNotFoundException("NGen requires the FakeSign utility.", fakeSign);
}

var ngen = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\ngen.exe");
var ngen64 = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Microsoft.NET\Framework64\ngen.exe");

void NGen(string file, bool x86Only = false)
{
    // We'll have to remove the "fake sign" bit in order to ngen.
    // Note: "FakeSign -u" may fail for reasons that should not prevent us from trying to ngen
    // (for instance, if the binary was not "fake signed" to begin with).
    ShellOut(fakeSign + " -u " + destinationFile);

    var result = ShellOut(ngen, $"install {destinationFile} /nodependencies");
    if (!result.Succeeded)
    {
        return result.Code;
    }

    if (!x86Only)
    {
        result = ShellOut(ngen64, $"install {destinationFile} /nodependencies");
        if (!result.Succeeded)
        {
            return result.Code;
        }
    }
}
