// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace OverflowSensitivity
{
    public static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern ErrorModes SetErrorMode(ErrorModes uMode);

        [Flags]
        private enum ErrorModes : uint
        {
            SYSTEM_DEFAULT = 0x0,
            SEM_FAILCRITICALERRORS = 0x0001,
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
            SEM_NOGPFAULTERRORBOX = 0x0002,
            SEM_NOOPENFILEERRORBOX = 0x8000
        }

        public static int Main(string[] args)
        {
            // Prevent the "This program has stopped working" messages.
            SetErrorMode(ErrorModes.SEM_NOGPFAULTERRORBOX);

            if (args.Length != 1)
            {
                Console.WriteLine("You must pass an integer argument in to this program.");
                return -1;
            }

            Console.WriteLine($"Running in {IntPtr.Size * 8}-bit mode");

            if (int.TryParse(args[0], out var i))
            {
                CompileCode(MakeCode(i));
                return 0;
            }
            else
            {
                Console.WriteLine($"Could not parse {args[0]}");
                return -1;
            }
        }

        private static string MakeCode(int depth)
        {
            var builder = new StringBuilder();
            builder.AppendLine(
    @"class C {
    C M(string x) { return this; }
    void M2() {
        new C()
");
            for (int i = 0; i < depth; i++)
            {
                builder.AppendLine(@"            .M(""test"")");
            }
            builder.AppendLine(
           @"            .M(""test"");
    }
}");
            return builder.ToString();
        }
        private static void CompileCode(string stringText)
        {
            var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.None);
            var options = new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false);
            var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(stringText, encoding: null, SourceHashAlgorithm.Sha256), parseOptions);
            var reference = MetadataReference.CreateFromFile(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.2\mscorlib.dll");
            var comp = CSharpCompilation.Create("assemblyName", new SyntaxTree[] { tree }, references: new MetadataReference[] { reference }, options: options);
            var diag = comp.GetDiagnostics();
            if (!diag.IsDefaultOrEmpty)
            {
                throw new Exception(diag[0].ToString());
            }
        }
    }
}
