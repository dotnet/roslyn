// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities;

/// <summary>
/// This type produces stdole.dll that mimic the version used in the original Roslyn 
/// interop tests.
/// </summary>
public static class StdOle
{
    public static PortableExecutableReference Build(IEnumerable<MetadataReference> references)
    {
        const string assemblyAttributes = """
            using System.Reflection;
            using System.Runtime.InteropServices;

            [assembly: ImportedFromTypeLib("stdole")]
            [assembly: Guid("00020430-0000-0000-c000-000000000046")]
            [assembly: PrimaryInteropAssembly(2, 0)]
            [assembly: AssemblyVersion("7.0.3300.0")]

            """;

        const string code = """
            using System.Runtime.InteropServices;
            namespace stdole;

            public struct GUID
            {
                public uint Data1;
                public ushort Data2;
                public ushort Data3;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
                public byte[] Data4;
            }

            [ComImport]
            [Guid("00020400-0000-0000-C000-000000000046")]
            [TypeLibType(512)]
            public interface IDispatch
            {
            }
            """;

        var publicKeyText = "" +
            "002400000480000094000000060200000024000052534131000400000100010007d1fa57c4aed9f0a32e8" +
            "4aa0faefd0de9e8fd6aec8f87fb03766c834c99921eb23be79ad9d5dcc1dd9ad236132102900b723cf980" +
            "957fc4e177108fc607774f29e8320e92ea05ece4e821c0a5efe8f1645c4c0c93c1ab99285d622caa652c1" +
            "dfad63d745d6f2de5f17e5eaf0fc4963d261c8a12436518206dc093344d5ad293";
        var publicKey = TestHelpers.HexToByte(publicKeyText.AsSpan());
        var publicKeyToken = AssemblyIdentity.CalculatePublicKeyToken(publicKey);
        Debug.Assert("B0-3F-5F-7F-11-D5-0A-3A" == BitConverter.ToString(publicKeyToken.ToArray()));

        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            cryptoPublicKey: publicKey,
            optimizationLevel: OptimizationLevel.Release);
        var compilation = CSharpCompilation.Create(
            "stdole",
            [
                CSharpSyntaxTree.ParseText(SourceText.From(code)),
                CSharpSyntaxTree.ParseText(SourceText.From(assemblyAttributes))
            ],
            references: references,
            options);

        return compilation.EmitToPortableExecutableReference();
    }
}
