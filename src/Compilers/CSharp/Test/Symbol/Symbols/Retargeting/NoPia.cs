// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Roslyn.Test.Utilities;
using static Roslyn.Test.Utilities.TestMetadata;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Retargeting
{
    public class NoPia : CSharpTestBase
    {
        /// <summary>
        /// Translation of Roslyn\Main\Open\Compilers\Test\Resources\Core\SymbolsTests\NoPia\Pia1.vb
        /// Disassembly of Roslyn\Main\Open\Compilers\Test\Resources\Core\SymbolsTests\NoPia\Pia1.dll
        /// </summary>
        private const string s_sourcePia1 =
@"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

[Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c01""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
public interface I1
{
	void Sub1(int x);
}

public struct S1
{
	public int F1;
}

namespace NS1
{
	[Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c02""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	public interface I2
	{
		void Sub1(int x);
	}

	public struct S2
	{
		public int F1;
	}
}
";

        /// <summary>
        /// Disassembly of Roslyn\Main\Open\Compilers\Test\Resources\Core\SymbolsTests\NoPia\LocalTypes1.dll
        /// </summary>
        private const string s_sourceLocalTypes1_IL =
@"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NS1;

[CompilerGenerated, Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c01""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
[ComImport]
public interface I1
{
}

public class LocalTypes1
{
	public void Test1(I1 x, I2 y)
	{
	}
}

namespace NS1
{
	[CompilerGenerated, Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c02""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
	[ComImport]
	public interface I2
	{
	}
}
";

        /// <summary>
        /// Translation of Roslyn\Main\Open\Compilers\Test\Resources\Core\SymbolsTests\NoPia\LocalTypes1.vb
        /// </summary>
        private const string s_sourceLocalTypes1 =
@"
using NS1;

public class LocalTypes1
{
	public void Test1(I1 x, I2 y)
	{
	}
}
";

        /// <summary>
        /// Disassembly of Roslyn\Main\Open\Compilers\Test\Resources\Core\SymbolsTests\NoPia\LocalTypes2.dll
        /// </summary>
        private const string s_sourceLocalTypes2_IL =
@"
using NS1;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class LocalTypes2
{
	public void Test2(S1 x, S2 y)
	{
	}
}

[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""S1"")]
public struct S1
{
	public int F1;
}

namespace NS1
{
	[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""NS1.S2"")]
	public struct S2
	{
		public int F1;
	}
}
";

        /// <summary>
        /// Translation of Roslyn\Main\Open\Compilers\Test\Resources\Core\SymbolsTests\NoPia\LocalTypes2.vb
        /// </summary>
        private const string s_sourceLocalTypes2 =
@"
using NS1;

public class LocalTypes2
{
	public void Test2(S1 x, S2 y)
	{
	}
}
";

        /// <summary>
        /// Disassembly of Roslyn\Main\Open\Compilers\Test\Resources\Core\SymbolsTests\NoPia\LocalTypes3.dll
        /// </summary>
        private const string s_sourceLocalTypes3_IL =
@"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class C31<T>
{
	public interface I31<S>
	{
	}
}

public class C32<T>
{
}

public class C33
{
}

[CompilerGenerated, Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c01""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), TypeIdentifier]
[ComImport]
public interface I1
{
}

public interface I32<S>
{
}

public class LocalTypes3
{
	public C31<C33>.I31<C33> Test1()
	{
		return null;
	}
	public C31<C33>.I31<I1> Test2()
	{
		return null;
	}
	public C31<I1>.I31<C33> Test3()
	{
		return null;
	}
	public C31<C33>.I31<I32<I1>> Test4()
	{
		return null;
	}
	public C31<I32<I1>>.I31<C33> Test5()
	{
		return null;
	}
	public List<I1> Test6()
	{
		return null;
	}
}
";

        /// <summary>
        /// Translation of Roslyn\Main\Open\Compilers\Test\Resources\Core\SymbolsTests\NoPia\LocalTypes3.vb
        /// </summary>
        private const string s_sourceLocalTypes3 =
@"
using System;
using System.Collections.Generic;

public class C31<T>
{
	public interface I31<S>
	{
	}
}

public class C32<T>
{
}

public class C33
{
}

public interface I32<S>
{
}

public class LocalTypes3
{
	public C31<C33>.I31<C33> Test1()
	{
		return null;
	}
	public C31<C33>.I31<I1> Test2()
	{
		return null;
	}
	public C31<I1>.I31<C33> Test3()
	{
		return null;
	}
	public C31<C33>.I31<I32<I1>> Test4()
	{
		return null;
	}
	public C31<I32<I1>>.I31<C33> Test5()
	{
		return null;
	}
	public List<I1> Test6()
	{
		return null;
	}
}
";

        [ClrOnlyFact]
        public void HideLocalTypeDefinitions()
        {
            var LocalTypes1 = CreateEmptyCompilation(s_sourceLocalTypes1_IL, new[] { MscorlibRef }, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes1");
            CompileAndVerify(LocalTypes1);

            var LocalTypes2 = CreateEmptyCompilation(s_sourceLocalTypes2_IL, new[] { MscorlibRef }, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2");
            CompileAndVerify(LocalTypes2);

            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                         null,
                                                                         new MetadataReference[] { MscorlibRef });

            var localTypes1 = assemblies[0].Modules[0];
            var localTypes2 = assemblies[1].Modules[0];

            Assert.Same(assemblies[2], LocalTypes1.Assembly.CorLibrary);
            Assert.Same(assemblies[2], LocalTypes2.Assembly.CorLibrary);

            Assert.Equal(2, localTypes1.GlobalNamespace.GetMembers().Length);
            Assert.Equal(2, localTypes1.GlobalNamespace.GetMembersUnordered().Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetMembers("I1").Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetMembers("S1").Length);
            Assert.Equal(1, localTypes1.GlobalNamespace.GetTypeMembers().Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetTypeMembers("I1").Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetTypeMembers("S1").Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetTypeMembers("I1", 0).Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetTypeMembers("S1", 0).Length);
            Assert.Equal(0, localTypes1.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().
                                        GetTypeMembers().Length);

            Assert.Equal(2, localTypes2.GlobalNamespace.GetMembers().Length);
            Assert.Equal(2, localTypes2.GlobalNamespace.GetMembersUnordered().Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetMembers("I1").Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetMembers("S1").Length);
            Assert.Equal(1, localTypes2.GlobalNamespace.GetTypeMembers().Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetTypeMembers("I1").Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetTypeMembers("S1").Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetTypeMembers("I1", 0).Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetTypeMembers("S1", 0).Length);
            Assert.Equal(0, localTypes2.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().
                                        GetTypeMembers().Length);

            var fullName_I1 = MetadataTypeName.FromFullName("I1");
            var fullName_I2 = MetadataTypeName.FromFullName("NS1.I2");
            var fullName_S1 = MetadataTypeName.FromFullName("S1");
            var fullName_S2 = MetadataTypeName.FromFullName("NS1.S2");

            Assert.Null(localTypes1.LookupTopLevelMetadataType(ref fullName_I1));
            Assert.Null(localTypes1.LookupTopLevelMetadataType(ref fullName_I2));
            Assert.Null(localTypes1.LookupTopLevelMetadataType(ref fullName_S1));
            Assert.Null(localTypes1.LookupTopLevelMetadataType(ref fullName_S2));

            Assert.Null(assemblies[0].GetTypeByMetadataName(fullName_I1.FullName));
            Assert.Null(assemblies[0].GetTypeByMetadataName(fullName_I2.FullName));
            Assert.Null(assemblies[0].GetTypeByMetadataName(fullName_S1.FullName));
            Assert.Null(assemblies[0].GetTypeByMetadataName(fullName_S2.FullName));

            Assert.Null(localTypes2.LookupTopLevelMetadataType(ref fullName_I1));
            Assert.Null(localTypes2.LookupTopLevelMetadataType(ref fullName_I2));
            Assert.Null(localTypes2.LookupTopLevelMetadataType(ref fullName_S1));
            Assert.Null(localTypes2.LookupTopLevelMetadataType(ref fullName_S2));

            Assert.Null(assemblies[1].GetTypeByMetadataName(fullName_I1.FullName));
            Assert.Null(assemblies[1].GetTypeByMetadataName(fullName_I2.FullName));
            Assert.Null(assemblies[1].GetTypeByMetadataName(fullName_S1.FullName));
            Assert.Null(assemblies[1].GetTypeByMetadataName(fullName_S2.FullName));
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void LocalTypeSubstitution1_1()
        {
            var LocalTypes1 = CreateCompilationWithMscorlib40(s_sourceLocalTypes1_IL, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes1");
            CompileAndVerify(LocalTypes1);

            var LocalTypes2 = CreateCompilationWithMscorlib40(s_sourceLocalTypes2_IL, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2");
            CompileAndVerify(LocalTypes2);

            var assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    }, null);

            var localTypes1_1 = assemblies1[0];
            var localTypes2_1 = assemblies1[1];
            var pia1_1 = assemblies1[2];

            var varI1 = pia1_1.GlobalNamespace.GetTypeMembers("I1").Single();
            var varS1 = pia1_1.GlobalNamespace.GetTypeMembers("S1").Single();
            var varNS1 = pia1_1.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single();
            var varI2 = varNS1.GetTypeMembers("I2").Single();
            var varS2 = varNS1.GetTypeMembers("S2").Single();

            NamedTypeSymbol classLocalTypes1;
            NamedTypeSymbol classLocalTypes2;

            classLocalTypes1 = localTypes1_1.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_1.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            MethodSymbol test1;
            MethodSymbol test2;

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            ImmutableArray<ParameterSymbol> param;

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef
                                                                                                    },
                                                                          null);

            var localTypes1_2 = assemblies2[0];
            var localTypes2_2 = assemblies2[1];

            Assert.NotSame(localTypes1_1, localTypes1_2);
            Assert.NotSame(localTypes2_1, localTypes2_2);
            Assert.Same(pia1_1, assemblies2[2]);

            classLocalTypes1 = localTypes1_2.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_2.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies3 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] { TestReferences.SymbolsTests.NoPia.Pia1 },
                                                                          null);

            var localTypes1_3 = assemblies3[0];
            var localTypes2_3 = assemblies3[1];
            var pia1_3 = assemblies3[2];

            Assert.NotSame(localTypes1_1, localTypes1_3);
            Assert.NotSame(localTypes2_1, localTypes2_3);
            Assert.NotSame(localTypes1_2, localTypes1_3);
            Assert.NotSame(localTypes2_2, localTypes2_3);
            Assert.NotSame(pia1_1, pia1_3);

            classLocalTypes1 = localTypes1_3.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_3.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(pia1_3.GlobalNamespace.GetTypeMembers("I1").Single(), param[0].Type);
            Assert.Same(pia1_3.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().GetTypeMembers("I2").Single(), param[1].Type);

            // This tests that we cannot find canonical type for an embedded structure if we don't know
            // whether it is a structure because we can't find definition of the base class. Mscorlib is
            // not referenced.
            param = test2.Parameters;

            NoPiaMissingCanonicalTypeSymbol missing;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes2_3, missing.EmbeddingAssembly);
            Assert.Null(missing.Guid);
            Assert.Equal(varS1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope);
            Assert.Equal(varS1.ToTestDisplayString(), missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies4 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    }, null);

            for (int i = 0; i < assemblies1.Length; i++)
            {
                Assert.Same(assemblies1[i], assemblies4[i]);
            }

            var assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia2,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_5 = assemblies5[0];
            var localTypes2_5 = assemblies5[1];

            classLocalTypes1 = localTypes1_5.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_5.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_5, missing.EmbeddingAssembly);
            Assert.Equal("27e3e649-994b-4f58-b3c6-f8089a5f2c01", missing.Guid);
            Assert.Equal(varI1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Null(missing.Scope);
            Assert.Null(missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia3,
                                                                                                        MscorlibRef
                                                                                                    }, null);
            var localTypes1_6 = assemblies6[0];
            var localTypes2_6 = assemblies6[1];

            classLocalTypes1 = localTypes1_6.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_6.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_7 = assemblies7[0];
            var localTypes2_7 = assemblies7[1];

            classLocalTypes1 = localTypes1_7.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_7.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(TypeKind.Interface, param[0].Type.TypeKind);
            Assert.Equal(TypeKind.Interface, param[1].Type.TypeKind);
            Assert.NotEqual(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, param[1].Type.Kind);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies8 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_8 = assemblies8[0];
            var localTypes2_8 = assemblies8[1];
            var pia4_8 = assemblies8[2];
            var pia1_8 = assemblies8[3];

            classLocalTypes1 = localTypes1_8.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_8.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            NoPiaAmbiguousCanonicalTypeSymbol ambiguous;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            ambiguous = (NoPiaAmbiguousCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_8, ambiguous.EmbeddingAssembly);
            Assert.Same(pia4_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.FirstCandidate);
            Assert.Same(pia1_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.SecondCandidate);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaAmbiguousCanonicalTypeSymbol>(param[1].Type);

            var assemblies9 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var library1_9 = assemblies9[0];
            var localTypes1_9 = assemblies9[1];

            var assemblies10 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                           null,
                                                                           new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    }, null);

            var library1_10 = assemblies10[0];
            var localTypes1_10 = assemblies10[1];

            Assert.NotSame(library1_9, library1_10);
            Assert.NotSame(localTypes1_9, localTypes1_10);

            GC.KeepAlive(localTypes1_1);
            GC.KeepAlive(localTypes2_1);
            GC.KeepAlive(pia1_1);
            GC.KeepAlive(localTypes1_9);
            GC.KeepAlive(library1_9);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void LocalTypeSubstitution1_2()
        {
            var LocalTypes1 = CreateCompilation(s_sourceLocalTypes1, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes1",
                                        targetFramework: TargetFramework.Standard,
                                        references: new[] { TestReferences.SymbolsTests.NoPia.Pia1.WithEmbedInteropTypes(true) });
            CompileAndVerify(LocalTypes1);

            var LocalTypes2 = CreateCompilation(s_sourceLocalTypes2, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2",
                                        targetFramework: TargetFramework.Standard,
                                        references: new[] { TestReferences.SymbolsTests.NoPia.Pia1.WithEmbedInteropTypes(true) });
            CompileAndVerify(LocalTypes2);

            var assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    });

            var localTypes1_1 = assemblies1[0];
            var localTypes2_1 = assemblies1[1];
            var pia1_1 = assemblies1[2];

            var varI1 = pia1_1.GlobalNamespace.GetTypeMembers("I1").Single();
            var varS1 = pia1_1.GlobalNamespace.GetTypeMembers("S1").Single();
            var varNS1 = pia1_1.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single();
            var varI2 = varNS1.GetTypeMembers("I2").Single();
            var varS2 = varNS1.GetTypeMembers("S2").Single();

            NamedTypeSymbol classLocalTypes1;
            NamedTypeSymbol classLocalTypes2;

            classLocalTypes1 = localTypes1_1.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_1.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            MethodSymbol test1;
            MethodSymbol test2;

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            ImmutableArray<ParameterSymbol> param;

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef
                                                                                                    },
                                                                          null);

            var localTypes1_2 = assemblies2[0];
            var localTypes2_2 = assemblies2[1];

            Assert.NotSame(localTypes1_1, localTypes1_2);
            Assert.NotSame(localTypes2_1, localTypes2_2);
            Assert.Same(pia1_1, assemblies2[2]);

            classLocalTypes1 = localTypes1_2.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_2.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies3 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] { TestReferences.SymbolsTests.NoPia.Pia1 },
                                                                          null);

            var localTypes1_3 = assemblies3[0];
            var localTypes2_3 = assemblies3[1];
            var pia1_3 = assemblies3[2];

            Assert.NotSame(localTypes1_1, localTypes1_3);
            Assert.NotSame(localTypes2_1, localTypes2_3);
            Assert.NotSame(localTypes1_2, localTypes1_3);
            Assert.NotSame(localTypes2_2, localTypes2_3);
            Assert.NotSame(pia1_1, pia1_3);

            classLocalTypes1 = localTypes1_3.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_3.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(pia1_3.GlobalNamespace.GetTypeMembers("I1").Single(), param[0].Type);
            Assert.Same(pia1_3.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().GetTypeMembers("I2").Single(), param[1].Type);

            // This tests that we cannot find canonical type for an embedded structure if we don't know
            // whether it is a structure because we can't find definition of the base class. Mscorlib is
            // not referenced.
            param = test2.Parameters;

            NoPiaMissingCanonicalTypeSymbol missing;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes2_3, missing.EmbeddingAssembly);
            Assert.Null(missing.Guid);
            Assert.Equal(varS1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope);
            Assert.Equal(varS1.ToTestDisplayString(), missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies4 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    }, null);

            for (int i = 0; i < assemblies1.Length; i++)
            {
                Assert.Same(assemblies1[i], assemblies4[i]);
            }

            var assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia2,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_5 = assemblies5[0];
            var localTypes2_5 = assemblies5[1];

            classLocalTypes1 = localTypes1_5.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_5.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_5, missing.EmbeddingAssembly);
            Assert.Equal("27e3e649-994b-4f58-b3c6-f8089a5f2c01", missing.Guid);
            Assert.Equal(varI1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Null(missing.Scope);
            Assert.Null(missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia3,
                                                                                                        MscorlibRef
                                                                                                    }, null);
            var localTypes1_6 = assemblies6[0];
            var localTypes2_6 = assemblies6[1];

            classLocalTypes1 = localTypes1_6.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_6.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_7 = assemblies7[0];
            var localTypes2_7 = assemblies7[1];

            classLocalTypes1 = localTypes1_7.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_7.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(TypeKind.Interface, param[0].Type.TypeKind);
            Assert.Equal(TypeKind.Interface, param[1].Type.TypeKind);
            Assert.NotEqual(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, param[1].Type.Kind);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies8 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_8 = assemblies8[0];
            var localTypes2_8 = assemblies8[1];
            var pia4_8 = assemblies8[2];
            var pia1_8 = assemblies8[3];

            classLocalTypes1 = localTypes1_8.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_8.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            NoPiaAmbiguousCanonicalTypeSymbol ambiguous;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            ambiguous = (NoPiaAmbiguousCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_8, ambiguous.EmbeddingAssembly);
            Assert.Same(pia4_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.FirstCandidate);
            Assert.Same(pia1_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.SecondCandidate);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaAmbiguousCanonicalTypeSymbol>(param[1].Type);

            var assemblies9 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var library1_9 = assemblies9[0];
            var localTypes1_9 = assemblies9[1];

            var assemblies10 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    }, null);

            var library1_10 = assemblies10[0];
            var localTypes1_10 = assemblies10[1];

            Assert.NotSame(library1_9, library1_10);
            Assert.NotSame(localTypes1_9, localTypes1_10);

            GC.KeepAlive(localTypes1_1);
            GC.KeepAlive(localTypes2_1);
            GC.KeepAlive(pia1_1);
            GC.KeepAlive(localTypes1_9);
            GC.KeepAlive(library1_9);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void LocalTypeSubstitution1_3()
        {
            var Pia1 = CreateCompilation(s_sourcePia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1", targetFramework: TargetFramework.Standard);
            CompileAndVerify(Pia1);

            var LocalTypes1 = CreateCompilation(s_sourceLocalTypes1, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes1",
                                        targetFramework: TargetFramework.Standard,
                                        references: new MetadataReference[] { new CSharpCompilationReference(Pia1, embedInteropTypes: true) });
            CompileAndVerify(LocalTypes1);

            var LocalTypes2 = CreateCompilation(s_sourceLocalTypes2, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2",
                                        targetFramework: TargetFramework.Standard,
                                        references: new MetadataReference[] { new CSharpCompilationReference(Pia1, embedInteropTypes: true) });
            CompileAndVerify(LocalTypes2);

            var assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    }, null);

            var localTypes1_1 = assemblies1[0];
            var localTypes2_1 = assemblies1[1];
            var pia1_1 = assemblies1[2];

            var varI1 = pia1_1.GlobalNamespace.GetTypeMembers("I1").Single();
            var varS1 = pia1_1.GlobalNamespace.GetTypeMembers("S1").Single();
            var varNS1 = pia1_1.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single();
            var varI2 = varNS1.GetTypeMembers("I2").Single();
            var varS2 = varNS1.GetTypeMembers("S2").Single();

            NamedTypeSymbol classLocalTypes1;
            NamedTypeSymbol classLocalTypes2;

            classLocalTypes1 = localTypes1_1.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_1.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            MethodSymbol test1;
            MethodSymbol test2;

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            ImmutableArray<ParameterSymbol> param;

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef
                                                                                                    },
                                                                          null);

            var localTypes1_2 = assemblies2[0];
            var localTypes2_2 = assemblies2[1];

            Assert.NotSame(localTypes1_1, localTypes1_2);
            Assert.NotSame(localTypes2_1, localTypes2_2);
            Assert.Same(pia1_1, assemblies2[2]);

            classLocalTypes1 = localTypes1_2.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_2.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(varI1, param[0].Type);
            Assert.Same(varI2, param[1].Type);

            param = test2.Parameters;

            Assert.Same(varS1, param[0].Type);
            Assert.Same(varS2, param[1].Type);

            var assemblies3 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] { TestReferences.SymbolsTests.NoPia.Pia1 },
                                                                          null);

            var localTypes1_3 = assemblies3[0];
            var localTypes2_3 = assemblies3[1];
            var pia1_3 = assemblies3[2];

            Assert.NotSame(localTypes1_1, localTypes1_3);
            Assert.NotSame(localTypes2_1, localTypes2_3);
            Assert.NotSame(localTypes1_2, localTypes1_3);
            Assert.NotSame(localTypes2_2, localTypes2_3);
            Assert.NotSame(pia1_1, pia1_3);

            classLocalTypes1 = localTypes1_3.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_3.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Same(pia1_3.GlobalNamespace.GetTypeMembers("I1").Single(), param[0].Type);
            Assert.Same(pia1_3.GlobalNamespace.GetMembers("NS1").OfType<NamespaceSymbol>().Single().GetTypeMembers("I2").Single(), param[1].Type);

            // This tests that we cannot find canonical type for an embedded structure if we don't know
            // whether it is a structure because we can't find definition of the base class. Mscorlib is
            // not referenced.
            param = test2.Parameters;

            NoPiaMissingCanonicalTypeSymbol missing;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes2_3, missing.EmbeddingAssembly);
            Assert.Null(missing.Guid);
            Assert.Equal(varS1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Equal("f9c2d51d-4f44-45f0-9eda-c9d599b58257", missing.Scope);
            Assert.Equal(varS1.ToTestDisplayString(), missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies4 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    }, null);

            for (int i = 0; i < assemblies1.Length; i++)
            {
                Assert.Same(assemblies1[i], assemblies4[i]);
            }

            var assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia2,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_5 = assemblies5[0];
            var localTypes2_5 = assemblies5[1];

            classLocalTypes1 = localTypes1_5.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_5.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            missing = (NoPiaMissingCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_5, missing.EmbeddingAssembly);
            Assert.Equal("27e3e649-994b-4f58-b3c6-f8089a5f2c01", missing.Guid);
            Assert.Equal(varI1.ToTestDisplayString(), missing.FullTypeName);
            Assert.Null(missing.Scope);
            Assert.Null(missing.Identifier);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia3,
                                                                                                        MscorlibRef
                                                                                                    }, null);
            var localTypes1_6 = assemblies6[0];
            var localTypes2_6 = assemblies6[1];

            classLocalTypes1 = localTypes1_6.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_6.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_7 = assemblies7[0];
            var localTypes2_7 = assemblies7[1];

            classLocalTypes1 = localTypes1_7.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_7.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            Assert.Equal(TypeKind.Interface, param[0].Type.TypeKind);
            Assert.Equal(TypeKind.Interface, param[1].Type.TypeKind);
            Assert.NotEqual(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, param[1].Type.Kind);

            param = test2.Parameters;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[0].Type);
            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaMissingCanonicalTypeSymbol>(param[1].Type);

            var assemblies8 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia1,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var localTypes1_8 = assemblies8[0];
            var localTypes2_8 = assemblies8[1];
            var pia4_8 = assemblies8[2];
            var pia1_8 = assemblies8[3];

            classLocalTypes1 = localTypes1_8.GlobalNamespace.GetTypeMembers("LocalTypes1").Single();
            classLocalTypes2 = localTypes2_8.GlobalNamespace.GetTypeMembers("LocalTypes2").Single();

            test1 = classLocalTypes1.GetMembers("Test1").OfType<MethodSymbol>().Single();
            test2 = classLocalTypes2.GetMembers("Test2").OfType<MethodSymbol>().Single();

            param = test1.Parameters;

            NoPiaAmbiguousCanonicalTypeSymbol ambiguous;

            Assert.Equal(SymbolKind.ErrorType, param[0].Type.Kind);
            ambiguous = (NoPiaAmbiguousCanonicalTypeSymbol)param[0].Type;
            Assert.Same(localTypes1_8, ambiguous.EmbeddingAssembly);
            Assert.Same(pia4_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.FirstCandidate);
            Assert.Same(pia1_8.GlobalNamespace.GetTypeMembers("I1").Single(), ambiguous.SecondCandidate);

            Assert.Equal(SymbolKind.ErrorType, param[1].Type.Kind);
            Assert.IsType<NoPiaAmbiguousCanonicalTypeSymbol>(param[1].Type);

            var assemblies9 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef
                                                                                                    }, null);

            var library1_9 = assemblies9[0];
            var localTypes1_9 = assemblies9[1];

            var assemblies10 = MetadataTestHelpers.GetSymbolsForReferences(new CSharpCompilation[] { LocalTypes1, LocalTypes2 },
                                                                          null,
                                                                          new MetadataReference[] {
                                                                                                        TestReferences.SymbolsTests.NoPia.Pia4,
                                                                                                        MscorlibRef,
                                                                                                        TestReferences.SymbolsTests.MDTestLib1
                                                                                                    }, null);

            var library1_10 = assemblies10[0];
            var localTypes1_10 = assemblies10[1];

            Assert.NotSame(library1_9, library1_10);
            Assert.NotSame(localTypes1_9, localTypes1_10);

            GC.KeepAlive(localTypes1_1);
            GC.KeepAlive(localTypes2_1);
            GC.KeepAlive(pia1_1);
            GC.KeepAlive(localTypes1_9);
            GC.KeepAlive(library1_9);
        }

        [ClrOnlyFact]
        public void CyclicReference_1()
        {
            var mscorlibRef = TestReferences.SymbolsTests.MDTestLib1;
            var cyclic2Ref = TestReferences.SymbolsTests.Cyclic.Cyclic2.dll;
            var piaRef = TestReferences.SymbolsTests.NoPia.Pia1;

            var LocalTypes1 = CreateCompilation(s_sourceLocalTypes1_IL, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes1");
            CompileAndVerify(LocalTypes1);

            var localTypes1Ref = new CSharpCompilationReference(LocalTypes1);

            var tc1 = CSharpCompilation.Create("Cyclic1", references: new MetadataReference[] { mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref });
            Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol

            var tc2 = CSharpCompilation.Create("Cyclic1", references: new MetadataReference[] { mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref });
            Assert.NotNull(tc2.Assembly); // force creation of SourceAssemblySymbol

            Assert.NotSame(tc1.GetReferencedAssemblySymbol(localTypes1Ref),
                            tc2.GetReferencedAssemblySymbol(localTypes1Ref));

            GC.KeepAlive(tc1);
            GC.KeepAlive(tc2);
        }

        [ClrOnlyFact]
        public void CyclicReference_2()
        {
            var mscorlibRef = TestReferences.SymbolsTests.MDTestLib1;
            var cyclic2Ref = TestReferences.SymbolsTests.Cyclic.Cyclic2.dll;
            var piaRef = TestReferences.SymbolsTests.NoPia.Pia1;

            var LocalTypes1 = CreateCompilation(s_sourceLocalTypes1, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes1",
                                        references: new[] { TestReferences.SymbolsTests.NoPia.Pia1.WithEmbedInteropTypes(true) });
            CompileAndVerify(LocalTypes1);

            var localTypes1Ref = new CSharpCompilationReference(LocalTypes1);

            var tc1 = CSharpCompilation.Create("Cyclic1", references: new MetadataReference[] { mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref });
            Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol

            var tc2 = CSharpCompilation.Create("Cyclic1", references: new MetadataReference[] { mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref });
            Assert.NotNull(tc2.Assembly); // force creation of SourceAssemblySymbol

            Assert.NotSame(tc1.GetReferencedAssemblySymbol(localTypes1Ref),
                            tc2.GetReferencedAssemblySymbol(localTypes1Ref));

            GC.KeepAlive(tc1);
            GC.KeepAlive(tc2);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void CyclicReference_3()
        {
            var mscorlibRef = TestReferences.SymbolsTests.MDTestLib1;
            var cyclic2Ref = TestReferences.SymbolsTests.Cyclic.Cyclic2.dll;

            var Pia1 = CreateCompilation(s_sourcePia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1");
            CompileAndVerify(Pia1);

            var piaRef = new CSharpCompilationReference(Pia1);

            var LocalTypes1 = CreateCompilation(s_sourceLocalTypes1, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes1",
                                        references: new MetadataReference[] { new CSharpCompilationReference(Pia1, embedInteropTypes: true) });
            CompileAndVerify(LocalTypes1);

            var localTypes1Ref = new CSharpCompilationReference(LocalTypes1);

            var tc1 = CSharpCompilation.Create("Cyclic1", references: new MetadataReference[] { mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref });
            Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol

            var tc2 = CSharpCompilation.Create("Cyclic1", references: new MetadataReference[] { mscorlibRef, cyclic2Ref, piaRef, localTypes1Ref });
            Assert.NotNull(tc2.Assembly); // force creation of SourceAssemblySymbol

            Assert.NotSame(tc1.GetReferencedAssemblySymbol(localTypes1Ref),
                            tc2.GetReferencedAssemblySymbol(localTypes1Ref));

            GC.KeepAlive(tc1);
            GC.KeepAlive(tc2);
        }

        [ClrOnlyFact]
        public void GenericsClosedOverLocalTypes1_1()
        {
            var LocalTypes3 = CreateCompilation(s_sourceLocalTypes3_IL, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes3");
            CompileAndVerify(LocalTypes3);

            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { LocalTypes3 },
                                null,
                                new MetadataReference[]
                                {
                                    TestReferences.SymbolsTests.NoPia.Pia1
                                }, null);

            var asmLocalTypes3 = assemblies[0];
            var localTypes3 = asmLocalTypes3.GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.Equal(SymbolKind.ErrorType, localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType.Kind);

            NoPiaIllegalGenericInstantiationSymbol illegal = (NoPiaIllegalGenericInstantiationSymbol)localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType;
            Assert.Equal("C31<I1>.I31<C33>", illegal.UnderlyingSymbol.ToTestDisplayString());

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { LocalTypes3 },
                                null,
                                new MetadataReference[]
                                {
                                    TestReferences.SymbolsTests.NoPia.Pia1,
                                    MscorlibRef
                                }, null);

            localTypes3 = assemblies[0].GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test6").OfType<MethodSymbol>().Single().ReturnType);
        }

        [ClrOnlyFact]
        public void ValueTupleWithMissingCanonicalType()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}

[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""S1"")]
public struct S1 { }

