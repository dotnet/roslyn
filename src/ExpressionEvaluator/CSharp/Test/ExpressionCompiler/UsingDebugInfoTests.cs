// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.DiaSymReader;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class UsingDebugInfoTests : ExpressionCompilerTestBase
    {
        #region Grouped import strings 

        [Fact]
        public void SimplestCase()
        {
            var source = @"
using System;

class C
{
    void M()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var importStrings = GetGroupedImportStrings(comp, "M");
            Assert.Equal("USystem", importStrings.Single().Single());
        }

        [Fact]
        public void NestedNamespaces()
        {
            var source = @"
using System;

namespace A
{
    using System.IO;
    using System.Text;

    class C
    {
        void M()
        {
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var importStrings = GetGroupedImportStrings(comp, "M");
            Assert.Equal(2, importStrings.Length);
            AssertEx.Equal(importStrings[0], new[] { "USystem.IO", "USystem.Text" });
            AssertEx.Equal(importStrings[1], new[] { "USystem" });
        }

        [Fact]
        public void Forward()
        {
            var source = @"
using System;

namespace A
{
    using System.IO;
    using System.Text;

    class C
    {
        // One of these methods will forward to the other since they're adjacent.
        void M1() { }
        void M2() { }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var importStrings1 = GetGroupedImportStrings(comp, "M1");
            Assert.Equal(2, importStrings1.Length);
            AssertEx.Equal(importStrings1[0], new[] { "USystem.IO", "USystem.Text" });
            AssertEx.Equal(importStrings1[1], new[] { "USystem" });

            var importStrings2 = GetGroupedImportStrings(comp, "M2");
            Assert.Equal(2, importStrings2.Length);
            AssertEx.Equal(importStrings2[0], importStrings1[0]);
            AssertEx.Equal(importStrings2[1], importStrings1[1]);
        }

        [Fact]
        public void ImportKinds()
        {
            var source = @"
extern alias A;
using S = System;

namespace B
{
    using F = S.IO.File;
    using System.Text;

    class C
    {
        void M()
        {
        }
    }
}
";
            var aliasedRef = new CSharpCompilationReference(CreateCompilation("", assemblyName: "Lib"), aliases: ImmutableArray.Create("A"));
            var comp = CreateCompilationWithMscorlib(source, new[] { aliasedRef });
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            ImmutableArray<string> externAliasStrings;
            var importStrings = GetGroupedImportStrings(comp, "M", out externAliasStrings);
            Assert.Equal(2, importStrings.Length);
            AssertEx.Equal(importStrings[0], new[] { "USystem.Text", "AF TSystem.IO.File, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" });
            AssertEx.Equal(importStrings[1], new[] { "XA", "AS USystem" });
            Assert.Equal("ZA Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", externAliasStrings.Single());
        }

        [WorkItem(1084059)]
        [Fact]
        public void ImportKinds_StaticType()
        {
            var libSource = @"
namespace N
{
    public static class Static
    {
    }
}
";

            var source = @"
extern alias A;
using static System.Math;

namespace B
{
    using static A::N.Static;

    class C
    {
        void M()
        {
        }
    }
}
";
            var aliasedRef = new CSharpCompilationReference(CreateCompilation(libSource, assemblyName: "Lib"), aliases: ImmutableArray.Create("A"));
            var comp = CreateCompilationWithMscorlib(source, new[] { aliasedRef });
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            ImmutableArray<string> externAliasStrings;
            var importStrings = GetGroupedImportStrings(comp, "M", out externAliasStrings);
            Assert.Equal(2, importStrings.Length);
            AssertEx.Equal(importStrings[0], new[] { "TN.Static, Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" });
            AssertEx.Equal(importStrings[1], new[] { "XA", "TSystem.Math, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" });
            Assert.Equal("ZA Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", externAliasStrings.Single());
        }

        [Fact]
        public void ForwardToModule()
        {
            var source = @"
extern alias A;

namespace B
{
    using System;

    class C
    {
        void M1()
        {
        }
    }
}

namespace D
{
    using System.Text; // Different using to prevent normal forwarding.

    class E
    {
        void M2()
        {
        }
    }
}
";
            var aliasedRef = new CSharpCompilationReference(CreateCompilation("", assemblyName: "Lib"), aliases: ImmutableArray.Create("A"));
            var comp = CreateCompilationWithMscorlib(source, new[] { aliasedRef });
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            ImmutableArray<string> externAliasStrings1;
            var importStrings1 = GetGroupedImportStrings(comp, "M1", out externAliasStrings1);
            Assert.Equal(2, importStrings1.Length);
            AssertEx.Equal("USystem", importStrings1[0].Single());
            AssertEx.Equal("XA", importStrings1[1].Single());
            Assert.Equal("ZA Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", externAliasStrings1.Single());

            ImmutableArray<string> externAliasStrings2;
            var importStrings2 = GetGroupedImportStrings(comp, "M2", out externAliasStrings2);
            Assert.Equal(2, importStrings2.Length);
            AssertEx.Equal("USystem.Text", importStrings2[0].Single());
            AssertEx.Equal(importStrings1[1].Single(), importStrings2[1].Single());
            Assert.Equal(externAliasStrings1.Single(), externAliasStrings2.Single());
        }

        private static ImmutableArray<ImmutableArray<string>> GetGroupedImportStrings(Compilation compilation, string methodName)
        {
            ImmutableArray<string> externAliasStrings;
            ImmutableArray<ImmutableArray<string>> result = GetGroupedImportStrings(compilation, methodName, out externAliasStrings);
            Assert.Equal(0, externAliasStrings.Length);
            return result;
        }

        private static ImmutableArray<ImmutableArray<string>> GetGroupedImportStrings(Compilation compilation, string methodName, out ImmutableArray<string> externAliasStrings)
        {
            Assert.NotNull(compilation);
            Assert.NotNull(methodName);

            using (var exebits = new MemoryStream())
            {
                using (var pdbbits = new MemoryStream())
                {
                    compilation.Emit(exebits, pdbbits);

                    exebits.Position = 0;
                    using (var module = new PEModule(new PEReader(exebits, PEStreamOptions.LeaveOpen), metadataOpt: IntPtr.Zero, metadataSizeOpt: 0))
                    {
                        var metadataReader = module.MetadataReader;
                        MethodDefinitionHandle methodHandle = metadataReader.MethodDefinitions.Single(mh => metadataReader.GetString(metadataReader.GetMethodDefinition(mh).Name) == methodName);
                        int methodToken = metadataReader.GetToken(methodHandle);

                        // Create a SymReader, rather than a raw COM object, because
                        // SymReader implements ISymUnmanagedReader3 and the COM object
                        // might not.
                        pdbbits.Position = 0;
                        var reader = SymReaderFactory.CreateReader(pdbbits);
                        return reader.GetCSharpGroupedImportStrings(methodToken, methodVersion: 1, externAliasStrings: out externAliasStrings);
                    }
                }
            }
        }

        #endregion Grouped import strings 

        #region Invalid PDBs

        [Fact]
        public void BadPdb_ForwardChain()
        {
            const int methodVersion = 1;
            const int methodToken1 = 0x600057a; // Forwards to 2
            const int methodToken2 = 0x600055d; // Forwards to 3
            const int methodToken3 = 0x6000540; // Has a using
            const string importString = "USystem";

            ISymUnmanagedReader reader = new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
            {
                { methodToken1, new MethodDebugInfoBytes.Builder().AddForward(methodToken2).Build() },
                { methodToken2, new MethodDebugInfoBytes.Builder().AddForward(methodToken3).Build() },
                { methodToken3, new MethodDebugInfoBytes.Builder(new [] { new [] { importString } }).Build() },
            }.ToImmutableDictionary());

            ImmutableArray<string> externAliasStrings;
            var importStrings = reader.GetCSharpGroupedImportStrings(methodToken1, methodVersion, out externAliasStrings);
            Assert.True(importStrings.IsDefault);
            Assert.True(externAliasStrings.IsDefault);

            importStrings = reader.GetCSharpGroupedImportStrings(methodToken2, methodVersion, out externAliasStrings);
            Assert.Equal(importString, importStrings.Single().Single());
            Assert.Equal(0, externAliasStrings.Length);

            importStrings = reader.GetCSharpGroupedImportStrings(methodToken2, methodVersion, out externAliasStrings);
            Assert.Equal(importString, importStrings.Single().Single());
            Assert.Equal(0, externAliasStrings.Length);
        }

        [Fact]
        public void BadPdb_Cycle()
        {
            const int methodVersion = 1;
            const int methodToken1 = 0x600057a; // Forwards to itself

            ISymUnmanagedReader reader = new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
            {
                { methodToken1, new MethodDebugInfoBytes.Builder().AddForward(methodToken1).Build() },
            }.ToImmutableDictionary());

            ImmutableArray<string> externAliasStrings;
            var importStrings = reader.GetCSharpGroupedImportStrings(methodToken1, methodVersion, out externAliasStrings);
            Assert.True(importStrings.IsDefault);
            Assert.True(externAliasStrings.IsDefault);
        }

        [WorkItem(999086)]
        [Fact]
        public void BadPdb_InvalidAliasSyntax()
        {
            var source = @"
public class C
{
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            byte[] exeBytes;
            byte[] unusedPdbBytes;
            ImmutableArray<MetadataReference> references;
            var result = comp.EmitAndGetReferences(out exeBytes, out unusedPdbBytes, out references);
            Assert.True(result);

            var symReader = ExpressionCompilerTestHelpers.ConstructSymReaderWithImports(
                exeBytes,
                "Main",
                "USystem", // Valid.
                "UACultureInfo TSystem.Globalization.CultureInfo, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", // Invalid - skipped.
                "ASI USystem.IO"); // Valid.

            var runtime = CreateRuntimeInstance("assemblyName", references, exeBytes, symReader);
            var evalContext = CreateMethodContext(runtime, "C.Main");
            var compContext = evalContext.CreateCompilationContext(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)); // Used to throw.
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal("System", imports.Usings.Single().NamespaceOrType.ToTestDisplayString());
            Assert.Equal("SI", imports.UsingAliases.Keys.Single());
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        [WorkItem(999086)]
        [Fact]
        public void BadPdb_DotInAlias()
        {
            var source = @"
public class C
{
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            byte[] exeBytes;
            byte[] unusedPdbBytes;
            ImmutableArray<MetadataReference> references;
            var result = comp.EmitAndGetReferences(out exeBytes, out unusedPdbBytes, out references);
            Assert.True(result);

            var symReader = ExpressionCompilerTestHelpers.ConstructSymReaderWithImports(
                exeBytes,
                "Main",
                "USystem", // Valid.
                "AMy.Alias TSystem.Globalization.CultureInfo, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", // Invalid - skipped.
                "ASI USystem.IO"); // Valid.

            var runtime = CreateRuntimeInstance("assemblyName", references, exeBytes, symReader);
            var evalContext = CreateMethodContext(runtime, "C.Main");
            var compContext = evalContext.CreateCompilationContext(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)); // Used to throw.
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal("System", imports.Usings.Single().NamespaceOrType.ToTestDisplayString());
            Assert.Equal("SI", imports.UsingAliases.Keys.Single());
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        [WorkItem(1007917)]
        [Fact]
        public void BadPdb_NestingLevel_TooMany()
        {
            var source = @"
public class C
{
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            byte[] exeBytes;
            byte[] unusedPdbBytes;
            ImmutableArray<MetadataReference> references;
            var result = comp.EmitAndGetReferences(out exeBytes, out unusedPdbBytes, out references);
            Assert.True(result);

            ISymUnmanagedReader symReader;
            using (var peReader = new PEReader(ImmutableArray.Create(exeBytes)))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = metadataReader.MethodDefinitions.Single(h => metadataReader.StringComparer.Equals(metadataReader.GetMethodDefinition(h).Name, "Main"));
                var methodToken = metadataReader.GetToken(methodHandle);

                symReader = new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
                {
                    { methodToken, new MethodDebugInfoBytes.Builder(new [] { new[] { "USystem", "USystem.IO" } }, suppressUsingInfo: true).AddUsingInfo(1, 1).Build() },
                }.ToImmutableDictionary());
            }

            var runtime = CreateRuntimeInstance("assemblyName", references, exeBytes, symReader);
            var evalContext = CreateMethodContext(runtime, "C.Main");
            var compContext = evalContext.CreateCompilationContext(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal("System.IO", imports.Usings.Single().NamespaceOrType.ToTestDisplayString()); // Note: some information is preserved.
            Assert.Equal(0, imports.UsingAliases.Count);
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        [WorkItem(1007917)]
        [Fact]
        public void BadPdb_NestingLevel_TooFew()
        {
            var source = @"
namespace N
{
    public class C
    {
        public static void Main()
        {
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            byte[] exeBytes;
            byte[] unusedPdbBytes;
            ImmutableArray<MetadataReference> references;
            var result = comp.EmitAndGetReferences(out exeBytes, out unusedPdbBytes, out references);
            Assert.True(result);

            ISymUnmanagedReader symReader;
            using (var peReader = new PEReader(ImmutableArray.Create(exeBytes)))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = metadataReader.MethodDefinitions.Single(h => metadataReader.StringComparer.Equals(metadataReader.GetMethodDefinition(h).Name, "Main"));
                var methodToken = metadataReader.GetToken(methodHandle);

                symReader = new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
                {
                    { methodToken, new MethodDebugInfoBytes.Builder(new [] { new[] { "USystem" } }, suppressUsingInfo: true).AddUsingInfo(1).Build() },
                }.ToImmutableDictionary());
            }

            var runtime = CreateRuntimeInstance("assemblyName", references, exeBytes, symReader);
            var evalContext = CreateMethodContext(runtime, "N.C.Main");
            var compContext = evalContext.CreateCompilationContext(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal("System", imports.Usings.Single().NamespaceOrType.ToTestDisplayString()); // Note: some information is preserved.
            Assert.Equal(0, imports.UsingAliases.Count);
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        [WorkItem(1084059)]
        [Fact]
        public void BadPdb_NonStaticTypeImport()
        {
            var source = @"
namespace N
{
    public class C
    {
        public static void Main()
        {
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            byte[] exeBytes;
            byte[] unusedPdbBytes;
            ImmutableArray<MetadataReference> references;
            var result = comp.EmitAndGetReferences(out exeBytes, out unusedPdbBytes, out references);
            Assert.True(result);

            ISymUnmanagedReader symReader;
            using (var peReader = new PEReader(ImmutableArray.Create(exeBytes)))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = metadataReader.MethodDefinitions.Single(h => metadataReader.StringComparer.Equals(metadataReader.GetMethodDefinition(h).Name, "Main"));
                var methodToken = metadataReader.GetToken(methodHandle);

                symReader = new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
                {
                    { methodToken, new MethodDebugInfoBytes.Builder(new [] { new[] { "TSystem.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" } }, suppressUsingInfo: true).AddUsingInfo(1).Build() },
                }.ToImmutableDictionary());
            }

            var runtime = CreateRuntimeInstance("assemblyName", references, exeBytes, symReader);
            var evalContext = CreateMethodContext(runtime, "N.C.Main");
            var compContext = evalContext.CreateCompilationContext(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal(0, imports.Usings.Length); // Note: the import is dropped
            Assert.Equal(0, imports.UsingAliases.Count);
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        #endregion Invalid PDBs

        #region Binder chain

        [Fact]
        public void ImportsForSimpleUsing()
        {
            var source = @"
using System;

class C
{
    int M()
    {
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.UsingAliases.Count);
            Assert.Equal(0, imports.ExternAliases.Length);

            var actualNamespace = imports.Usings.Single().NamespaceOrType;
            Assert.Equal(SymbolKind.Namespace, actualNamespace.Kind);
            Assert.Equal(NamespaceKind.Module, ((NamespaceSymbol)actualNamespace).Extent.Kind);
            Assert.Equal("System", actualNamespace.ToTestDisplayString());
        }

        [Fact]
        public void ImportsForMultipleUsings()
        {
            var source = @"
using System;
using System.IO;
using System.Text;

class C
{
    int M()
    {
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.UsingAliases.Count);
            Assert.Equal(0, imports.ExternAliases.Length);

            var usings = imports.Usings.Select(u => u.NamespaceOrType).ToArray();
            Assert.Equal(3, usings.Length);

            var expectedNames = new[] { "System", "System.IO", "System.Text" };
            for (int i = 0; i < usings.Length; i++)
            {
                var actualNamespace = usings[i];
                Assert.Equal(SymbolKind.Namespace, actualNamespace.Kind);
                Assert.Equal(NamespaceKind.Module, ((NamespaceSymbol)actualNamespace).Extent.Kind);
                Assert.Equal(expectedNames[i], actualNamespace.ToTestDisplayString());
            }
        }

        [Fact]
        public void ImportsForNestedNamespaces()
        {
            var source = @"
using System;

namespace A
{
    using System.IO;

    class C
    {
        int M()
        {
            return 1;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "A.C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single()).AsEnumerable().ToArray();
            Assert.Equal(2, importsList.Length);

            var expectedNames = new[] { "System.IO", "System" }; // Innermost-to-outermost
            for (int i = 0; i < importsList.Length; i++)
            {
                var imports = importsList[i];

                Assert.Equal(0, imports.UsingAliases.Count);
                Assert.Equal(0, imports.ExternAliases.Length);

                var actualNamespace = imports.Usings.Single().NamespaceOrType;
                Assert.Equal(SymbolKind.Namespace, actualNamespace.Kind);
                Assert.Equal(NamespaceKind.Module, ((NamespaceSymbol)actualNamespace).Extent.Kind);
                Assert.Equal(expectedNames[i], actualNamespace.ToTestDisplayString());
            }
        }

        [Fact]
        public void ImportsForNamespaceAlias()
        {
            var source = @"
using S = System;

class C
{
    int M()
    {
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.Usings.Length);
            Assert.Equal(0, imports.ExternAliases.Length);

            var usingAliases = imports.UsingAliases;

            Assert.Equal(1, usingAliases.Count);
            Assert.Equal("S", usingAliases.Keys.Single());

            var aliasSymbol = usingAliases.Values.Single().Alias;
            Assert.Equal("S", aliasSymbol.Name);

            var namespaceSymbol = aliasSymbol.Target;
            Assert.Equal(SymbolKind.Namespace, namespaceSymbol.Kind);
            Assert.Equal(NamespaceKind.Module, ((NamespaceSymbol)namespaceSymbol).Extent.Kind);
            Assert.Equal("System", namespaceSymbol.ToTestDisplayString());
        }

        [WorkItem(1084059)]
        [Fact]
        public void ImportsForStaticType()
        {
            var source = @"
using static System.Math;

class C
{
    int M()
    {
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.UsingAliases.Count);
            Assert.Equal(0, imports.ExternAliases.Length);

            var actualType = imports.Usings.Single().NamespaceOrType;
            Assert.Equal(SymbolKind.NamedType, actualType.Kind);
            Assert.Equal("System.Math", actualType.ToTestDisplayString());
        }

        [Fact]
        public void ImportsForTypeAlias()
        {
            var source = @"
using I = System.Int32;

class C
{
    int M()
    {
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.Usings.Length);
            Assert.Equal(0, imports.ExternAliases.Length);

            var usingAliases = imports.UsingAliases;

            Assert.Equal(1, usingAliases.Count);
            Assert.Equal("I", usingAliases.Keys.Single());

            var aliasSymbol = usingAliases.Values.Single().Alias;
            Assert.Equal("I", aliasSymbol.Name);

            var typeSymbol = aliasSymbol.Target;
            Assert.Equal(SymbolKind.NamedType, typeSymbol.Kind);
            Assert.Equal(SpecialType.System_Int32, ((NamedTypeSymbol)typeSymbol).SpecialType);
        }

        [Fact]
        public void ImportsForVerbatimIdentifiers()
        {
            var source = @"
using @namespace;
using @object = @namespace;
using @string = @namespace.@class<@namespace.@interface>.@struct;

namespace @namespace
{
    public class @class<T>
    {
        public struct @struct
        {
        }
    }

    public interface @interface
    {
    }
}

class C
{
    int M()
    {
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.ExternAliases.Length);

            var @using = imports.Usings.Single();
            var importedNamespace = @using.NamespaceOrType;
            Assert.Equal(SymbolKind.Namespace, importedNamespace.Kind);
            Assert.Equal("namespace", importedNamespace.Name);

            var usingAliases = imports.UsingAliases;

            const string keyword1 = "object";
            const string keyword2 = "string";
            AssertEx.SetEqual(usingAliases.Keys, keyword1, keyword2);

            var namespaceAlias = usingAliases[keyword1];
            var typeAlias = usingAliases[keyword2];

            Assert.Equal(keyword1, namespaceAlias.Alias.Name);
            var aliasedNamespace = namespaceAlias.Alias.Target;
            Assert.Equal(SymbolKind.Namespace, aliasedNamespace.Kind);
            Assert.Equal("@namespace", aliasedNamespace.ToTestDisplayString());

            Assert.Equal(keyword2, typeAlias.Alias.Name);
            var aliasedType = typeAlias.Alias.Target;
            Assert.Equal(SymbolKind.NamedType, aliasedType.Kind);
            Assert.Equal("@namespace.@class<@namespace.@interface>.@struct", aliasedType.ToTestDisplayString());
        }

        [Fact]
        public void ImportsForGenericTypeAlias()
        {
            var source = @"
using I = System.Collections.Generic.IEnumerable<string>;

class C
{
    int M()
    {
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.Usings.Length);
            Assert.Equal(0, imports.ExternAliases.Length);

            var usingAliases = imports.UsingAliases;

            Assert.Equal(1, usingAliases.Count);
            Assert.Equal("I", usingAliases.Keys.Single());

            var aliasSymbol = usingAliases.Values.Single().Alias;
            Assert.Equal("I", aliasSymbol.Name);

            var typeSymbol = aliasSymbol.Target;
            Assert.Equal(SymbolKind.NamedType, typeSymbol.Kind);
            Assert.Equal("System.Collections.Generic.IEnumerable<System.String>", typeSymbol.ToTestDisplayString());
        }

        [Fact]
        public void ImportsForExternAlias()
        {
            var source = @"
extern alias X;

class C
{
    int M()
    {
        X::System.Xml.Linq.LoadOptions.None.ToString();
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemXmlLinqRef.WithAliases(ImmutableArray.Create("X")) });
            comp.VerifyDiagnostics();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.Usings.Length);
            Assert.Equal(0, imports.UsingAliases.Count);

            var externAliases = imports.ExternAliases;

            Assert.Equal(1, externAliases.Length);

            var aliasSymbol = externAliases.Single().Alias;
            Assert.Equal("X", aliasSymbol.Name);

            var targetSymbol = aliasSymbol.Target;
            Assert.Equal(SymbolKind.Namespace, targetSymbol.Kind);
            Assert.True(((NamespaceSymbol)targetSymbol).IsGlobalNamespace);
            Assert.Equal("System.Xml.Linq", targetSymbol.ContainingAssembly.Name);

            var moduleInstance = runtime.Modules.Single(m => m.ModuleMetadata.Name.StartsWith("System.Xml.Linq", StringComparison.OrdinalIgnoreCase));
            AssertEx.SetEqual(moduleInstance.MetadataReference.Properties.Aliases, "X");
        }

        [Fact]
        public void ImportsForUsingsConsumingExternAlias()
        {
            var source = @"
extern alias X;
using SXL = X::System.Xml.Linq;
using LO = X::System.Xml.Linq.LoadOptions;
using X::System.Xml;

class C
{
    int M()
    {
        X::System.Xml.Linq.LoadOptions.None.ToString();
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemXmlLinqRef.WithAliases(ImmutableArray.Create("X")) });
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(1, imports.ExternAliases.Length);

            var @using = imports.Usings.Single();
            var importedNamespace = @using.NamespaceOrType;
            Assert.Equal(SymbolKind.Namespace, importedNamespace.Kind);
            Assert.Equal("System.Xml", importedNamespace.ToTestDisplayString());

            var usingAliases = imports.UsingAliases;
            Assert.Equal(2, usingAliases.Count);
            AssertEx.SetEqual(usingAliases.Keys, "SXL", "LO");

            var typeAlias = usingAliases["SXL"].Alias;
            Assert.Equal("SXL", typeAlias.Name);
            Assert.Equal("System.Xml.Linq", typeAlias.Target.ToTestDisplayString());

            var namespaceAlias = usingAliases["LO"].Alias;
            Assert.Equal("LO", namespaceAlias.Name);
            Assert.Equal("System.Xml.Linq.LoadOptions", namespaceAlias.Target.ToTestDisplayString());
        }

        [Fact]
        public void ImportsForUsingsConsumingExternAliasAndGlobal()
        {
            var source = @"
extern alias X;
using A = X::System.Xml.Linq;
using B = global::System.Xml.Linq;

class C
{
    int M()
    {
        A.LoadOptions.None.ToString();
        B.LoadOptions.None.ToString();
        return 1;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemXmlLinqRef.WithAliases(ImmutableArray.Create("global", "X")) });
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            var runtime = CreateRuntimeInstance(comp, includeSymbols: true);
            var importsList = GetImports(runtime, "C.M", comp.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<Syntax.LiteralExpressionSyntax>().Single());

            var imports = importsList.Single();

            Assert.Equal(0, imports.Usings.Length);
            Assert.Equal(1, imports.ExternAliases.Length);

            var usingAliases = imports.UsingAliases;
            Assert.Equal(2, usingAliases.Count);
            AssertEx.SetEqual(usingAliases.Keys, "A", "B");

            var aliasA = usingAliases["A"].Alias;
            Assert.Equal("A", aliasA.Name);
            Assert.Equal("System.Xml.Linq", aliasA.Target.ToTestDisplayString());

            var aliasB = usingAliases["B"].Alias;
            Assert.Equal("B", aliasB.Name);
            Assert.Equal(aliasA.Target, aliasB.Target);
        }

        private static ImportChain GetImports(RuntimeInstance runtime, string methodName, Syntax.ExpressionSyntax syntax)
        {
            var evalContext = CreateMethodContext(
                runtime,
                methodName: methodName);
            var compContext = evalContext.CreateCompilationContext(syntax);
            return compContext.NamespaceBinder.ImportChain;
        }

        #endregion Binder chain

        [Fact]
        public void NoSymbols()
        {
            var source =
@"using N;
class A
{
    static void M() { }
}
namespace N
{
    class B
    {
        static void M() { }
    }
}";
            ResultProperties resultProperties;
            string error;

            // With symbols, type reference without namespace qualifier.
            var testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "A.M",
                expr: "typeof(B)",
                resultProperties: out resultProperties,
                error: out error,
                includeSymbols: true);
            Assert.Null(error);

            // Without symbols, type reference without namespace qualifier.
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "A.M",
                expr: "typeof(B)",
                resultProperties: out resultProperties,
                error: out error,
                includeSymbols: false);
            Assert.Equal(error, "error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)");

            // With symbols, type reference inside namespace.
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "N.B.M",
                expr: "typeof(B)",
                resultProperties: out resultProperties,
                error: out error,
                includeSymbols: true);
            Assert.Null(error);

            // Without symbols, type reference inside namespace.
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName: "N.B.M",
                expr: "typeof(B)",
                resultProperties: out resultProperties,
                error: out error,
                includeSymbols: false);
            Assert.Null(error);
        }

        [WorkItem(2441, "https://github.com/dotnet/roslyn/issues/2441")]
        [Fact]
        public void AssemblyQualifiedNameResolutionWithUnification()
        {
            var source1 = @"
using SI = System.Int32;

public class C1
{
    void M()
    {
    }
}
";

            var source2 = @"
public class C2 : C1
{
}
";
            ImmutableArray<MetadataReference> unused;

            var comp1 = CreateCompilation(source1, new[] { MscorlibRef_v20 }, TestOptions.DebugDll, assemblyName: "A");
            byte[] dllBytes1;
            byte[] pdbBytes1;
            comp1.EmitAndGetReferences(out dllBytes1, out pdbBytes1, out unused);
            var ref1 = AssemblyMetadata.CreateFromImage(dllBytes1).GetReference(display: "A");

            var comp2 = CreateCompilation(source2, new[] { MscorlibRef_v45, ref1 }, TestOptions.DebugDll, assemblyName: "B");
            byte[] dllBytes2;
            byte[] pdbBytes2;
            comp2.EmitAndGetReferences(out dllBytes2, out pdbBytes2, out unused);
            var ref2 = AssemblyMetadata.CreateFromImage(dllBytes2).GetReference(display: "B");

            var modulesBuilder = ArrayBuilder<ModuleInstance>.GetInstance();
            modulesBuilder.Add(ref1.ToModuleInstance(dllBytes1, SymReaderFactory.CreateReader(pdbBytes1)));
            modulesBuilder.Add(ref2.ToModuleInstance(dllBytes2, SymReaderFactory.CreateReader(pdbBytes2)));
            modulesBuilder.Add(MscorlibRef_v45.ToModuleInstance(fullImage: null, symReader: null));
            modulesBuilder.Add(ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.ToModuleInstance(fullImage: null, symReader: null));

            using (var runtime = new RuntimeInstance(modulesBuilder.ToImmutableAndFree()))
            {
                var context = CreateMethodContext(runtime, "C1.M");

                string error;
                var testData = new CompilationTestData();
                context.CompileExpression("typeof(SI)", out error, testData);
                Assert.Null(error);

                testData.GetMethodData("<>x.<>m0").VerifyIL(@"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldtoken    ""int""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  ret
}
");
            }
        }
    }

    internal static class ImportChainExtensions
    {
        internal static Imports Single(this ImportChain importChain)
        {
            return importChain.AsEnumerable().Single();
        }

        internal static IEnumerable<Imports> AsEnumerable(this ImportChain importChain)
        {
            for (var chain = importChain; chain != null; chain = chain.ParentOpt)
            {
                yield return chain.Imports;
            }
        }
    }
}
