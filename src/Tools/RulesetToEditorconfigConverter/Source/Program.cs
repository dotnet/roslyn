// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.RulesetToEditorconfig
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
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
        }
    }
}