public class C
{
    public ValueTuple<S1, S1> Test1()
    {
        throw new Exception();
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseDll, assemblyName: "comp");
            comp.VerifyDiagnostics();
            CompileAndVerify(comp);

            var assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { comp },
                                null,
                                new MetadataReference[] { },
                                null);

            Assert.Equal(SymbolKind.ErrorType, assemblies1[0].GlobalNamespace.GetMember<MethodSymbol>("C.Test1").ReturnType.Kind);

            var assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { },
                                null,
                                new MetadataReference[] { comp.ToMetadataReference() },
                                null);

            Assert.Equal(SymbolKind.ErrorType, assemblies2[0].GlobalNamespace.GetMember<MethodSymbol>("C.Test1").ReturnType.Kind);
        }

        [ClrOnlyFact]
        public void EmbeddedValueTuple()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    [CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""ValueTuple"")]
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}

public class C
{
    public ValueTuple<int, int> Test1()
    {
        throw new Exception();
    }
}
";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseDll, assemblyName: "comp");
            comp.VerifyDiagnostics();

            var assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { comp },
                                null,
                                new MetadataReference[] { },
                                null);

            Assert.Equal(SymbolKind.ErrorType, assemblies1[0].GlobalNamespace.GetMember<MethodSymbol>("C.Test1").ReturnType.Kind);

            var assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { },
                                null,
                                new MetadataReference[] { comp.ToMetadataReference() },
                                null);

            Assert.Equal(SymbolKind.ErrorType, assemblies2[0].GlobalNamespace.GetMember<MethodSymbol>("C.Test1").ReturnType.Kind);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void CannotEmbedValueTuple()
        {
            string piaSource = @"
using System;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}
";

            var pia = CreateCompilationWithMscorlib46(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyDiagnostics();

            string source = @"
public class C
{
    public System.ValueTuple<string, string> TestValueTuple()
    {
        throw new System.Exception();
    }
    public (int, int) TestTuple()
    {
        throw new System.Exception();
    }
    public object TestTupleLiteral()
    {
        return (1, 2);
    }
    public void TestDeconstruction()
    {
        int x, y;
        var resultingTuple = ((x, y) = new C());
    }
    public void Deconstruct(out int a, out int b) { a = b = 1; }
}";

            var expectedDiagnostics = new[]
            {
                // (8,12): error CS1768: Type '(T1, T2)' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public (int, int) TestTuple()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "(int, int)").WithArguments("(T1, T2)").WithLocation(8, 12),
                // (4,19): error CS1768: Type '(T1, T2)' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public System.ValueTuple<string, string> TestValueTuple()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "ValueTuple<string, string>").WithArguments("(T1, T2)").WithLocation(4, 19),
                // (14,16): error CS1768: Type '(T1, T2)' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //         return (1, 2);
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "(1, 2)").WithArguments("(T1, T2)").WithLocation(14, 16),
                // (19,31): error CS1768: Type '(T1, T2)' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //         var resultingTuple = ((x, y) = new C());
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "(x, y)").WithArguments("(T1, T2)").WithLocation(19, 31)
            };

            var comp1 = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.ToMetadataReference(embedInteropTypes: true) });
            comp1.VerifyDiagnostics(expectedDiagnostics);

            var comp2 = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.EmitToImageReference(embedInteropTypes: true) });
            comp2.VerifyDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(13200, "https://github.com/dotnet/roslyn/issues/13200")]
        public void CannotEmbedValueTupleImplicitlyReferred_ByMethod()
        {
            string piaSource = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S<T> { }

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest1
{
    IEnumerable<IEnumerable<(int, int)>> M();
    IEnumerable<IEnumerable<S<int>>> M2();
}";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyEmitDiagnostics();

            string source = @"
public interface ITest2 : ITest1 { }
";

            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // error CS1768: Type '(T1, T2)' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("(T1, T2)").WithLocation(1, 1),
                // error CS1768: Type 'S<T>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("S<T>").WithLocation(1, 1)
            };

            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.ToMetadataReference(embedInteropTypes: true) });
            comp1.VerifyEmitDiagnostics(expectedDiagnostics);

            var comp2 = CreateCompilation(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.EmitToImageReference(embedInteropTypes: true) });
            comp2.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(13200, "https://github.com/dotnet/roslyn/issues/13200")]
        public void CannotEmbedValueTupleImplicitlyReferred_ByProperty()
        {
            string piaSource = @"
using System;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S<T> { }

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest1
{
    (int, int) P {get;}
    S<int> P2 {get;}
}";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyEmitDiagnostics();

            string source = @"
public interface ITest2 : ITest1 { }
";

            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // error CS1768: Type '(T1, T2)' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("(T1, T2)").WithLocation(1, 1),
                // error CS1768: Type 'S<T>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("S<T>").WithLocation(1, 1)
            };

            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.ToMetadataReference(embedInteropTypes: true) });
            comp1.VerifyEmitDiagnostics(expectedDiagnostics);

            var comp2 = CreateCompilation(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.EmitToImageReference(embedInteropTypes: true) });
            comp2.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(13200, "https://github.com/dotnet/roslyn/issues/13200")]
        public void CannotEmbedGenericDelegateReferred_ByEvent()
        {
            string piaSource = @"
using System;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public delegate void S<T> (T x);

[ComImport()]
[Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58280"")]
public interface ITest1
{
    event S<int> E;
}";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyEmitDiagnostics();

            string source = @"
