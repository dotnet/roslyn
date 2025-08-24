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
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities;

/// <summary>
/// The assemblies produced here are designed to mimic the public key token structure of 
/// silverlight references. This often presents challenges to the compiler because it has
/// to know that two mscorlib with different public key tokens need to be treated as the 
/// identicial. The assemblies produced here have the same identity of those that come
/// from silverlight but without necessarily the same type contents.
/// </summary>
public static class Silverlight
{
    private static readonly Lazy<(byte[], byte[])> s_tuple = new Lazy<(byte[], byte[])>(
        () => BuildImages(),
        LazyThreadSafetyMode.PublicationOnly);

    public static byte[] Mscorlib => s_tuple.Value.Item1;

    public static byte[] System => s_tuple.Value.Item2;

    private static (byte[], byte[]) BuildImages()
    {
        const string corlibExtraCode = """
            namespace System.Reflection;

            [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
            public sealed class AssemblyFileVersionAttribute : Attribute
            {
                public string Version { get; }
                public AssemblyFileVersionAttribute(string version)
                {
                    Version = version;
                }
            }
            [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
            public sealed class AssemblyVersionAttribute : Attribute
            {
                public string Version { get; }
                public AssemblyVersionAttribute(string version)
                {
                    Version = version;
                }
            }
            """;

        const string assemblyAttributes = """
            using System.Reflection;

            [assembly: AssemblyFileVersion("5.0.5.0")]
            [assembly: AssemblyVersion("5.0.5.0")]
            """;

        var publicKeyText = "" +
            "00240000048000009400000006020000002400005253413100040000010001008d56c76f9e8649383049f" +
            "383c44be0ec204181822a6c31cf5eb7ef486944d032188ea1d3920763712ccb12d75fb77e9811149e6148" +
            "e5d32fbaab37611c1878ddc19e20ef135d0cb2cff2bfec3d115810c3d9069638fe4be215dbf795861920e" +
            "5ab6f7db2e2ceef136ac23d5dd2bf031700aec232f6c6b1c785b4305c123b37ab";
        var publicKey = TestHelpers.HexToByte(publicKeyText.AsSpan());
        var publicKeyToken = AssemblyIdentity.CalculatePublicKeyToken(publicKey);
        Debug.Assert("7C-EC-85-D7-BE-A7-79-8E" == BitConverter.ToString(publicKeyToken.ToArray()));

        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            cryptoPublicKey: publicKey,
            optimizationLevel: OptimizationLevel.Release);
        var mscorlibCompilation = CSharpCompilation.Create(
            "mscorlib",
            [
                CSharpSyntaxTree.ParseText(SourceText.From(TestResources.NetFX.Minimal.mincorlib_cs)),
                CSharpSyntaxTree.ParseText(SourceText.From(corlibExtraCode)),
                CSharpSyntaxTree.ParseText(SourceText.From(assemblyAttributes)),
            ],
            options: options);

        var mscorlib = mscorlibCompilation.EmitToStream(EmitOptions.Default.WithRuntimeMetadataVersion("v4.0.30319"));

        var systemCompilation = CSharpCompilation.Create(
            "System",
            syntaxTrees: [CSharpSyntaxTree.ParseText(SourceText.From(assemblyAttributes))],
            references: [mscorlibCompilation.EmitToImageReference()],
            options: options);

        var system = systemCompilation.EmitToStream();
        return (mscorlib.ToArray(), system.ToArray());
    }
}
