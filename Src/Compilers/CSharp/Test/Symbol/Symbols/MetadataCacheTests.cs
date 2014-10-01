// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CS0618 // MetadataCache to be removed

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CSReferenceManager = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.ReferenceManager;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MetadataCacheTests : CSharpTestBase
    {
        #region Helpers

        public override void Dispose()
        {
            // invoke finalizers of all remaining streams and memory mapps before we attempt to delete temp files
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            base.Dispose();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void AssertNonNullTarget<T>(WeakReference<T> wr)
            where T : class
        {
            Assert.NotNull(wr);
            T target = wr.GetTarget();
            Assert.NotNull(target);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private CSharpCompilation CreateCompilation(string name, params object[] dependencies)
        {
            var result = CSharpCompilation.Create(name, references: MetadataCacheTestHelpers.CreateMetadataReferences(dependencies));
            var asm = result.Assembly;
            GC.KeepAlive(asm);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private ObjectReference CreateWeakCompilation(string name, params object[] dependencies)
        {
            // this compilation should only be reachable via the WeakReference we return
            var result = CSharpCompilation.Create(name, references: MetadataCacheTestHelpers.CreateMetadataReferences(dependencies));

            var asm = result.Assembly;
            GC.KeepAlive(asm);
            return new ObjectReference(result);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ObjectReference GetWeakModuleFromCache(FileKey fileKey)
        {
            MetadataCache.CachedModule netModule1;
            lock (MetadataCache.Guard)
            {
                netModule1 = MetadataCache.ModulesFromFiles[fileKey];

                bool found = false;

                foreach (var key in MetadataCache.ModuleKeys)
                {
                    if (fileKey.Equals(key))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found);
            }

            var module = netModule1.Metadata.GetTarget().Module;
            Assert.NotNull(module);
            return new ObjectReference(module);
        }

        private ObjectReference GetWeakAssemblyFromCache(FileKey fileKey)
        {
            MetadataCache.CachedAssembly mdTestLib1;

            lock (MetadataCache.Guard)
            {
                mdTestLib1 = MetadataCache.AssembliesFromFiles[fileKey];

                bool found = false;

                foreach (var key in MetadataCache.AssemblyKeys)
                {
                    if (fileKey.Equals(key))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found);
            }

            Assert.Equal(2, mdTestLib1.CachedSymbols.Count());

            object assembly = mdTestLib1.Metadata.GetTarget().GetAssembly();
            Assert.NotNull(assembly);
            return new ObjectReference(assembly);
        }

        #endregion

        [Fact]
        public void CompilationWithEmptyInput()
        {
            using (MetadataCache.LockAndClean())
            {
                var c1 = CSharpCompilation.Create("Test");

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                Assert.NotNull(c1);
                Assert.NotNull(c1.Assembly);
                Assert.Equal(0, c1.RetargetingAssemblySymbols.WeakCount);

                Assert.Equal(0, c1.ExternalReferences.Length);
                Assert.IsType(typeof(SourceAssemblySymbol), c1.Assembly);

                var a1 = (SourceAssemblySymbol)c1.Assembly;
                Assert.Equal("Test", a1.Name, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(1, a1.Modules.Length);
                Assert.IsType(typeof(SourceModuleSymbol), a1.Modules[0]);

                var m1 = (SourceModuleSymbol)a1.Modules[0];
                Assert.Same(c1.SourceModule, m1);
                Assert.Same(m1.ContainingAssembly, c1.Assembly);
                Assert.Same(m1.ContainingSymbol, c1.Assembly);
                Assert.Equal(null, m1.ContainingType);

                Assert.Equal(0, m1.GetReferencedAssemblies().Length);
                Assert.Equal(0, m1.GetReferencedAssemblySymbols().Length);
                Assert.Same(m1.CorLibrary(), a1);
            }
        }

        [Fact]
        public void CompilationWithMscorlibReference()
        {
            var mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;

            var mscorlibRef = new MetadataFileReference(mscorlibPath);

            Assert.True(mscorlibRef.Properties.Aliases.IsDefault);
            Assert.Equal(false, mscorlibRef.Properties.EmbedInteropTypes);
            Assert.Equal(mscorlibPath, mscorlibRef.FilePath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(MetadataImageKind.Assembly, mscorlibRef.Properties.Kind);

            using (MetadataCache.LockAndClean())
            {
                var c1 = CSharpCompilation.Create("Test", references: new[] { mscorlibRef });

                Assert.NotNull(c1.Assembly); // force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                var cachedAssembly = MetadataCache.AssembliesFromFiles.Values.Single();

                Assert.Equal(mscorlibPath, MetadataCache.AssembliesFromFiles.Keys.Single().FullPath, StringComparer.OrdinalIgnoreCase);

                var assembly = cachedAssembly.Metadata.GetTarget().GetAssembly();
                Assert.NotNull(assembly);

                Assert.Equal("mscorlib", assembly.Identity.Name);
                Assert.Equal(0, assembly.AssemblyReferences.Length);
                Assert.Equal(1, assembly.ModuleReferenceCounts.Length);
                Assert.Equal(0, assembly.ModuleReferenceCounts[0]);
                Assert.Equal(1, cachedAssembly.CachedSymbols.Count());

                var mscorlibAsm = (PEAssemblySymbol)cachedAssembly.CachedSymbols.First();

                Assert.NotNull(mscorlibAsm);
                Assert.Same(mscorlibAsm.Assembly, cachedAssembly.Metadata.GetTarget().GetAssembly());
                Assert.Equal("mscorlib", mscorlibAsm.Identity.Name);
                Assert.Equal("mscorlib", mscorlibAsm.Name);
                Assert.Equal(1, mscorlibAsm.Modules.Length);
                Assert.Same(mscorlibAsm.Modules[0], mscorlibAsm.Locations.Single().MetadataModule);
                Assert.Same(mscorlibAsm.Modules[0], mscorlibAsm.Modules[0].Locations.Single().MetadataModule);

                var mscorlibMod = (PEModuleSymbol)mscorlibAsm.Modules[0];

                Assert.Same(mscorlibMod.Module, mscorlibAsm.Assembly.Modules[0]);
                Assert.Same(mscorlibMod.ContainingAssembly, mscorlibAsm);
                Assert.Same(mscorlibMod.ContainingSymbol, mscorlibAsm);
                Assert.Equal(null, mscorlibMod.ContainingType);

                Assert.Equal(0, mscorlibMod.GetReferencedAssemblies().Length);
                Assert.Equal(0, mscorlibMod.GetReferencedAssemblySymbols().Length);
                Assert.Same(mscorlibMod.CorLibrary(), mscorlibAsm);
                Assert.Equal("CommonLanguageRuntimeLibrary", mscorlibMod.Name);
                Assert.Equal("CommonLanguageRuntimeLibrary", mscorlibMod.ToTestDisplayString());

                Assert.NotNull(c1);
                Assert.NotNull(c1.Assembly);
                Assert.Equal(0, c1.RetargetingAssemblySymbols.WeakCount);

                Assert.Equal(1, c1.ExternalReferences.Length);
                Assert.Same(c1.ExternalReferences[0], mscorlibRef);
                Assert.Same(c1.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm);

                Assert.IsType(typeof(SourceAssemblySymbol), c1.Assembly);

                var a1 = (SourceAssemblySymbol)c1.Assembly;
                Assert.Equal("Test", a1.Name, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(1, a1.Modules.Length);
                Assert.IsType(typeof(SourceModuleSymbol), a1.Modules[0]);

                var m1 = (SourceModuleSymbol)a1.Modules[0];
                Assert.Same(c1.SourceModule, m1);
                Assert.Same(m1.ContainingAssembly, c1.Assembly);
                Assert.Same(m1.ContainingSymbol, c1.Assembly);
                Assert.Equal(null, m1.ContainingType);

                Assert.Equal(1, m1.GetReferencedAssemblies().Length);
                Assert.Equal(1, m1.GetReferencedAssemblySymbols().Length);
                Assert.Same(m1.GetReferencedAssemblySymbols()[0], mscorlibAsm);
                Assert.Same(m1.CorLibrary(), mscorlibAsm);

                var c2 = CSharpCompilation.Create("Test2", references: new MetadataReference[] { new MetadataFileReference(mscorlibPath) });

                Assert.NotNull(c2.Assembly); // force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                cachedAssembly = MetadataCache.AssembliesFromFiles.Values.Single();
                Assert.Equal(1, cachedAssembly.CachedSymbols.Count());
                Assert.Same(c2.GetReferencedAssemblySymbol(c2.ExternalReferences[0]), mscorlibAsm);

                GC.KeepAlive(c1);
                GC.KeepAlive(c2);
            }
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

        [Fact]
        public void ReferenceAnotherCompilation()
        {
            using (MetadataCache.LockAndClean())
            {
                var tc1 = CSharpCompilation.Create("Test1");
                Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol

                var varC1Ref = new CSharpCompilationReference(tc1);

                Assert.True(varC1Ref.Properties.Aliases.IsDefault);
                Assert.Equal(false, varC1Ref.Properties.EmbedInteropTypes);
                Assert.Same(varC1Ref.Compilation, tc1);
                Assert.Equal(MetadataImageKind.Assembly, varC1Ref.Properties.Kind);
                Assert.Same(tc1.Assembly, tc1.Assembly.CorLibrary);

                var tc2 = CSharpCompilation.Create("Test2", references: new[] { varC1Ref });
                Assert.NotNull(tc2.Assembly); // force creation of SourceAssemblySymbol

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                Assert.NotNull(tc2.Assembly);
                Assert.Equal(1, tc1.RetargetingAssemblySymbols.WeakCount);
                Assert.Equal(0, tc2.RetargetingAssemblySymbols.WeakCount);

                Assert.Equal(1, tc2.ExternalReferences.Length);
                Assert.Same(tc2.ExternalReferences[0], varC1Ref);
                Assert.NotSame(tc2.GetReferencedAssemblySymbol(varC1Ref), tc1.Assembly);
                Assert.Same(((RetargetingAssemblySymbol)(tc2.GetReferencedAssemblySymbol(varC1Ref))).UnderlyingAssembly, tc1.Assembly);
                Assert.IsType(typeof(SourceAssemblySymbol), tc2.Assembly);

                var a1 = (SourceAssemblySymbol)tc2.Assembly;
                Assert.Equal("Test2", a1.Name);
                Assert.Equal(1, a1.Modules.Length);
                Assert.IsType(typeof(SourceModuleSymbol), a1.Modules[0]);

                var m1 = (SourceModuleSymbol)a1.Modules[0];
                Assert.Same(tc2.SourceModule, m1);
                Assert.Same(m1.ContainingAssembly, tc2.Assembly);
                Assert.Same(m1.ContainingSymbol, tc2.Assembly);
                Assert.Equal(null, m1.ContainingType);

                Assert.Equal(1, m1.GetReferencedAssemblies().Length);
                Assert.Equal("Test1", m1.GetReferencedAssemblies()[0].Name);
                Assert.Equal(1, m1.GetReferencedAssemblySymbols().Length);
                Assert.NotSame(m1.GetReferencedAssemblySymbols()[0], tc1.Assembly);
                Assert.Same(((RetargetingAssemblySymbol)(m1.GetReferencedAssemblySymbols()[0])).UnderlyingAssembly, tc1.Assembly);
                Assert.NotSame(m1.CorLibrary(), tc1.Assembly);

                var mscorlibRef = new MetadataFileReference(Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path);

                var tc3 = CSharpCompilation.Create("Test3", references: new[] { mscorlibRef });

                Assert.NotNull(tc3.Assembly); // force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                var mscorlibAsm = ((SourceModuleSymbol)tc3.Assembly.Modules[0]).CorLibrary();

                var varC3Ref = new CSharpCompilationReference(tc3);

                var tc4 = CSharpCompilation.Create("Test4", references: new MetadataReference[] { mscorlibRef, varC3Ref });
                Assert.NotNull(tc4.Assembly); // force creation of SourceAssemblySymbol

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Count());

                Assert.Equal(2, tc4.ExternalReferences.Length);
                Assert.Same(tc4.ExternalReferences[0], mscorlibRef);
                Assert.Same(tc4.ExternalReferences[1], varC3Ref);
                Assert.Same(tc4.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm);
                Assert.Same(tc4.GetReferencedAssemblySymbol(varC3Ref), tc3.Assembly);

                var varA4 = (SourceAssemblySymbol)tc4.Assembly;
                Assert.Equal(1, varA4.Modules.Length);

                var m4 = (SourceModuleSymbol)varA4.Modules[0];

                Assert.Equal(2, m4.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m4.GetReferencedAssemblies()[0].Name);
                Assert.Equal("Test3", m4.GetReferencedAssemblies()[1].Name);
                Assert.Equal(2, m4.GetReferencedAssemblySymbols().Length);
                Assert.Same(m4.GetReferencedAssemblySymbols()[0], mscorlibAsm);
                Assert.Same(m4.GetReferencedAssemblySymbols()[1], tc3.Assembly);
                Assert.Same(m4.CorLibrary(), mscorlibAsm);

                var tc5 = CSharpCompilation.Create("Test5", references: new MetadataReference[] { varC3Ref, mscorlibRef });
                Assert.NotNull(tc5.Assembly); // force creation of SourceAssemblySymbol

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Count());

                Assert.Equal(2, tc5.ExternalReferences.Length);
                Assert.Same(tc5.ExternalReferences[1], mscorlibRef);
                Assert.Same(tc5.ExternalReferences[0], varC3Ref);
                Assert.Same(tc5.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm);
                Assert.Same(tc5.GetReferencedAssemblySymbol(varC3Ref), tc3.Assembly);

                var a5 = (SourceAssemblySymbol)tc5.Assembly;
                Assert.Equal(1, a5.Modules.Length);

                var m5 = (SourceModuleSymbol)a5.Modules[0];

                Assert.Equal(2, m5.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m5.GetReferencedAssemblies()[1].Name);
                Assert.Equal("Test3", m5.GetReferencedAssemblies()[0].Name);
                Assert.Equal(2, m5.GetReferencedAssemblySymbols().Length);
                Assert.Same(m5.GetReferencedAssemblySymbols()[1], mscorlibAsm);
                Assert.Same(m5.GetReferencedAssemblySymbols()[0], tc3.Assembly);
                Assert.Same(m5.CorLibrary(), mscorlibAsm);

                GC.KeepAlive(tc1);
                GC.KeepAlive(tc2);
                GC.KeepAlive(tc3);
                GC.KeepAlive(tc4);
                GC.KeepAlive(tc5);
            }
        }

        [Fact]
        public void ReferenceModule()
        {
            using (MetadataCache.LockAndClean())
            {
                var mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;
                var mscorlibRef = new MetadataFileReference(mscorlibPath);

                var module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path;
                var module1Ref = new MetadataFileReference(module1Path, MetadataImageKind.Module);
                Assert.True(module1Ref.Properties.Aliases.IsDefault);
                Assert.Equal(false, module1Ref.Properties.EmbedInteropTypes);
                Assert.Equal(MetadataImageKind.Module, module1Ref.Properties.Kind);

                var tc1 = CSharpCompilation.Create("Test1", references: new MetadataReference[] { module1Ref, mscorlibRef });

                Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);

                var mscorlibAsm = (PEAssemblySymbol)MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Single();

                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count);
                Assert.Equal(module1Path, MetadataCache.ModulesFromFiles.Keys.Single().FullPath);
                var cachedModule1 = MetadataCache.ModulesFromFiles.Values.Single();

                var module1 = cachedModule1.Metadata.GetTarget().Module;
                Assert.NotNull(module1);

                Assert.Equal(1, module1.ReferencedAssemblies.Length);
                Assert.Equal("mscorlib", module1.ReferencedAssemblies[0].Name);

                Assert.Equal(2, tc1.ExternalReferences.Length);
                Assert.Same(tc1.ExternalReferences[1], mscorlibRef);
                Assert.Same(tc1.ExternalReferences[0], module1Ref);
                Assert.Same(tc1.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm);
                Assert.Same(tc1.GetReferencedModuleSymbol(module1Ref), tc1.Assembly.Modules[1]);

                Assert.Equal(2, tc1.Assembly.Modules.Length);

                var m11 = (SourceModuleSymbol)tc1.Assembly.Modules[0];

                Assert.Equal(1, m11.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m11.GetReferencedAssemblies()[0].Name);
                Assert.Equal(1, m11.GetReferencedAssemblySymbols().Length);
                Assert.Same(m11.GetReferencedAssemblySymbols()[0], mscorlibAsm);
                Assert.Same(m11.CorLibrary(), mscorlibAsm);

                var m12 = (PEModuleSymbol)tc1.Assembly.Modules[1];

                Assert.Same(m12.Module, cachedModule1.Metadata.GetTarget().Module);
                Assert.Same(m12.ContainingAssembly, tc1.Assembly);
                Assert.Same(m12.ContainingSymbol, tc1.Assembly);
                Assert.Equal(null, m12.ContainingType);

                Assert.Equal(1, m12.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m12.GetReferencedAssemblies()[0].Name);
                Assert.Equal(1, m12.GetReferencedAssemblySymbols().Length);
                Assert.Same(m12.GetReferencedAssemblySymbols()[0], mscorlibAsm);
                Assert.Same(m12.CorLibrary(), mscorlibAsm);
                Assert.Equal("netModule1.netmodule", m12.Name);
                Assert.Equal("netModule1.netmodule", m12.ToTestDisplayString());

                Assert.Same(m12, m12.Locations.Single().MetadataModule);

                var module2Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule2).Path;
                var module2Ref = new MetadataFileReference(module2Path, MetadataImageKind.Module);

                var varMTTestLib1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path;
                var varMTTestLib1Ref = new MetadataFileReference(varMTTestLib1Path);

                var varTc1Ref = new CSharpCompilationReference(tc1);

                var tc2 = CSharpCompilation.Create("Test2", references: new MetadataReference[] { varTc1Ref, module2Ref, varMTTestLib1Ref, mscorlibRef, module1Ref });

                Assert.NotNull(tc2.Assembly); // force creation of SourceAssemblySymbol
                Assert.Equal(2, MetadataCache.AssembliesFromFiles.Count);

                var mscorlibInfo = MetadataCache.AssembliesFromFiles[FileKey.Create(mscorlibPath)];
                Assert.Equal(1, mscorlibInfo.CachedSymbols.Count());
                Assert.Same(mscorlibInfo.CachedSymbols.First(), mscorlibAsm);

                var varMTTestLib1Info = MetadataCache.AssembliesFromFiles[FileKey.Create(varMTTestLib1Path)];
                var assembly = varMTTestLib1Info.Metadata.GetTarget().GetAssembly();
                Assert.NotNull(assembly);
                Assert.Equal(1, varMTTestLib1Info.CachedSymbols.Count());

                var varMTTestLib1Asm = (PEAssemblySymbol)varMTTestLib1Info.CachedSymbols.First();
                Assert.Equal("MTTestLib1", assembly.Identity.Name);
                Assert.Equal(3, assembly.AssemblyReferences.Length);

                int mscorlibRefIndex = -1;
                int msvbRefIndex = -1;
                int systemRefIndex = -1;

                for (int i = 0; i <= 2; i++)
                {
                    if (assembly.AssemblyReferences[i].Name.Equals("mscorlib"))
                    {
                        Assert.Equal(-1, mscorlibRefIndex);
                        mscorlibRefIndex = i;
                    }
                    else if (assembly.AssemblyReferences[i].Name.Equals("Microsoft.VisualBasic"))
                    {
                        Assert.Equal(-1, msvbRefIndex);
                        msvbRefIndex = i;
                    }
                    else if (assembly.AssemblyReferences[i].Name.Equals("System"))
                    {
                        Assert.Equal(-1, systemRefIndex);
                        systemRefIndex = i;
                    }
                    else
                    {
                        Assert.True(false, assembly.AssemblyReferences[i].Name);
                    }
                }

                Assert.Equal(1, assembly.ModuleReferenceCounts.Length);
                Assert.Equal(3, assembly.ModuleReferenceCounts[0]);

                Assert.Equal(2, MetadataCache.ModulesFromFiles.Count);
                Assert.Same(MetadataCache.ModulesFromFiles[FileKey.Create(module1Path)].Metadata, cachedModule1.Metadata);

                var cachedModule2 = MetadataCache.ModulesFromFiles[FileKey.Create(module2Path)];

                var module2 = cachedModule2.Metadata.GetTarget().Module;
                Assert.NotNull(module2);

                Assert.Equal(1, module2.ReferencedAssemblies.Length);
                Assert.Equal("mscorlib", module2.ReferencedAssemblies[0].Name);

                Assert.Equal(0, tc1.RetargetingAssemblySymbols.WeakCount);

                Assert.Equal(5, tc2.ExternalReferences.Length); // tc1Ref, module2Ref, varMTTestLib1Ref, mscorlibRef, module1Ref
                Assert.Same(tc2.ExternalReferences[0], varTc1Ref);
                Assert.Same(tc2.ExternalReferences[1], module2Ref);
                Assert.Same(tc2.ExternalReferences[2], varMTTestLib1Ref);
                Assert.Same(tc2.ExternalReferences[3], mscorlibRef);
                Assert.Same(tc2.ExternalReferences[4], module1Ref);
                Assert.Same(tc2.GetReferencedAssemblySymbol(mscorlibRef), mscorlibAsm);
                Assert.Same(tc2.GetReferencedAssemblySymbol(varTc1Ref), tc1.Assembly);
                Assert.Same(tc2.GetReferencedAssemblySymbol(varMTTestLib1Ref), varMTTestLib1Info.CachedSymbols.First());
                Assert.Same(tc2.GetReferencedModuleSymbol(module1Ref), tc2.Assembly.Modules[2]);
                Assert.Same(tc2.GetReferencedModuleSymbol(module2Ref), tc2.Assembly.Modules[1]);

                Assert.Equal(3, tc2.Assembly.Modules.Length);

                var m21 = (SourceModuleSymbol)tc2.Assembly.Modules[0];

                Assert.Equal(3, m21.GetReferencedAssemblies().Length);
                Assert.Equal("Test1", m21.GetReferencedAssemblies()[0].Name);
                Assert.Equal("MTTestLib1", m21.GetReferencedAssemblies()[1].Name);
                Assert.Equal("mscorlib", m21.GetReferencedAssemblies()[2].Name);
                Assert.Equal(3, m21.GetReferencedAssemblySymbols().Length);
                Assert.Same(m21.GetReferencedAssemblySymbols()[0], tc1.Assembly);
                Assert.Same(m21.GetReferencedAssemblySymbols()[1], varMTTestLib1Asm);
                Assert.Same(m21.GetReferencedAssemblySymbols()[2], mscorlibAsm);
                Assert.Same(m21.CorLibrary(), mscorlibAsm);

                var m22 = (PEModuleSymbol)tc2.Assembly.Modules[1];

                Assert.Same(m22.Module, cachedModule2.Metadata.GetTarget().Module);
                Assert.Same(m22.ContainingAssembly, tc2.Assembly);
                Assert.Same(m22.ContainingSymbol, tc2.Assembly);
                Assert.Equal(null, m22.ContainingType);

                Assert.Equal(1, m22.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m22.GetReferencedAssemblies()[0].Name);
                Assert.Equal(1, m22.GetReferencedAssemblySymbols().Length);
                Assert.Same(m22.GetReferencedAssemblySymbols()[0], mscorlibAsm);
                Assert.Same(m22.CorLibrary(), mscorlibAsm);
                Assert.Equal("netModule2.netmodule", m22.Name);
                Assert.Equal("netModule2.netmodule", m22.ToTestDisplayString());

                var m23 = (PEModuleSymbol)tc2.Assembly.Modules[2];

                Assert.Same(m23.Module, cachedModule1.Metadata.GetTarget().Module);
                Assert.Same(m23.ContainingAssembly, tc2.Assembly);
                Assert.Same(m23.ContainingSymbol, tc2.Assembly);
                Assert.Equal(null, m23.ContainingType);

                Assert.Equal(1, m23.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m23.GetReferencedAssemblies()[0].Name);
                Assert.Equal(1, m23.GetReferencedAssemblySymbols().Length);
                Assert.Same(m23.GetReferencedAssemblySymbols()[0], mscorlibAsm);
                Assert.Same(m23.CorLibrary(), mscorlibAsm);
                Assert.Equal("netModule1.netmodule", m23.Name);
                Assert.Equal("netModule1.netmodule", m23.ToTestDisplayString());

                Assert.Equal(1, varMTTestLib1Asm.Modules.Length);
                var varMTTestLib1Module = (PEModuleSymbol)varMTTestLib1Asm.Modules[0];

                Assert.Equal(3, varMTTestLib1Module.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", varMTTestLib1Module.GetReferencedAssemblies()[mscorlibRefIndex].Name);
                Assert.Equal("Microsoft.VisualBasic", varMTTestLib1Module.GetReferencedAssemblies()[msvbRefIndex].Name);
                Assert.Equal("System", varMTTestLib1Module.GetReferencedAssemblies()[systemRefIndex].Name);

                Assert.Equal(3, varMTTestLib1Module.GetReferencedAssemblySymbols().Length);
                Assert.Same(varMTTestLib1Module.GetReferencedAssemblySymbols()[mscorlibRefIndex], mscorlibAsm);
                Assert.True(varMTTestLib1Module.GetReferencedAssemblySymbols()[msvbRefIndex].IsMissing);
                Assert.True(varMTTestLib1Module.GetReferencedAssemblySymbols()[systemRefIndex].IsMissing);

                Assert.Same(varMTTestLib1Module.CorLibrary(), mscorlibAsm);

                var varMTTestLib1V2Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path;
                var varMTTestLib1V2Ref = new MetadataFileReference(varMTTestLib1V2Path);

                var varTc2Ref = new CSharpCompilationReference(tc2);

                var tc3 = CSharpCompilation.Create("Test3", references: new MetadataReference[] { varMTTestLib1V2Ref, varTc2Ref });
                Assert.NotNull(tc3.Assembly); // force creation of SourceAssemblySymbol

                Assert.Equal(3, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(2, MetadataCache.ModulesFromFiles.Count);

                var varA3 = (SourceAssemblySymbol)tc3.Assembly;

                var varA3tc2 = (RetargetingAssemblySymbol)tc3.GetReferencedAssemblySymbol(varTc2Ref);
                var varMTTestLib1V2Asm = (PEAssemblySymbol)tc3.GetReferencedAssemblySymbol(varMTTestLib1V2Ref);

                Assert.NotSame(varMTTestLib1Asm, varMTTestLib1V2Asm);
                Assert.Equal(1, tc2.RetargetingAssemblySymbols.WeakCount);
                Assert.Same(varA3tc2, tc2.RetargetingAssemblySymbols.GetWeakReference(0).GetTarget());

                Assert.Equal("Test2", varA3tc2.Name);
                Assert.Equal(3, varA3tc2.Modules.Length);

                var varA3m1tc2 = (RetargetingModuleSymbol)varA3tc2.Modules[0];

                Assert.Same(varA3m1tc2.ContainingAssembly, varA3tc2);
                Assert.Same(varA3m1tc2.ContainingSymbol, varA3tc2);
                Assert.Equal(null, varA3m1tc2.ContainingType);
                Assert.Same(varA3m1tc2.UnderlyingModule, m21);

                Assert.True(varA3m1tc2.GetReferencedAssemblies().Equals(m21.GetReferencedAssemblies()));
                Assert.Equal(3, varA3m1tc2.GetReferencedAssemblySymbols().Length);
                Assert.True(varA3m1tc2.GetReferencedAssemblySymbols()[0].IsMissing);
                Assert.Same(varA3m1tc2.GetReferencedAssemblySymbols()[1], varMTTestLib1V2Asm);
                Assert.True(varA3m1tc2.GetReferencedAssemblySymbols()[2].IsMissing);

                var varA3m2tc2 = (PEModuleSymbol)varA3tc2.Modules[1];

                Assert.Same(varA3m2tc2.ContainingAssembly, varA3tc2);
                Assert.Same(varA3m2tc2.ContainingSymbol, varA3tc2);
                Assert.Equal(null, varA3m2tc2.ContainingType);
                Assert.NotEqual(varA3m2tc2, m22);
                Assert.Same(varA3m2tc2.Module, m22.Module);

                Assert.True(varA3m2tc2.GetReferencedAssemblies().Equals(m22.GetReferencedAssemblies()));
                Assert.Equal(1, varA3m2tc2.GetReferencedAssemblySymbols().Length);
                Assert.True(varA3m2tc2.GetReferencedAssemblySymbols()[0].IsMissing);

                var varA3m3tc2 = (PEModuleSymbol)varA3tc2.Modules[2];

                Assert.Same(varA3m3tc2.ContainingAssembly, varA3tc2);
                Assert.Same(varA3m3tc2.ContainingSymbol, varA3tc2);
                Assert.Equal(null, varA3m3tc2.ContainingType);
                Assert.NotEqual(varA3m3tc2, m23);
                Assert.Same(varA3m3tc2.Module, m23.Module);

                Assert.True(varA3m3tc2.GetReferencedAssemblies().Equals(m23.GetReferencedAssemblies()));
                Assert.Equal(1, varA3m3tc2.GetReferencedAssemblySymbols().Length);
                Assert.True(varA3m3tc2.GetReferencedAssemblySymbols()[0].IsMissing);

                var tc4 = CSharpCompilation.Create("Test4", references: new MetadataReference[] { varMTTestLib1V2Ref, varTc2Ref, mscorlibRef });
                Assert.NotNull(tc4.Assembly); // force creation of SourceAssemblySymbol

                Assert.Equal(3, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(2, MetadataCache.ModulesFromFiles.Count);

                var varA4 = (SourceAssemblySymbol)tc4.Assembly;

                var varA4tc2 = (RetargetingAssemblySymbol)tc4.GetReferencedAssemblySymbol(varTc2Ref);
                var var_mTTestLib1V2Asm = (PEAssemblySymbol)tc4.GetReferencedAssemblySymbol(varMTTestLib1V2Ref);

                Assert.NotSame(varMTTestLib1V2Asm, var_mTTestLib1V2Asm);
                Assert.NotSame(varMTTestLib1Asm, var_mTTestLib1V2Asm);
                Assert.Equal(2, tc2.RetargetingAssemblySymbols.WeakCount);
                Assert.Same(varA4tc2, tc2.RetargetingAssemblySymbols.GetWeakReference(1).GetTarget());

                Assert.Equal("Test2", varA4tc2.Name);
                Assert.Equal(3, varA4tc2.Modules.Length);

                var varA4m1tc2 = (RetargetingModuleSymbol)varA4tc2.Modules[0];

                Assert.Same(varA4m1tc2.ContainingAssembly, varA4tc2);
                Assert.Same(varA4m1tc2.ContainingSymbol, varA4tc2);
                Assert.Equal(null, varA4m1tc2.ContainingType);
                Assert.Same(varA4m1tc2.UnderlyingModule, m21);

                Assert.True(varA4m1tc2.GetReferencedAssemblies().Equals(m21.GetReferencedAssemblies()));
                Assert.Equal(3, varA4m1tc2.GetReferencedAssemblySymbols().Length);
                Assert.True(varA4m1tc2.GetReferencedAssemblySymbols()[0].IsMissing);
                Assert.Same(varA4m1tc2.GetReferencedAssemblySymbols()[1], var_mTTestLib1V2Asm);
                Assert.Same(varA4m1tc2.GetReferencedAssemblySymbols()[2], mscorlibAsm);

                var varA4m2tc2 = (PEModuleSymbol)varA4tc2.Modules[1];

                Assert.Same(varA4m2tc2.ContainingAssembly, varA4tc2);
                Assert.Same(varA4m2tc2.ContainingSymbol, varA4tc2);
                Assert.Equal(null, varA4m2tc2.ContainingType);
                Assert.NotEqual(varA4m2tc2, m22);
                Assert.Same(varA4m2tc2.Module, m22.Module);

                Assert.True(varA4m2tc2.GetReferencedAssemblies().Equals(m22.GetReferencedAssemblies()));
                Assert.Equal(1, varA4m2tc2.GetReferencedAssemblySymbols().Length);
                Assert.Same(varA4m2tc2.GetReferencedAssemblySymbols()[0], mscorlibAsm);

                var varA4m3tc2 = (PEModuleSymbol)varA4tc2.Modules[2];

                Assert.Same(varA4m3tc2.ContainingAssembly, varA4tc2);
                Assert.Same(varA4m3tc2.ContainingSymbol, varA4tc2);
                Assert.Equal(null, varA4m3tc2.ContainingType);
                Assert.NotEqual(varA4m3tc2, m23);
                Assert.Same(varA4m3tc2.Module, m23.Module);

                Assert.True(varA4m3tc2.GetReferencedAssemblies().Equals(m23.GetReferencedAssemblies()));
                Assert.Equal(1, varA4m3tc2.GetReferencedAssemblySymbols().Length);
                Assert.Same(varA4m3tc2.GetReferencedAssemblySymbols()[0], mscorlibAsm);

                GC.KeepAlive(tc1);
                GC.KeepAlive(tc2);
                GC.KeepAlive(tc3);
                GC.KeepAlive(tc4);
            }
        }

        [Fact]
        public void ReferenceMultiModuleAssembly()
        {
            using (MetadataCache.LockAndClean())
            {
                var dir = Temp.CreateDirectory();
                var mscorlibRef = new MetadataFileReference(dir.CreateFile("mscorlib.dll").WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path);
                var systemCoreRef = new MetadataFileReference(dir.CreateFile("System.Core.dll").WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.System_Core).Path);

                var mm = dir.CreateFile("MultiModule.dll").WriteAllBytes(TestResources.SymbolsTests.MultiModule.MultiModule).Path;
                dir.CreateFile("mod2.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod2);
                dir.CreateFile("mod3.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod3);
                var multimoduleRef = new MetadataFileReference(mm);

                var tc1 = CSharpCompilation.Create("Test1", references: new MetadataReference[] { mscorlibRef, systemCoreRef, multimoduleRef });

                Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol
                Assert.Equal(3, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                var fileInfo = MetadataCache.AssembliesFromFiles[FileKey.Create(multimoduleRef.FilePath)];

                var assembly = fileInfo.Metadata.GetTarget().GetAssembly();

                // tc1 compilation is holding on the Assembly:
                Assert.NotNull(assembly);

                Assert.Equal("MultiModule", assembly.Identity.Name);
                Assert.Equal(5, assembly.AssemblyReferences.Length);
                Assert.Equal("mscorlib", assembly.AssemblyReferences[0].Name);
                Assert.Equal("System.Core", assembly.AssemblyReferences[1].Name);
                Assert.Equal("mscorlib", assembly.AssemblyReferences[2].Name);
                Assert.Equal("mscorlib", assembly.AssemblyReferences[3].Name);
                Assert.Equal("System.Core", assembly.AssemblyReferences[4].Name);
                Assert.Equal(3, assembly.ModuleReferenceCounts.Length);
                Assert.Equal(2, assembly.ModuleReferenceCounts[0]);
                Assert.Equal(1, assembly.ModuleReferenceCounts[1]);
                Assert.Equal(2, assembly.ModuleReferenceCounts[2]);

                var multiModuleAsm = (PEAssemblySymbol)tc1.GetReferencedAssemblySymbol(multimoduleRef);
                var mscorlibAsm = (PEAssemblySymbol)tc1.GetReferencedAssemblySymbol(mscorlibRef);
                var systemCoreAsm = (PEAssemblySymbol)tc1.GetReferencedAssemblySymbol(systemCoreRef);

                Assert.Equal(3, multiModuleAsm.Modules.Length);
                Assert.Same(multiModuleAsm.Modules[0], multiModuleAsm.Locations.Single().MetadataModule);

                foreach (var m in multiModuleAsm.Modules)
                {
                    Assert.Same(m, m.Locations.Single().MetadataModule);
                }

                var m1 = (PEModuleSymbol)multiModuleAsm.Modules[0];

                Assert.Equal(2, m1.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m1.GetReferencedAssemblies()[0].Name);
                Assert.Equal("System.Core", m1.GetReferencedAssemblies()[1].Name);
                Assert.Equal(2, m1.GetReferencedAssemblySymbols().Length);
                Assert.Same(m1.GetReferencedAssemblySymbols()[0], mscorlibAsm);
                Assert.Same(m1.GetReferencedAssemblySymbols()[1], systemCoreAsm);

                var m2 = (PEModuleSymbol)multiModuleAsm.Modules[1];

                Assert.Equal(1, m2.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m2.GetReferencedAssemblies()[0].Name);
                Assert.Equal(1, m2.GetReferencedAssemblySymbols().Length);
                Assert.Same(m2.GetReferencedAssemblySymbols()[0], mscorlibAsm);

                var m3 = (PEModuleSymbol)multiModuleAsm.Modules[2];

                Assert.Equal(2, m3.GetReferencedAssemblies().Length);
                Assert.Equal("mscorlib", m3.GetReferencedAssemblies()[0].Name);
                Assert.Equal("System.Core", m3.GetReferencedAssemblies()[1].Name);
                Assert.Equal(2, m3.GetReferencedAssemblySymbols().Length);
                Assert.Same(m3.GetReferencedAssemblySymbols()[0], mscorlibAsm);
                Assert.Same(m3.GetReferencedAssemblySymbols()[1], systemCoreAsm);
            }
        }

        [Fact]
        public void LazySourceAssembly_Assembly()
        {
            var mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path;

            var mscorlibRef = new MetadataFileReference(mscorlibPath);
            var module1Ref = new MetadataFileReference(module1Path, MetadataImageKind.Module);

            using (var @lock = MetadataCache.LockAndClean())
            {
                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                // Test Assembly property
                var c1 = CSharpCompilation.Create("Test", references: new MetadataReference[] { mscorlibRef, module1Ref });

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                Assert.False(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.False(CSReferenceManager.IsReferenceManagerInitialized(c1));

                var a = c1.Assembly;

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count);

                Assert.True(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.True(CSReferenceManager.IsReferenceManagerInitialized(c1));

                GC.KeepAlive(c1);
            }
        }

        [Fact]
        public void LazySourceAssembly_GetReferencedAssemblySymbol()
        {
            var mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path;

            var mscorlibRef = new MetadataFileReference(mscorlibPath);
            var module1Ref = new MetadataFileReference(module1Path, MetadataImageKind.Module);

            using (var @lock = MetadataCache.LockAndClean())
            {
                // Test GetReferencedAssemblySymbol method
                var c1 = CSharpCompilation.Create("Test", references: new MetadataReference[] { mscorlibRef, module1Ref });

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                Assert.False(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.False(CSReferenceManager.IsReferenceManagerInitialized(c1));

                var a = c1.GetReferencedAssemblySymbol(mscorlibRef);

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count);

                Assert.True(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.True(CSReferenceManager.IsReferenceManagerInitialized(c1));

                GC.KeepAlive(c1);
            }
        }

        [Fact]
        public void LazySourceAssembly_GetReferencedModuleSymbol()
        {
            var mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path;

            using (MetadataCache.LockAndClean())
            {
                var mscorlibRef = new MetadataFileReference(mscorlibPath);
                var module1Ref = new MetadataFileReference(module1Path, MetadataImageKind.Module);

                // Test GetReferencedModuleSymbol method
                var c1 = CSharpCompilation.Create("Test", references: new MetadataReference[] { mscorlibRef, module1Ref });

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                Assert.False(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.False(CSReferenceManager.IsReferenceManagerInitialized(c1));

                var a = c1.GetReferencedModuleSymbol(module1Ref).ContainingAssembly;

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count);

                Assert.True(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.True(CSReferenceManager.IsReferenceManagerInitialized(c1));

                GC.KeepAlive(c1);
            }
        }

        [Fact]
        public void LazySourceAssembly_CompilationReference1()
        {
            var mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path;

            var mscorlibRef = new MetadataFileReference(mscorlibPath);
            var module1Ref = new MetadataFileReference(module1Path, MetadataImageKind.Module);

            using (var @lock = MetadataCache.LockAndClean())
            {
                // Test compilation reference
                var c1 = CSharpCompilation.Create("Test1", references: new MetadataReference[] { mscorlibRef, module1Ref });
                var c2 = CSharpCompilation.Create("Test2", references: new MetadataReference[] { mscorlibRef, module1Ref, new CSharpCompilationReference(c1) });

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                Assert.False(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.False(CSReferenceManager.IsReferenceManagerInitialized(c1));

                Assert.False(CSReferenceManager.IsSourceAssemblySymbolCreated(c2));
                Assert.False(CSReferenceManager.IsReferenceManagerInitialized(c2));

                var a = c1.Assembly;

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count);

                Assert.True(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.True(CSReferenceManager.IsReferenceManagerInitialized(c1));

                Assert.False(CSReferenceManager.IsSourceAssemblySymbolCreated(c2));
                Assert.False(CSReferenceManager.IsReferenceManagerInitialized(c2));

                a = c2.Assembly;

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Count());
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count);

                Assert.True(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.True(CSReferenceManager.IsReferenceManagerInitialized(c1));

                Assert.True(CSReferenceManager.IsSourceAssemblySymbolCreated(c2));
                Assert.True(CSReferenceManager.IsReferenceManagerInitialized(c2));

                GC.KeepAlive(c1);
                GC.KeepAlive(c2);
            }
        }

        [Fact]
        public void LazySourceAssembly_CompilationReference2()
        {
            var mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var module1Path = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path;

            var mscorlibRef = new MetadataFileReference(mscorlibPath);
            var module1Ref = new MetadataFileReference(module1Path, MetadataImageKind.Module);

            using (var @lock = MetadataCache.LockAndClean())
            {
                var c1 = CSharpCompilation.Create("Test1", references: new MetadataReference[] { mscorlibRef, module1Ref });
                var c2 = CSharpCompilation.Create("Test2", references: new MetadataReference[] { mscorlibRef, module1Ref, new CSharpCompilationReference(c1) });

                Assert.Equal(0, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                Assert.False(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.False(CSReferenceManager.IsReferenceManagerInitialized(c1));

                Assert.False(CSReferenceManager.IsSourceAssemblySymbolCreated(c2));
                Assert.False(CSReferenceManager.IsReferenceManagerInitialized(c2));

                var a = c2.Assembly;

                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Values.Single().CachedSymbols.Count());
                Assert.Equal(1, MetadataCache.ModulesFromFiles.Count);

                Assert.True(CSReferenceManager.IsSourceAssemblySymbolCreated(c1));
                Assert.True(CSReferenceManager.IsReferenceManagerInitialized(c1));

                Assert.True(CSReferenceManager.IsSourceAssemblySymbolCreated(c2));
                Assert.True(CSReferenceManager.IsReferenceManagerInitialized(c2));

                GC.KeepAlive(c1);
                GC.KeepAlive(c2);
            }
        }
        
        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void CompactRetargetingCache1()
        {
            using (MetadataCache.LockAndClean())
            {
                var mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
                var mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;
                var V1MTTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path;
                var V2MTTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path;

                // Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
                var c1 = CSharpCompilation.Create("Test1", references: MetadataCacheTestHelpers.CreateMetadataReferences(
                    mscorlib2,
                    V1MTTestLib1));

                var c2 = CreateWeakCompilation("Test2",
                    mscorlib3,
                    c1,
                    V1MTTestLib1);

                var c3 = CreateCompilation("Test3",
                    mscorlib3,
                    c1,
                    V2MTTestLib1);

                var ras1 = c1.RetargetingAssemblySymbols;

                Assert.Equal(2, ras1.WeakCount);
                WeakReference<IAssemblySymbol> weakRetargetingAsm1 = ras1.GetWeakReference(0);
                WeakReference<IAssemblySymbol> weakRetargetingAsm2 = ras1.GetWeakReference(1);

                // remove strong reference, the only reference left is weak
                c2.Strong = null;

                while (!weakRetargetingAsm1.IsNull())
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                }

                List<AssemblySymbol> symbols = new List<AssemblySymbol>();
                lock (CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard)
                {
                    c1.AddRetargetingAssemblySymbolsNoLock(symbols);
                }

                Assert.Equal(1, symbols.Count);
                Assert.Equal(2, ras1.WeakCount); // weak list hasn't been compacted

                var asm2 = weakRetargetingAsm2.GetTarget();
                Assert.NotNull(asm2);
                Assert.Null(ras1.GetWeakReference(0).GetTarget());
                Assert.Same(ras1.GetWeakReference(1).GetTarget(), asm2);

                GC.KeepAlive(c3);
                GC.KeepAlive(c1);
            }
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void CompactRetargetingCache2()
        {
            using (MetadataCache.LockAndClean())
            {
                var mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
                var mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;
                var libV1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path;
                var libV2 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path;

                // Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
                var c1 = CSharpCompilation.Create("Test1", references: MetadataCacheTestHelpers.CreateMetadataReferences(
                    mscorlib2,
                    libV1));

                var c2 = CreateWeakCompilation("Test2",
                    mscorlib3,
                    c1,
                    libV1);

                var c3 = CreateWeakCompilation("Test3",
                    mscorlib3,
                    c1,
                    libV2);

                var ras1 = c1.RetargetingAssemblySymbols;

                Assert.Equal(2, ras1.WeakCount);
                WeakReference<IAssemblySymbol> weakRetargetingAsm1 = ras1.GetWeakReference(0);
                WeakReference<IAssemblySymbol> weakRetargetingAsm2 = ras1.GetWeakReference(1);

                // remove strong references, the only references left are weak
                c2.Strong = null;
                c3.Strong = null;

                while (!weakRetargetingAsm1.IsNull() || !weakRetargetingAsm2.IsNull())
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                }

                List<AssemblySymbol> symbols = new List<AssemblySymbol>();
                lock (CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard)
                {
                    c1.AddRetargetingAssemblySymbolsNoLock(symbols);
                }
                Assert.Equal(0, symbols.Count);
                Assert.Equal(0, ras1.WeakCount);

                GC.KeepAlive(c1);
            }
        }

        [WorkItem(975881)]
        [Fact(Skip = "975881")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void CompactFileCache1()
        {
            var mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;
            var MDTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.General.MDTestLib1).Path;

            // Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
            var c1 = CreateWeakCompilation("Test1",
                mscorlib2,
                MDTestLib1);

            var c2 = CreateWeakCompilation("Test2",
                mscorlib3,
                MDTestLib1);

            var fileKey = FileKey.Create(MDTestLib1);
            var assembly = GetWeakAssemblyFromCache(fileKey);

            Assert.True(MetadataCache.CompactTimerIsOn);

            // remove strong reference, the only reference left is weak
            c1.Strong = null;
            c2.Strong = null;
            assembly.Strong = null;

            while (assembly.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            MetadataCache.TriggerCacheCompact();

            for (int i = 0; i < 100; i++)
            {
                lock (MetadataCache.Guard)
                {
                    if (!MetadataCache.AssembliesFromFiles.ContainsKey(fileKey))
                    {
                        break;
                    }
                }

                System.Threading.Thread.Sleep(10);
            }

            lock (MetadataCache.Guard)
            {
                Assert.False(MetadataCache.AssembliesFromFiles.ContainsKey(fileKey));

                bool found = false;

                foreach (var key in MetadataCache.AssemblyKeys)
                {
                    if (fileKey.Equals(key))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.False(found);

                MetadataCache.DisposeCachedMetadata();
            }
        }

        [WorkItem(975881)]
        [Fact(Skip = "975881")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void CompactFileCache2()
        {
            var mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;
            var MDTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.General.MDTestLib1).Path;

            // Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
            var c1 = CreateCompilation("Test1",
                mscorlib2,
                MDTestLib1);

            var c2 = CreateWeakCompilation("Test2",
                mscorlib3,
                MDTestLib1);

            var fileKey = FileKey.Create(MDTestLib1);

            MetadataCache.CachedAssembly mdTestLib1;
            lock (MetadataCache.Guard)
            {
                mdTestLib1 = MetadataCache.AssembliesFromFiles[fileKey];
            }

            Assert.Equal(2, mdTestLib1.CachedSymbols.Count());
            var weakAsm2 = mdTestLib1.CachedSymbols.GetWeakReference(1);

            AssertNonNullTarget(weakAsm2);
            Assert.True(MetadataCache.CompactTimerIsOn);

            // remove strong reference, the only reference left is weak
            c2.Strong = null;

            while (!weakAsm2.IsNull())
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            MetadataCache.TriggerCacheCompact();

            for (int i = 0; i < 100; i++)
            {
                lock (MetadataCache.Guard)
                {
                    if (mdTestLib1.CachedSymbols.Count() != 2)
                    {
                        break;
                    }
                }

                System.Threading.Thread.Sleep(10);
            }

            lock (MetadataCache.Guard)
            {
                Assert.Equal(1, mdTestLib1.CachedSymbols.Count());
                Assert.NotNull(mdTestLib1.Metadata.GetTarget().GetAssembly());
                Assert.False(mdTestLib1.CachedSymbols.GetWeakReference(0).IsNull());
                Assert.Same(mdTestLib1.Metadata, MetadataCache.AssembliesFromFiles[fileKey].Metadata);

                bool found = false;

                foreach (var key in MetadataCache.AssemblyKeys)
                {
                    if (fileKey.Equals(key))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.True(found);

                MetadataCache.DisposeCachedMetadata();
            }

            GC.KeepAlive(c1);
        }

        [WorkItem(975881)]
        [Fact(Skip = "975881")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void CompactFileCache3()
        {
            var mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;
            var netModule1 = Temp.CreateFile(extension: ".netmodule").WriteAllBytes(TestResources.SymbolsTests.netModule.netModule1).Path;

            // Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
            var c1 = CreateWeakCompilation("Test1",
                mscorlib2,
                netModule1);

            var c2 = CreateWeakCompilation("Test2",
                mscorlib3,
                netModule1);

            var fileKey = FileKey.Create(netModule1);
            var module = GetWeakModuleFromCache(fileKey);

            Assert.True(MetadataCache.CompactTimerIsOn);

            c1.Strong = null;
            c2.Strong = null;
            module.Strong = null;

            while (module.Weak.IsAlive)
            {
                GC.Collect(2, GCCollectionMode.Forced);
            }

            MetadataCache.TriggerCacheCompact();

            for (int i = 0; i < 100; i++)
            {
                lock (MetadataCache.Guard)
                {
                    if (!MetadataCache.ModulesFromFiles.ContainsKey(fileKey))
                    {
                        break;
                    }
                }

                System.Threading.Thread.Sleep(10);
            }

            lock (MetadataCache.Guard)
            {
                Assert.False(MetadataCache.ModulesFromFiles.ContainsKey(fileKey));

                bool found = false;

                foreach (var key in MetadataCache.ModuleKeys)
                {
                    if (fileKey.Equals(key))
                    {
                        found = true;
                        break;
                    }
                }

                Assert.False(found);

                MetadataCache.DisposeCachedMetadata();
            }
        }
    }
}