public interface ITest2 : ITest1 { }
";

            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // error CS1768: Type 'S<T>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("S<T>").WithLocation(1, 1)
            };

            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.ToMetadataReference(embedInteropTypes: true) });
            comp1.VerifyEmitDiagnostics(expectedDiagnostics);

            var comp2 = CreateCompilation(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.EmitToImageReference(embedInteropTypes: true) });
            comp2.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(13200, "https://github.com/dotnet/roslyn/issues/13200")]
        public void CannotEmbedValueTupleImplicitlyReferred_ByField()
        {
            string piaSource = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S<T> { }

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}

public struct Test1
{
    public IEnumerable<IEnumerable<(int, int)>> F;
    public IEnumerable<IEnumerable<S<int>>> F2;
}";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyEmitDiagnostics();

            string source = @"
public interface ITest2
{
    void M(Test1 x);
}
";

            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // error CS1768: Type '(T1, T2)' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("(T1, T2)").WithLocation(1, 1),
                // error CS1768: Type 'S<T>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("S<T>").WithLocation(1, 1)
            };

            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.ToMetadataReference(embedInteropTypes: true) });
            comp1.VerifyEmitDiagnostics(expectedDiagnostics);

            var comp2 = CreateCompilation(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.EmitToImageReference(embedInteropTypes: true) });
            comp2.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        [WorkItem(13200, "https://github.com/dotnet/roslyn/issues/13200")]
        public void CannotEmbedValueTupleImplicitlyReferredFromMetadata()
        {
            string piaSource = @"
using System;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S<T> { }

namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}";

            var libSource = @"
