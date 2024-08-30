// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestHelpers;
using KeyValuePairUtil = Roslyn.Utilities.KeyValuePairUtil;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CompilationAPITests : CSharpTestBase
    {
        private CSharpCompilationOptions WithDiagnosticOptions(
            SyntaxTree tree,
            params (string, ReportDiagnostic)[] options)
            => TestOptions.DebugDll.WithSyntaxTreeOptionsProvider(new TestSyntaxTreeOptionsProvider(tree, options));

        [Fact]
        public void TreeDiagnosticOptionsDoNotAffectTreeDiagnostics()
        {
#pragma warning disable CS0618 // This test is intentionally calling the obsolete method to assert the diagnosticOptions input is now ignored

            var tree = SyntaxFactory.ParseSyntaxTree(@"
/// <see cref=""x...y""/>
class C { }",
                options: CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose),
                path: "",
                encoding: null,
                diagnosticOptions: CreateImmutableDictionary(("CS1584", ReportDiagnostic.Suppress)),
                cancellationToken: default);

            tree.GetDiagnostics().Verify(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'x...y'
                // /// <see cref="x...y"/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "x").WithArguments("x...y").WithLocation(2, 16),
                // (2,17): warning CS1658: Unexpected character '.'. See also error CS1056.
                // /// <see cref="x...y"/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "").WithArguments("Unexpected character '.'", "1056").WithLocation(2, 17));

#pragma warning restore CS0618
        }

        [Fact]
        public void PerTreeVsGlobalSuppress()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class C { long _f = 0l;}");
            var options = TestOptions.DebugDll
                .WithGeneralDiagnosticOption(ReportDiagnostic.Suppress);
            var comp = CreateCompilation(tree, options: options);
            comp.VerifyDiagnostics();

            options = options.WithSyntaxTreeOptionsProvider(
                new TestSyntaxTreeOptionsProvider((tree, new[] { ("CS0078", ReportDiagnostic.Warn) })));

            comp = CreateCompilation(tree, options: options);
            // Syntax tree diagnostic options override global settting
            comp.VerifyDiagnostics(
                // (1,22): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                // class C { long _f = 0l;}
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(1, 22));
        }

        [Fact]
        public void PerTreeDiagnosticOptionsParseWarnings()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class C { long _f = 0l;}");

            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics(
                // (1,22): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                // class C { long _f = 0l;}
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(1, 22),
                // (1,16): warning CS0414: The field 'C._f' is assigned but its value is never used
                // class C { long _f = 0l;}
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(1, 16));

            var options = WithDiagnosticOptions(tree, ("CS0078", ReportDiagnostic.Suppress));
            comp = CreateCompilation(tree, options: options);
            comp.VerifyDiagnostics(
                // (1,16): warning CS0414: The field 'C._f' is assigned but its value is never used
                // class C { long _f = 0l;}
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(1, 16));
        }

        [Fact]
        public void PerTreeDiagnosticOptionsVsPragma()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C {
#pragma warning disable CS0078
long _f = 0l;
#pragma warning restore CS0078
}");
            tree.GetDiagnostics().Verify();

            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics(
                // (4,6): warning CS0414: The field 'C._f' is assigned but its value is never used
                // long _f = 0l;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(4, 6));

            var options = WithDiagnosticOptions(tree, ("CS0078", ReportDiagnostic.Error));
            comp = CreateCompilation(tree, options: options);
            // Pragma should have precedence over per-tree options
            comp.VerifyDiagnostics(
                // (4,6): warning CS0414: The field 'C._f' is assigned but its value is never used
                // long _f = 0l;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(4, 6));
        }

        [Fact]
        public void PerTreeDiagnosticOptionsVsSpecificOptions()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class C { long _f = 0l; }");
            var options = WithDiagnosticOptions(tree, ("CS0078", ReportDiagnostic.Suppress));

            var comp = CreateCompilation(tree, options: options);
            comp.VerifyDiagnostics(
                // (1,16): warning CS0414: The field 'C._f' is assigned but its value is never used
                // class C { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(1, 16)
            );

            options = options.WithSpecificDiagnosticOptions(
                CreateImmutableDictionary(("CS0078", ReportDiagnostic.Error)));

            var comp2 = CreateCompilation(tree, options: options);
            // Specific diagnostic options have precedence over per-tree options
            comp2.VerifyDiagnostics(
                // (1,16): warning CS0414: The field 'C._f' is assigned but its value is never used
                //  class C { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(1, 16),
                // (1,22): error CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                // class C { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(1, 22).WithWarningAsError(true));
        }

        [Fact]
        public void DifferentDiagnosticOptionsForTrees()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@" class C { long _f = 0l; }");
            var newTree = SyntaxFactory.ParseSyntaxTree(@" class D { long _f = 0l; }");
            var options = TestOptions.DebugDll.WithSyntaxTreeOptionsProvider(
                new TestSyntaxTreeOptionsProvider(
                    (tree, new[] { ("CS0078", ReportDiagnostic.Suppress) }),
                    (newTree, new[] { ("CS0078", ReportDiagnostic.Error) })
                )
            );

            var comp = CreateCompilation(new[] { tree, newTree }, options: options);
            comp.VerifyDiagnostics(
                // (1,23): error CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                //  class D { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(1, 23).WithWarningAsError(true),
                // (1,17): warning CS0414: The field 'D._f' is assigned but its value is never used
                //  class D { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("D._f").WithLocation(1, 17),
                // (1,17): warning CS0414: The field 'C._f' is assigned but its value is never used
                //  class C { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(1, 17)
            );
        }

        [Fact]
        public void TreeOptionsComparerRespected()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@" class C { long _f = 0l; }");

            // Default options have case insensitivity
            var options = TestOptions.DebugDll.WithSyntaxTreeOptionsProvider(
                new TestSyntaxTreeOptionsProvider((tree, new[] { ("cs0078", ReportDiagnostic.Suppress) }))
            );

            CreateCompilation(tree, options: options).VerifyDiagnostics(
                // (1,17): warning CS0414: The field 'C._f' is assigned but its value is never used
                //  class C { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(1, 17)
            );

            options = TestOptions.DebugDll.WithSyntaxTreeOptionsProvider(
                new TestSyntaxTreeOptionsProvider(
                    StringComparer.Ordinal,
                    globalOption: default,
                    (tree, new[] { ("cs0078", ReportDiagnostic.Suppress) }))
            );

            CreateCompilation(tree, options: options).VerifyDiagnostics(
                // (1,23): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                //  class C { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(1, 23),
                // (1,17): warning CS0414: The field 'C._f' is assigned but its value is never used
                //  class C { long _f = 0l; }
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "_f").WithArguments("C._f").WithLocation(1, 17)
            );
        }

        [Fact]
        public void WarningLevelRespectedForLexerWarnings()
        {
            var source = @"public class C { public long Field = 0l; }";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,39): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                // public class C { public long Field = 0l; }
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(1, 39)
                );
            CreateCompilation(source, options: TestOptions.ReleaseDll.WithWarningLevel(0)).VerifyDiagnostics(
                );
        }

        [WorkItem(8360, "https://github.com/dotnet/roslyn/issues/8360")]
        [WorkItem(9153, "https://github.com/dotnet/roslyn/issues/9153")]
        [Fact]
        public void PublicSignWithRelativeKeyPath()
        {
            var options = TestOptions.DebugDll
                .WithPublicSign(true).WithCryptoKeyFile("test.snk");
            var comp = CSharpCompilation.Create("test", options: options);
            comp.VerifyDiagnostics(
                // error CS7104: Option 'CryptoKeyFile' must be an absolute path.
                Diagnostic(ErrorCode.ERR_OptionMustBeAbsolutePath).WithArguments("CryptoKeyFile").WithLocation(1, 1),
                // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
                Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1)
            );
        }

        [Fact]
        [WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")]
        public void PublicSignWithEmptyKeyPath()
        {
            CreateCompilation("", options: TestOptions.ReleaseDll.WithPublicSign(true).WithCryptoKeyFile("")).VerifyDiagnostics(
                // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
                Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1));
        }

        [Fact]
        [WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")]
        public void PublicSignWithEmptyKeyPath2()
        {
            CreateCompilation("", options: TestOptions.ReleaseDll.WithPublicSign(true).WithCryptoKeyFile("\"\"")).VerifyDiagnostics(
                // error CS8106: Option 'CryptoKeyFile' must be an absolute path.
                Diagnostic(ErrorCode.ERR_OptionMustBeAbsolutePath).WithArguments("CryptoKeyFile").WithLocation(1, 1),
                // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
                Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1));
        }

        [Fact]
        [WorkItem(233669, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=233669")]
        public void CompilationName()
        {
            // report an error, rather then silently ignoring the directory
            // (see cli partition II 22.30)
            CSharpCompilation.Create(@"C:/goo/Test.exe").VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // error CS8203: Invalid assembly name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name contains invalid characters.").WithLocation(1, 1),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1)
                );
            CSharpCompilation.Create(@"C:\goo\Test.exe").GetDeclarationDiagnostics().Verify(
                // error CS8203: Invalid assembly name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );
            var compilationOptions = TestOptions.DebugDll.WithWarningLevel(0);
            CSharpCompilation.Create(@"\goo/Test.exe", options: compilationOptions).VerifyEmitDiagnostics(
                // error CS8203: Invalid assembly name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );
            CSharpCompilation.Create(@"C:Test.exe", options: compilationOptions).VerifyEmitDiagnostics(
                // error CS8203: Invalid assembly name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );
            CSharpCompilation.Create(@"Te\0st.exe", options: compilationOptions).VerifyEmitDiagnostics(
                // error CS8203: Invalid assembly name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );
            CSharpCompilation.Create(@"   \t  ", options: compilationOptions).VerifyEmitDiagnostics(
                // error CS8203: Invalid assembly name: Name cannot start with whitespace.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name cannot start with whitespace.").WithLocation(1, 1)
                );
            CSharpCompilation.Create(@"\uD800", options: compilationOptions).VerifyEmitDiagnostics(
                // error CS8203: Invalid assembly name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );
            CSharpCompilation.Create(@"", options: compilationOptions).VerifyEmitDiagnostics(
                // error CS8203: Invalid assembly name: Name cannot be empty.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name cannot be empty.").WithLocation(1, 1)
                );
            CSharpCompilation.Create(@" a", options: compilationOptions).VerifyEmitDiagnostics(
                // error CS8203: Invalid assembly name: Name cannot start with whitespace.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name cannot start with whitespace.").WithLocation(1, 1)
                );
            CSharpCompilation.Create(@"\u2000a", options: compilationOptions).VerifyEmitDiagnostics( // U+20700 is whitespace
                                                                                                     // error CS8203: Invalid assembly name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );
            CSharpCompilation.Create("..\\..\\RelativePath", options: compilationOptions).VerifyEmitDiagnostics(
                // error CS8203: Invalid assembly name: Name contains invalid characters.
                Diagnostic(ErrorCode.ERR_BadAssemblyName).WithArguments("Name contains invalid characters.").WithLocation(1, 1)
                );

            // other characters than directory and volume separators are ok:
            CSharpCompilation.Create(@";,*?<>#!@&", options: compilationOptions).VerifyEmitDiagnostics();
            CSharpCompilation.Create("goo", options: compilationOptions).VerifyEmitDiagnostics();
            CSharpCompilation.Create(".goo", options: compilationOptions).VerifyEmitDiagnostics();
            CSharpCompilation.Create("goo ", options: compilationOptions).VerifyEmitDiagnostics(); // can end with whitespace
            CSharpCompilation.Create("....", options: compilationOptions).VerifyEmitDiagnostics();
            CSharpCompilation.Create(null, options: compilationOptions).VerifyEmitDiagnostics();
        }

        [Fact]
        public void CreateAPITest()
        {
            var listSyntaxTree = new List<SyntaxTree>();
            var listRef = new List<MetadataReference>();

            var s1 = @"using Goo;
namespace A.B {
   class C {
     class D {
       class E { }
     }
   }
   class G<T> {
     class Q<S1,S2> { }
   }
   class G<T1,T2> { }
}";
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree(s1);
            listSyntaxTree.Add(t1);

            // System.dll
            listRef.Add(NetFramework.System.WithEmbedInteropTypes(true));
            var ops = TestOptions.ReleaseExe;
            // Create Compilation with Option is not null
            var comp = CSharpCompilation.Create("Compilation", listSyntaxTree, listRef, ops);
            Assert.Equal(ops, comp.Options);
            Assert.NotEqual(default, comp.SyntaxTrees);
            Assert.NotNull(comp.References);
            Assert.Equal(1, comp.SyntaxTrees.Length);
            Assert.Equal(1, comp.ExternalReferences.Length);
            var ref1 = comp.ExternalReferences[0];
            Assert.True(ref1.Properties.EmbedInteropTypes);
            Assert.True(ref1.Properties.Aliases.IsEmpty);

            // Create Compilation with PreProcessorSymbols of Option is empty
            var ops1 = TestOptions.DebugExe;

            // Create Compilation with Assembly name contains invalid char
            var asmname = "ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â";
            comp = CSharpCompilation.Create(asmname, listSyntaxTree, listRef, ops);
            var comp1 = CSharpCompilation.Create(asmname, listSyntaxTree, listRef, null);
        }

        [Fact]
        public void EmitToNonWritableStreams()
        {
            var peStream = new TestStream(canRead: false, canSeek: false, canWrite: false);
            var pdbStream = new TestStream(canRead: false, canSeek: false, canWrite: false);

            var c = CSharpCompilation.Create("a",
                new[] { SyntaxFactory.ParseSyntaxTree("class C { static void Main() {} }") },
                new[] { MscorlibRef });

            Assert.Throws<ArgumentException>(() => c.Emit(peStream));
            Assert.Throws<ArgumentException>(() => c.Emit(new MemoryStream(), pdbStream));
        }

        [Fact]
        public void EmitOptionsDiagnostics()
        {
            var c = CreateCompilation("class C {}");
            var stream = new MemoryStream();

            var options = new EmitOptions(
                debugInformationFormat: (DebugInformationFormat)(-1),
                outputNameOverride: " ",
                fileAlignment: 513,
                subsystemVersion: SubsystemVersion.Create(1000000, -1000000),
                pdbChecksumAlgorithm: new HashAlgorithmName("invalid hash algorithm name"));

            EmitResult result = c.Emit(stream, options: options);

            result.Diagnostics.Verify(
                // error CS2042: Invalid debug information format: -1
                Diagnostic(ErrorCode.ERR_InvalidDebugInformationFormat).WithArguments("-1").WithLocation(1, 1),
                // error CS2041: Invalid output name: Name cannot start with whitespace.
                Diagnostic(ErrorCode.ERR_InvalidOutputName).WithArguments("Name cannot start with whitespace.").WithLocation(1, 1),
                // error CS2024: Invalid file section alignment '513'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("513").WithLocation(1, 1),
                // error CS1773: Invalid version 1000000.-1000000 for /subsystemversion. The version must be 6.02 or greater for ARM or AppContainerExe, and 4.00 or greater otherwise
                Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("1000000.-1000000").WithLocation(1, 1),
                // error CS8113: Invalid hash algorithm name: 'invalid hash algorithm name'
                Diagnostic(ErrorCode.ERR_InvalidHashAlgorithmName).WithArguments("invalid hash algorithm name").WithLocation(1, 1));

            Assert.False(result.Success);
        }

        [Fact]
        public void EmitOptions_PdbChecksumAndDeterminism()
        {
            var options = new EmitOptions(pdbChecksumAlgorithm: default(HashAlgorithmName));
            var diagnosticBag = new DiagnosticBag();

            options.ValidateOptions(diagnosticBag, MessageProvider.Instance, isDeterministic: true);

            diagnosticBag.Verify(
                // error CS8113: Invalid hash algorithm name: ''
                Diagnostic(ErrorCode.ERR_InvalidHashAlgorithmName).WithArguments(""));

            diagnosticBag.Clear();

            options.ValidateOptions(diagnosticBag, MessageProvider.Instance, isDeterministic: false);
            diagnosticBag.Verify();
        }

        [Fact]
        public void Emit_BadArgs()
        {
            var comp = CSharpCompilation.Create("Compilation", options: TestOptions.ReleaseDll);

            Assert.Throws<ArgumentNullException>("peStream", () => comp.Emit(peStream: null));
            Assert.Throws<ArgumentException>("peStream", () => comp.Emit(peStream: new TestStream(canRead: true, canWrite: false, canSeek: true)));
            Assert.Throws<ArgumentException>("pdbStream", () => comp.Emit(peStream: new MemoryStream(), pdbStream: new TestStream(canRead: true, canWrite: false, canSeek: true)));
            Assert.Throws<ArgumentException>("pdbStream", () => comp.Emit(peStream: new MemoryStream(), pdbStream: new MemoryStream(), options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded)));

            Assert.Throws<ArgumentException>("sourceLinkStream", () => comp.Emit(
                peStream: new MemoryStream(),
                pdbStream: new MemoryStream(),
                options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb),
                sourceLinkStream: new TestStream(canRead: false, canWrite: true, canSeek: true)));

            Assert.Throws<ArgumentException>("embeddedTexts", () => comp.Emit(
                peStream: new MemoryStream(),
                pdbStream: null,
                options: null,
                embeddedTexts: new[] { EmbeddedText.FromStream("_", new MemoryStream()) }));

            Assert.Throws<ArgumentException>("embeddedTexts", () => comp.Emit(
                peStream: new MemoryStream(),
                pdbStream: null,
                options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb),
                embeddedTexts: new[] { EmbeddedText.FromStream("_", new MemoryStream()) }));

            Assert.Throws<ArgumentException>("win32Resources", () => comp.Emit(
                peStream: new MemoryStream(),
                win32Resources: new TestStream(canRead: true, canWrite: false, canSeek: false)));

            Assert.Throws<ArgumentException>("win32Resources", () => comp.Emit(
                peStream: new MemoryStream(),
                win32Resources: new TestStream(canRead: false, canWrite: false, canSeek: true)));

            // we don't report an error when we can't write to the XML doc stream:
            Assert.True(comp.Emit(
                peStream: new MemoryStream(),
                pdbStream: new MemoryStream(),
                xmlDocumentationStream: new TestStream(canRead: true, canWrite: false, canSeek: true)).Success);
        }

        [Fact]
        public void ReferenceAPITest()
        {
            var opt = TestOptions.DebugExe;
            // Create Compilation takes two args
            var comp = CSharpCompilation.Create("Compilation", options: TestOptions.DebugExe);
            var ref1 = NetFramework.mscorlib;
            var ref2 = NetFramework.System;
            var ref3 = new TestMetadataReference(fullPath: @"c:\xml.bms");
            var ref4 = new TestMetadataReference(fullPath: @"c:\aaa.dll");
            // Add a new empty item
            comp = comp.AddReferences(Enumerable.Empty<MetadataReference>());
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Add a new valid item
            comp = comp.AddReferences(ref1);
            var assemblySmb = comp.GetReferencedAssemblySymbol(ref1);
            Assert.NotNull(assemblySmb);
            Assert.Equal("mscorlib", assemblySmb.Name, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(1, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Assembly, comp.ExternalReferences[0].Properties.Kind);
            Assert.Equal(ref1, comp.ExternalReferences[0]);

            // Replace an existing item with another valid item
            comp = comp.ReplaceReference(ref1, ref2);
            Assert.Equal(1, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Assembly, comp.ExternalReferences[0].Properties.Kind);
            Assert.Equal(ref2, comp.ExternalReferences[0]);

            // Remove an existing item
            comp = comp.RemoveReferences(ref2);
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Overload with Hashset
            var hs = new HashSet<MetadataReference> { ref1, ref2, ref3 };
            var compCollection = CSharpCompilation.Create("Compilation", references: hs, options: opt);
            compCollection = compCollection.AddReferences(ref1, ref2, ref3, ref4).RemoveReferences(hs);
            Assert.Equal(1, compCollection.ExternalReferences.Length);
            compCollection = compCollection.AddReferences(hs).RemoveReferences(ref1, ref2, ref3, ref4);
            Assert.Equal(0, compCollection.ExternalReferences.Length);

            // Overload with Collection
            var col = new Collection<MetadataReference> { ref1, ref2, ref3 };
            compCollection = CSharpCompilation.Create("Compilation", references: col, options: opt);
            compCollection = compCollection.AddReferences(col).RemoveReferences(ref1, ref2, ref3);
            Assert.Equal(0, compCollection.ExternalReferences.Length);
            compCollection = compCollection.AddReferences(ref1, ref2, ref3).RemoveReferences(col);
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Overload with ConcurrentStack
            var stack = new ConcurrentStack<MetadataReference> { };
            stack.Push(ref1);
            stack.Push(ref2);
            stack.Push(ref3);
            compCollection = CSharpCompilation.Create("Compilation", references: stack, options: opt);
            compCollection = compCollection.AddReferences(stack).RemoveReferences(ref1, ref3, ref2);
            Assert.Equal(0, compCollection.ExternalReferences.Length);
            compCollection = compCollection.AddReferences(ref2, ref1, ref3).RemoveReferences(stack);
            Assert.Equal(0, compCollection.ExternalReferences.Length);

            // Overload with ConcurrentQueue
            var queue = new ConcurrentQueue<MetadataReference> { };
            queue.Enqueue(ref1);
            queue.Enqueue(ref2);
            queue.Enqueue(ref3);
            compCollection = CSharpCompilation.Create("Compilation", references: queue, options: opt);
            compCollection = compCollection.AddReferences(queue).RemoveReferences(ref3, ref2, ref1);
            Assert.Equal(0, compCollection.ExternalReferences.Length);
            compCollection = compCollection.AddReferences(ref2, ref1, ref3).RemoveReferences(queue);
            Assert.Equal(0, compCollection.ExternalReferences.Length);
        }

        [Fact]
        public void ReferenceDirectiveTests()
        {
            var t1 = Parse(@"
#r ""a.dll""
#r ""a.dll""
", filename: "1.csx", options: TestOptions.Script);

            var rd1 = t1.GetRoot().GetDirectives().Cast<ReferenceDirectiveTriviaSyntax>().ToArray();
            Assert.Equal(2, rd1.Length);

            var t2 = Parse(@"
#r ""a.dll""
#r ""b.dll""
", options: TestOptions.Script);

            var rd2 = t2.GetRoot().GetDirectives().Cast<ReferenceDirectiveTriviaSyntax>().ToArray();
            Assert.Equal(2, rd2.Length);

            var t3 = Parse(@"
#r ""a.dll""
", filename: "1.csx", options: TestOptions.Script);

            var rd3 = t3.GetRoot().GetDirectives().Cast<ReferenceDirectiveTriviaSyntax>().ToArray();
            Assert.Equal(1, rd3.Length);

            var t4 = Parse(@"
#r ""a.dll""
", filename: "4.csx", options: TestOptions.Script);

            var rd4 = t4.GetRoot().GetDirectives().Cast<ReferenceDirectiveTriviaSyntax>().ToArray();
            Assert.Equal(1, rd4.Length);

            var c = CreateCompilationWithMscorlib461(new[] { t1, t2 }, options: TestOptions.ReleaseDll.WithMetadataReferenceResolver(
                new TestMetadataReferenceResolver(files: new Dictionary<string, PortableExecutableReference>()
                {
                    { @"a.dll", NetFramework.MicrosoftCSharp },
                    { @"b.dll", NetFramework.MicrosoftVisualBasic },
                })));

            c.VerifyDiagnostics();

            // same containing script file name and directive string
            Assert.Same(NetFramework.MicrosoftCSharp, c.GetDirectiveReference(rd1[0]));
            Assert.Same(NetFramework.MicrosoftCSharp, c.GetDirectiveReference(rd1[1]));
            Assert.Same(NetFramework.MicrosoftCSharp, c.GetDirectiveReference(rd2[0]));
            Assert.Same(NetFramework.MicrosoftVisualBasic, c.GetDirectiveReference(rd2[1]));
            Assert.Same(NetFramework.MicrosoftCSharp, c.GetDirectiveReference(rd3[0]));

            // different script name or directive string:
            Assert.Null(c.GetDirectiveReference(rd4[0]));
        }

        [Fact, WorkItem(530131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530131")]
        public void MetadataReferenceWithInvalidAlias()
        {
            var refcomp = CSharpCompilation.Create("DLL",
                options: TestOptions.ReleaseDll,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree("public class C {}") },
                references: new MetadataReference[] { MscorlibRef });

            var mtref = refcomp.EmitToImageReference(aliases: ImmutableArray.Create("a", "Alias(*#$@^%*&)"));

            // not use exported type
            var comp = CSharpCompilation.Create("APP",
                options: TestOptions.ReleaseDll,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    @"class D {}"
                    ) },
                references: new MetadataReference[] { MscorlibRef, mtref }
                );

            Assert.Empty(comp.GetDiagnostics());

            // use exported type with partial alias
            comp = CSharpCompilation.Create("APP1",
             options: TestOptions.ReleaseDll,
             syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    @"extern alias Alias; class D : Alias::C {}"
                    ) },
             references: new MetadataReference[] { MscorlibRef, mtref }
             );

            var errs = comp.GetDiagnostics();
            //  error CS0430: The extern alias 'Alias' was not specified in a /reference option
            Assert.Equal(430, errs.FirstOrDefault().Code);

            // use exported type with invalid alias
            comp = CSharpCompilation.Create("APP2",
             options: TestOptions.ReleaseExe,
             syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    "extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}",
                    options: TestOptions.Regular9) },
             references: new MetadataReference[] { MscorlibRef, mtref }
             );

            comp.VerifyDiagnostics(
                // (1,21): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(1, 21),
                // (1,61): error CS1026: ) expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 61),
                // (1,61): error CS1002: ; expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 61),
                // (1,14): error CS8112: Local function 'Alias()' must declare a body because it is not marked 'static extern'.
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "Alias").WithArguments("Alias()").WithLocation(1, 14),
                // (1,8): error CS0246: The type or namespace name 'alias' could not be found (are you missing a using directive or an assembly reference?)
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias").WithArguments("alias").WithLocation(1, 8),
                // (1,14): warning CS0626: Method, operator, or accessor 'Alias()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "Alias").WithArguments("Alias()").WithLocation(1, 14),
                // (1,14): warning CS8321: The local function 'Alias' is declared but never used
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Alias").WithArguments("Alias").WithLocation(1, 14)
                );
        }

        [Fact, WorkItem(530131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530131")]
        public void MetadataReferenceWithInvalidAliasWithCSharp6()
        {
            var refcomp = CSharpCompilation.Create("DLL",
                options: TestOptions.ReleaseDll,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree("public class C {}", options: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)) },
                references: new MetadataReference[] { MscorlibRef });

            var mtref = refcomp.EmitToImageReference(aliases: ImmutableArray.Create("a", "Alias(*#$@^%*&)"));

            // not use exported type
            var comp = CSharpCompilation.Create("APP",
                options: TestOptions.ReleaseDll,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    @"class D {}",
                    options: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)) },
                references: new MetadataReference[] { MscorlibRef, mtref }
                );

            Assert.Empty(comp.GetDiagnostics());

            // use exported type with partial alias
            comp = CSharpCompilation.Create("APP1",
             options: TestOptions.ReleaseDll,
             syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    @"extern alias Alias; class D : Alias::C {}",
                    options: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)) },
             references: new MetadataReference[] { MscorlibRef, mtref }
             );

            var errs = comp.GetDiagnostics();
            //  error CS0430: The extern alias 'Alias' was not specified in a /reference option
            Assert.Equal(430, errs.FirstOrDefault().Code);

            // use exported type with invalid alias
            comp = CSharpCompilation.Create("APP2",
             options: TestOptions.ReleaseExe,
             syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    "extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}",
                    options: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)) },
             references: new MetadataReference[] { MscorlibRef, mtref }
             );

            comp.VerifyDiagnostics(
                // (1,1): error CS8059: Feature 'top-level statements' is not available in C# 6. Please use language version 9.0 or greater.
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}").WithArguments("top-level statements", "9.0").WithLocation(1, 1),
                // (1,1): error CS8059: Feature 'extern local functions' is not available in C# 6. Please use language version 9.0 or greater.
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "extern").WithArguments("extern local functions", "9.0").WithLocation(1, 1),
                // (1,14): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7.0 or greater.
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "Alias").WithArguments("local functions", "7.0").WithLocation(1, 14),
                // (1,21): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#").WithLocation(1, 21),
                // (1,61): error CS1026: ) expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 61),
                // (1,61): error CS1002: ; expected
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 61),
                // (1,14): error CS8112: Local function 'Alias()' must declare a body because it is not marked 'static extern'.
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "Alias").WithArguments("Alias()").WithLocation(1, 14),
                // (1,8): error CS0246: The type or namespace name 'alias' could not be found (are you missing a using directive or an assembly reference?)
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "alias").WithArguments("alias").WithLocation(1, 8),
                // (1,14): warning CS0626: Method, operator, or accessor 'Alias()' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.WRN_ExternMethodNoImplementation, "Alias").WithArguments("Alias()").WithLocation(1, 14),
                // (1,14): warning CS8321: The local function 'Alias' is declared but never used
                // extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Alias").WithArguments("Alias").WithLocation(1, 14)
                );
        }

        [Fact]
        public void SyntreeAPITest()
        {
            var s1 = "namespace System.Linq {}";
            var s2 = @"
namespace NA.NB
{
  partial class C<T>
  {
    public partial class D
    {
      intttt F;
    }
  }
  class C { }
}
";
            var s3 = @"int x;";
            var s4 = @"Imports System ";
            var s5 = @"
class D
{
    public static int Goo()
    {
        long l = 25l;
        return 0;
    }
}
";
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree(s1);
            SyntaxTree withErrorTree = SyntaxFactory.ParseSyntaxTree(s2);
            SyntaxTree withErrorTree1 = SyntaxFactory.ParseSyntaxTree(s3);
            SyntaxTree withErrorTreeVB = SyntaxFactory.ParseSyntaxTree(s4);
            SyntaxTree withExpressionRootTree = SyntaxFactory.ParseExpression(s3).SyntaxTree;
            var withWarning = SyntaxFactory.ParseSyntaxTree(s5);

            // Create compilation takes three args
            var comp = CSharpCompilation.Create("Compilation", syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(s1) }, options: TestOptions.ReleaseDll);
            comp = comp.AddSyntaxTrees(t1, withErrorTree, withErrorTree1, withErrorTreeVB);
            Assert.Equal(5, comp.SyntaxTrees.Length);
            comp = comp.RemoveSyntaxTrees(t1, withErrorTree, withErrorTree1, withErrorTreeVB);
            Assert.Equal(1, comp.SyntaxTrees.Length);

            // Add a new empty item
            comp = comp.AddSyntaxTrees(Enumerable.Empty<SyntaxTree>());
            Assert.Equal(1, comp.SyntaxTrees.Length);

            // Add a new valid item
            comp = comp.AddSyntaxTrees(t1);
            Assert.Equal(2, comp.SyntaxTrees.Length);
            Assert.Contains(t1, comp.SyntaxTrees);
            Assert.False(comp.SyntaxTrees.Contains(SyntaxFactory.ParseSyntaxTree(s1)));

            comp = comp.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(s1));
            Assert.Equal(3, comp.SyntaxTrees.Length);

            // Replace an existing item with another valid item
            comp = comp.ReplaceSyntaxTree(t1, SyntaxFactory.ParseSyntaxTree(s1));
            Assert.Equal(3, comp.SyntaxTrees.Length);

            // Replace an existing item with same item
            comp = comp.AddSyntaxTrees(t1).ReplaceSyntaxTree(t1, t1);
            Assert.Equal(4, comp.SyntaxTrees.Length);

            // add again and verify that it throws
            Assert.Throws<ArgumentException>(() => comp.AddSyntaxTrees(t1));

            // replace with existing and verify that it throws
            Assert.Throws<ArgumentException>(() => comp.ReplaceSyntaxTree(t1, comp.SyntaxTrees[0]));

            // SyntaxTrees have reference equality. This removal should fail.
            Assert.Throws<ArgumentException>(() => comp = comp.RemoveSyntaxTrees(SyntaxFactory.ParseSyntaxTree(s1)));
            Assert.Equal(4, comp.SyntaxTrees.Length);

            // Remove non-existing item
            Assert.Throws<ArgumentException>(() => comp = comp.RemoveSyntaxTrees(withErrorTree));
            Assert.Equal(4, comp.SyntaxTrees.Length);

            // Add syntaxtree with error
            comp = comp.AddSyntaxTrees(withErrorTree1);
            var error = comp.GetDiagnostics();
            Assert.InRange(comp.GetDiagnostics().Count(), 0, int.MaxValue);
            Assert.InRange(comp.GetDeclarationDiagnostics().Count(), 0, int.MaxValue);

            Assert.True(comp.SyntaxTrees.Contains(t1));

            SyntaxTree t4 = SyntaxFactory.ParseSyntaxTree("Using System;");
            SyntaxTree t5 = SyntaxFactory.ParseSyntaxTree("Usingsssssssssssss System;");
            SyntaxTree t6 = SyntaxFactory.ParseSyntaxTree("Import System;");

            // Overload with Hashset
            var hs = new HashSet<SyntaxTree> { t4, t5, t6 };
            var compCollection = CSharpCompilation.Create("Compilation", syntaxTrees: hs);
            compCollection = compCollection.RemoveSyntaxTrees(hs);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);
            compCollection = compCollection.AddSyntaxTrees(hs).RemoveSyntaxTrees(t4, t5, t6);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);

            // Overload with Collection
            var col = new Collection<SyntaxTree> { t4, t5, t6 };
            compCollection = CSharpCompilation.Create("Compilation", syntaxTrees: col);
            compCollection = compCollection.RemoveSyntaxTrees(t4, t5, t6);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);
            Assert.Throws<ArgumentException>(() => compCollection = compCollection.AddSyntaxTrees(t4, t5).RemoveSyntaxTrees(col));
            Assert.Equal(0, compCollection.SyntaxTrees.Length);

            // Overload with ConcurrentStack
            var stack = new ConcurrentStack<SyntaxTree> { };
            stack.Push(t4);
            stack.Push(t5);
            stack.Push(t6);
            compCollection = CSharpCompilation.Create("Compilation", syntaxTrees: stack);
            compCollection = compCollection.RemoveSyntaxTrees(t4, t6, t5);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);
            Assert.Throws<ArgumentException>(() => compCollection = compCollection.AddSyntaxTrees(t4, t6).RemoveSyntaxTrees(stack));
            Assert.Equal(0, compCollection.SyntaxTrees.Length);

            // Overload with ConcurrentQueue
            var queue = new ConcurrentQueue<SyntaxTree> { };
            queue.Enqueue(t4);
            queue.Enqueue(t5);
            queue.Enqueue(t6);
            compCollection = CSharpCompilation.Create("Compilation", syntaxTrees: queue);
            compCollection = compCollection.RemoveSyntaxTrees(t4, t6, t5);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);
            Assert.Throws<ArgumentException>(() => compCollection = compCollection.AddSyntaxTrees(t4, t6).RemoveSyntaxTrees(queue));
            Assert.Equal(0, compCollection.SyntaxTrees.Length);

            // Get valid binding
            var bind = comp.GetSemanticModel(syntaxTree: t1);
            Assert.Equal(t1, bind.SyntaxTree);
            Assert.Equal("C#", bind.Language);

            // Remove syntaxtree without error
            comp = comp.RemoveSyntaxTrees(t1);
            Assert.InRange(comp.GetDiagnostics().Count(), 0, int.MaxValue);

            // Remove syntaxtree with error
            comp = comp.RemoveSyntaxTrees(withErrorTree1);
            var e = comp.GetDiagnostics(cancellationToken: default(CancellationToken));
            Assert.Equal(0, comp.GetDiagnostics(cancellationToken: default(CancellationToken)).Count());

            // Add syntaxtree which is VB language
            comp = comp.AddSyntaxTrees(withErrorTreeVB);
            error = comp.GetDiagnostics(cancellationToken: CancellationToken.None);
            Assert.InRange(comp.GetDiagnostics().Count(), 0, int.MaxValue);

            comp = comp.RemoveSyntaxTrees(withErrorTreeVB);
            Assert.Equal(0, comp.GetDiagnostics().Count());

            // Add syntaxtree with error
            comp = comp.AddSyntaxTrees(withWarning);
            error = comp.GetDeclarationDiagnostics();
            Assert.InRange(error.Count(), 1, int.MaxValue);

            comp = comp.RemoveSyntaxTrees(withWarning);
            Assert.Equal(0, comp.GetDiagnostics().Count());

            // Compilation.Create with syntaxtree with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.False(withExpressionRootTree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?");
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("Compilation", new SyntaxTree[] { withExpressionRootTree }));

            // AddSyntaxTrees with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.Throws<ArgumentException>(() => comp.AddSyntaxTrees(withExpressionRootTree));

            // ReplaceSyntaxTrees syntaxtree with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.Throws<ArgumentException>(() => comp.ReplaceSyntaxTree(comp.SyntaxTrees[0], withExpressionRootTree));
        }

        [Fact]
        public void ChainedOperations()
        {
            var s1 = "using System.Linq;";
            var s2 = "";
            var s3 = "Import System";
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree(s1);
            SyntaxTree t2 = SyntaxFactory.ParseSyntaxTree(s2);
            SyntaxTree t3 = SyntaxFactory.ParseSyntaxTree(s3);

            var listSyntaxTree = new List<SyntaxTree>();
            listSyntaxTree.Add(t1);
            listSyntaxTree.Add(t2);

            // Remove second SyntaxTree
            CSharpCompilation comp = CSharpCompilation.Create(options: TestOptions.ReleaseDll, assemblyName: "Compilation", references: null, syntaxTrees: null);
            comp = comp.AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(t2);
            Assert.Equal(1, comp.SyntaxTrees.Length);

            // Remove mid SyntaxTree
            listSyntaxTree.Add(t3);
            comp = comp.RemoveSyntaxTrees(t1).AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(t2);
            Assert.Equal(2, comp.SyntaxTrees.Length);

            // remove list
            listSyntaxTree.Remove(t2);
            comp = comp.AddSyntaxTrees().RemoveSyntaxTrees(listSyntaxTree);
            comp = comp.AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(listSyntaxTree);
            Assert.Equal(0, comp.SyntaxTrees.Length);

            listSyntaxTree.Clear();
            listSyntaxTree.Add(t1);
            listSyntaxTree.Add(t1);
            // Chained operation count > 2
            Assert.Throws<ArgumentException>(() => comp = comp.AddSyntaxTrees(listSyntaxTree).AddReferences().ReplaceSyntaxTree(t1, t2));
            comp = comp.AddSyntaxTrees(t1).AddReferences().ReplaceSyntaxTree(t1, t2);

            Assert.Equal(1, comp.SyntaxTrees.Length);
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Create compilation with args is disordered
            CSharpCompilation comp1 = CSharpCompilation.Create(assemblyName: "Compilation", syntaxTrees: null, options: TestOptions.ReleaseDll, references: null);
            var ref1 = NetFramework.mscorlib;
            var listRef = new List<MetadataReference>();
            listRef.Add(ref1);
            listRef.Add(ref1);

            // Remove with no args
            comp1 = comp1.AddReferences(listRef).AddSyntaxTrees(t1).RemoveReferences().RemoveSyntaxTrees();
            Assert.Equal(1, comp1.ExternalReferences.Length);
            Assert.Equal(1, comp1.SyntaxTrees.Length);
        }

        [WorkItem(713356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713356")]
        [ClrOnlyFact]
        public void MissedModuleA()
        {
            var netModule1 = CreateCompilation(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                source: new string[] { "public class C1 {}" });
            netModule1.VerifyEmitDiagnostics();

            var netModule2 = CreateCompilation(
                options: TestOptions.ReleaseModule,
                assemblyName: "a2",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                source: new string[] {
                    @"
public class C2 {
public static void M() {
    var a = new C1();
}
}"
                });
            netModule2.VerifyEmitDiagnostics();

            var assembly = CreateCompilation(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule2.EmitToImageReference() },
                source: new string[] {
                @"
public class C3 {
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics(
                Diagnostic(ErrorCode.ERR_MissingNetModuleReference).WithArguments("a1.netmodule"));

            assembly = CreateCompilation(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule1.EmitToImageReference(), netModule2.EmitToImageReference() },
                source: new string[] {
                @"
public class C3 {
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics();
            // ILVerify: Assembly or module not found: a2
            CompileAndVerify(assembly, verify: Verification.FailsILVerify);
        }

        [WorkItem(713356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713356")]
        [Fact]
        public void MissedModuleB_OneError()
        {
            var netModule1 = CreateCompilation(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                source: new string[] { "public class C1 {}" });
            netModule1.VerifyEmitDiagnostics();

            var netModule2 = CreateCompilation(
                options: TestOptions.ReleaseModule,
                assemblyName: "a2",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                source: new string[] {
                    @"
public class C2 {
public static void M() {
    var a = new C1();
}
}"
                });
            netModule2.VerifyEmitDiagnostics();

            var netModule3 = CreateCompilation(
                options: TestOptions.ReleaseModule,
                assemblyName: "a3",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                source: new string[] {
                    @"
public class C2a {
public static void M() {
    var a = new C1();
}
}"
                });
            netModule3.VerifyEmitDiagnostics();

            var assembly = CreateCompilation(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule2.EmitToImageReference(), netModule3.EmitToImageReference() },
                source: new string[] {
                @"
public class C3 {
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics(
                Diagnostic(ErrorCode.ERR_MissingNetModuleReference).WithArguments("a1.netmodule"));
        }

        [WorkItem(718500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718500")]
        [WorkItem(716762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716762")]
        [Fact]
        public void MissedModuleB_NoErrorForUnmanagedModules()
        {
            var netModule1 = CreateCompilation(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                source: new string[] {
                    @"
using System;
using System.Runtime.InteropServices;

public class C2 {
    [DllImport(""user32.dll"", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);
}"
                });
            netModule1.VerifyEmitDiagnostics();

            var assembly = CreateCompilation(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                source: new string[] {
                @"
public class C3 {
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics();
        }

        [WorkItem(715872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715872")]
        [Fact]
        public void MissedModuleC()
        {
            var netModule1 = CreateCompilation(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                source: new string[] { "public class C1 {}" });
            netModule1.VerifyEmitDiagnostics();

            var netModule2 = CreateCompilation(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                source: new string[] {
                    @"
public class C2 {
public static void M() {
    var a = new C1();
}
}"
                });
            netModule2.VerifyEmitDiagnostics();

            var assembly = CreateCompilation(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule1.EmitToImageReference(), netModule2.EmitToImageReference() },
                source: new string[] {
                @"
public class C3 {
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics(Diagnostic(ErrorCode.ERR_NetModuleNameMustBeUnique).WithArguments("a1.netmodule"));
        }

        [Fact]
        public void MixedRefType()
        {
            var vbComp = VB.VisualBasicCompilation.Create("CompilationVB");
            var comp = CSharpCompilation.Create("Compilation");

            vbComp = vbComp.AddReferences(SystemRef);

            // Add VB reference to C# compilation
            foreach (var item in vbComp.References)
            {
                comp = comp.AddReferences(item);
                comp = comp.ReplaceReference(item, item);
            }
            Assert.Equal(1, comp.ExternalReferences.Length);

            var text1 = @"class A {}";
            var comp1 = CSharpCompilation.Create("Test1", new[] { SyntaxFactory.ParseSyntaxTree(text1) });
            var comp2 = CSharpCompilation.Create("Test2", new[] { SyntaxFactory.ParseSyntaxTree(text1) });

            var compRef1 = comp1.ToMetadataReference();
            var compRef2 = comp2.ToMetadataReference();

            var compRef = vbComp.ToMetadataReference(embedInteropTypes: true);

            var ref1 = NetFramework.mscorlib;
            var ref2 = NetFramework.System;

            // Add CompilationReference
            comp = CSharpCompilation.Create(
                "Test1",
                new[] { SyntaxFactory.ParseSyntaxTree(text1) },
                new MetadataReference[] { compRef1, compRef2 });

            Assert.Equal(2, comp.ExternalReferences.Length);
            Assert.True(comp.References.Contains(compRef1));
            Assert.True(comp.References.Contains(compRef2));
            var smb = comp.GetReferencedAssemblySymbol(compRef1);
            Assert.Equal(SymbolKind.Assembly, smb.Kind);
            Assert.Equal("Test1", smb.Identity.Name, StringComparer.OrdinalIgnoreCase);

            // Mixed reference type
            comp = comp.AddReferences(ref1);
            Assert.Equal(3, comp.ExternalReferences.Length);
            Assert.True(comp.References.Contains(ref1));

            // Replace Compilation reference with Assembly file reference
            comp = comp.ReplaceReference(compRef2, ref2);
            Assert.Equal(3, comp.ExternalReferences.Length);
            Assert.True(comp.References.Contains(ref2));

            // Replace Assembly file reference with Compilation reference
            comp = comp.ReplaceReference(ref1, compRef2);
            Assert.Equal(3, comp.ExternalReferences.Length);
            Assert.True(comp.References.Contains(compRef2));

            var modRef1 = TestReferences.MetadataTests.NetModule01.ModuleCS00;

            // Add Module file reference
            comp = comp.AddReferences(modRef1);
            // Not implemented code
            //var modSmb = comp.GetReferencedModuleSymbol(modRef1);
            //Assert.Equal("ModuleCS00.mod", modSmb.Name);
            //Assert.Equal(4, comp.References.Count);
            //Assert.True(comp.References.Contains(modRef1));

            //smb = comp.GetReferencedAssemblySymbol(reference: modRef1);
            //Assert.Equal(smb.Kind, SymbolKind.Assembly);
            //Assert.Equal("Test1", smb.Identity.Name, StringComparer.OrdinalIgnoreCase);

            // GetCompilationNamespace Not implemented(Derived Class AssemblySymbol)
            //var m = smb.GlobalNamespace.GetMembers();
            //var nsSmb = smb.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            //var ns = comp.GetCompilationNamespace(ns: nsSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            //var asbSmb = smb as Symbol;
            //var ns1 = comp.GetCompilationNamespace(ns: asbSmb as NamespaceSymbol);
            //Assert.Equal(ns1.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns1.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // Get Referenced Module Symbol
            //var moduleSmb = comp.GetReferencedModuleSymbol(reference: modRef1);
            //Assert.Equal(SymbolKind.NetModule, moduleSmb.Kind);
            //Assert.Equal("ModuleCS00.mod", moduleSmb.Name, StringComparer.OrdinalIgnoreCase);

            // GetCompilationNamespace Not implemented(Derived Class ModuleSymbol)
            //nsSmb = moduleSmb.GlobalNamespace.GetMembers("Runtime").Single() as NamespaceSymbol;
            //ns = comp.GetCompilationNamespace(ns: nsSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            //var modSmbol = moduleSmb as Symbol;
            //ns1 = comp.GetCompilationNamespace(ns: modSmbol as NamespaceSymbol);
            //Assert.Equal(ns1.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns1.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // Get Compilation Namespace
            //nsSmb = comp.GlobalNamespace;
            //ns = comp.GetCompilationNamespace(ns: nsSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // GetCompilationNamespace Not implemented(Derived Class MergedNamespaceSymbol)
            //NamespaceSymbol merged = MergedNamespaceSymbol.Create(new NamespaceExtent(new MockAssemblySymbol("Merged")), null, null);
            //ns = comp.GetCompilationNamespace(ns: merged);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // GetCompilationNamespace Not implemented(Derived Class RetargetingNamespaceSymbol)
            //Retargeting.RetargetingNamespaceSymbol retargetSmb = nsSmb as Retargeting.RetargetingNamespaceSymbol;
            //ns = comp.GetCompilationNamespace(ns: retargetSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // GetCompilationNamespace Not implemented(Derived Class PENamespaceSymbol)
            //Symbols.Metadata.PE.PENamespaceSymbol pensSmb = nsSmb as Symbols.Metadata.PE.PENamespaceSymbol;
            //ns = comp.GetCompilationNamespace(ns: pensSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // Replace Module file reference with compilation reference
            comp = comp.RemoveReferences(compRef1).ReplaceReference(modRef1, compRef1);
            Assert.Equal(3, comp.ExternalReferences.Length);
            // Check the reference order after replace
            Assert.True(comp.ExternalReferences[2] is CSharpCompilationReference, "Expected compilation reference");
            Assert.Equal(compRef1, comp.ExternalReferences[2]);

            // Replace compilation Module file reference with Module file reference
            comp = comp.ReplaceReference(compRef1, modRef1);
            // Check the reference order after replace
            Assert.Equal(3, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Module, comp.ExternalReferences[2].Properties.Kind);
            Assert.Equal(modRef1, comp.ExternalReferences[2]);

            // Add VB compilation ref
            Assert.Throws<ArgumentException>(() => comp.AddReferences(compRef));

            foreach (var item in comp.References)
            {
                comp = comp.RemoveReferences(item);
            }
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Not Implemented
            // var asmByteRef = MetadataReference.CreateFromImage(new byte[5], embedInteropTypes: true);
            //var asmObjectRef = new AssemblyObjectReference(assembly: System.Reflection.Assembly.GetAssembly(typeof(object)),embedInteropTypes :true);
            //comp =comp.AddReferences(asmByteRef, asmObjectRef);
            //Assert.Equal(2, comp.References.Count);
            //Assert.Equal(ReferenceKind.AssemblyBytes, comp.References[0].Kind);
            //Assert.Equal(ReferenceKind.AssemblyObject , comp.References[1].Kind);
            //Assert.Equal(asmByteRef, comp.References[0]);
            //Assert.Equal(asmObjectRef, comp.References[1]);
            //Assert.True(comp.References[0].EmbedInteropTypes);
            //Assert.True(comp.References[1].EmbedInteropTypes);
        }

        [Fact]
        public void NegGetCompilationNamespace()
        {
            var comp = CSharpCompilation.Create("Compilation");

            // Throw exception when the parameter of GetCompilationNamespace is null
            Assert.Throws<NullReferenceException>(
            delegate
            {
                comp.GetCompilationNamespace(namespaceSymbol: (INamespaceSymbol)null);
            });

            Assert.Throws<NullReferenceException>(
            delegate
            {
                comp.GetCompilationNamespace(namespaceSymbol: (NamespaceSymbol)null);
            });
        }

        [WorkItem(537623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537623")]
        [Fact]
        public void NegCreateCompilation()
        {
            Assert.Throws<ArgumentNullException>(() => CSharpCompilation.Create("goo", syntaxTrees: new SyntaxTree[] { null }));
            Assert.Throws<ArgumentNullException>(() => CSharpCompilation.Create("goo", references: new MetadataReference[] { null }));
        }

        [WorkItem(537637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537637")]
        [Fact]
        public void NegGetSymbol()
        {
            // Create Compilation with miss mid args
            var comp = CSharpCompilation.Create("Compilation");
            Assert.Null(comp.GetReferencedAssemblySymbol(reference: MscorlibRef));

            var modRef1 = TestReferences.MetadataTests.NetModule01.ModuleCS00;
            // Get not exist Referenced Module Symbol
            Assert.Null(comp.GetReferencedModuleSymbol(modRef1));

            // Throw exception when the parameter of GetReferencedAssemblySymbol is null
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp.GetReferencedAssemblySymbol(null);
            });

            // Throw exception when the parameter of GetReferencedModuleSymbol is null
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                var modSmb1 = comp.GetReferencedModuleSymbol(null);
            });
        }

        [WorkItem(537778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537778")]
        // Throw exception when the parameter of the parameter type of GetReferencedAssemblySymbol is VB.CompilationReference
        [Fact]
        public void NegGetSymbol1()
        {
            var opt = TestOptions.ReleaseDll;
            var comp = CSharpCompilation.Create("Compilation");
            var vbComp = VB.VisualBasicCompilation.Create("CompilationVB");
            vbComp = vbComp.AddReferences(SystemRef);
            var compRef = vbComp.ToMetadataReference();
            Assert.Throws<ArgumentException>(() => comp.AddReferences(compRef));

            // Throw exception when the parameter of GetBinding is null
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp.GetSemanticModel(null);
            });

            // Throw exception when the parameter of GetTypeByNameAndArity is NULL
            //Assert.Throws<Exception>(
            //delegate
            //{
            //    comp.GetTypeByNameAndArity(fullName: null, arity: 1);
            //});

            // Throw exception when the parameter of GetTypeByNameAndArity is less than 0
            //Assert.Throws<Exception>(
            //delegate
            //{
            //    comp.GetTypeByNameAndArity(string.Empty, -4);
            //});
        }

        // Add already existing item
        [Fact, WorkItem(537574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537574")]
        public void NegReference2()
        {
            var ref1 = NetFramework.mscorlib;
            var ref2 = NetFramework.System;
            var ref3 = NetFramework.SystemData;
            var ref4 = NetFramework.SystemXml;
            var comp = CSharpCompilation.Create("Compilation");

            comp = comp.AddReferences(ref1, ref1);
            Assert.Equal(1, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Assembly, comp.ExternalReferences[0].Properties.Kind);
            Assert.Equal(ref1, comp.ExternalReferences[0]);

            var listRef = new List<MetadataReference> { ref1, ref2, ref3, ref4 };
            // Chained operation count > 3
            // ReplaceReference throws if the reference to be replaced is not found.
            comp = comp.AddReferences(listRef).AddReferences(ref2).RemoveReferences(ref1, ref3, ref4).ReplaceReference(ref2, ref2);
            Assert.Equal(1, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Assembly, comp.ExternalReferences[0].Properties.Kind);
            Assert.Equal(ref2, comp.ExternalReferences[0]);
            Assert.Throws<ArgumentException>(() => comp.AddReferences(listRef).AddReferences(ref2).RemoveReferences(ref1, ref2, ref3, ref4).ReplaceReference(ref2, ref2));
        }

        // Add a new invalid item
        [Fact, WorkItem(537575, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537575")]
        public void NegReference3()
        {
            var ref1 = InvalidRef;
            var comp = CSharpCompilation.Create("Compilation");
            // Remove non-existing item
            Assert.Throws<ArgumentException>(() => comp = comp.RemoveReferences(ref1));
            // Add a new invalid item
            comp = comp.AddReferences(ref1);
            Assert.Equal(1, comp.ExternalReferences.Length);
            // Replace a non-existing item with another invalid item
            Assert.Throws<ArgumentException>(() => comp = comp.ReplaceReference(MscorlibRef, ref1));
            Assert.Equal(1, comp.ExternalReferences.Length);
        }

        // Replace a non-existing item with null
        [Fact, WorkItem(537567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537567")]
        public void NegReference4()
        {
            var ref1 = NetFramework.mscorlib;
            var comp = CSharpCompilation.Create("Compilation");

            Assert.Throws<ArgumentException>(
            delegate
            {
                comp = comp.ReplaceReference(ref1, null);
            });

            // Replace null and the arg order of replace is vise
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp = comp.ReplaceReference(newReference: ref1, oldReference: null);
            });
        }

        // Replace a non-existing item with another valid item
        [Fact, WorkItem(537566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537566")]
        public void NegReference5()
        {
            var ref1 = NetFramework.mscorlib;
            var ref2 = NetFramework.SystemXml;
            var comp = CSharpCompilation.Create("Compilation");
            Assert.Throws<ArgumentException>(
            delegate
            {
                comp = comp.ReplaceReference(ref1, ref2);
            });

            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree("Using System;");
            // Replace a non-existing item with another valid item and disorder the args
            Assert.Throws<ArgumentException>(
            delegate
            {
                comp.ReplaceReference(newReference: NetFramework.System, oldReference: ref2);
            });
            Assert.Equal(0, comp.SyntaxTrees.Length);
            Assert.Throws<ArgumentException>(() => comp.ReplaceSyntaxTree(newTree: SyntaxFactory.ParseSyntaxTree("Using System;"), oldTree: t1));
            Assert.Equal(0, comp.SyntaxTrees.Length);
        }

        [WorkItem(527256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527256")]
        // Throw exception when the parameter of SyntaxTrees.Contains is null
        [Fact]
        public void NegSyntaxTreesContains()
        {
            var comp = CSharpCompilation.Create("Compilation");
            Assert.False(comp.SyntaxTrees.Contains(null));
        }

        [WorkItem(537784, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537784")]
        // Throw exception when the parameter of GetSpecialType() is out of range
        [Fact]
        public void NegGetSpecialType()
        {
            var comp = CSharpCompilation.Create("Compilation");

            // Throw exception when the parameter of GetBinding is out of range
            Assert.Throws<ArgumentOutOfRangeException>(
            delegate
            {
                comp.GetSpecialType((SpecialType)100);
            });

            Assert.Throws<ArgumentOutOfRangeException>(
            delegate
            {
                comp.GetSpecialType(SpecialType.None);
            });

            Assert.Throws<ArgumentOutOfRangeException>(
            delegate
            {
                comp.GetSpecialType((SpecialType)000);
            });

            Assert.Throws<ArgumentOutOfRangeException>(
            delegate
            {
                comp.GetSpecialType(default(SpecialType));
            });
        }

        [WorkItem(538168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538168")]
        // Replace a non-existing item with another valid item and disorder the args
        [Fact]
        public void NegTree2()
        {
            var comp = CSharpCompilation.Create("API");
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree("Using System;");
            Assert.Throws<ArgumentException>(
            delegate
            {
                comp = comp.ReplaceSyntaxTree(newTree: SyntaxFactory.ParseSyntaxTree("Using System;"), oldTree: t1);
            });
        }

        [WorkItem(537576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537576")]
        // Add already existing item
        [Fact]
        public void NegSynTree1()
        {
            var comp = CSharpCompilation.Create("Compilation");
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree("Using System;");
            Assert.Throws<ArgumentException>(() => (comp.AddSyntaxTrees(t1, t1)));
            Assert.Equal(0, comp.SyntaxTrees.Length);
        }

        [Fact]
        public void NegSynTree()
        {
            var comp = CSharpCompilation.Create("Compilation");
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree("Using Goo;");
            // Throw exception when add null SyntaxTree
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp.AddSyntaxTrees(null);
            });

            // Throw exception when Remove null SyntaxTree
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp.RemoveSyntaxTrees(null);
            });

            // No exception when replacing a SyntaxTree with null
            var compP = comp.AddSyntaxTrees(syntaxTree);
            comp = compP.ReplaceSyntaxTree(syntaxTree, null);
            Assert.Equal(0, comp.SyntaxTrees.Length);

            // Throw exception when remove null SyntaxTree
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp = comp.ReplaceSyntaxTree(null, syntaxTree);
            });

            var s1 = "Imports System.Text";
            SyntaxTree t1 = VB.VisualBasicSyntaxTree.ParseText(s1);
            SyntaxTree t2 = t1;
            var t3 = t2;

            var vbComp = VB.VisualBasicCompilation.Create("CompilationVB");
            vbComp = vbComp.AddSyntaxTrees(t1, VB.VisualBasicSyntaxTree.ParseText("Using Goo;"));
            // Throw exception when cast SyntaxTree
            foreach (var item in vbComp.SyntaxTrees)
            {
                t3 = item;
                Exception invalidCastSynTreeEx = Assert.Throws<InvalidCastException>(
                delegate
                {
                    comp = comp.AddSyntaxTrees(t3);
                });
                invalidCastSynTreeEx = Assert.Throws<InvalidCastException>(
                delegate
                {
                    comp = comp.RemoveSyntaxTrees(t3);
                });
                invalidCastSynTreeEx = Assert.Throws<InvalidCastException>(
                delegate
                {
                    comp = comp.ReplaceSyntaxTree(t3, t3);
                });
            }
            // Get Binding with tree is not exist
            SyntaxTree t4 = SyntaxFactory.ParseSyntaxTree(s1);
            Assert.Throws<ArgumentException>(
            delegate
            {
                comp.RemoveSyntaxTrees(new SyntaxTree[] { t4 }).GetSemanticModel(t4);
            });
        }

        [Fact]
        public void GetEntryPoint_Exe()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();

            var mainMethod = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<MethodSymbol>("Main");

            Assert.Equal(mainMethod, compilation.GetEntryPoint(default(CancellationToken)));

            var entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(mainMethod, entryPointAndDiagnostics.MethodSymbol);
            entryPointAndDiagnostics.Diagnostics.Verify();
        }

        [Fact]
        public void GetEntryPoint_Dll()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();

            Assert.Null(compilation.GetEntryPoint(default(CancellationToken)));
            Assert.Same(CSharpCompilation.EntryPoint.None, compilation.GetEntryPointAndDiagnostics(default(CancellationToken)));
        }

        [Fact]
        public void GetEntryPoint_Module()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseModule);
            compilation.VerifyDiagnostics();

            Assert.Null(compilation.GetEntryPoint(default(CancellationToken)));
            Assert.Same(CSharpCompilation.EntryPoint.None, compilation.GetEntryPointAndDiagnostics(default(CancellationToken)));
        }

        [Fact]
        public void CreateCompilationForModule()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            // equivalent of csc with no /moduleassemblyname specified:
            var compilation = CSharpCompilation.Create(assemblyName: null, options: TestOptions.ReleaseModule, syntaxTrees: new[] { Parse(source) }, references: new[] { MscorlibRef });
            compilation.VerifyEmitDiagnostics();

            Assert.Null(compilation.AssemblyName);
            Assert.Equal("?", compilation.Assembly.Name);
            Assert.Equal("?", compilation.Assembly.Identity.Name);

            // no name is allowed for assembly as well, although it isn't useful:
            compilation = CSharpCompilation.Create(assemblyName: null, options: TestOptions.ReleaseDll, syntaxTrees: new[] { Parse(source) }, references: new[] { MscorlibRef });
            compilation.VerifyEmitDiagnostics();

            Assert.Null(compilation.AssemblyName);
            Assert.Equal("?", compilation.Assembly.Name);
            Assert.Equal("?", compilation.Assembly.Identity.Name);

            // equivalent of csc with /moduleassemblyname specified:
            compilation = CSharpCompilation.Create(assemblyName: "ModuleAssemblyName", options: TestOptions.ReleaseModule, syntaxTrees: new[] { Parse(source) }, references: new[] { MscorlibRef });
            compilation.VerifyDiagnostics();

            Assert.Equal("ModuleAssemblyName", compilation.AssemblyName);
            Assert.Equal("ModuleAssemblyName", compilation.Assembly.Name);
            Assert.Equal("ModuleAssemblyName", compilation.Assembly.Identity.Name);
        }

        [WorkItem(8506, "https://github.com/dotnet/roslyn/issues/8506")]
        [WorkItem(17403, "https://github.com/dotnet/roslyn/issues/17403")]
        [Fact]
        public void CrossCorlibSystemObjectReturnType_Script()
        {
            // MinAsyncCorlibRef corlib is used since it provides just enough corlib type definitions
            // and Task APIs necessary for script hosting are provided by MinAsyncRef. This ensures that
            // `System.Object, mscorlib, Version=4.0.0.0` will not be provided (since it's unversioned).
            //
            // In the original bug, Xamarin iOS, Android, and Mac Mobile profile corlibs were
            // realistic cross-compilation targets.

            void AssertCompilationCorlib(CSharpCompilation compilation)
            {
                Assert.True(compilation.IsSubmission);

                var taskOfT = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
                var taskOfObject = taskOfT.Construct(compilation.ObjectType);
                var entryPoint = compilation.GetEntryPoint(default(CancellationToken));

                Assert.Same(compilation.ObjectType.ContainingAssembly, taskOfT.ContainingAssembly);
                Assert.Same(compilation.ObjectType.ContainingAssembly, taskOfObject.ContainingAssembly);
                Assert.Equal(taskOfObject, entryPoint.ReturnType);
            }

            var firstCompilation = CSharpCompilation.CreateScriptCompilation(
                "submission-assembly-1",
                references: new[] { MinAsyncCorlibRef },
                syntaxTree: Parse("true", options: TestOptions.Script)
            ).VerifyDiagnostics();

            AssertCompilationCorlib(firstCompilation);

            var secondCompilation = CSharpCompilation.CreateScriptCompilation(
                "submission-assembly-2",
                previousScriptCompilation: firstCompilation,
                syntaxTree: Parse("false", options: TestOptions.Script))
                .WithScriptCompilationInfo(new CSharpScriptCompilationInfo(firstCompilation, null, null))
                .VerifyDiagnostics();

            AssertCompilationCorlib(secondCompilation);

            Assert.Same(firstCompilation.ObjectType, secondCompilation.ObjectType);

            Assert.Null(new CSharpScriptCompilationInfo(null, null, null)
                .WithPreviousScriptCompilation(firstCompilation)
                .ReturnTypeOpt);
        }

        [WorkItem(3719, "https://github.com/dotnet/roslyn/issues/3719")]
        [Fact]
        public void GetEntryPoint_Script()
        {
            var source = @"System.Console.WriteLine(1);";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics();

            var scriptMethod = compilation.GetMember<MethodSymbol>("Script.<Main>");
            Assert.NotNull(scriptMethod);

            var method = compilation.GetEntryPoint(default(CancellationToken));
            Assert.Equal(method, scriptMethod);
            var entryPoint = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod);
        }

        [Fact]
        public void GetEntryPoint_Script_MainIgnored()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics(
                // (4,17): warning CS7022: The entry point of the program is global script code; ignoring 'A.Main()' entry point.
                //     static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("A.Main()").WithLocation(4, 17));

            var scriptMethod = compilation.GetMember<MethodSymbol>("Script.<Main>");
            Assert.NotNull(scriptMethod);

            var entryPoint = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod);
            entryPoint.Diagnostics.Verify(
                // (4,17): warning CS7022: The entry point of the program is global script code; ignoring 'A.Main()' entry point.
                //     static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("A.Main()").WithLocation(4, 17));
        }

        [Fact]
        public void GetEntryPoint_Submission()
        {
            var source = @"1 + 1";
            var compilation = CSharpCompilation.CreateScriptCompilation("sub",
                references: new[] { MscorlibRef },
                syntaxTree: Parse(source, options: TestOptions.Script));
            compilation.VerifyDiagnostics();

            var scriptMethod = compilation.GetMember<MethodSymbol>("Script.<Factory>");
            Assert.NotNull(scriptMethod);

            var method = compilation.GetEntryPoint(default(CancellationToken));
            Assert.Equal(method, scriptMethod);
            var entryPoint = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod);
            entryPoint.Diagnostics.Verify();
        }

        [Fact]
        public void GetEntryPoint_Submission_MainIgnored()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CSharpCompilation.CreateScriptCompilation("sub",
                references: new[] { MscorlibRef },
                syntaxTree: Parse(source, options: TestOptions.Script));
            compilation.VerifyDiagnostics(
                // (4,17): warning CS7022: The entry point of the program is global script code; ignoring 'A.Main()' entry point.
                //     static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("A.Main()").WithLocation(4, 17));

            Assert.True(compilation.IsSubmission);

            var scriptMethod = compilation.GetMember<MethodSymbol>("Script.<Factory>");
            Assert.NotNull(scriptMethod);

            var entryPoint = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod);
            entryPoint.Diagnostics.Verify(
                // (4,17): warning CS7022: The entry point of the program is global script code; ignoring 'A.Main()' entry point.
                //     static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("A.Main()").WithLocation(4, 17));
        }

        [Fact]
        public void GetEntryPoint_MainType()
        {
            var source = @"
class A
{
    static void Main() { }
}

class B
{
    static void Main() { }
}
";
            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe.WithMainTypeName("B"));
            compilation.VerifyDiagnostics();

            var mainMethod = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<MethodSymbol>("Main");

            Assert.Equal(mainMethod, compilation.GetEntryPoint(default(CancellationToken)));

            var entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(mainMethod, entryPointAndDiagnostics.MethodSymbol);
            entryPointAndDiagnostics.Diagnostics.Verify();
        }

        [Fact]
        public void CanReadAndWriteDefaultWin32Res()
        {
            var comp = CSharpCompilation.Create("Compilation");
            var mft = new MemoryStream(new byte[] { 0, 1, 2, 3, });
            var res = comp.CreateDefaultWin32Resources(true, false, mft, null);
            var list = comp.MakeWin32ResourceList(res, new DiagnosticBag());
            Assert.Equal(2, list.Count);
        }

        [Fact, WorkItem(750437, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750437")]
        public void ConflictingAliases()
        {
            var alias = NetFramework.System.WithAliases(new[] { "alias" });

            var text =
@"extern alias alias;
using alias=alias;
class myClass : alias::Uri
{
}";
            var comp = CreateEmptyCompilation(text, references: new[] { MscorlibRef, alias });
            Assert.Equal(2, comp.References.Count());
            Assert.Equal("alias", comp.References.Last().Properties.Aliases.Single());
            comp.VerifyDiagnostics(
                // (2,7): warning CS8981: The type name 'alias' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // using alias=alias;
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "alias").WithArguments("alias").WithLocation(2, 7),
                // (2,1): error CS1537: The using alias 'alias' appeared previously in this namespace
                // using alias=alias;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "using alias=alias;").WithArguments("alias").WithLocation(2, 1),
                // (3,17): error CS0104: 'alias' is an ambiguous reference between '<global namespace>' and '<global namespace>'
                // class myClass : alias::Uri
                Diagnostic(ErrorCode.ERR_AmbigContext, "alias").WithArguments("alias", "<global namespace>", "<global namespace>").WithLocation(3, 17));
        }

        [WorkItem(546088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546088")]
        [Fact]
        public void CompilationDiagsIncorrectResult()
        {
            string source1 = @"
using SysAttribute = System.Attribute;
using MyAttribute = MyAttribute2Attribute;

public class MyAttributeAttribute : SysAttribute  {}
public class MyAttribute2Attribute : SysAttribute {}

[MyAttribute]
public class TestClass
{
}
";
            string source2 = @"";

            // Ask for model diagnostics first.
            {
                var compilation = CreateCompilation(source: new string[] { source1, source2 });

                var tree2 = compilation.SyntaxTrees[1]; //tree for empty file
                var model2 = compilation.GetSemanticModel(tree2);

                model2.GetDiagnostics().Verify(); // None, since the file is empty.
                compilation.GetDiagnostics().Verify(
                    // (8,2): error CS1614: 'MyAttribute' is ambiguous between 'MyAttribute2Attribute' and 'MyAttributeAttribute'; use either '@MyAttribute' or 'MyAttributeAttribute'
                    // [MyAttribute]
                    Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "MyAttribute").WithArguments("MyAttribute", "MyAttribute2Attribute", "MyAttributeAttribute"));
            }

            // Ask for compilation diagnostics first.
            {
                var compilation = CreateCompilation(source: new string[] { source1, source2 });

                var tree2 = compilation.SyntaxTrees[1]; //tree for empty file
                var model2 = compilation.GetSemanticModel(tree2);

                compilation.GetDiagnostics().Verify(
                    // (10,2): error CS1614: 'MyAttribute' is ambiguous between 'MyAttribute2Attribute' and 'MyAttributeAttribute'; use either '@MyAttribute' or 'MyAttributeAttribute'
                    // [MyAttribute]
                    Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "MyAttribute").WithArguments("MyAttribute", "MyAttribute2Attribute", "MyAttributeAttribute"));
                model2.GetDiagnostics().Verify(); // None, since the file is empty.
            }
        }

        [Fact]
        public void ReferenceManagerReuse_WithOptions()
        {
            var c1 = CSharpCompilation.Create("c", options: TestOptions.ReleaseDll);

            var c2 = c1.WithOptions(TestOptions.ReleaseExe);
            Assert.True(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsApplication));
            Assert.True(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.ReleaseDll);
            Assert.True(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule));
            Assert.False(c1.ReferenceManagerEquals(c2));

            c1 = CSharpCompilation.Create("c", options: TestOptions.ReleaseModule);

            c2 = c1.WithOptions(TestOptions.ReleaseExe);
            Assert.False(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.ReleaseDll);
            Assert.False(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.CreateTestOptions(OutputKind.WindowsApplication, OptimizationLevel.Debug));
            Assert.False(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.DebugModule.WithAllowUnsafe(true));
            Assert.True(c1.ReferenceManagerEquals(c2));
        }

        [Fact]
        public void ReferenceManagerReuse_WithMetadataReferenceResolver()
        {
            var c1 = CSharpCompilation.Create("c", options: TestOptions.ReleaseDll);

            var c2 = c1.WithOptions(TestOptions.ReleaseDll.WithMetadataReferenceResolver(new TestMetadataReferenceResolver()));

            Assert.False(c1.ReferenceManagerEquals(c2));

            var c3 = c1.WithOptions(TestOptions.ReleaseDll.WithMetadataReferenceResolver(c1.Options.MetadataReferenceResolver));
            Assert.True(c1.ReferenceManagerEquals(c3));
        }

        [Fact]
        public void ReferenceManagerReuse_WithXmlFileResolver()
        {
            var c1 = CSharpCompilation.Create("c", options: TestOptions.ReleaseDll);

            var c2 = c1.WithOptions(TestOptions.ReleaseDll.WithXmlReferenceResolver(new XmlFileResolver(null)));
            Assert.False(c1.ReferenceManagerEquals(c2));

            var c3 = c1.WithOptions(TestOptions.ReleaseDll.WithXmlReferenceResolver(c1.Options.XmlReferenceResolver));
            Assert.True(c1.ReferenceManagerEquals(c3));
        }

        [Fact]
        public void ReferenceManagerReuse_WithName()
        {
            var c1 = CSharpCompilation.Create("c1");

            var c2 = c1.WithAssemblyName("c2");
            Assert.False(c1.ReferenceManagerEquals(c2));

            var c3 = c1.WithAssemblyName("c1");
            Assert.True(c1.ReferenceManagerEquals(c3));

            var c4 = c1.WithAssemblyName(null);
            Assert.False(c1.ReferenceManagerEquals(c4));

            var c5 = c4.WithAssemblyName(null);
            Assert.True(c4.ReferenceManagerEquals(c5));
        }

        [Fact]
        public void ReferenceManagerReuse_WithReferences()
        {
            var c1 = CSharpCompilation.Create("c1");

            var c2 = c1.WithReferences(new[] { MscorlibRef });
            Assert.False(c1.ReferenceManagerEquals(c2));

            var c3 = c2.WithReferences(new[] { MscorlibRef, SystemCoreRef });
            Assert.False(c3.ReferenceManagerEquals(c2));

            c3 = c2.AddReferences(SystemCoreRef);
            Assert.False(c3.ReferenceManagerEquals(c2));

            c3 = c2.RemoveAllReferences();
            Assert.False(c3.ReferenceManagerEquals(c2));

            c3 = c2.ReplaceReference(MscorlibRef, SystemCoreRef);
            Assert.False(c3.ReferenceManagerEquals(c2));

            c3 = c2.RemoveReferences(MscorlibRef);
            Assert.False(c3.ReferenceManagerEquals(c2));
        }

        [Fact]
        public void ReferenceManagerReuse_WithSyntaxTrees()
        {
            var ta = Parse("class C { }", options: TestOptions.Regular10);

            var tb = Parse(@"
class C { }", options: TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp10));

            var tc = Parse(@"
#r ""bar""  // error: #r in regular code
class D { }", options: TestOptions.Regular10);

            var tr = Parse(@"
#r ""goo""
class C { }", options: TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp10));

            var ts = Parse(@"
#r ""bar""
class C { }", options: TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp10));

            var a = CSharpCompilation.Create("c", syntaxTrees: new[] { ta });

            // add:

            var ab = a.AddSyntaxTrees(tb);
            Assert.True(a.ReferenceManagerEquals(ab));

            var ac = a.AddSyntaxTrees(tc);
            Assert.True(a.ReferenceManagerEquals(ac));

            var ar = a.AddSyntaxTrees(tr);
            Assert.False(a.ReferenceManagerEquals(ar));

            var arc = ar.AddSyntaxTrees(tc);
            Assert.True(ar.ReferenceManagerEquals(arc));

            // remove:

            var ar2 = arc.RemoveSyntaxTrees(tc);
            Assert.True(arc.ReferenceManagerEquals(ar2));

            var c = arc.RemoveSyntaxTrees(ta, tr);
            Assert.False(arc.ReferenceManagerEquals(c));

            var none1 = c.RemoveSyntaxTrees(tc);
            Assert.True(c.ReferenceManagerEquals(none1));

            var none2 = arc.RemoveAllSyntaxTrees();
            Assert.False(arc.ReferenceManagerEquals(none2));

            var none3 = ac.RemoveAllSyntaxTrees();
            Assert.True(ac.ReferenceManagerEquals(none3));

            // replace:

            var asc = arc.ReplaceSyntaxTree(tr, ts);
            Assert.False(arc.ReferenceManagerEquals(asc));

            var brc = arc.ReplaceSyntaxTree(ta, tb);
            Assert.True(arc.ReferenceManagerEquals(brc));

            var abc = arc.ReplaceSyntaxTree(tr, tb);
            Assert.False(arc.ReferenceManagerEquals(abc));

            var ars = arc.ReplaceSyntaxTree(tc, ts);
            Assert.False(arc.ReferenceManagerEquals(ars));
        }

        [Fact]
        public void ReferenceManagerReuse_WithScriptCompilationInfo()
        {
            // Note: The following results would change if we optimized sharing more: https://github.com/dotnet/roslyn/issues/43397

            var c1 = CSharpCompilation.CreateScriptCompilation("c1");
            Assert.NotNull(c1.ScriptCompilationInfo);
            Assert.Null(c1.ScriptCompilationInfo.PreviousScriptCompilation);

            var c2 = c1.WithScriptCompilationInfo(null);
            Assert.Null(c2.ScriptCompilationInfo);
            Assert.True(c2.ReferenceManagerEquals(c1));

            var c3 = c2.WithScriptCompilationInfo(new CSharpScriptCompilationInfo(previousCompilationOpt: null, returnType: typeof(int), globalsType: null));
            Assert.NotNull(c3.ScriptCompilationInfo);
            Assert.Null(c3.ScriptCompilationInfo.PreviousScriptCompilation);
            Assert.True(c3.ReferenceManagerEquals(c2));

            var c4 = c3.WithScriptCompilationInfo(null);
            Assert.Null(c4.ScriptCompilationInfo);
            Assert.True(c4.ReferenceManagerEquals(c3));

            var c5 = c4.WithScriptCompilationInfo(new CSharpScriptCompilationInfo(previousCompilationOpt: c1, returnType: typeof(int), globalsType: null));
            Assert.False(c5.ReferenceManagerEquals(c4));

            var c6 = c5.WithScriptCompilationInfo(new CSharpScriptCompilationInfo(previousCompilationOpt: c1, returnType: typeof(bool), globalsType: null));
            Assert.True(c6.ReferenceManagerEquals(c5));

            var c7 = c6.WithScriptCompilationInfo(new CSharpScriptCompilationInfo(previousCompilationOpt: c2, returnType: typeof(bool), globalsType: null));
            Assert.False(c7.ReferenceManagerEquals(c6));

            var c8 = c7.WithScriptCompilationInfo(new CSharpScriptCompilationInfo(previousCompilationOpt: null, returnType: typeof(bool), globalsType: null));
            Assert.False(c8.ReferenceManagerEquals(c7));

            var c9 = c8.WithScriptCompilationInfo(null);
            Assert.True(c9.ReferenceManagerEquals(c8));
        }

        private sealed class EvolvingTestReference : PortableExecutableReference
        {
            private readonly IEnumerator<Metadata> _metadataSequence;
            public int QueryCount;

            public EvolvingTestReference(IEnumerable<Metadata> metadataSequence)
                : base(MetadataReferenceProperties.Assembly)
            {
                _metadataSequence = metadataSequence.GetEnumerator();
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                return DocumentationProvider.Default;
            }

            protected override Metadata GetMetadataImpl()
            {
                QueryCount++;
                _metadataSequence.MoveNext();
                return _metadataSequence.Current;
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        [ConditionalFact(typeof(NoUsedAssembliesValidation), typeof(NoIOperationValidation), Reason = "IOperation skip: Compilation changes over time, adds new errors")]
        public void MetadataConsistencyWhileEvolvingCompilation()
        {
            var md1 = AssemblyMetadata.CreateFromImage(CreateCompilation("public class C { }").EmitToArray());
            var md2 = AssemblyMetadata.CreateFromImage(CreateCompilation("public class D { }").EmitToArray());

            var reference = new EvolvingTestReference(new[] { md1, md2 });

            var c1 = CreateEmptyCompilation("public class Main { public static C C; }", new[] { MscorlibRef, reference, reference });
            var c2 = c1.WithAssemblyName("c2");
            var c3 = c2.AddSyntaxTrees(Parse("public class Main2 { public static int a; }"));
            var c4 = c3.WithOptions(TestOptions.DebugModule);
            var c5 = c4.WithReferences(new[] { MscorlibRef, reference });

            c3.VerifyDiagnostics();
            c1.VerifyDiagnostics();
            c4.VerifyDiagnostics();
            c2.VerifyDiagnostics();

            Assert.Equal(1, reference.QueryCount);

            c5.VerifyDiagnostics(
                // (1,36): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
                // public class Main2 { public static C C; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C").WithArguments("C"));

            Assert.Equal(2, reference.QueryCount);
        }

        [Fact]
        public unsafe void LinkedNetmoduleMetadataMustProvideFullPEImage()
        {
            var netModule = TestResources.MetadataTests.NetModule01.ModuleCS00;
            PEHeaders h = new PEHeaders(new MemoryStream(netModule));

            fixed (byte* ptr = &netModule[h.MetadataStartOffset])
            {
                using (var mdModule = ModuleMetadata.CreateFromMetadata((IntPtr)ptr, h.MetadataSize))
                {
                    var c = CSharpCompilation.Create("Goo", references: new[] { MscorlibRef, mdModule.GetReference(display: "ModuleCS00") }, options: TestOptions.ReleaseDll);
                    c.VerifyDiagnostics(
                        // error CS7098: Linked netmodule metadata must provide a full PE image: 'ModuleCS00'.
                        Diagnostic(ErrorCode.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage).WithArguments("ModuleCS00").WithLocation(1, 1));
                }
            }
        }

        [Fact]
        public void AppConfig1()
        {
            var references = new MetadataReference[]
            {
                NetFramework.mscorlib,
                NetFramework.System,
                TestReferences.NetFx.silverlight_v5_0_5_0.System
            };

            var compilation = CreateEmptyCompilation(
                new[] { Parse("") },
                references,
                options: TestOptions.ReleaseDll.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default));

            compilation.VerifyDiagnostics(
                // error CS1703: Multiple assemblies with equivalent identity have been imported: 'System (net461)' and 'System.v5.0.5.0_silverlight.dll'. Remove one of the duplicate references.
                Diagnostic(ErrorCode.ERR_DuplicateImport).WithArguments("System (net461)", "System.v5.0.5.0_silverlight.dll").WithLocation(1, 1));

            var appConfig = new MemoryStream(Encoding.UTF8.GetBytes(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>"));

            var comparer = DesktopAssemblyIdentityComparer.LoadFromXml(appConfig);

            compilation = CreateEmptyCompilation(
                new[] { Parse("") },
                references,
                options: TestOptions.ReleaseDll.WithAssemblyIdentityComparer(comparer));

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void AppConfig2()
        {
            // Create a dll with a reference to .NET system
            string libSource = @"
using System.Runtime.Versioning;
public class C { public static FrameworkName Goo() { return null; }}";
            var libComp = CreateEmptyCompilation(
                libSource,
                references: new[] { MscorlibRef, NetFramework.System },
                options: TestOptions.ReleaseDll);

            libComp.VerifyDiagnostics();

            var refData = libComp.EmitToArray();
            var mdRef = MetadataReference.CreateFromImage(refData);

            var references = new[]
            {
                NetFramework.mscorlib,
                NetFramework.System,
                TestReferences.NetFx.silverlight_v5_0_5_0.System,
                mdRef
            };

            // Source references the type in the dll
            string src1 = @"class A { public static void Main(string[] args) { C.Goo(); } }";

            var c1 = CreateEmptyCompilation(
                new[] { Parse(src1) },
                references,
                options: TestOptions.ReleaseDll.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default));

            c1.VerifyDiagnostics(
                // error CS1703: Multiple assemblies with equivalent identity have been imported: 'System (net461)' and 'System.v5.0.5.0_silverlight.dll'. Remove one of the duplicate references.
                Diagnostic(ErrorCode.ERR_DuplicateImport).WithArguments("System (net461)", "System.v5.0.5.0_silverlight.dll").WithLocation(1, 1),
                // (1,52): error CS7069: Reference to type 'FrameworkName' claims it is defined in 'System', but it could not be found
                // class A { public static void Main(string[] args) { C.Goo(); } }
                Diagnostic(ErrorCode.ERR_MissingTypeInAssembly, "C.Goo").WithArguments("System.Runtime.Versioning.FrameworkName", "System").WithLocation(1, 52));

            var appConfig = new MemoryStream(Encoding.UTF8.GetBytes(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>"));

            var comparer = DesktopAssemblyIdentityComparer.LoadFromXml(appConfig);

            var src2 = @"class A { public static void Main(string[] args) { C.Goo(); } }";
            var c2 = CreateEmptyCompilation(
                new[] { Parse(src2) },
                references,
                options: TestOptions.ReleaseDll.WithAssemblyIdentityComparer(comparer));

            c2.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(797640, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797640")]
        public void GetMetadataReferenceAPITest()
        {
            var comp = CSharpCompilation.Create("Compilation");
            var metadata = NetFramework.mscorlib;
            comp = comp.AddReferences(metadata);
            var assemblySmb = comp.GetReferencedAssemblySymbol(metadata);
            var reference = comp.GetMetadataReference(assemblySmb);
            Assert.NotNull(reference);

            var comp2 = CSharpCompilation.Create("Compilation");
            comp2 = comp2.AddReferences(metadata);
            var reference2 = comp2.GetMetadataReference(assemblySmb);
            Assert.NotNull(reference2);
        }

        [Fact]
        [WorkItem(40466, "https://github.com/dotnet/roslyn/issues/40466")]
        public void GetMetadataReference_VisualBasicSymbols()
        {
            var comp = CreateCompilation("");

            var vbComp = CreateVisualBasicCompilation("", referencedAssemblies: TargetFrameworkUtil.GetReferences(TargetFramework.Standard));
            var assembly = (IAssemblySymbol)vbComp.GetBoundReferenceManager().GetReferencedAssemblies().First().Value;

            Assert.Null(comp.GetMetadataReference(assembly));
            Assert.Null(comp.GetMetadataReference(vbComp.Assembly));
            Assert.Null(comp.GetMetadataReference((IAssemblySymbol)null));
        }

        [Fact]
        public void ConsistentParseOptions()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
            var tree2 = SyntaxFactory.ParseSyntaxTree("", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
            var tree3 = SyntaxFactory.ParseSyntaxTree("", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));

            var assemblyName = GetUniqueName();
            var compilationOptions = TestOptions.DebugDll;
            CSharpCompilation.Create(assemblyName, new[] { tree1, tree2 }, new[] { MscorlibRef }, compilationOptions);
            Assert.Throws<ArgumentException>(() =>
            {
                CSharpCompilation.Create(assemblyName, new[] { tree1, tree3 }, new[] { MscorlibRef }, compilationOptions);
            });
        }

        [Fact]
        public void SubmissionCompilation_Errors()
        {
            var genericParameter = typeof(List<>).GetGenericArguments()[0];
            var open = typeof(Dictionary<,>).MakeGenericType(typeof(int), genericParameter);
            var ptr = typeof(int).MakePointerType();
            var byref = typeof(int).MakeByRefType();

            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", returnType: genericParameter));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", returnType: open));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", returnType: typeof(void)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", returnType: byref));

            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: genericParameter));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: open));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: typeof(void)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: typeof(int)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: ptr));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: byref));

            var s0 = CSharpCompilation.CreateScriptCompilation("a0", globalsType: typeof(List<int>));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a1", previousScriptCompilation: s0, globalsType: typeof(List<bool>)));

            // invalid options:
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseExe));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeApplication)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsApplication)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithCryptoKeyContainer("goo")));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithCryptoKeyFile("goo.snk")));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithDelaySign(true)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithDelaySign(false)));
        }

        [Fact]
        public void HasSubmissionResult()
        {
            Assert.False(CSharpCompilation.CreateScriptCompilation("sub").HasSubmissionResult());
            Assert.True(CreateSubmission("1", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("1;", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("void goo() { }", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("using System;", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("int i;", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("System.Console.WriteLine();", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("System.Console.WriteLine()", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.True(CreateSubmission("null", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.True(CreateSubmission("System.Console.WriteLine", parseOptions: TestOptions.Script).HasSubmissionResult());
        }

        /// <summary>
        /// Previous submission has to have no errors.
        /// </summary>
        [Fact]
        public void PreviousSubmissionWithError()
        {
            var s0 = CreateSubmission("int a = \"x\";");
            s0.VerifyDiagnostics(
                // (1,9): error CS0029: Cannot implicitly convert type 'string' to 'int'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""x""").WithArguments("string", "int"));

            Assert.Throws<InvalidOperationException>(() => CreateSubmission("a + 1", previous: s0));
        }

        [Fact]
        [WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")]
        public void CreateArrayType_DefaultArgs()
        {
            var comp = (Compilation)CSharpCompilation.Create("");
            var elementType = comp.GetSpecialType(SpecialType.System_Object);

            var arrayType = comp.CreateArrayTypeSymbol(elementType);
            Assert.Equal(1, arrayType.Rank);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementNullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementType.NullableAnnotation);

            Assert.Throws<ArgumentException>(() => comp.CreateArrayTypeSymbol(elementType, default));
            Assert.Throws<ArgumentException>(() => comp.CreateArrayTypeSymbol(elementType, 0));

            arrayType = comp.CreateArrayTypeSymbol(elementType, 1, default);
            Assert.Equal(1, arrayType.Rank);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementNullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementType.NullableAnnotation);

            Assert.Throws<ArgumentException>(() => comp.CreateArrayTypeSymbol(elementType, rank: default));
            Assert.Throws<ArgumentException>(() => comp.CreateArrayTypeSymbol(elementType, rank: 0));

            arrayType = comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: default);
            Assert.Equal(1, arrayType.Rank);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementNullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementType.NullableAnnotation);
        }

        [Fact]
        [WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")]
        public void CreateArrayType_ElementNullableAnnotation()
        {
            var comp = (Compilation)CSharpCompilation.Create("");
            var elementType = comp.GetSpecialType(SpecialType.System_Object);

            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType).ElementNullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType).ElementType.NullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: CodeAnalysis.NullableAnnotation.None).ElementNullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: CodeAnalysis.NullableAnnotation.None).ElementType.NullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: CodeAnalysis.NullableAnnotation.None).ElementNullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: CodeAnalysis.NullableAnnotation.None).ElementType.NullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.NotAnnotated, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: CodeAnalysis.NullableAnnotation.NotAnnotated).ElementNullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.NotAnnotated, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: CodeAnalysis.NullableAnnotation.NotAnnotated).ElementType.NullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.Annotated, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: CodeAnalysis.NullableAnnotation.Annotated).ElementNullableAnnotation);
            Assert.Equal(CodeAnalysis.NullableAnnotation.Annotated, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation: CodeAnalysis.NullableAnnotation.Annotated).ElementType.NullableAnnotation);
        }

        [Fact]
        public void CreateAnonymousType_IncorrectLengths()
        {
            var compilation = CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                    ImmutableArray.Create((ITypeSymbol)null),
                    ImmutableArray.Create("m1", "m2")));
        }

        [Fact]
        public void CreateAnonymousType_IncorrectLengths_IsReadOnly()
        {
            var compilation = (Compilation)CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                    ImmutableArray.Create((ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32),
                                          (ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32)),
                    ImmutableArray.Create("m1", "m2"),
                    ImmutableArray.Create(true)));
        }

        [Fact]
        public void CreateAnonymousType_IncorrectLengths_Locations()
        {
            var compilation = (Compilation)CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                    ImmutableArray.Create((ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32),
                                          (ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32)),
                    ImmutableArray.Create("m1", "m2"),
                    memberLocations: ImmutableArray.Create(Location.None)));
        }

        [Fact]
        public void CreateAnonymousType_WritableProperty()
        {
            var compilation = (Compilation)CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                    ImmutableArray.Create((ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32),
                                          (ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32)),
                    ImmutableArray.Create("m1", "m2"),
                    ImmutableArray.Create(false, false)));
        }

        [Fact]
        public void CreateAnonymousType_NullLocations()
        {
            var compilation = (Compilation)CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentNullException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                    ImmutableArray.Create((ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32),
                                          (ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32)),
                    ImmutableArray.Create("m1", "m2"),
                    memberLocations: ImmutableArray.Create(Location.None, null)));
        }

        [Fact]
        public void CreateAnonymousType_NullArgument1()
        {
            var compilation = CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentNullException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                        default(ImmutableArray<ITypeSymbol>),
                        ImmutableArray.Create("m1")));
        }

        [Fact]
        public void CreateAnonymousType_NullArgument2()
        {
            var compilation = CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentNullException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create((ITypeSymbol)null),
                        default(ImmutableArray<string>)));
        }

        [Fact]
        public void CreateAnonymousType_NullArgument3()
        {
            var compilation = CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentNullException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create((ITypeSymbol)null),
                        ImmutableArray.Create("m1")));
        }

        [Fact]
        public void CreateAnonymousType_NullArgument4()
        {
            var compilation = (Compilation)CSharpCompilation.Create("HelloWorld");
            Assert.Throws<ArgumentNullException>(() =>
                compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create((ITypeSymbol)compilation.GetSpecialType(SpecialType.System_Int32)),
                        ImmutableArray.Create((string)null)));
        }

        [Fact]
        public void CreateAnonymousType1()
        {
            var compilation = (Compilation)CSharpCompilation.Create("HelloWorld");
            var type = compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create<ITypeSymbol>(compilation.GetSpecialType(SpecialType.System_Int32)),
                        ImmutableArray.Create("m1"));

            Assert.True(type.IsAnonymousType);
            Assert.Equal(1, type.GetMembers().OfType<IPropertySymbol>().Count());
            Assert.Equal("<anonymous type: int m1>", type.ToDisplayString());
            Assert.All(type.GetMembers().OfType<IPropertySymbol>().Select(p => p.Locations.FirstOrDefault()),
                loc => Assert.Equal(loc, Location.None));
        }

        [Fact]
        public void CreateAnonymousType_Locations()
        {
            var compilation = (Compilation)CSharpCompilation.Create("HelloWorld");
            var tree = CSharpSyntaxTree.ParseText("class C { }");
            var loc1 = Location.Create(tree, new TextSpan(0, 1));
            var loc2 = Location.Create(tree, new TextSpan(1, 1));

            var type = compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create<ITypeSymbol>(compilation.GetSpecialType(SpecialType.System_Int32),
                                                           compilation.GetSpecialType(SpecialType.System_Int32)),
                        ImmutableArray.Create("m1", "m2"),
                        memberLocations: ImmutableArray.Create(loc1, loc2));

            Assert.True(type.IsAnonymousType);
            Assert.Equal(2, type.GetMembers().OfType<IPropertySymbol>().Count());
            Assert.Equal(loc1, type.GetMembers("m1").Single().Locations.Single());
            Assert.Equal(loc2, type.GetMembers("m2").Single().Locations.Single());
            Assert.Equal("<anonymous type: int m1, int m2>", type.ToDisplayString());
        }

        [Fact]
        public void CreateAnonymousType2()
        {
            var compilation = (Compilation)CSharpCompilation.Create("HelloWorld");
            var type = compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create<ITypeSymbol>(compilation.GetSpecialType(SpecialType.System_Int32), compilation.GetSpecialType(SpecialType.System_Boolean)),
                        ImmutableArray.Create("m1", "m2"));

            Assert.True(type.IsAnonymousType);
            Assert.Equal(2, type.GetMembers().OfType<IPropertySymbol>().Count());
            Assert.Equal("<anonymous type: int m1, bool m2>", type.ToDisplayString());
            Assert.All(type.GetMembers().OfType<IPropertySymbol>().Select(p => p.Locations.FirstOrDefault()),
                loc => Assert.Equal(loc, Location.None));
        }

        [Fact]
        [WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")]
        public void CreateAnonymousType_DefaultArgs()
        {
            var comp = (Compilation)CSharpCompilation.Create("");
            var memberTypes = ImmutableArray.Create<ITypeSymbol>(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String));
            var memberNames = ImmutableArray.Create("P", "Q");

            var type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames);
            Assert.Equal("<anonymous type: System.Object P, System.String Q>", type.ToTestDisplayString(includeNonNullable: true));

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, default);
            Assert.Equal("<anonymous type: System.Object P, System.String Q>", type.ToTestDisplayString(includeNonNullable: true));

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, default, default);
            Assert.Equal("<anonymous type: System.Object P, System.String Q>", type.ToTestDisplayString(includeNonNullable: true));

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, default, default, default);
            Assert.Equal("<anonymous type: System.Object P, System.String Q>", type.ToTestDisplayString(includeNonNullable: true));

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberIsReadOnly: default);
            Assert.Equal("<anonymous type: System.Object P, System.String Q>", type.ToTestDisplayString(includeNonNullable: true));

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberLocations: default);
            Assert.Equal("<anonymous type: System.Object P, System.String Q>", type.ToTestDisplayString(includeNonNullable: true));

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberNullableAnnotations: default);
            Assert.Equal("<anonymous type: System.Object P, System.String Q>", type.ToTestDisplayString(includeNonNullable: true));
        }

        [Fact]
        [WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")]
        public void CreateAnonymousType_MemberNullableAnnotations_Empty()
        {
            var comp = (Compilation)CSharpCompilation.Create("");
            var type = comp.CreateAnonymousTypeSymbol(ImmutableArray<ITypeSymbol>.Empty, ImmutableArray<string>.Empty, memberNullableAnnotations: ImmutableArray<CodeAnalysis.NullableAnnotation>.Empty);
            Assert.Equal("<empty anonymous type>", type.ToTestDisplayString(includeNonNullable: true));
            AssertEx.Equal(Array.Empty<CodeAnalysis.NullableAnnotation>(), GetAnonymousTypeNullableAnnotations(type));
        }

        [Fact]
        [WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")]
        public void CreateAnonymousType_MemberNullableAnnotations()
        {
            var comp = (Compilation)CSharpCompilation.Create("");
            var memberTypes = ImmutableArray.Create<ITypeSymbol>(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String));
            var memberNames = ImmutableArray.Create("P", "Q");

            var type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames);
            Assert.Equal("<anonymous type: System.Object P, System.String Q>", type.ToTestDisplayString(includeNonNullable: true));
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None }, GetAnonymousTypeNullableAnnotations(type));

            Assert.Throws<ArgumentException>(() => comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberNullableAnnotations: ImmutableArray.Create(CodeAnalysis.NullableAnnotation.NotAnnotated)));

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberNullableAnnotations: ImmutableArray.Create(CodeAnalysis.NullableAnnotation.NotAnnotated, CodeAnalysis.NullableAnnotation.Annotated));
            Assert.Equal("<anonymous type: System.Object! P, System.String? Q>", type.ToTestDisplayString(includeNonNullable: true));
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.NotAnnotated, CodeAnalysis.NullableAnnotation.Annotated }, GetAnonymousTypeNullableAnnotations(type));
        }

        private static ImmutableArray<CodeAnalysis.NullableAnnotation> GetAnonymousTypeNullableAnnotations(ITypeSymbol type)
        {
            return type.GetMembers().OfType<IPropertySymbol>().SelectAsArray(p =>
            {
                var result = p.Type.NullableAnnotation;
                Assert.Equal(result, p.NullableAnnotation);
                return result;
            });
        }

        [Theory]
        [InlineData(WellKnownMemberNames.CheckedAdditionOperatorName, "int.operator checked +(int, int)")]
        [InlineData(WellKnownMemberNames.AdditionOperatorName, "int.operator +(int, int)")]
        [InlineData(WellKnownMemberNames.BitwiseAndOperatorName, "int.operator &(int, int)")]
        [InlineData(WellKnownMemberNames.CheckedDivisionOperatorName, "int.operator checked /(int, int)")]
        [InlineData(WellKnownMemberNames.DivisionOperatorName, "int.operator /(int, int)")]
        [InlineData(WellKnownMemberNames.LeftShiftOperatorName, "int.operator <<(int, int)")]
        [InlineData(WellKnownMemberNames.CheckedMultiplyOperatorName, "int.operator checked *(int, int)")]
        [InlineData(WellKnownMemberNames.MultiplyOperatorName, "int.operator *(int, int)")]
        [InlineData(WellKnownMemberNames.BitwiseOrOperatorName, "int.operator |(int, int)")]
        [InlineData(WellKnownMemberNames.ModulusOperatorName, "int.operator %(int, int)")]
        [InlineData(WellKnownMemberNames.RightShiftOperatorName, "int.operator >>(int, int)")]
        [InlineData(WellKnownMemberNames.UnsignedRightShiftOperatorName, "int.operator >>>(int, int)")]
        [InlineData(WellKnownMemberNames.CheckedSubtractionOperatorName, "int.operator checked -(int, int)")]
        [InlineData(WellKnownMemberNames.SubtractionOperatorName, "int.operator -(int, int)")]
        [InlineData(WellKnownMemberNames.ExclusiveOrOperatorName, "int.operator ^(int, int)")]
        public void CreateBuiltinBinaryOperator_Supported(
            string name, string display)
        {
            var compilation = CreateCompilation("");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            var op = compilation.CreateBuiltinOperator(name, intType, intType, intType);
            var result = op.ToDisplayString();
            AssertEx.Equal(display, result);
        }

        [Theory]
        [InlineData(WellKnownMemberNames.EqualityOperatorName, "int.operator ==(int, int)")]
        [InlineData(WellKnownMemberNames.GreaterThanOperatorName, "int.operator >(int, int)")]
        [InlineData(WellKnownMemberNames.GreaterThanOrEqualOperatorName, "int.operator >=(int, int)")]
        [InlineData(WellKnownMemberNames.LessThanOperatorName, "int.operator <(int, int)")]
        [InlineData(WellKnownMemberNames.LessThanOrEqualOperatorName, "int.operator <=(int, int)")]
        [InlineData(WellKnownMemberNames.InequalityOperatorName, "int.operator !=(int, int)")]
        public void CreateBuiltinBinaryOperator_Supported_BoolReturnType(
            string name, string display)
        {
            var compilation = CreateCompilation("");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            var boolType = compilation.GetSpecialType(SpecialType.System_Boolean).GetPublicSymbol();
            var op = compilation.CreateBuiltinOperator(name, boolType, intType, intType);
            var result = op.ToDisplayString();
            AssertEx.Equal(display, result);
        }

        [Fact]
        public void CreateBuiltinBinaryOperator_BogusErrorType()
        {
            var compilation = CreateCompilation("");
            var fakeIntType = compilation.CreateErrorTypeSymbol(compilation.CreateErrorNamespaceSymbol(compilation.GlobalNamespace, "System"), "Int32", arity: 0);
            Assert.Throws<ArgumentException>(() =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, fakeIntType, fakeIntType, fakeIntType));
        }

        [Fact]
        public void CreateBuiltinBinaryOperator_RealErrorType()
        {
            var compilation = CreateCompilation("", references: Array.Empty<MetadataReference>(), targetFramework: TargetFramework.Empty);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            var op = compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, intType, intType);
            var result = op.ToDisplayString();
            AssertEx.Equal("int.operator +(int, int)", result);
        }

        [Fact]
        public void CreateBuiltinBinaryOperator_NotSupported()
        {
            var compilation = CreateCompilation("");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            var nullableIntType = compilation.GetSpecialType(SpecialType.System_Nullable_T).GetPublicSymbol().Construct(intType);

            // vb binary operator name
            Assert.Throws<ArgumentException>("name", () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.LikeOperatorName, intType, intType, intType));

            // unary operator name
            Assert.Throws<ArgumentException>("name", () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, intType, intType, intType));

            // nullable type 1
            Assert.Throws<ArgumentException>(null, () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedAdditionOperatorName, nullableIntType, intType, intType));

            // nullable type 2
            Assert.Throws<ArgumentException>(null, () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedAdditionOperatorName, intType, nullableIntType, intType));

            // nullable type 3
            Assert.Throws<ArgumentException>(null, () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedAdditionOperatorName, intType, intType, nullableIntType));
        }

        [Fact]
        public void CreateBuiltinBinaryOperator_NullChecks()
        {
            var compilation = CreateCompilation("");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            Assert.Throws<ArgumentNullException>("returnType", () => compilation.CreateBuiltinOperator(
                WellKnownMemberNames.AdditionOperatorName, null, intType, intType));
            Assert.Throws<ArgumentNullException>("leftType", () => compilation.CreateBuiltinOperator(
                WellKnownMemberNames.AdditionOperatorName, intType, null, intType));
            Assert.Throws<ArgumentNullException>("rightType", () => compilation.CreateBuiltinOperator(
                WellKnownMemberNames.AdditionOperatorName, intType, intType, null));
        }

        [Fact]
        public void CreateBuiltinBinaryOperator_CompareToActualSymbols1()
        {
            var compilation = CreateCompilation(@"
class C
{
    void M()
    {
        var m = /*<bind>*/1 + 1/*</bind>*/;
    }
}");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();

            var syntaxTree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var expr = GetSyntaxNodeOfTypeForBinding<BinaryExpressionSyntax>(GetSyntaxNodeList(syntaxTree));
            var symbol = semanticModel.GetSymbolInfo(expr).Symbol;

            Assert.NotNull(symbol);

            var addBuiltIn = compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, intType, intType);
            var addBuiltInChecked = compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedAdditionOperatorName, intType, intType, intType);
            var subtractBuiltIn = compilation.CreateBuiltinOperator(WellKnownMemberNames.SubtractionOperatorName, intType, intType, intType);
            var subtractBuiltInChecked = compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedSubtractionOperatorName, intType, intType, intType);

            Assert.Equal(addBuiltIn, symbol);
            Assert.NotEqual(addBuiltInChecked, symbol);
            Assert.NotEqual(subtractBuiltIn, symbol);
            Assert.NotEqual(subtractBuiltInChecked, symbol);
        }

        [Fact]
        public void CreateBuiltinBinaryOperator_CompareToActualSymbols2()
        {
            var compilation = CreateCompilation(@"
class C
{
    void M()
    {
        checked
        {
            var m = /*<bind>*/1 + 1/*</bind>*/;
        }
    }
}");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();

            var syntaxTree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var expr = GetSyntaxNodeOfTypeForBinding<BinaryExpressionSyntax>(GetSyntaxNodeList(syntaxTree));
            var symbol = semanticModel.GetSymbolInfo(expr).Symbol;

            Assert.NotNull(symbol);

            var addBuiltIn = compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, intType, intType);
            var addBuiltInChecked = compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedAdditionOperatorName, intType, intType, intType);
            var subtractBuiltIn = compilation.CreateBuiltinOperator(WellKnownMemberNames.SubtractionOperatorName, intType, intType, intType);
            var subtractBuiltInChecked = compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedSubtractionOperatorName, intType, intType, intType);

            Assert.NotEqual(addBuiltIn, symbol);
            Assert.Equal(addBuiltInChecked, symbol);
            Assert.NotEqual(subtractBuiltIn, symbol);
            Assert.NotEqual(subtractBuiltInChecked, symbol);
        }

        [Fact]
        public void CreateBuiltinBinaryOperator_CompareToActualSymbols3()
        {
            var compilation = CreateCompilation(@"
class C
{
    void M()
    {
        var m = /*<bind>*/1 + 1/*</bind>*/;
    }
}", options: TestOptions.ReleaseDll.WithOverflowChecks(true));
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();

            var syntaxTree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var expr = GetSyntaxNodeOfTypeForBinding<BinaryExpressionSyntax>(GetSyntaxNodeList(syntaxTree));
            var symbol = semanticModel.GetSymbolInfo(expr).Symbol;

            Assert.NotNull(symbol);

            var addBuiltIn = compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, intType, intType);
            var addBuiltInChecked = compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedAdditionOperatorName, intType, intType, intType);
            var subtractBuiltIn = compilation.CreateBuiltinOperator(WellKnownMemberNames.SubtractionOperatorName, intType, intType, intType);
            var subtractBuiltInChecked = compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedSubtractionOperatorName, intType, intType, intType);

            Assert.NotEqual(addBuiltIn, symbol);
            Assert.Equal(addBuiltInChecked, symbol);
            Assert.NotEqual(subtractBuiltIn, symbol);
            Assert.NotEqual(subtractBuiltInChecked, symbol);
        }

        [Theory]
        [InlineData(WellKnownMemberNames.UnaryPlusOperatorName, "int.operator +(int)")]
        [InlineData(WellKnownMemberNames.CheckedUnaryNegationOperatorName, "int.operator checked -(int)")]
        [InlineData(WellKnownMemberNames.UnaryNegationOperatorName, "int.operator -(int)")]
        [InlineData(WellKnownMemberNames.OnesComplementOperatorName, "int.operator ~(int)")]
        [InlineData(WellKnownMemberNames.CheckedIncrementOperatorName, "int.operator checked ++(int)")]
        [InlineData(WellKnownMemberNames.IncrementOperatorName, "int.operator ++(int)")]
        [InlineData(WellKnownMemberNames.CheckedDecrementOperatorName, "int.operator checked --(int)")]
        [InlineData(WellKnownMemberNames.DecrementOperatorName, "int.operator --(int)")]
        public void CreateBuiltinUnaryOperator_Supported(
            string name, string display)
        {
            var compilation = CreateCompilation("");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            var op = compilation.CreateBuiltinOperator(name, intType, intType);
            var result = op.ToDisplayString();
            AssertEx.Equal(display, result);
        }

        [Theory]
        [InlineData(WellKnownMemberNames.LogicalNotOperatorName, "bool.operator !(bool)")]
        public void CreateBuiltinUnaryOperator_Supported_Bool(
            string name, string display)
        {
            var compilation = CreateCompilation("");
            var boolType = compilation.GetSpecialType(SpecialType.System_Boolean).GetPublicSymbol();
            var op = compilation.CreateBuiltinOperator(name, boolType, boolType);
            var result = op.ToDisplayString();
            AssertEx.Equal(display, result);
        }

        [Fact]
        public void CreateBuiltinUnaryOperator_BogusErrorType()
        {
            var compilation = CreateCompilation("");
            var fakeIntType = compilation.CreateErrorTypeSymbol(compilation.CreateErrorNamespaceSymbol(compilation.GlobalNamespace, "System"), "Int32", arity: 0);
            Assert.Throws<ArgumentException>(null, () => compilation.CreateBuiltinOperator(
                WellKnownMemberNames.UnaryPlusOperatorName, fakeIntType, fakeIntType));
        }

        [Fact]
        public void CreateBuiltinUnaryOperator_RealErrorType()
        {
            var compilation = CreateCompilation("", references: Array.Empty<MetadataReference>(), targetFramework: TargetFramework.Empty);
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            var op = compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, intType, intType);
            var result = op.ToDisplayString();
            AssertEx.Equal("int.operator +(int)", result);
        }

        [Fact]
        public void CreateBuiltinUnaryOperator_NotSupported()
        {
            var compilation = CreateCompilation("");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            var nullableIntType = compilation.GetSpecialType(SpecialType.System_Nullable_T).GetPublicSymbol().Construct(intType);

            // Binary operator name
            Assert.Throws<ArgumentException>("name", () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, intType));

            // Nullable type 1
            Assert.Throws<ArgumentException>(null, () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedUnaryNegationOperatorName, nullableIntType, intType));

            // Nullable type 2
            Assert.Throws<ArgumentException>(null, () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedUnaryNegationOperatorName, intType, nullableIntType));

            // op_Implicit
            Assert.Throws<ArgumentException>("name", () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.ImplicitConversionName, intType, intType));

            // op_Explicit
            Assert.Throws<ArgumentException>("name", () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.ExplicitConversionName, intType, intType));

            // op_True
            Assert.Throws<ArgumentException>("name", () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.TrueOperatorName, intType, intType));

            // op_False
            Assert.Throws<ArgumentException>("name", () =>
                compilation.CreateBuiltinOperator(WellKnownMemberNames.FalseOperatorName, intType, intType));
        }

        [Fact]
        public void CreateBuiltinUnaryOperator_NullChecks()
        {
            var compilation = CreateCompilation("");
            var intType = compilation.GetSpecialType(SpecialType.System_Int32).GetPublicSymbol();
            Assert.Throws<ArgumentNullException>("returnType", () => compilation.CreateBuiltinOperator(
                WellKnownMemberNames.UnaryPlusOperatorName, null, intType));
            Assert.Throws<ArgumentNullException>("operandType", () => compilation.CreateBuiltinOperator(
                WellKnownMemberNames.UnaryPlusOperatorName, intType, null));
        }

        [Fact]
        [WorkItem(36046, "https://github.com/dotnet/roslyn/issues/36046")]
        public void ConstructTypeWithNullability()
        {
            var source =
@"class Pair<T, U>
{
}";
            var comp = (Compilation)CreateCompilation(source);
            var genericType = (INamedTypeSymbol)comp.GetMember("Pair");
            var typeArguments = ImmutableArray.Create<ITypeSymbol>(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String));

            Assert.Throws<ArgumentException>(() => genericType.Construct(default(ImmutableArray<ITypeSymbol>), default(ImmutableArray<CodeAnalysis.NullableAnnotation>)));
            Assert.Throws<ArgumentException>(() => genericType.Construct(typeArguments: default, typeArgumentNullableAnnotations: default));

            var type = genericType.Construct(typeArguments, default);
            Assert.Equal("Pair<System.Object, System.String>", type.ToTestDisplayString(includeNonNullable: true));
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None }, type.TypeArgumentNullableAnnotations);
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None }, type.TypeArgumentNullableAnnotations());

            Assert.Throws<ArgumentException>(() => genericType.Construct(typeArguments, ImmutableArray<CodeAnalysis.NullableAnnotation>.Empty));
            Assert.Throws<ArgumentException>(() => genericType.Construct(ImmutableArray.Create<ITypeSymbol>(null, null), default));

            type = genericType.Construct(typeArguments, ImmutableArray.Create(CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.NotAnnotated));
            Assert.Equal("Pair<System.Object?, System.String!>", type.ToTestDisplayString(includeNonNullable: true));
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.NotAnnotated }, type.TypeArgumentNullableAnnotations);
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.NotAnnotated }, type.TypeArgumentNullableAnnotations());

            // Type arguments from VB.
            comp = CreateVisualBasicCompilation("");
            typeArguments = ImmutableArray.Create<ITypeSymbol>(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String));
            Assert.Throws<ArgumentException>(() => genericType.Construct(typeArguments, default));
        }

        [Fact]
        [WorkItem(37310, "https://github.com/dotnet/roslyn/issues/37310")]
        public void ConstructMethodWithNullability()
        {
            var source =
@"class Program
{
    static void M<T, U>() { }
}";
            var comp = (Compilation)CreateCompilation(source);
            var genericMethod = (IMethodSymbol)comp.GetMember("Program.M");
            var typeArguments = ImmutableArray.Create<ITypeSymbol>(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String));

            Assert.Throws<ArgumentException>(() => genericMethod.Construct(default(ImmutableArray<ITypeSymbol>), default(ImmutableArray<CodeAnalysis.NullableAnnotation>)));
            Assert.Throws<ArgumentException>(() => genericMethod.Construct(typeArguments: default, typeArgumentNullableAnnotations: default));

            var type = genericMethod.Construct(typeArguments, default);
            Assert.Equal("void Program.M<System.Object, System.String>()", type.ToTestDisplayString(includeNonNullable: true));
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None }, type.TypeArgumentNullableAnnotations);
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None }, type.TypeArgumentNullableAnnotations());

            Assert.Throws<ArgumentException>(() => genericMethod.Construct(typeArguments, ImmutableArray<CodeAnalysis.NullableAnnotation>.Empty));
            Assert.Throws<ArgumentException>(() => genericMethod.Construct(ImmutableArray.Create<ITypeSymbol>(null, null), default));

            type = genericMethod.Construct(typeArguments, ImmutableArray.Create(CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.NotAnnotated));
            Assert.Equal("void Program.M<System.Object?, System.String!>()", type.ToTestDisplayString(includeNonNullable: true));
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.NotAnnotated }, type.TypeArgumentNullableAnnotations);
            AssertEx.Equal(new[] { CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.NotAnnotated }, type.TypeArgumentNullableAnnotations());

            // Type arguments from VB.
            comp = CreateVisualBasicCompilation("");
            typeArguments = ImmutableArray.Create<ITypeSymbol>(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String));
            Assert.Throws<ArgumentException>(() => genericMethod.Construct(typeArguments, default));
        }

        [Fact, WorkItem(65499, "https://github.com/dotnet/roslyn/issues/65499")]
        public void NetModuleReference()
        {
            var module = CreateCompilation(string.Empty, options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule));
            module.VerifyDiagnostics();

            var moduleStream = new MemoryStream();
            var result = module.Emit(moduleStream);
            Assert.True(result.Success);
            moduleStream.Position = 0;
            var moduleReference = MetadataReference.CreateFromStream(moduleStream, MetadataReferenceProperties.Module);

            var comp = CreateCompilation(string.Empty, references: new[] { moduleReference });
            comp.VerifyEmitDiagnostics();
            Assert.Equal(2, comp.Assembly.Modules.Length);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem(65499, "https://github.com/dotnet/roslyn/issues/65499")]
        [WorkItem(71236, "https://github.com/dotnet/roslyn/issues/71236")]
        public void NetModuleReference_AssemblySpecifiedAsModule()
        {
            var module = CreateCompilation(string.Empty);
            module.VerifyDiagnostics();

            var moduleStream = new MemoryStream();
            var result = module.Emit(moduleStream);
            Assert.True(result.Success);
            moduleStream.Position = 0;
            var moduleReference = MetadataReference.CreateFromStream(moduleStream, MetadataReferenceProperties.Module);

            var comp = CreateCompilation(string.Empty, references: new[] { moduleReference });
            comp.VerifyEmitDiagnostics(
                // error CS1542: '<in-memory module>' cannot be added to this assembly because it already is an assembly
                Diagnostic(ErrorCode.ERR_AddModuleAssembly).WithArguments("<in-memory module>").WithLocation(1, 1));
            Assert.Equal(1, comp.Assembly.Modules.Length);
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        [WorkItem(65499, "https://github.com/dotnet/roslyn/issues/65499")]
        [WorkItem(71236, "https://github.com/dotnet/roslyn/issues/71236")]
        public void NetModuleReference_ModuleSpecifiedAsAssembly()
        {
            var module = CreateCompilation(string.Empty, options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule));
            module.VerifyDiagnostics();

            var moduleStream = new MemoryStream();
            var result = module.Emit(moduleStream);
            Assert.True(result.Success);
            moduleStream.Position = 0;
            var moduleReference = MetadataReference.CreateFromStream(moduleStream, MetadataReferenceProperties.Assembly);

            var comp = CreateCompilation(string.Empty, references: new[] { moduleReference });
            comp.VerifyEmitDiagnostics(
                // error CS1509: The referenced file '<in-memory assembly>' is not an assembly
                Diagnostic(ErrorCode.ERR_ImportNonAssembly).WithArguments("<in-memory assembly>").WithLocation(1, 1));
            Assert.Equal(1, comp.Assembly.Modules.Length);
        }

        #region Script return values

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnNullAsObject()
        {
            var script = CreateSubmission("return null;", returnType: typeof(object));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnStringAsObject()
        {
            var script = CreateSubmission("return \"¡Hola!\";", returnType: typeof(object));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnIntAsObject()
        {
            var script = CreateSubmission("return 42;", returnType: typeof(object));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void TrailingReturnVoidAsObject()
        {
            var script = CreateSubmission("return", returnType: typeof(object));
            script.VerifyDiagnostics(
                // (1,7): error CS1733: Expected expression
                // return
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 7),
                // (1,7): error CS1002: ; expected
                // return
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 7));
            Assert.False(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnIntAsInt()
        {
            var script = CreateSubmission("return 42;", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnNullResultType()
        {
            // test that passing null is the same as passing typeof(object)
            var script = CreateSubmission("return 42;", returnType: null);
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnNoSemicolon()
        {
            var script = CreateSubmission("return 42", returnType: typeof(uint));
            script.VerifyDiagnostics(
                // (1,10): error CS1002: ; expected
                // return 42
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 10));
            Assert.False(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnAwait()
        {
            var script = CreateSubmission("return await System.Threading.Tasks.Task.FromResult(42);", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());

            script = CreateSubmission("return await System.Threading.Tasks.Task.FromResult(42);", returnType: typeof(Task<int>));
            script.VerifyDiagnostics(
                // (1,8): error CS0029: Cannot implicitly convert type 'int' to 'System.Threading.Tasks.Task<int>'
                // return await System.Threading.Tasks.Task.FromResult(42);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "await System.Threading.Tasks.Task.FromResult(42)").WithArguments("int", "System.Threading.Tasks.Task<int>").WithLocation(1, 8));
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnTaskNoAwait()
        {
            var script = CreateSubmission("return System.Threading.Tasks.Task.FromResult(42);", returnType: typeof(int));
            script.VerifyDiagnostics(
                // (1,8): error CS4016: Since this is an async method, the return expression must be of type 'int' rather than 'Task<int>'
                // return System.Threading.Tasks.Task.FromResult(42);
                Diagnostic(ErrorCode.ERR_BadAsyncReturnExpression, "System.Threading.Tasks.Task.FromResult(42)").WithArguments("int", "System.Threading.Tasks.Task<int>").WithLocation(1, 8));
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnInNestedScopes()
        {
            var script = CreateSubmission(@"
bool condition = false;
if (condition)
{
    return 1;
}
else
{
    return -1;
}", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnInNestedScopeWithTrailingExpression()
        {
            var script = CreateSubmission(@"
if (true)
{
    return 1;
}
System.Console.WriteLine();", returnType: typeof(object));
            script.VerifyDiagnostics(
                // (6,1): warning CS0162: Unreachable code detected
                // System.Console.WriteLine();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(6, 1));
            Assert.True(script.HasSubmissionResult());

            script = CreateSubmission(@"
if (true)
{
    return 1;
}
System.Console.WriteLine()", returnType: typeof(object));
            script.VerifyDiagnostics(
                // (6,1): warning CS0162: Unreachable code detected
                // System.Console.WriteLine();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(6, 1));
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnInNestedScopeNoTrailingExpression()
        {
            var script = CreateSubmission(@"
bool condition = false;
if (condition)
{
    return 1;
}
System.Console.WriteLine();", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnInNestedMethod()
        {
            var script = CreateSubmission(@"
int TopMethod()
{
    return 42;
}", returnType: typeof(string));
            script.VerifyDiagnostics();
            Assert.False(script.HasSubmissionResult());

            script = CreateSubmission(@"
object TopMethod()
{
    return new System.Exception();
}
TopMethod().ToString()", returnType: typeof(string));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnInNestedLambda()
        {
            var script = CreateSubmission(@"
System.Func<object> f = () =>
{
    return new System.Exception();
};
42", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());

            script = CreateSubmission(@"
System.Func<object> f = () => new System.Exception();
42", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnInNestedAnonymousMethod()
        {
            var script = CreateSubmission(@"
System.Func<object> f = delegate ()
{
    return new System.Exception();
};
42", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void LoadedFileWithWrongReturnType()
        {
            var resolver = TestSourceReferenceResolver.Create(
                KeyValuePairUtil.Create("a.csx", "return \"Who returns a string?\";"));
            var script = CreateSubmission(@"
#load ""a.csx""
42", returnType: typeof(int), options: TestOptions.DebugDll.WithSourceReferenceResolver(resolver));
            script.VerifyDiagnostics(
                // a.csx(1,8): error CS0029: Cannot implicitly convert type 'string' to 'int'
                // return "Who returns a string?"
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""Who returns a string?""").WithArguments("string", "int").WithLocation(1, 8),
                // (3,1): warning CS0162: Unreachable code detected
                // 42
                Diagnostic(ErrorCode.WRN_UnreachableCode, "42").WithLocation(3, 1));
            Assert.True(script.HasSubmissionResult());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void ReturnVoidInNestedMethodOrLambda()
        {
            var script = CreateSubmission(@"
void M1()
{
    return;
}
System.Action a = () => { return; };
42", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());

            var compilation = CreateCompilationWithMscorlib461(@"
void M1()
{
    return;
}
System.Action a = () => { return; };
42", parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics();
        }

        #endregion

        #region GetTypesByMetadataName Tests

        [Fact]
        public void GetTypesByMetadataName_NotInSourceNotInReferences()
        {
            var comp = CreateCompilation("");
            comp.VerifyDiagnostics();

            var types = comp.GetTypesByMetadataName("N.C`1");
            Assert.Empty(types);
        }

        [Theory]
        [CombinatorialData]
        public void GetTypesByMetadataName_SingleInSourceNotInReferences(
            bool useMetadataReference,
            [CombinatorialValues("public", "internal")] string accessibility)
        {
            var referenceComp = CreateCompilation("");

            var comp = CreateCompilation(
$@"namespace N;
{accessibility} class C<T> {{}}", new[] { useMetadataReference ? referenceComp.ToMetadataReference() : referenceComp.EmitToImageReference() });

            comp.VerifyDiagnostics();

            var types = comp.GetTypesByMetadataName("N.C`1");

            Assert.Single(types);
            AssertEx.Equal("N.C<T>", types[0].ToTestDisplayString());
        }

        [Theory]
        [CombinatorialData]
        public void GetTypesByMetadataName_MultipleInSourceNotInReferences(
            bool useMetadataReference,
            [CombinatorialValues("public", "internal")] string accessibility)
        {
            var referenceComp = CreateCompilation("");

            var comp = CreateCompilation(
$@"namespace N;
{accessibility} class C<T> {{}}
{accessibility} class C<T> {{}}", new[] { useMetadataReference ? referenceComp.ToMetadataReference() : referenceComp.EmitToImageReference() });

            comp.VerifyDiagnostics(
                // (3,16): error CS0101: The namespace 'N' already contains a definition for 'C'
                // internal class C<T> {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInNS, "C").WithArguments("C", "N").WithLocation(3, 8 + accessibility.Length)
            );

            var types = comp.GetTypesByMetadataName("N.C`1");

            Assert.Single(types);
            AssertEx.Equal("N.C<T>", types[0].ToTestDisplayString());
            Assert.Equal(2, types[0].Locations.Length);
        }

        [Theory]
        [CombinatorialData]
        public void GetTypesByMetadataName_SingleInSourceSingleInReferences(
            bool useMetadataReference,
            [CombinatorialValues("public", "internal")] string accessibility)
        {
            string source = $@"namespace N;
{accessibility} class C<T> {{}}";

            var referenceComp = CreateCompilation(source);

            referenceComp.VerifyDiagnostics();

            MetadataReference reference = useMetadataReference ? referenceComp.ToMetadataReference() : referenceComp.EmitToImageReference();
            var comp = CreateCompilation(source, new[] { reference });
            comp.VerifyDiagnostics();

            var types = comp.GetTypesByMetadataName("N.C`1");

            Assert.Equal(2, types.Length);
            AssertEx.Equal("N.C<T>", types[0].ToTestDisplayString());
            Assert.Same(comp.Assembly.GetPublicSymbol(), types[0].ContainingAssembly);
            AssertEx.Equal("N.C<T>", types[1].ToTestDisplayString());

            var assembly1 = comp.GetAssemblyOrModuleSymbol(reference).GetPublicSymbol();
            Assert.Same(types[1].ContainingAssembly, assembly1);
        }

        [Theory]
        [CombinatorialData]
        public void GetTypesByMetadataName_NotInSourceSingleInReferences(
            bool useMetadataReference,
            [CombinatorialValues("public", "internal")] string accessibility)
        {
            string source = @$"namespace N;
{accessibility} class C<T> {{}}";

            var referenceComp = CreateCompilation(source);

            referenceComp.VerifyDiagnostics();

            MetadataReference reference = useMetadataReference ? referenceComp.ToMetadataReference() : referenceComp.EmitToImageReference();
            var comp = CreateCompilation("", new[] { reference });
            comp.VerifyDiagnostics();

            var types = comp.GetTypesByMetadataName("N.C`1");

            Assert.Single(types);
            AssertEx.Equal("N.C<T>", types[0].ToTestDisplayString());

            var assembly1 = comp.GetAssemblyOrModuleSymbol(reference).GetPublicSymbol();
            Assert.Same(types[0].ContainingAssembly, assembly1);
        }

        [Theory]
        [CombinatorialData]
        public void GetTypesByMetadataName_NotInSourceMultipleInReferences(
            bool useMetadataReference,
            [CombinatorialValues("public", "internal")] string accessibility)
        {
            string source = @$"namespace N;
{accessibility} class C<T> {{}}";

            var referenceComp1 = CreateCompilation(source);
            referenceComp1.VerifyDiagnostics();

            var referenceComp2 = CreateCompilation(source);
            referenceComp2.VerifyDiagnostics();

            MetadataReference reference1 = getReference(referenceComp1);
            MetadataReference reference2 = getReference(referenceComp2);
            var comp = CreateCompilation("", new[] { reference1, reference2 });
            comp.VerifyDiagnostics();

            var types = comp.GetTypesByMetadataName("N.C`1");

            Assert.Equal(2, types.Length);
            AssertEx.Equal("N.C<T>", types[0].ToTestDisplayString());
            AssertEx.Equal("N.C<T>", types[1].ToTestDisplayString());

            var assembly1 = comp.GetAssemblyOrModuleSymbol(reference1).GetPublicSymbol();
            Assert.Same(types[0].ContainingAssembly, assembly1);

            var assembly2 = comp.GetAssemblyOrModuleSymbol(reference2).GetPublicSymbol();
            Assert.Same(types[1].ContainingAssembly, assembly2);

            MetadataReference getReference(CSharpCompilation referenceComp1)
            {
                return useMetadataReference ? referenceComp1.ToMetadataReference() : referenceComp1.EmitToImageReference();
            }
        }

        [Theory]
        [CombinatorialData]
        public void GetTypesByMetadataName_SingleInSourceMultipleInReferences(
            bool useMetadataReference,
            [CombinatorialValues("public", "internal")] string accessibility)
        {
            string source = @$"namespace N;
{accessibility} class C<T> {{}}";

            var referenceComp1 = CreateCompilation(source);
            referenceComp1.VerifyDiagnostics();

            var referenceComp2 = CreateCompilation(source);
            referenceComp2.VerifyDiagnostics();

            MetadataReference reference1 = getReference(referenceComp1);
            MetadataReference reference2 = getReference(referenceComp2);
            var comp = CreateCompilation(source, new[] { reference1, reference2 });
            comp.VerifyDiagnostics();

            var types = comp.GetTypesByMetadataName("N.C`1");

            Assert.Equal(3, types.Length);
            AssertEx.Equal("N.C<T>", types[0].ToTestDisplayString());
            Assert.Same(comp.Assembly.GetPublicSymbol(), types[0].ContainingAssembly);
            AssertEx.Equal("N.C<T>", types[1].ToTestDisplayString());
            AssertEx.Equal("N.C<T>", types[2].ToTestDisplayString());

            var assembly1 = comp.GetAssemblyOrModuleSymbol(reference1).GetPublicSymbol();
            Assert.Same(types[1].ContainingAssembly, assembly1);

            var assembly2 = comp.GetAssemblyOrModuleSymbol(reference2).GetPublicSymbol();
            Assert.Same(types[2].ContainingAssembly, assembly2);

            MetadataReference getReference(CSharpCompilation referenceComp1)
            {
                return useMetadataReference ? referenceComp1.ToMetadataReference() : referenceComp1.EmitToImageReference();
            }
        }

        [Fact]
        public void GetTypesByMetadataName_Ordering()
        {
            var corlibSource = @"
namespace System
{
    public class Object {}
    public class Void {}
}

public class C {}
";

            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var corlib = CreateEmptyCompilation(corlibSource, parseOptions: parseOptions);
            var corlibReference = corlib.EmitToImageReference();

            var other = CreateEmptyCompilation(@"public class C {}", new[] { corlibReference }, parseOptions: parseOptions);
            var otherReference = other.EmitToImageReference();

            var current = CreateEmptyCompilation(@"public class C {}", new[] { otherReference, corlibReference });
            current.VerifyDiagnostics();

            var types = current.GetTypesByMetadataName("C");

            AssertEx.Equal(types.Select(t => t.ToTestDisplayString()), new[] { "C", "C", "C" });
            Assert.Same(types[0].ContainingAssembly, current.Assembly.GetPublicSymbol());

            var corlibAssembly = current.GetAssemblyOrModuleSymbol(corlibReference).GetPublicSymbol();
            Assert.Same(types[1].ContainingAssembly, corlibAssembly);

            var otherAssembly = current.GetAssemblyOrModuleSymbol(otherReference).GetPublicSymbol();
            Assert.Same(types[2].ContainingAssembly, otherAssembly);
        }

        [Fact]
        public void GetTypeByMetadataName_CorLibViaExtern()
        {
            var corlibSource = @"
namespace System
{
    public class Object {}
    public class Void {}
}

public class C {}
";

            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var corlib = CreateEmptyCompilation(corlibSource, parseOptions: parseOptions);
            var corlibReference = corlib.EmitToImageReference(aliases: ImmutableArray.Create("corlib"));

            var current = CreateEmptyCompilation(@"", new[] { corlibReference }, parseOptions: parseOptions);
            current.VerifyDiagnostics();

            var type = ((Compilation)current).GetTypeByMetadataName("C");
            Assert.NotNull(type);

            var corlibAssembly = current.GetAssemblyOrModuleSymbol(corlibReference).GetPublicSymbol();
            Assert.Same(type.ContainingAssembly, corlibAssembly);
        }

        [Fact]
        public void GetTypeByMetadataName_OtherViaExtern()
        {
            var corlibSource = @"
namespace System
{
    public class Object {}
    public class Void {}
}
";

            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var corlib = CreateEmptyCompilation(corlibSource, parseOptions: parseOptions);
            var corlibReference = corlib.EmitToImageReference();

            var other = CreateEmptyCompilation(@"public class C {}", new[] { corlibReference }, parseOptions: parseOptions);
            var otherReference = other.EmitToImageReference(aliases: ImmutableArray.Create("other"));

            var current = CreateEmptyCompilation(@"", new[] { otherReference, corlibReference });
            current.VerifyDiagnostics();

            var type = ((Compilation)current).GetTypeByMetadataName("C");
            Assert.Null(type);
        }

        #endregion
    }
}
