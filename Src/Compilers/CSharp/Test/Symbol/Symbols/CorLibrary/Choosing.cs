// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Metadata;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using CSReferenceManager = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.ReferenceManager;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.CorLibrary
{
    public class Choosing : CSharpTestBase
    {

        [Fact]
        public void MultipleMscorlibReferencesInMetadata()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                                        TestReferences.SymbolsTests.CorLibrary.GuidTest2.exe,
                                        TestReferences.NetFx.v4_0_21006.mscorlib
                                    });

            Assert.Same(assemblies[1], ((PEModuleSymbol)assemblies[0].Modules[0]).CorLibrary()); 
        }

        [Fact]
        public void NoExplicitCorLibraryReference()
        {
            var noMsCorLibRefPath = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.CorLibrary.NoMsCorLibRef).Path;
            var corlib2Path = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var corlib3Path = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;

            using (var @lock = MetadataCache.LockAndClean())
            {
                var assemblies1 = MetadataTestHelpers.GetSymbolsForReferences(new[] { new MetadataFileReference(noMsCorLibRefPath) });

                var noMsCorLibRef_1 = assemblies1[0];

                Assert.Equal(0, noMsCorLibRef_1.Modules[0].GetReferencedAssemblies().Length);
                Assert.Equal("<Missing Core Assembly>", noMsCorLibRef_1.CorLibrary.Name);
                Assert.Equal(TypeKind.Error,
                    noMsCorLibRef_1.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType<MethodSymbol>().Single().
                    Parameters[0].Type.TypeKind);

                var assemblies2 = MetadataTestHelpers.GetSymbolsForReferences(new MetadataReference[]
                                    {
                                        new MetadataFileReference(noMsCorLibRefPath),
                                        new MetadataFileReference(corlib2Path)
                                    });

                var noMsCorLibRef_2 = assemblies2[0];
                var msCorLib_2 = assemblies2[1];

                Assert.NotSame(noMsCorLibRef_1, noMsCorLibRef_2);
                Assert.Same(msCorLib_2, msCorLib_2.CorLibrary);
                Assert.Same(msCorLib_2, noMsCorLibRef_2.CorLibrary);
                Assert.Same(msCorLib_2.GetSpecialType(SpecialType.System_Int32),
                    noMsCorLibRef_2.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType<MethodSymbol>().Single().
                    Parameters[0].Type);

                var assemblies3 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                    new MetadataFileReference(noMsCorLibRefPath),
                    new MetadataFileReference(corlib3Path)
                });

                var noMsCorLibRef_3 = assemblies3[0];
                var msCorLib_3 = assemblies3[1];

                Assert.NotSame(noMsCorLibRef_1, noMsCorLibRef_3);
                Assert.NotSame(noMsCorLibRef_2, noMsCorLibRef_3);
                Assert.NotSame(msCorLib_2, msCorLib_3);
                Assert.Same(msCorLib_3, msCorLib_3.CorLibrary);
                Assert.Same(msCorLib_3, noMsCorLibRef_3.CorLibrary);
                Assert.Same(msCorLib_3.GetSpecialType(SpecialType.System_Int32),
                    noMsCorLibRef_3.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType<MethodSymbol>().Single().
                    Parameters[0].Type);

                var assemblies4 = MetadataTestHelpers.GetSymbolsForReferences(new[] { new MetadataFileReference(noMsCorLibRefPath) });

                for (int i = 0; i < assemblies1.Length; i++)
                {
                    Assert.Same(assemblies1[i], assemblies4[i]);
                }

                var assemblies5 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                    new MetadataFileReference(noMsCorLibRefPath),
                    new MetadataFileReference(corlib2Path)
                });

                for (int i = 0; i < assemblies2.Length; i++)
                {
                    Assert.Same(assemblies2[i], assemblies5[i]);
                }

                var assemblies6 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                    new MetadataFileReference(noMsCorLibRefPath),
                    new MetadataFileReference(corlib3Path)
                });

                for (int i = 0; i < assemblies3.Length; i++)
                {
                    Assert.Same(assemblies3[i], assemblies6[i]);
                }

                GC.KeepAlive(assemblies1);
                GC.KeepAlive(assemblies2);
                GC.KeepAlive(assemblies3);

                @lock.CleanCaches();

                var assemblies7 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                                    {
                    new MetadataFileReference(noMsCorLibRefPath),
                    new MetadataFileReference(corlib2Path)
                });

                var noMsCorLibRef_7 = assemblies7[0];
                var msCorLib_7 = assemblies7[1];

                Assert.NotSame(noMsCorLibRef_1, noMsCorLibRef_7);
                Assert.NotSame(noMsCorLibRef_2, noMsCorLibRef_7);
                Assert.NotSame(noMsCorLibRef_3, noMsCorLibRef_7);
                Assert.NotSame(msCorLib_2, msCorLib_7);
                Assert.NotSame(msCorLib_3, msCorLib_7);
                Assert.Same(msCorLib_7, msCorLib_7.CorLibrary);
                Assert.Same(msCorLib_7, noMsCorLibRef_7.CorLibrary);
                Assert.Same(msCorLib_7.GetSpecialType(SpecialType.System_Int32),
                    noMsCorLibRef_7.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType<MethodSymbol>().Single().
                    Parameters[0].Type);

                var assemblies8 = MetadataTestHelpers.GetSymbolsForReferences(new[] { new MetadataFileReference(noMsCorLibRefPath) });

                var noMsCorLibRef_8 = assemblies8[0];

                Assert.NotSame(noMsCorLibRef_1, noMsCorLibRef_8);
                Assert.NotSame(noMsCorLibRef_2, noMsCorLibRef_8);
                Assert.NotSame(noMsCorLibRef_3, noMsCorLibRef_8);
                Assert.NotSame(noMsCorLibRef_7, noMsCorLibRef_8);
                Assert.Equal("<Missing Core Assembly>", noMsCorLibRef_8.CorLibrary.Name);
                Assert.Equal(TypeKind.Error,
                    noMsCorLibRef_8.GlobalNamespace.GetTypeMembers("I1").Single().
                    GetMembers("M1").OfType<MethodSymbol>().Single().
                    Parameters[0].Type.TypeKind);

                GC.KeepAlive(assemblies7);
            }
        }

        [Fact, WorkItem(760148)]
        public void Bug760148_1()
        {
            var corLib = CreateCompilation(@"
namespace System
{
    public class Object
    {
    }
}
",compOptions: TestOptions.Dll);

            var obj = corLib.GetSpecialType(SpecialType.System_Object);

            Assert.False(obj.IsErrorType());
            Assert.Same(corLib.Assembly, obj.ContainingAssembly);

            var consumer = CreateCompilation(@"
public class Test
{
}
", new[] { new CSharpCompilationReference(corLib)}, compOptions: TestOptions.Dll);

            Assert.Same(obj, consumer.GetSpecialType(SpecialType.System_Object));
        }

        [Fact, WorkItem(760148)]
        public void Bug760148_2()
        {
            var corLib = CreateCompilation(@"
namespace System
{
    class Object
    {
    }
}
", compOptions: TestOptions.Dll);

            var consumer = CreateCompilation(@"
public class Test
{
}
", new[] { new CSharpCompilationReference(corLib) }, compOptions: TestOptions.Dll);

            Assert.True(consumer.GetSpecialType(SpecialType.System_Object).IsErrorType());
        }
    }
}
