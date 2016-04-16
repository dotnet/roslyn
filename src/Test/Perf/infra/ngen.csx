// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../util/test_util.csx"

using System.IO;

var ngen = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework\v4.0.30319\ngen.exe");
var ngen64 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework64\v4.0.30319\ngen.exe");

void NGen(string file, bool x86Only = false)
{
    Log($"> NGen {file}");
    var output = StdoutFrom(ngen, $"install \"{file}\" /nologo /nodependencies");
    Log(output);

    if (!x86Only)
    {
        Log($"> NGen (64 bit) {file}");
        output = StdoutFrom(ngen64, $"install \"{file}\" /nologo /nodependencies");
        Log(output);
    }
}