public class D
{
    public static (int, int) M() { throw new System.Exception(); }
    public static S<int> M2() { throw new System.Exception(); }
}";

            var pia = CreateCompilationWithMscorlib46(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyDiagnostics();

            var lib = CreateCompilationWithMscorlib46(libSource, options: TestOptions.ReleaseDll, references: new[] { pia.ToMetadataReference() });
            lib.VerifyEmitDiagnostics();

            string source = @"
public class C
{
    public void TestTupleFromMetadata()
    {
        D.M();
        D.M2();
    }
    public void TestTupleAssignmentFromMetadata()
    {
        var t = D.M();
        t.ToString();
        var t2 = D.M2();
        t2.ToString();
    }
}";

            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // error CS1768: Type '(T1, T2)' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("(T1, T2)").WithLocation(1, 1),
                // error CS1768: Type 'S<T>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType).WithArguments("S<T>").WithLocation(1, 1)
            };

            var comp1 = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.ToMetadataReference(embedInteropTypes: true), lib.ToMetadataReference() });
            comp1.VerifyEmitDiagnostics(expectedDiagnostics);

            var comp2 = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { pia.EmitToImageReference(embedInteropTypes: true), lib.EmitToImageReference() });
            comp2.VerifyEmitDiagnostics(expectedDiagnostics);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void CheckForUnembeddableTypesInTuples()
        {
            string piaSource = @"
using System;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct Generic<T1> { }
";

            var pia = CreateCompilationWithMscorlib46(piaSource, options: TestOptions.ReleaseDll, assemblyName: "pia");
            pia.VerifyDiagnostics();

            string source = @"
public class C
{
    public System.ValueTuple<Generic<string>, Generic<string>> Test1()
    {
        throw new System.Exception();
    }
    public (Generic<int>, Generic<int>) Test2()
    {
        throw new System.Exception();
    }
}
namespace System
{
    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 item1, T2 item2) => throw null;
    }
}";

            var comp1 = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { new CSharpCompilationReference(pia).WithEmbedInteropTypes(true) });
            comp1.VerifyDiagnostics(
                // (8,13): error CS1768: Type 'Generic<T1>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public (Generic<int>, Generic<int>) Test2()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Generic<int>").WithArguments("Generic<T1>").WithLocation(8, 13),
                // (8,27): error CS1768: Type 'Generic<T1>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public (Generic<int>, Generic<int>) Test2()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Generic<int>").WithArguments("Generic<T1>").WithLocation(8, 27),
                // (4,30): error CS1768: Type 'Generic<T1>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public System.ValueTuple<Generic<string>, Generic<string>> Test1()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Generic<string>").WithArguments("Generic<T1>").WithLocation(4, 30),
                // (4,47): error CS1768: Type 'Generic<T1>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public System.ValueTuple<Generic<string>, Generic<string>> Test1()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Generic<string>").WithArguments("Generic<T1>").WithLocation(4, 47)
                );

            var comp2 = CreateCompilationWithMscorlib46(source, options: TestOptions.ReleaseDll,
                            references: new MetadataReference[] { MetadataReference.CreateFromImage(pia.EmitToArray()).WithEmbedInteropTypes(true) });
            comp2.VerifyDiagnostics(
                // (8,13): error CS1768: Type 'Generic<T1>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public (Generic<int>, Generic<int>) Test2()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Generic<int>").WithArguments("Generic<T1>").WithLocation(8, 13),
                // (8,27): error CS1768: Type 'Generic<T1>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public (Generic<int>, Generic<int>) Test2()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Generic<int>").WithArguments("Generic<T1>").WithLocation(8, 27),
                // (4,30): error CS1768: Type 'Generic<T1>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public System.ValueTuple<Generic<string>, Generic<string>> Test1()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Generic<string>").WithArguments("Generic<T1>").WithLocation(4, 30),
                // (4,47): error CS1768: Type 'Generic<T1>' cannot be embedded because it has a generic argument. Consider setting the 'Embed Interop Types' property to false.
                //     public System.ValueTuple<Generic<string>, Generic<string>> Test1()
                Diagnostic(ErrorCode.ERR_GenericsUsedInNoPIAType, "Generic<string>").WithArguments("Generic<T1>").WithLocation(4, 47)
                );
        }

        [ClrOnlyFact]
        public void GenericsClosedOverLocalTypes1_2()
        {
            var LocalTypes3 = CreateCompilation(s_sourceLocalTypes3, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes3",
                                        references: new[] { TestReferences.SymbolsTests.NoPia.Pia1.WithEmbedInteropTypes(true) });
            CompileAndVerify(LocalTypes3);

            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { LocalTypes3 },
                                null,
                                new MetadataReference[]
                                {
                                    TestReferences.SymbolsTests.NoPia.Pia1
                                }, null);

            var asmLocalTypes3 = assemblies[0];
            var localTypes3 = asmLocalTypes3.GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.Equal(SymbolKind.ErrorType, localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType.Kind);

            NoPiaIllegalGenericInstantiationSymbol illegal = (NoPiaIllegalGenericInstantiationSymbol)localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType;
            Assert.Equal("C31<I1>.I31<C33>", illegal.UnderlyingSymbol.ToTestDisplayString());

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { LocalTypes3 },
                                null,
                                new MetadataReference[]
                                {
                                    TestReferences.SymbolsTests.NoPia.Pia1,
                                    MscorlibRef
                                }, null);

            localTypes3 = assemblies[0].GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test6").OfType<MethodSymbol>().Single().ReturnType);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void GenericsClosedOverLocalTypes1_3()
        {
            var Pia1 = CreateCompilation(s_sourcePia1, options: TestOptions.ReleaseDll, assemblyName: "Pia1");
            CompileAndVerify(Pia1);

            var LocalTypes3 = CreateCompilation(s_sourceLocalTypes3, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes3",
                                        references: new MetadataReference[] { new CSharpCompilationReference(Pia1, embedInteropTypes: true) });
            CompileAndVerify(LocalTypes3);

            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { LocalTypes3 },
                                null,
                                new MetadataReference[]
                                {
                                    TestReferences.SymbolsTests.NoPia.Pia1
                                }, null);

            var asmLocalTypes3 = assemblies[0];
            var localTypes3 = asmLocalTypes3.GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.Equal(SymbolKind.ErrorType, localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType.Kind);

            NoPiaIllegalGenericInstantiationSymbol illegal = (NoPiaIllegalGenericInstantiationSymbol)localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType;
            Assert.Equal("C31<I1>.I31<C33>", illegal.UnderlyingSymbol.ToTestDisplayString());

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);

            assemblies = MetadataTestHelpers.GetSymbolsForReferences(
                                new CSharpCompilation[] { LocalTypes3 },
                                null,
                                new MetadataReference[]
                                {
                                    TestReferences.SymbolsTests.NoPia.Pia1,
                                    MscorlibRef
                                }, null);

            localTypes3 = assemblies[0].GlobalNamespace.GetTypeMembers("LocalTypes3").Single();

            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test1").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test2").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test3").OfType<MethodSymbol>().Single().ReturnType);
            Assert.NotEqual(SymbolKind.ErrorType, localTypes3.GetMembers("Test4").OfType<MethodSymbol>().Single().ReturnType.Kind);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test5").OfType<MethodSymbol>().Single().ReturnType);
            Assert.IsType<NoPiaIllegalGenericInstantiationSymbol>(localTypes3.GetMembers("Test6").OfType<MethodSymbol>().Single().ReturnType);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void NestedType1()
        {
            string source =
@"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class LocalTypes2
{
	public void Test2(S1 x, S1.S2 y)
	{
	}
}

[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""S1"")]
public struct S1
{
	public int F1;

