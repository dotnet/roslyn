// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#load "../../util/test_util.csx"
using System.IO;

InitUtilities();

DownloadProject("csharp", 1);

string responseFile = "@" + Path.Combine(MyTempDirectory(), "csharp", "CSharpCompiler.rsp");
string keyfileLocation = Path.Combine(MyTempDirectory(), "csharp", "keyfile", "35MSSharedLib1024.snk");
string args = $"{responseFile} /keyfile:{keyfileLocation}";

string executeInDirectory = Path.Combine(MyTempDirectory(), "csharp");

var msToCompile = WalltimeMs(() => ShellOutVital(DebugCscPath(), args, executeInDirectory));
Report("compile duration (ms)", msToCompile);
