// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.DiaSymReader;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    using System;
    using Debugging;
    using static MethodDebugInfoValidation;

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
            var comp = CreateCompilation(source);
            WithRuntimeInstance(comp, runtime =>
            {
                GetMethodDebugInfo(runtime, "C.M").ImportRecordGroups.Verify(@"
                {
                    Namespace: string='System'
                }");
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21386")]
        public void Gaps()
        {
            var source = @"
using System;

namespace N1
{
  namespace N2 
  {
    using System.Collections;

    namespace N3 
    {
      class C { void M() { } }
    }
  }
}
";
            var comp = CreateCompilation(source);
            WithRuntimeInstance(comp, runtime =>
            {
                GetMethodDebugInfo(runtime, "N1.N2.N3.C.M").ImportRecordGroups.Verify(@"
                {
                }
                {
                    Namespace: string='System.Collections'
                }
                {
                }
                {
                    Namespace: string='System'
                }");
            });
        }

        [Fact]
        public void NestedScopes()
        {
            var source = @"
using System;

class C
{
    void M()
    {
        int i = 1;
        {
            int j = 2;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            CompileAndVerify(comp).VerifyIL("C.M", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (int V_0, //i
                int V_1) //j
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  nop
  IL_0004:  ldc.i4.2
  IL_0005:  stloc.1
  IL_0006:  nop
  IL_0007:  ret
}
");

            WithRuntimeInstance(comp, runtime =>
            {
                GetMethodDebugInfo(runtime, "C.M", ilOffset: 0x0004).ImportRecordGroups.Verify(@"
                {
                    Namespace: string='System'
                }");
            });
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
            var comp = CreateCompilation(source);
            WithRuntimeInstance(comp, runtime =>
            {
                GetMethodDebugInfo(runtime, "A.C.M").ImportRecordGroups.Verify(@"
                {
                    Namespace: string='System.IO'
                    Namespace: string='System.Text'
                }
                {
                    Namespace: string='System'
                }");
            });
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
            var comp = CreateCompilation(source);
            WithRuntimeInstance(comp, runtime =>
            {
                GetMethodDebugInfo(runtime, "A.C.M1").ImportRecordGroups.Verify(@"
                {
                    Namespace: string='System.IO'
                    Namespace: string='System.Text'
                }
                {
                    Namespace: string='System'
                }");

                GetMethodDebugInfo(runtime, "A.C.M2").ImportRecordGroups.Verify(@"
                {
                    Namespace: string='System.IO'
                    Namespace: string='System.Text'
                }
                {
                    Namespace: string='System'
                }");
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30030")]
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
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var aliasedRef = CreateEmptyCompilation("", assemblyName: "Lib", parseOptions: parseOptions).EmitToImageReference(aliases: ImmutableArray.Create("A"));
            var comp = CreateCompilation(source, new[] { aliasedRef }, parseOptions: parseOptions);
            WithRuntimeInstance(comp, runtime =>
            {
                var info = GetMethodDebugInfo(runtime, "B.C.M");

                info.ImportRecordGroups.Verify(@"
                {
                    Namespace: string='System.Text'
                    Type: alias='F' type='System.IO.File'
                }
                {
                    Assembly: alias='A'
                    Namespace: alias='S' string='System'
                }");

                info.ExternAliasRecords.Verify(
                    "A = 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'");
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084059")]
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
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var aliasedRef = CreateCompilation(libSource, assemblyName: "Lib", parseOptions: parseOptions).EmitToImageReference(aliases: ImmutableArray.Create("A"));
            var comp = CreateCompilation(source, new[] { aliasedRef }, parseOptions: parseOptions);

            WithRuntimeInstance(comp, runtime =>
            {
                var info = GetMethodDebugInfo(runtime, "B.C.M");

                info.ImportRecordGroups.Verify(@"
                {
                    Type: type='N.Static'
                }
                {
                    Assembly: alias='A'
                    Type: type='System.Math'
                }");

                info.ExternAliasRecords.Verify(
                    "A = 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'");
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30030")]
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
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var aliasedRef = CreateEmptyCompilation("", assemblyName: "Lib", parseOptions: parseOptions).EmitToImageReference(aliases: ImmutableArray.Create("A"));
            var comp = CreateCompilation(source, new[] { aliasedRef }, parseOptions: parseOptions);

            WithRuntimeInstance(comp, runtime =>
            {
                var debugInfo1 = GetMethodDebugInfo(runtime, "B.C.M1");

                debugInfo1.ImportRecordGroups.Verify(@"
                {
                    Namespace: string='System'
                }
                {
                    Assembly: alias='A'
                }");

                debugInfo1.ExternAliasRecords.Verify(
                    "A = 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'");

                var debugInfo2 = GetMethodDebugInfo(runtime, "D.E.M2");

                debugInfo2.ImportRecordGroups.Verify(@"
                {
                    Namespace: string='System.Text'
                }
                {
                    Assembly: alias='A'
                }");

                debugInfo2.ExternAliasRecords.Verify(
                    "A = 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'");
            });
        }

        #endregion

        #region Invalid PDBs

        [Fact]
        public void BadPdb_ForwardChain()
        {
            const int methodToken1 = 0x600057a; // Forwards to 2
            const int methodToken2 = 0x600055d; // Forwards to 3
            const int methodToken3 = 0x6000540; // Has a using
            const string importString = "USystem";

            var getMethodCustomDebugInfo = new Func<int, int, byte[]>((token, _) =>
            {
                switch (token)
                {
                    case methodToken1: return new MethodDebugInfoBytes.Builder().AddForward(methodToken2).Build().Bytes.ToArray();
                    case methodToken2: return new MethodDebugInfoBytes.Builder().AddForward(methodToken3).Build().Bytes.ToArray();
                    case methodToken3: return new MethodDebugInfoBytes.Builder([new[] { importString }]).Build().Bytes.ToArray();
                    default: throw null;
                }
            });

            var getMethodImportStrings = new Func<int, int, ImmutableArray<string>>((token, _) =>
            {
                switch (token)
                {
                    case methodToken3: return ImmutableArray.Create(importString);
                    default: throw null;
                }
            });

            ImmutableArray<string> externAliasStrings;
            var importStrings = CustomDebugInfoReader.GetCSharpGroupedImportStrings(methodToken1, 0, getMethodCustomDebugInfo, getMethodImportStrings, out externAliasStrings);
            Assert.True(importStrings.IsDefault);
            Assert.True(externAliasStrings.IsDefault);

            importStrings = CustomDebugInfoReader.GetCSharpGroupedImportStrings(methodToken2, 0, getMethodCustomDebugInfo, getMethodImportStrings, out externAliasStrings);
            Assert.Equal(importString, importStrings.Single().Single());
            Assert.Empty(externAliasStrings);

            importStrings = CustomDebugInfoReader.GetCSharpGroupedImportStrings(methodToken2, 0, getMethodCustomDebugInfo, getMethodImportStrings, out externAliasStrings);
            Assert.Equal(importString, importStrings.Single().Single());
            Assert.Empty(externAliasStrings);
        }

        [Fact]
        public void BadPdb_Cycle()
        {
            const int methodToken1 = 0x600057a; // Forwards to itself

            var getMethodCustomDebugInfo = new Func<int, int, byte[]>((token, _) =>
            {
                switch (token)
                {
                    case methodToken1: return new MethodDebugInfoBytes.Builder().AddForward(methodToken1).Build().Bytes.ToArray();
                    default: throw null;
                }
            });

            var getMethodImportStrings = new Func<int, int, ImmutableArray<string>>((token, _) =>
            {
                return ImmutableArray<string>.Empty;
            });

            ImmutableArray<string> externAliasStrings;
            var importStrings = CustomDebugInfoReader.GetCSharpGroupedImportStrings(methodToken1, 0, getMethodCustomDebugInfo, getMethodImportStrings, out externAliasStrings);
            Assert.True(importStrings.IsDefault);
            Assert.True(externAliasStrings.IsDefault);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/999086")]
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
            var comp = CreateCompilation(source);
            var peImage = comp.EmitToArray();

            var symReader = ExpressionCompilerTestHelpers.ConstructSymReaderWithImports(
                peImage,
                "Main",
                "USystem", // Valid.
                "UACultureInfo TSystem.Globalization.CultureInfo, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", // Invalid - skipped.
                "ASI USystem.IO"); // Valid.

            var module = ModuleInstance.Create(peImage, symReader);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var evalContext = CreateMethodContext(runtime, "C.Main");
            var compContext = evalContext.CreateCompilationContext(); // Used to throw.
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal("System", imports.Usings.Single().NamespaceOrType.ToTestDisplayString());
            Assert.Equal("SI", imports.UsingAliases.Keys.Single());
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/999086")]
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
            var comp = CreateCompilation(source);
            var peImage = comp.EmitToArray();

            var symReader = ExpressionCompilerTestHelpers.ConstructSymReaderWithImports(
                peImage,
                "Main",
                "USystem", // Valid.
                "AMy.Alias TSystem.Globalization.CultureInfo, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", // Invalid - skipped.
                "ASI USystem.IO"); // Valid.

            var module = ModuleInstance.Create(peImage, symReader);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var evalContext = CreateMethodContext(runtime, "C.Main");
            var compContext = evalContext.CreateCompilationContext(); // Used to throw.
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal("System", imports.Usings.Single().NamespaceOrType.ToTestDisplayString());
            Assert.Equal("SI", imports.UsingAliases.Keys.Single());
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1007917")]
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
            var comp = CreateCompilation(source);
            var peImage = comp.EmitToArray();

            ISymUnmanagedReader symReader;
            using (var peReader = new PEReader(peImage))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = metadataReader.MethodDefinitions.Single(h => metadataReader.StringComparer.Equals(metadataReader.GetMethodDefinition(h).Name, "Main"));
                var methodToken = metadataReader.GetToken(methodHandle);

                symReader = new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
                {
                    { methodToken, new MethodDebugInfoBytes.Builder([new[] { "USystem", "USystem.IO" }], suppressUsingInfo: true).AddUsingInfo(1, 1).Build() },
                }.ToImmutableDictionary());
            }

            var module = ModuleInstance.Create(peImage, symReader);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var evalContext = CreateMethodContext(runtime, "C.Main");
            var compContext = evalContext.CreateCompilationContext();
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal("System.IO", imports.Usings.Single().NamespaceOrType.ToTestDisplayString()); // Note: some information is preserved.
            Assert.Equal(0, imports.UsingAliases.Count);
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1007917")]
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
            var comp = CreateCompilation(source);
            var peImage = comp.EmitToArray();

            ISymUnmanagedReader symReader;
            using (var peReader = new PEReader(peImage))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = metadataReader.MethodDefinitions.Single(h => metadataReader.StringComparer.Equals(metadataReader.GetMethodDefinition(h).Name, "Main"));
                var methodToken = metadataReader.GetToken(methodHandle);

                symReader = new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
                {
                    { methodToken, new MethodDebugInfoBytes.Builder([new[] { "USystem" }], suppressUsingInfo: true).AddUsingInfo(1).Build() },
                }.ToImmutableDictionary());
            }

            var module = ModuleInstance.Create(peImage, symReader);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var evalContext = CreateMethodContext(runtime, "N.C.Main");
            var compContext = evalContext.CreateCompilationContext();
            var imports = compContext.NamespaceBinder.ImportChain.Single();
            Assert.Equal("System", imports.Usings.Single().NamespaceOrType.ToTestDisplayString()); // Note: some information is preserved.
            Assert.Equal(0, imports.UsingAliases.Count);
            Assert.Equal(0, imports.ExternAliases.Length);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084059")]
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
            var comp = CreateCompilation(source);
            var peImage = comp.EmitToArray();

            ISymUnmanagedReader symReader;
            using (var peReader = new PEReader(peImage))
            {
                var metadataReader = peReader.GetMetadataReader();
                var methodHandle = metadataReader.MethodDefinitions.Single(h => metadataReader.StringComparer.Equals(metadataReader.GetMethodDefinition(h).Name, "Main"));
                var methodToken = metadataReader.GetToken(methodHandle);

                symReader = new MockSymUnmanagedReader(new Dictionary<int, MethodDebugInfoBytes>
                {
                    { methodToken, new MethodDebugInfoBytes.Builder([new[] { "TSystem.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" }], suppressUsingInfo: true).AddUsingInfo(1).Build() },
                }.ToImmutableDictionary());
            }

            var module = ModuleInstance.Create(peImage, symReader);
            var runtime = CreateRuntimeInstance(module, new[] { MscorlibRef });
            var evalContext = CreateMethodContext(runtime, "N.C.Main");
            var compContext = evalContext.CreateCompilationContext();
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

                var imports = importsList.Single();

                Assert.Equal(0, imports.UsingAliases.Count);
                Assert.Equal(0, imports.ExternAliases.Length);

                var actualNamespace = imports.Usings.Single().NamespaceOrType;
                Assert.Equal(SymbolKind.Namespace, actualNamespace.Kind);
                Assert.Equal(NamespaceKind.Module, ((NamespaceSymbol)actualNamespace).Extent.Kind);
                Assert.Equal("System", actualNamespace.ToTestDisplayString());
            });
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

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
            });
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "A.C.M").AsEnumerable().ToArray();
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
            });
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

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
            });
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084059")]
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
            var comp = CreateCompilation(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

                var imports = importsList.Single();

                Assert.Equal(0, imports.UsingAliases.Count);
                Assert.Equal(0, imports.ExternAliases.Length);

                var actualType = imports.Usings.Single().NamespaceOrType;
                Assert.Equal(SymbolKind.NamedType, actualType.Kind);
                Assert.Equal("System.Math", actualType.ToTestDisplayString());
            });
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
            var comp = CreateCompilation(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

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
            });
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
            var comp = CreateCompilation(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

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
            });
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
            var comp = CreateCompilation(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

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
            });
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
            var comp = CreateCompilation(source, new[] { SystemXmlLinqRef.WithAliases(ImmutableArray.Create("X")) });
            comp.VerifyDiagnostics();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

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
            });
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
            var comp = CreateCompilation(source, new[] { SystemXmlLinqRef.WithAliases(ImmutableArray.Create("X")) });
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

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
            });
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
            var comp = CreateCompilation(source, new[] { SystemXmlLinqRef.WithAliases(ImmutableArray.Create("global", "X")) });
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

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
            });
        }

        [Fact]
        public void ImportsForUsingsToTypes()
        {
            var source = @"
using A = int;
using B = (int x, int y);

class C
{
    int M()
    {
        A.Parse(""0"");
        return 1;
    }
}
";
            var comp = CreateCompilation(source);
            comp.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            WithRuntimeInstance(comp, runtime =>
            {
                var importsList = GetImports(runtime, "C.M");

                var imports = importsList.Single();

                Assert.Equal(0, imports.Usings.Length);

                var usingAliases = imports.UsingAliases;
                Assert.Equal(2, usingAliases.Count);
                AssertEx.SetEqual(usingAliases.Keys, "A", "B");

                var aliasA = usingAliases["A"].Alias;
                Assert.Equal("A", aliasA.Name);
                Assert.Equal("System.Int32", aliasA.Target.ToTestDisplayString());

                var aliasB = usingAliases["B"].Alias;
                Assert.Equal("B", aliasB.Name);
                Assert.NotEqual(aliasA.Target, aliasB.Target);
            });
        }

        private static ImportChain GetImports(RuntimeInstance runtime, string methodName)
        {
            var evalContext = CreateMethodContext(runtime, methodName);
            var compContext = evalContext.CreateCompilationContext();
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
            Assert.Equal("error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)", error);

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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2441")]
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
            var comp1 = CreateEmptyCompilation(source1, new[] { MscorlibRef_v20 }, TestOptions.DebugDll);
            var module1 = comp1.ToModuleInstance();

            var comp2 = CreateEmptyCompilation(source2, new[] { MscorlibRef_v4_0_30316_17626, module1.GetReference() }, TestOptions.DebugDll);
            var module2 = comp2.ToModuleInstance();

            var runtime = CreateRuntimeInstance(new[]
            {
                module1,
                module2,
                MscorlibRef_v4_0_30316_17626.ToModuleInstance(),
                ExpressionCompilerTestHelpers.IntrinsicAssemblyReference.ToModuleInstance()
            });

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
