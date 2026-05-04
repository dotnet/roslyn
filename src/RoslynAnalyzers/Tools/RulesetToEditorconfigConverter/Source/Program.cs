// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.RulesetToEditorconfig;

if (args.Length is < 1 or > 2)
{
    ShowUsage();
    return 1;
}

var rulesetFilePath = args[0];
var editorconfigFilePath = args.Length == 2 ?
    args[1] :
    Path.Combine(Environment.CurrentDirectory, ".editorconfig");
try
{
    Converter.GenerateEditorconfig(rulesetFilePath, editorconfigFilePath);
}
catch (IOException ex)
{
    Console.WriteLine(ex.Message);
    return 2;
}

Console.WriteLine($"Successfully converted to '{editorconfigFilePath}'");
return 0;

static void ShowUsage()
{
    Console.WriteLine("Usage: RulesetToEditorconfigConverter.exe <%ruleset_file%> [<%path_to_editorconfig%>]");
    return;
}