	[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""S1.S2"")]
	public struct S2
	{
		public int F1;
	}
}

[ComEventInterface(typeof(S1), typeof(S1.S2))]
interface AttrTest1
{
}
";

            var localTypes2 = CreateCompilation(source, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2");
            CompileAndVerify(localTypes2);

            var localTypes2Image = MetadataReference.CreateFromImage(localTypes2.EmitToArray());

            string piaSource =
@"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S1
{
	public int F1;

	public struct S2
	{
		public int F1;
	}
}
";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "Pia");
            CompileAndVerify(pia);

            var piaImage = MetadataReference.CreateFromImage(pia.EmitToArray());

            var compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                            references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                                 new CSharpCompilationReference(pia)});

            NamedTypeSymbol lt = compilation.GetTypeByMetadataName("LocalTypes2");
            var test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            NamedTypeSymbol attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            var args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {localTypes2Image,
                                                                                             new CSharpCompilationReference(pia)});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {localTypes2Image,
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        public void NestedType2()
        {
            string source =
@"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class LocalTypes2
{
	public void Test2(S1 x, S1.S2 y)
	{
	}
}

[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""S1"")]
public struct S1
{
	public int F1;

	public struct S2
	{
		public int F1;
	}
}

[ComEventInterface(typeof(S1), typeof(S1.S2))]
interface AttrTest1
{
}
";

            var localTypes2 = CreateCompilation(source, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2");
            CompileAndVerify(localTypes2);

            var localTypes2Image = MetadataReference.CreateFromImage(localTypes2.EmitToArray());

            string piaSource =
@"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S1
{
	public int F1;

	public struct S2
	{
		public int F1;
	}
}
";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "Pia");
            CompileAndVerify(pia);

            var piaImage = MetadataReference.CreateFromImage(pia.EmitToArray());

            var compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                            references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                                 new CSharpCompilationReference(pia)});

            NamedTypeSymbol lt = compilation.GetTypeByMetadataName("LocalTypes2");
            var test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            NamedTypeSymbol attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            var args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {localTypes2Image,
                                                                                             new CSharpCompilationReference(pia)});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {localTypes2Image,
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void NestedType3()
        {
            string source =
@"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class LocalTypes2
{
	public void Test2(S1 x, S1.S2 y)
	{
	}
}

public struct S1
{
	public int F1;

	[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""S1.S2"")]
	public struct S2
	{
		public int F1;
	}
}

[ComEventInterface(typeof(S1), typeof(S1.S2))]
interface AttrTest1
{
}
";

            var localTypes2 = CreateCompilation(source, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2");
            //CompileAndVerify(localTypes2);

            var localTypes2Image = MetadataReference.CreateFromImage(localTypes2.EmitToArray());

            string piaSource =
@"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S1
{
	public int F1;

	public struct S2
	{
		public int F1;
	}
}
";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "Pia");
            CompileAndVerify(pia);

            var piaImage = MetadataReference.CreateFromImage(pia.EmitToArray());

            var compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                            references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                                 new CSharpCompilationReference(pia)});

            NamedTypeSymbol lt = compilation.GetTypeByMetadataName("LocalTypes2");
            var test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("LocalTypes2", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.Equal("LocalTypes2", test2.Parameters[1].Type.ContainingAssembly.Name);

            NamedTypeSymbol attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            var args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("LocalTypes2", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.Equal("LocalTypes2", ((TypeSymbol)args[1].ValueInternal).ContainingAssembly.Name);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {localTypes2Image,
                                                                                             new CSharpCompilationReference(pia)});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("LocalTypes2", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.Equal("LocalTypes2", test2.Parameters[1].Type.ContainingAssembly.Name);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("LocalTypes2", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.Equal("LocalTypes2", ((TypeSymbol)args[1].ValueInternal).ContainingAssembly.Name);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("LocalTypes2", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.Equal("LocalTypes2", test2.Parameters[1].Type.ContainingAssembly.Name);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("LocalTypes2", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.Equal("LocalTypes2", ((TypeSymbol)args[1].ValueInternal).ContainingAssembly.Name);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {localTypes2Image,
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("LocalTypes2", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.Equal("LocalTypes2", test2.Parameters[1].Type.ContainingAssembly.Name);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("LocalTypes2", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.Equal("LocalTypes2", ((TypeSymbol)args[1].ValueInternal).ContainingAssembly.Name);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void NestedType4()
        {
            string piaSource =
@"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S1
{
	public int F1;

	public struct S2
	{
		public int F1;
	}
}
";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "Pia");
            CompileAndVerify(pia);

            string source =
@"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class LocalTypes2
{
	public void Test2(S1 x, S1.S2 y)
	{
	}
}

[ComEventInterface(typeof(S1), typeof(S1.S2))]
interface AttrTest1
{
}
";

            var localTypes2 = CreateCompilation(source, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2",
                                                            references: new MetadataReference[] { new CSharpCompilationReference(pia, embedInteropTypes: true) });

            var piaImage = MetadataReference.CreateFromImage(pia.EmitToArray());

            var compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                            references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                                 new CSharpCompilationReference(pia)});

            NamedTypeSymbol lt = compilation.GetTypeByMetadataName("LocalTypes2");
            var test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            NamedTypeSymbol attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            var args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);
        }

        [ConditionalFact(typeof(ClrOnly), typeof(DesktopOnly))]
        public void GenericType1()
        {
            string source =
@"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class LocalTypes2
{
	public void Test2(S1 x, S2<int> y)
	{
	}
}

[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""S1"")]
public struct S1
{
	public int F1;
}

[CompilerGenerated, TypeIdentifier(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"", ""S2`1"")]
public struct S2<T>
{
	public int F1;
}

[ComEventInterface(typeof(S1), typeof(S2<>))]
interface AttrTest1
{
}
";

            var localTypes2 = CreateCompilation(source, options: TestOptions.ReleaseDll, assemblyName: "LocalTypes2");
            //CompileAndVerify(localTypes2);

            var localTypes2Image = MetadataReference.CreateFromImage(localTypes2.EmitToArray());

            string piaSource =
@"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

public struct S1
{
	public int F1;
}

public struct S2<T>
{
	public int F1;
}
";

            var pia = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "Pia");
            CompileAndVerify(pia);

            var piaImage = MetadataReference.CreateFromImage(pia.EmitToArray());

            var compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                            references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                                 new CSharpCompilationReference(pia)});

            NamedTypeSymbol lt = compilation.GetTypeByMetadataName("LocalTypes2");
            var test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            NamedTypeSymbol attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            var args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {localTypes2Image,
                                                                                             new CSharpCompilationReference(pia)});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {new CSharpCompilationReference(localTypes2),
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);

            compilation = CreateCompilation("", options: TestOptions.ReleaseDll,
                                                        references: new MetadataReference[] {localTypes2Image,
                                                                                             piaImage});

            lt = compilation.GetTypeByMetadataName("LocalTypes2");
            test2 = lt.GetMember<MethodSymbol>("Test2");

            Assert.Equal("Pia", test2.Parameters[0].Type.ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(test2.Parameters[1].Type);

            attrTest1 = compilation.GetTypeByMetadataName("AttrTest1");
            args = attrTest1.GetAttributes()[0].CommonConstructorArguments;
            Assert.Equal("Pia", ((TypeSymbol)args[0].ValueInternal).ContainingAssembly.Name);
            Assert.IsType<UnsupportedMetadataTypeSymbol>(args[1].ValueInternal);
        }

        [ConditionalFact(typeof(DesktopOnly), typeof(ClrOnly))]
        [WorkItem(685240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/685240")]
        public void Bug685240()
        {
            string piaSource = @"
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]
[assembly: ImportedFromTypeLib(""Pia1.dll"")]

[Guid(""27e3e649-994b-4f58-b3c6-f8089a5f2c01""), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[ComImport]
public interface I1
{
	void Sub1(int x);
}
";

            var pia1 = CreateCompilation(piaSource, options: TestOptions.ReleaseDll, assemblyName: "Pia1");
            CompileAndVerify(pia1);

            string moduleSource = @"
public class Test
{
	public static I1 M1()
    {
        return null;
    }
}
";

            var module1 = CreateCompilation(moduleSource, options: TestOptions.ReleaseModule, assemblyName: "Module1",
                references: new[] { new CSharpCompilationReference(pia1, embedInteropTypes: true) });

            var multiModule = CreateCompilation("", options: TestOptions.ReleaseDll,
                references: new[] { module1.EmitToImageReference() });

            CompileAndVerify(multiModule);

            string consumerSource = @"
public class Consumer
{
	public static void M2()
    {
        var x = Test.M1();
    }
}
";

            var consumer = CreateCompilation(consumerSource, options: TestOptions.ReleaseDll,
                references: new[] { new CSharpCompilationReference(multiModule),
                                    new CSharpCompilationReference(pia1)});

            CompileAndVerify(consumer);
        }

        [Fact]
        [WorkItem(24964, "https://github.com/dotnet/roslyn/issues/24964")]
        public void UnificationAcrossDistinctCoreLibs()
        {
            string pia = @"
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: ImportedFromTypeLib(""GeneralPIA.dll"")]
[assembly: Guid(""f9c2d51d-4f44-45f0-9eda-c9d599b58257"")]

public struct Test
{
}
";

            var piaCompilation = CreateEmptyCompilation(pia, references: [Net40.mscorlib], options: TestOptions.DebugDll, assemblyName: "Pia");

            string consumer1 = @"
public class UsePia1
{
    public static Test M1()
    {
        return default;
    }
}
";

            string consumer2 = @"
class UsePia2
{
    public static void Main()
    {
        UsePia1.M1();
    }
}
";

            foreach (MetadataReference piaRef in new[] { piaCompilation.EmitToImageReference(), piaCompilation.ToMetadataReference() })
            {
                var compilation1 = CreateEmptyCompilation(consumer1, references: [Net40.mscorlib, piaRef.WithEmbedInteropTypes(true)]);

                foreach (MetadataReference consumer1Ref in new[] { compilation1.EmitToImageReference(), compilation1.ToMetadataReference() })
                {
                    var compilation2 = CreateEmptyCompilation(consumer2, references: [Net461.References.mscorlib, piaRef, consumer1Ref]);

                    compilation2.VerifyDiagnostics();

                    Assert.NotSame(compilation1.SourceAssembly.CorLibrary, compilation2.SourceAssembly.CorLibrary);

                    var test = compilation2.GetTypeByMetadataName("Test");
                    Assert.Equal("Pia.dll", test.ContainingModule.Name);

                    var usePia1 = compilation2.GetTypeByMetadataName("UsePia1");
                    Assert.Same(test, usePia1.GetMember<MethodSymbol>("M1").ReturnType);
                }
            }
        }
    }
}
