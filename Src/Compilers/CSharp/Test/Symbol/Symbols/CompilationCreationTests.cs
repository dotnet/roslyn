// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CSReferenceManager = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.ReferenceManager;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CompilationCreationTests : CSharpTestBase
    {
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

            Assert.Equal(null, mscorlibRef.Properties.Alias);
            Assert.Equal(false, mscorlibRef.Properties.EmbedInteropTypes);
            Assert.Equal(mscorlibPath, mscorlibRef.FullPath, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(MetadataImageKind.Assembly, mscorlibRef.Properties.Kind);

            using (MetadataCache.LockAndClean())
            {
                var c1 = CSharpCompilation.Create("Test", references: new[] { mscorlibRef });

                Assert.NotNull(c1.Assembly); // force creation of SourceAssemblySymbol
                Assert.Equal(1, MetadataCache.AssembliesFromFiles.Count);
                Assert.Equal(0, MetadataCache.ModulesFromFiles.Count);

                var cachedAssembly = MetadataCache.AssembliesFromFiles.Values.Single();

                Assert.Equal(mscorlibPath, MetadataCache.AssembliesFromFiles.Keys.Single().FullPath, StringComparer.OrdinalIgnoreCase);

                var assembly = cachedAssembly.Metadata.GetTarget().Assembly;
                Assert.NotNull(assembly);

                Assert.Equal("mscorlib", assembly.Identity.Name);
                Assert.Equal(0, assembly.AssemblyReferences.Length);
                Assert.Equal(1, assembly.ModuleReferenceCounts.Length);
                Assert.Equal(0, assembly.ModuleReferenceCounts[0]);
                Assert.Equal(1, cachedAssembly.CachedSymbols.Count());

                var mscorlibAsm = (PEAssemblySymbol)cachedAssembly.CachedSymbols.First();

                Assert.NotNull(mscorlibAsm);
                Assert.Same(mscorlibAsm.Assembly, cachedAssembly.Metadata.GetTarget().Assembly);
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
        public void CorLibTypes()
        {
            var mdTestLib1 = TestReferences.SymbolsTests.MDTestLib1;

            var c1 = CSharpCompilation.Create("Test", references: new MetadataReference[] { MscorlibRef_v4_0_30316_17626, mdTestLib1 });

                TypeSymbol c107 = c1.GlobalNamespace.GetTypeMembers("C107").Single();

                Assert.Equal(SpecialType.None, c107.SpecialType);

                for (int i = 1; i <= (int)SpecialType.Count; i++)
                {
                    NamedTypeSymbol type = c1.GetSpecialType((SpecialType)i);
                    Assert.False(type.IsErrorType());
                    Assert.Equal((SpecialType)i, type.SpecialType);
                }

                Assert.Equal(SpecialType.None, c107.SpecialType);

                var arrayOfc107 = new ArrayTypeSymbol(c1.Assembly, c107);

                Assert.Equal(SpecialType.None, arrayOfc107.SpecialType);

                var c2 = CSharpCompilation.Create("Test", references: new[] { mdTestLib1 });

                Assert.Equal(SpecialType.None, c2.GlobalNamespace.GetTypeMembers("C107").Single().SpecialType);
            }

        [Fact]
        public void ReferenceAnotherCompilation()
        {
            using (MetadataCache.LockAndClean())
            {
                var tc1 = CSharpCompilation.Create("Test1");
                Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol

                var varC1Ref = new CSharpCompilationReference(tc1);

                Assert.Equal(null, varC1Ref.Properties.Alias);
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
                Assert.Equal(null, module1Ref.Properties.Alias);
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
                var assembly = varMTTestLib1Info.Metadata.GetTarget().Assembly;
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

                var fileInfo = MetadataCache.AssembliesFromFiles[FileKey.Create(multimoduleRef.FullPath)];

                var assembly = fileInfo.Metadata.GetTarget().Assembly;

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
        public void CyclicReference()
        {
            var mscorlibRef = TestReferences.NetFx.v4_0_30319.mscorlib;
            var cyclic2Ref = TestReferences.SymbolsTests.Cyclic.Cyclic2.dll;

            var tc1 = CSharpCompilation.Create("Cyclic1", references: new[] { mscorlibRef, cyclic2Ref });
                Assert.NotNull(tc1.Assembly); // force creation of SourceAssemblySymbol

                var cyclic1Asm = (SourceAssemblySymbol)tc1.Assembly;
                var cyclic1Mod = (SourceModuleSymbol)cyclic1Asm.Modules[0];

                var cyclic2Asm = (PEAssemblySymbol)tc1.GetReferencedAssemblySymbol(cyclic2Ref);
                var cyclic2Mod = (PEModuleSymbol)cyclic2Asm.Modules[0];

                Assert.Same(cyclic2Mod.GetReferencedAssemblySymbols()[1], cyclic1Asm);
                Assert.Same(cyclic1Mod.GetReferencedAssemblySymbols()[1], cyclic2Asm);
            }

        [Fact]
        public void MultiTargeting1()
        {
            var varV1MTTestLib2Ref = TestReferences.SymbolsTests.V1.MTTestLib2.dll;
            var asm1 = MetadataTestHelpers.GetSymbolsForReferences(mrefs: new[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref
                });

                Assert.Equal("mscorlib", asm1[0].Identity.Name);
                Assert.Equal(0, asm1[0].BoundReferences().Length);
                Assert.Equal("MTTestLib2", asm1[1].Identity.Name);
                Assert.Equal(1, (from a in asm1[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm1[1].BoundReferences() where object.ReferenceEquals(a, asm1[0]) select a).Count());
                Assert.Equal(SymbolKind.ErrorType, asm1[1].GlobalNamespace.GetTypeMembers("Class4").
                                      Single().
                                      GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.Kind);

            var asm2 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref,
                TestReferences.SymbolsTests.V1.MTTestLib1.dll
                });

                Assert.Same(asm2[0], asm1[0]);

                Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
                Assert.NotSame(asm2[1], asm1[1]);
                Assert.Same(((PEAssemblySymbol)asm2[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
                Assert.Equal(2, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
                Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

                var retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
                Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
                Assert.Equal(1, asm2[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

            var varV2MTTestLib3Ref = TestReferences.SymbolsTests.V2.MTTestLib3.dll;
                var asm3 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref,
                TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                varV2MTTestLib3Ref
            });

                Assert.Same(asm3[0], asm1[0]);

                Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
                Assert.NotSame(asm3[1], asm1[1]);
                Assert.NotSame(asm3[1], asm2[1]);
                Assert.Same(((PEAssemblySymbol)asm3[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
                Assert.Equal(2, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                var retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
                Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
                Assert.NotSame(asm3[2], asm2[2]);
                Assert.NotSame(((PEAssemblySymbol)asm3[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
                Assert.Equal(2, asm3[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
                Assert.Equal(3, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                var type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                var retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
                Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
                Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
                Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

            var varV3MTTestLib4Ref = TestReferences.SymbolsTests.V3.MTTestLib4.dll;
            var asm4 = MetadataTestHelpers.GetSymbolsForReferences(new MetadataReference[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref,
                TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                varV2MTTestLib3Ref,
                varV3MTTestLib4Ref
            });

                Assert.Same(asm3[0], asm1[0]);

                Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
                Assert.NotSame(asm4[1], asm1[1]);
                Assert.NotSame(asm4[1], asm2[1]);
                Assert.NotSame(asm4[1], asm3[1]);
                Assert.Same(((PEAssemblySymbol)asm4[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
                Assert.Equal(2, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                var retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
                Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
                Assert.NotSame(asm4[2], asm2[2]);
                Assert.NotSame(asm4[2], asm3[2]);
                Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
                Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm3[2]).Assembly);
                Assert.Equal(3, asm4[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
                Assert.NotSame(asm4[3], asm3[3]);
                Assert.Same(((PEAssemblySymbol)asm4[3]).Assembly, ((PEAssemblySymbol)asm3[3]).Assembly);
                Assert.Equal(3, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                var type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                var retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
                Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
                Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
                Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
                Assert.Equal(4, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

                var type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                var retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
                Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
                Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
                Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

                var retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
                Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                var retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
                Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

            var asm5 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV2MTTestLib3Ref
            });

                Assert.Same(asm5[0], asm1[0]);
                Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

            var asm6 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref
            });

                Assert.Same(asm6[0], asm1[0]);
                Assert.Same(asm6[1], asm1[1]);

            var asm7 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref,
                varV2MTTestLib3Ref,
                varV3MTTestLib4Ref
            });

                Assert.Same(asm7[0], asm1[0]);
                Assert.Same(asm7[1], asm1[1]);
                Assert.NotSame(asm7[2], asm3[3]);
                Assert.NotSame(asm7[2], asm4[3]);
                Assert.NotSame(asm7[3], asm4[4]);

                Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
                Assert.Same(((PEAssemblySymbol)asm7[2]).Assembly, ((PEAssemblySymbol)asm3[3]).Assembly);
                Assert.Equal(2, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

                var type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                var retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval15.Kind);

                var retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval16.Kind);

                var retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
                Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
                Assert.Same(((PEAssemblySymbol)asm7[3]).Assembly, ((PEAssemblySymbol)asm4[4]).Assembly);
                Assert.Equal(3, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

                var type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                var retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval18.Kind);

                var retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval19.Kind);

                var retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval20.Kind);

                var retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
                Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                var retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
                Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());

                // This test shows that simple reordering of references doesn't pick different set of assemblies
            var asm8 = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV3MTTestLib4Ref,
                varV1MTTestLib2Ref,
                varV2MTTestLib3Ref
            });

                Assert.Same(asm8[0], asm1[0]);
                Assert.Same(asm8[0], asm1[0]);
                Assert.Same(asm8[2], asm7[1]);
                Assert.True(asm8[3].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[3]));
                Assert.Same(asm8[3], asm7[2]);
                Assert.True(asm8[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));
                Assert.Same(asm8[1], asm7[3]);

            var asm9 = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV3MTTestLib4Ref
            });

                Assert.Same(asm9[0], asm1[0]);
                Assert.True(asm9[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));

            var asm10 = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                varV1MTTestLib2Ref,
                TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                varV2MTTestLib3Ref,
                varV3MTTestLib4Ref
            });

                Assert.Same(asm10[0], asm1[0]);
                Assert.Same(asm10[1], asm4[1]);
                Assert.Same(asm10[2], asm4[2]);
                Assert.Same(asm10[3], asm4[3]);
                Assert.Same(asm10[4], asm4[4]);

                // Run the same tests again to make sure we didn't corrupt prior state by loading additional assemblies
                Assert.Equal("MTTestLib2", asm1[1].Identity.Name);
                Assert.Equal(1, (from a in asm1[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm1[1].BoundReferences() where ReferenceEquals(a, asm1[0]) select a).Count());
                Assert.Equal(SymbolKind.ErrorType, asm1[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType.Kind);

                Assert.Same(asm2[0], asm1[0]);

                Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
                Assert.NotSame(asm2[1], asm1[1]);
                Assert.Same(((PEAssemblySymbol)asm2[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
                Assert.Equal(2, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where ReferenceEquals(a, asm2[0]) select a).Count());
            Assert.Equal(1, (from a in asm2[1].BoundReferences() where ReferenceEquals(a, asm2[2]) select a).Count());

                retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
                Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
                Assert.Equal(1, asm2[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm2[2].BoundReferences() where ReferenceEquals(a, asm2[0]) select a).Count());

                Assert.Same(asm3[0], asm1[0]);

                Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
                Assert.NotSame(asm3[1], asm1[1]);
                Assert.NotSame(asm3[1], asm2[1]);
                Assert.Same(((PEAssemblySymbol)asm3[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
                Assert.Equal(2, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[1].BoundReferences() where ReferenceEquals(a, asm3[2]) select a).Count());

                retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
                Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
                Assert.NotSame(asm3[2], asm2[2]);
                Assert.NotSame(((PEAssemblySymbol)asm3[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
                Assert.Equal(2, asm3[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[2].BoundReferences() where ReferenceEquals(a, asm3[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
                Assert.Equal(3, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where ReferenceEquals(a, asm3[0]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where ReferenceEquals(a, asm3[1]) select a).Count());
            Assert.Equal(1, (from a in asm3[3].BoundReferences() where ReferenceEquals(a, asm3[2]) select a).Count());

                type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
                Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
                Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

                retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
                Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Same(asm3[0], asm1[0]);

                Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
                Assert.NotSame(asm4[1], asm1[1]);
                Assert.NotSame(asm4[1], asm2[1]);
                Assert.NotSame(asm4[1], asm3[1]);
                Assert.Same(((PEAssemblySymbol)asm4[1]).Assembly, ((PEAssemblySymbol)asm1[1]).Assembly);
                Assert.Equal(2, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[1].BoundReferences() where ReferenceEquals(a, asm4[2]) select a).Count());

                retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
                Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
                Assert.NotSame(asm4[2], asm2[2]);
                Assert.NotSame(asm4[2], asm3[2]);
                Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
                Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm3[2]).Assembly);
                Assert.Equal(3, asm4[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[2].BoundReferences() where ReferenceEquals(a, asm4[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
                Assert.NotSame(asm4[3], asm3[3]);
                Assert.Same(((PEAssemblySymbol)asm4[3]).Assembly, ((PEAssemblySymbol)asm3[3]).Assembly);
                Assert.Equal(3, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[3].BoundReferences() where ReferenceEquals(a, asm4[2]) select a).Count());

                type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
                Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
                Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
                Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
                Assert.Equal(4, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where ReferenceEquals(a, asm4[0]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where ReferenceEquals(a, asm4[1]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where ReferenceEquals(a, asm4[2]) select a).Count());
            Assert.Equal(1, (from a in asm4[4].BoundReferences() where ReferenceEquals(a, asm4[3]) select a).Count());

                type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
                Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
                Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
                Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

                retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
                Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
                Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

                Assert.Same(asm7[0], asm1[0]);
                Assert.Same(asm7[1], asm1[1]);
                Assert.NotSame(asm7[2], asm3[3]);
                Assert.NotSame(asm7[2], asm4[3]);
                Assert.NotSame(asm7[3], asm4[4]);

                Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
                Assert.Same(((PEAssemblySymbol)asm7[2]).Assembly, ((PEAssemblySymbol)asm3[3]).Assembly);
                Assert.Equal(2, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[2].BoundReferences() where ReferenceEquals(a, asm7[1]) select a).Count());

                type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval15.Kind);

                retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval16.Kind);

                retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
                Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
                Assert.Same(((PEAssemblySymbol)asm7[3]).Assembly, ((PEAssemblySymbol)asm4[4]).Assembly);
                Assert.Equal(3, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where ReferenceEquals(a, asm7[0]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where ReferenceEquals(a, asm7[1]) select a).Count());
            Assert.Equal(1, (from a in asm7[3].BoundReferences() where ReferenceEquals(a, asm7[2]) select a).Count());

                type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval18.Kind);

                retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval19.Kind);

                retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal(SymbolKind.ErrorType, retval20.Kind);

                retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
                Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
                Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());
            }

        [Fact]
        public void MultiTargeting2()
        {
            using (MetadataCache.LockAndClean())
            {
                var mscorlibPath = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;

                var varMTTestLib1_V1_Name = new AssemblyIdentity("MTTestLib1", new Version("1.0.0.0"));

                var varC_MTTestLib1_V1 = CreateCompilation(varMTTestLib1_V1_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.V1.MTTestModule1.netmodule
@"
public class Class1
{
}
"
                               },
                               new string[] { mscorlibPath },
                               null);

                var asm_MTTestLib1_V1 = varC_MTTestLib1_V1.SourceAssembly().BoundReferences();

                var varMTTestLib2_Name = new AssemblyIdentity("MTTestLib2");

                var varC_MTTestLib2 = CreateCompilation(varMTTestLib2_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.V1.MTTestModule2.netmodule
@"
public class Class4
{
    Class1 Foo()
    {
        return null;
    }

    public Class1 Bar;

}
"
                               },
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib1_V1 });

                var asm_MTTestLib2 = varC_MTTestLib2.SourceAssembly().BoundReferences();

                Assert.Same(asm_MTTestLib2[0], asm_MTTestLib1_V1[0]);
                Assert.Same(asm_MTTestLib2[1], varC_MTTestLib1_V1.SourceAssembly());

                var c2 = CreateCompilation(new AssemblyIdentity("c2"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib2, varC_MTTestLib1_V1 });

                var asm2 = c2.SourceAssembly().BoundReferences();

                Assert.Same(asm2[0], asm_MTTestLib1_V1[0]);
                Assert.Same(asm2[1], varC_MTTestLib2.SourceAssembly());
                Assert.Same(asm2[2], varC_MTTestLib1_V1.SourceAssembly());

                Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
                Assert.Equal(2, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
                Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

                var retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
                Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
                Assert.Equal(1, asm2[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

                var varMTTestLib1_V2_Name = new AssemblyIdentity("MTTestLib1", new Version("2.0.0.0"));

                var varC_MTTestLib1_V2 = CreateCompilation(varMTTestLib1_V2_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.V2.MTTestModule1.netmodule
@"
public class Class1
{
}

public class Class2
{
}
"
                               },
                               new string[] { mscorlibPath },
                               null);

                var asm_MTTestLib1_V2 = varC_MTTestLib1_V2.SourceAssembly().BoundReferences();

                var varMTTestLib3_Name = new AssemblyIdentity("MTTestLib3");

                var varC_MTTestLib3 = CreateCompilation(varMTTestLib3_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.V2.MTTestModule3.netmodule
@"
public class Class5
{
    Class1 Foo1()
    {
        return null;
    }

    Class2 Foo2()
    {
        return null;
    }

    Class4 Foo3()
    {
        return null;
    }

    Class1 Bar1;
    Class2 Bar2;
    Class4 Bar3;
}
"
                               },
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib2, varC_MTTestLib1_V2 });

                var asm_MTTestLib3 = varC_MTTestLib3.SourceAssembly().BoundReferences();

                Assert.Same(asm_MTTestLib3[0], asm_MTTestLib1_V1[0]);
                Assert.NotSame(asm_MTTestLib3[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm_MTTestLib3[2], varC_MTTestLib1_V1.SourceAssembly());

                var c3 = CreateCompilation(new AssemblyIdentity("c3"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib2, varC_MTTestLib1_V2, varC_MTTestLib3 });

                var asm3 = c3.SourceAssembly().BoundReferences();

                Assert.Same(asm3[0], asm_MTTestLib1_V1[0]);
                Assert.Same(asm3[1], asm_MTTestLib3[1]);
                Assert.Same(asm3[2], asm_MTTestLib3[2]);
                Assert.Same(asm3[3], varC_MTTestLib3.SourceAssembly());

                Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm3[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
                Assert.Equal(2, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                var retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
                Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
                Assert.NotSame(asm3[2], asm2[2]);
                Assert.NotSame(asm3[2].DeclaringCompilation, asm2[2].DeclaringCompilation);
                Assert.Equal(2, asm3[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
                Assert.Equal(3, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                var type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                var retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
                Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
                Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
                Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

                var varMTTestLib1_V3_Name = new AssemblyIdentity("MTTestLib1", new Version("3.0.0.0"));

                var varC_MTTestLib1_V3 = CreateCompilation(varMTTestLib1_V3_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.V3.MTTestModule1.netmodule
@"
public class Class1
{
}

public class Class2
{
}

public class Class3
{
}
"
                               },
                               new string[] { mscorlibPath },
                               null);

                var asm_MTTestLib1_V3 = varC_MTTestLib1_V3.SourceAssembly().BoundReferences();

                var varMTTestLib4_Name = new AssemblyIdentity("MTTestLib4");

                var varC_MTTestLib4 = CreateCompilation(varMTTestLib4_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.V3.MTTestModule4.netmodule
@"
public class Class6
{
    Class1 Foo1()
    {
        return null;
    }

    Class2 Foo2()
    {
        return null;
    }

    Class3 Foo3()
    {
        return null;
    }

    Class4 Foo4()
    {
        return null;
    }

    Class5 Foo5()
    {
        return null;
    }

    Class1 Bar1;
    Class2 Bar2;
    Class3 Bar3;
    Class4 Bar4;
    Class5 Bar5;

}
"
                               },
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib2, varC_MTTestLib1_V3, varC_MTTestLib3 });

                var asm_MTTestLib4 = varC_MTTestLib4.SourceAssembly().BoundReferences();

                Assert.Same(asm_MTTestLib4[0], asm_MTTestLib1_V1[0]);
                Assert.NotSame(asm_MTTestLib4[1], varC_MTTestLib2.SourceAssembly());
                Assert.Same(asm_MTTestLib4[2], varC_MTTestLib1_V3.SourceAssembly());
                Assert.NotSame(asm_MTTestLib4[3], varC_MTTestLib3.SourceAssembly());

                var c4 = CreateCompilation(new AssemblyIdentity("c4"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib2, varC_MTTestLib1_V3, varC_MTTestLib3, varC_MTTestLib4 });

                var asm4 = c4.SourceAssembly().BoundReferences();

                Assert.Same(asm4[0], asm_MTTestLib1_V1[0]);
                Assert.Same(asm4[1], asm_MTTestLib4[1]);
                Assert.Same(asm4[2], asm_MTTestLib4[2]);
                Assert.Same(asm4[3], asm_MTTestLib4[3]);
                Assert.Same(asm4[4], varC_MTTestLib4.SourceAssembly());

                Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
                Assert.NotSame(asm4[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm4[1], asm2[1]);
                Assert.NotSame(asm4[1], asm3[1]);
                Assert.Same(((RetargetingAssemblySymbol)asm4[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
                Assert.Equal(2, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                var retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
                Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
                Assert.NotSame(asm4[2], asm2[2]);
                Assert.NotSame(asm4[2], asm3[2]);
                Assert.NotSame(asm4[2].DeclaringCompilation, asm2[2].DeclaringCompilation);
                Assert.NotSame(asm4[2].DeclaringCompilation, asm3[2].DeclaringCompilation);
                Assert.Equal(3, asm4[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
                Assert.NotSame(asm4[3], asm3[3]);
                Assert.Same(((RetargetingAssemblySymbol)asm4[3]).UnderlyingAssembly, asm3[3]);
                Assert.Equal(3, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                var type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                var retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
                Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
                Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
                Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
                Assert.Equal(4, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

                var type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                var retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
                Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
                Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
                Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

                var retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
                Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                var retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
                Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

                var c5 = CreateCompilation(new AssemblyIdentity("c5"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib3 });

                var asm5 = c5.SourceAssembly().BoundReferences();

                Assert.Same(asm5[0], asm2[0]);
                Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

                var c6 = CreateCompilation(new AssemblyIdentity("c6"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib2 });

                var asm6 = c6.SourceAssembly().BoundReferences();

                Assert.Same(asm6[0], asm2[0]);
                Assert.True(asm6[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));

                var c7 = CreateCompilation(new AssemblyIdentity("c7"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib2, varC_MTTestLib3, varC_MTTestLib4 });

                var asm7 = c7.SourceAssembly().BoundReferences();

                Assert.Same(asm7[0], asm2[0]);
                Assert.True(asm7[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));
                Assert.NotSame(asm7[2], asm3[3]);
                Assert.NotSame(asm7[2], asm4[3]);
                Assert.NotSame(asm7[3], asm4[4]);

                Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm7[2]).UnderlyingAssembly, asm3[3]);
                Assert.Equal(2, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

                var type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                var retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval15.ContainingAssembly.Name);
                Assert.Equal(0, (from a in asm7 where a != null && a.Name == "MTTestLib1" select a).Count());

                var retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval16.ContainingAssembly.Name);

                var retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
                Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm7[3]).UnderlyingAssembly, asm4[4]);
                Assert.Equal(3, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

                var type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                var retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval18.ContainingAssembly.Name);

                var retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval19.ContainingAssembly.Name);

                var retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval20.ContainingAssembly.Name);

                var retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
                Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                var retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
                Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());

                // This test shows that simple reordering of references doesn't pick different set of assemblies
                var c8 = CreateCompilation(new AssemblyIdentity("c8"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib4, varC_MTTestLib2, varC_MTTestLib3 });

                var asm8 = c8.SourceAssembly().BoundReferences();

                Assert.Same(asm8[0], asm2[0]);
                Assert.True(asm8[2].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[1]));
                Assert.Same(asm8[2], asm7[1]);
                Assert.True(asm8[3].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[3]));
                Assert.Same(asm8[3], asm7[2]);
                Assert.True(asm8[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));
                Assert.Same(asm8[1], asm7[3]);

                var c9 = CreateCompilation(new AssemblyIdentity("c9"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib4 });

                var asm9 = c9.SourceAssembly().BoundReferences();

                Assert.Same(asm9[0], asm2[0]);
                Assert.True(asm9[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));

                var c10 = CreateCompilation(new AssemblyIdentity("c10"),
                               null,
                               new string[] { mscorlibPath },
                               new CSharpCompilation[] { varC_MTTestLib2, varC_MTTestLib1_V3, varC_MTTestLib3, varC_MTTestLib4 });

                var asm10 = c10.SourceAssembly().BoundReferences();

                Assert.Same(asm10[0], asm2[0]);
                Assert.Same(asm10[1], asm4[1]);
                Assert.Same(asm10[2], asm4[2]);
                Assert.Same(asm10[3], asm4[3]);
                Assert.Same(asm10[4], asm4[4]);

                // Run the same tests again to make sure we didn't corrupt prior state by loading additional assemblies
                Assert.Same(asm2[0], asm_MTTestLib1_V1[0]);

                Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
                Assert.Equal(2, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
                Assert.Equal(1, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

                retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
                Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
                Assert.Equal(1, asm2[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

                Assert.Same(asm_MTTestLib3[0], asm_MTTestLib1_V1[0]);
                Assert.NotSame(asm_MTTestLib3[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm_MTTestLib3[2], varC_MTTestLib1_V1.SourceAssembly());

                Assert.Same(asm3[0], asm_MTTestLib1_V1[0]);
                Assert.Same(asm3[1], asm_MTTestLib3[1]);
                Assert.Same(asm3[2], asm_MTTestLib3[2]);
                Assert.Same(asm3[3], varC_MTTestLib3.SourceAssembly());

                Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm3[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
                Assert.Equal(2, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(1, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
                Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
                Assert.NotSame(asm3[2], asm2[2]);
                Assert.NotSame(asm3[2].DeclaringCompilation, asm2[2].DeclaringCompilation);
                Assert.Equal(2, asm3[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
                Assert.Equal(3, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
                Assert.Equal(1, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
                Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
                Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

                retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
                Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Same(asm4[0], asm_MTTestLib1_V1[0]);
                Assert.Same(asm4[1], asm_MTTestLib4[1]);
                Assert.Same(asm4[2], asm_MTTestLib4[2]);
                Assert.Same(asm4[3], asm_MTTestLib4[3]);
                Assert.Same(asm4[4], varC_MTTestLib4.SourceAssembly());

                Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
                Assert.NotSame(asm4[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm4[1], asm2[1]);
                Assert.NotSame(asm4[1], asm3[1]);
                Assert.Same(((RetargetingAssemblySymbol)asm4[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
                Assert.Equal(2, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
                Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
                Assert.NotSame(asm4[2], asm2[2]);
                Assert.NotSame(asm4[2], asm3[2]);
                Assert.NotSame(asm4[2].DeclaringCompilation, asm2[2].DeclaringCompilation);
                Assert.NotSame(asm4[2].DeclaringCompilation, asm3[2].DeclaringCompilation);
                Assert.Equal(3, asm4[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
                Assert.NotSame(asm4[3], asm3[3]);
                Assert.Same(((RetargetingAssemblySymbol)asm4[3]).UnderlyingAssembly, asm3[3]);
                Assert.Equal(3, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(1, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
                Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
                Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
                Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
                Assert.Equal(4, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
                Assert.Equal(1, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

                type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
                Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
                Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
                Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

                retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
                Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
                Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

                Assert.Same(asm5[0], asm2[0]);
                Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

                Assert.Same(asm6[0], asm2[0]);
                Assert.True(asm6[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));

                Assert.Same(asm7[0], asm2[0]);
                Assert.True(asm7[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));
                Assert.NotSame(asm7[2], asm3[3]);
                Assert.NotSame(asm7[2], asm4[3]);
                Assert.NotSame(asm7[3], asm4[4]);

                Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm7[2]).UnderlyingAssembly, asm3[3]);
                Assert.Equal(2, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(1, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

                type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval15.ContainingAssembly.Name);
                Assert.Equal(0, (from a in asm7 where a != null && a.Name == "MTTestLib1" select a).Count());

                retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval16.ContainingAssembly.Name);

                retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
                Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm7[3]).UnderlyingAssembly, asm4[4]);
                Assert.Equal(3, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
                Assert.Equal(1, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

                type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval18.ContainingAssembly.Name);

                retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval19.ContainingAssembly.Name);

                retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", retval20.ContainingAssembly.Name);

                retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
                Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
                Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());
            }
        }

        [Fact]
        public void MultiTargeting3()
        {
            using (MetadataCache.LockAndClean())
            {
                var varMTTestLib2_Name = new AssemblyIdentity("MTTestLib2");

                var varC_MTTestLib2 = CreateCompilation(varMTTestLib2_Name, (string[])null,
                               new[] {
                                        TestReferences.NetFx.v4_0_30319.mscorlib,
                                        TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                                        TestReferences.SymbolsTests.V1.MTTestModule2.netmodule
                                     });

                var asm_MTTestLib2 = varC_MTTestLib2.SourceAssembly().BoundReferences();

                var c2 = CreateCompilation(new AssemblyIdentity("c2"),
                               null,
                               new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                                                           new CSharpCompilationReference(varC_MTTestLib2)
                                                       });

                var asm2Prime = c2.SourceAssembly().BoundReferences();
                var asm2 = new AssemblySymbol[] { asm2Prime[0], asm2Prime[2], asm2Prime[1] };

                Assert.Same(asm2[0], asm_MTTestLib2[0]);
                Assert.Same(asm2[1], varC_MTTestLib2.SourceAssembly());
                Assert.Same(asm2[2], asm_MTTestLib2[1]);

                Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
                Assert.Equal(4, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
                Assert.Equal(2, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

                var retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Same(retval1, asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Bar").OfType<FieldSymbol>().Single().Type);

                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
                Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
                Assert.Equal(1, asm2[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

                var varMTTestLib3_Name = new AssemblyIdentity("MTTestLib3");

                var varC_MTTestLib3 = CreateCompilation(varMTTestLib3_Name,
                               null,
                               new MetadataReference[]
                               {
                                    TestReferences.NetFx.v4_0_30319.mscorlib,
                                    TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                                    new CSharpCompilationReference(varC_MTTestLib2),
                                    TestReferences.SymbolsTests.V2.MTTestModule3.netmodule
                               });

                var asm_MTTestLib3Prime = varC_MTTestLib3.SourceAssembly().BoundReferences();
                var asm_MTTestLib3 = new AssemblySymbol[] { asm_MTTestLib3Prime[0], asm_MTTestLib3Prime[2], asm_MTTestLib3Prime[1] };

                Assert.Same(asm_MTTestLib3[0], asm_MTTestLib2[0]);
                Assert.NotSame(asm_MTTestLib3[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm_MTTestLib3[2], asm_MTTestLib2[1]);

                var c3 = CreateCompilation(new AssemblyIdentity("c3"),
                               null,
                               new MetadataReference[]
                               {
                                   TestReferences.NetFx.v4_0_30319.mscorlib,
                                   TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                                   new CSharpCompilationReference(varC_MTTestLib2),
                                   new CSharpCompilationReference(varC_MTTestLib3)
                               });

                var asm3Prime = c3.SourceAssembly().BoundReferences();
                var asm3 = new AssemblySymbol[] { asm3Prime[0], asm3Prime[2], asm3Prime[1], asm3Prime[3] };

                Assert.Same(asm3[0], asm_MTTestLib2[0]);
                Assert.Same(asm3[1], asm_MTTestLib3[1]);
                Assert.Same(asm3[2], asm_MTTestLib3[2]);
                Assert.Same(asm3[3], varC_MTTestLib3.SourceAssembly());

                Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm3[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
                Assert.Equal(4, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(2, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                var retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Same(retval2, asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Bar").OfType<FieldSymbol>().Single().Type);

                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
                Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
                Assert.NotSame(asm3[2], asm2[2]);
                Assert.NotSame(asm3[2], asm2[2]);
                Assert.NotSame(((PEAssemblySymbol)asm3[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
                Assert.Equal(2, asm3[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
                Assert.Equal(6, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
                Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                var type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").Single();

                var retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
                Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
                Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
                Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

                var varMTTestLib4_Name = new AssemblyIdentity("MTTestLib4");

                var varC_MTTestLib4 = CreateCompilation(varMTTestLib4_Name,
                               null,
                               new MetadataReference[]
                                {
                                    TestReferences.NetFx.v4_0_30319.mscorlib,
                                    TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                    new CSharpCompilationReference(varC_MTTestLib2),
                                    new CSharpCompilationReference(varC_MTTestLib3),
                                    TestReferences.SymbolsTests.V3.MTTestModule4.netmodule
                                });

                var asm_MTTestLib4Prime = varC_MTTestLib4.SourceAssembly().BoundReferences();
                var asm_MTTestLib4 = new AssemblySymbol[] { asm_MTTestLib4Prime[0], asm_MTTestLib4Prime[2], asm_MTTestLib4Prime[1], asm_MTTestLib4Prime[3] };

                Assert.Same(asm_MTTestLib4[0], asm_MTTestLib2[0]);
                Assert.NotSame(asm_MTTestLib4[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm_MTTestLib4[2], asm3[2]);
                Assert.NotSame(asm_MTTestLib4[2], asm2[2]);
                Assert.NotSame(asm_MTTestLib4[3], varC_MTTestLib3.SourceAssembly());

                var c4 = CreateCompilation(new AssemblyIdentity("c4"),
                               null,
                               new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                                           new CSharpCompilationReference(varC_MTTestLib2),
                                                           new CSharpCompilationReference(varC_MTTestLib3),
                                                           new CSharpCompilationReference(varC_MTTestLib4)
                                                       });

                var asm4Prime = c4.SourceAssembly().BoundReferences();
                var asm4 = new AssemblySymbol[] { asm4Prime[0], asm4Prime[2], asm4Prime[1], asm4Prime[3], asm4Prime[4] };

                Assert.Same(asm4[0], asm_MTTestLib2[0]);
                Assert.Same(asm4[1], asm_MTTestLib4[1]);
                Assert.Same(asm4[2], asm_MTTestLib4[2]);
                Assert.Same(asm4[3], asm_MTTestLib4[3]);
                Assert.Same(asm4[4], varC_MTTestLib4.SourceAssembly());

                Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
                Assert.NotSame(asm4[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm4[1], asm2[1]);
                Assert.NotSame(asm4[1], asm3[1]);
                Assert.Same(((RetargetingAssemblySymbol)asm4[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
                Assert.Equal(4, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(2, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                var retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
                Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
                Assert.NotSame(asm4[2], asm2[2]);
                Assert.NotSame(asm4[2], asm3[2]);
                Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
                Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm3[2]).Assembly);
                Assert.Equal(3, asm4[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
                Assert.NotSame(asm4[3], asm3[3]);
                Assert.Same(((RetargetingAssemblySymbol)asm4[3]).UnderlyingAssembly, asm3[3]);
                Assert.Equal(6, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                var type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                var retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
                Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
                Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
                Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
                Assert.Equal(8, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
                Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

                var type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                var retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
                Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                var retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
                Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                var retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
                Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

                var retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
                Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                var retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
                Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

                var c5 = CreateCompilation(new AssemblyIdentity("c5"),
                               null,
                               new MetadataReference[] {
                                                            TestReferences.NetFx.v4_0_30319.mscorlib,
                                                            new CSharpCompilationReference(varC_MTTestLib3)
                                                       });

                var asm5 = c5.SourceAssembly().BoundReferences();

                Assert.Same(asm5[0], asm2[0]);
                Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

                var c6 = CreateCompilation(new AssemblyIdentity("c6"),
                               null,
                               new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(varC_MTTestLib2)
                                                       });

                var asm6 = c6.SourceAssembly().BoundReferences();

                Assert.Same(asm6[0], asm2[0]);
                Assert.True(asm6[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));

                var c7 = CreateCompilation(new AssemblyIdentity("c7"),
                               null,
                               new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(varC_MTTestLib2),
                                                           new CSharpCompilationReference(varC_MTTestLib3),
                                                           new CSharpCompilationReference(varC_MTTestLib4)
                                                       });

                var asm7 = c7.SourceAssembly().BoundReferences();

                Assert.Same(asm7[0], asm2[0]);
                Assert.True(asm7[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));
                Assert.NotSame(asm7[2], asm3[3]);
                Assert.NotSame(asm7[2], asm4[3]);
                Assert.NotSame(asm7[3], asm4[4]);

                Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm7[2]).UnderlyingAssembly, asm3[3]);
                Assert.Equal(4, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(2, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

                var type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                var retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                AssemblySymbol missingAssembly;

                missingAssembly = retval15.ContainingAssembly;

                Assert.True(missingAssembly.IsMissing);
                Assert.Equal("MTTestLib1", missingAssembly.Identity.Name);

                var retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Same(missingAssembly, retval16.ContainingAssembly);

                var retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
                Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm7[3]).UnderlyingAssembly, asm4[4]);
                Assert.Equal(6, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
                Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

                var type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                var retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", ((MissingMetadataTypeSymbol)retval18).ContainingAssembly.Identity.Name);

                var retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Same(retval18.ContainingAssembly, retval19.ContainingAssembly);

                var retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Same(retval18.ContainingAssembly, retval20.ContainingAssembly);

                var retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
                Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                var retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
                Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());

                // This test shows that simple reordering of references doesn't pick different set of assemblies
                var c8 = CreateCompilation(new AssemblyIdentity("c8"),
                               null,
                               new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(varC_MTTestLib4),
                                                           new CSharpCompilationReference(varC_MTTestLib2),
                                                           new CSharpCompilationReference(varC_MTTestLib3)
                                                       });

                var asm8 = c8.SourceAssembly().BoundReferences();

                Assert.Same(asm8[0], asm2[0]);
                Assert.True(asm8[2].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[1]));
                Assert.Same(asm8[2], asm7[1]);
                Assert.True(asm8[3].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[3]));
                Assert.Same(asm8[3], asm7[2]);
                Assert.True(asm8[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));
                Assert.Same(asm8[1], asm7[3]);

                var c9 = CreateCompilation(new AssemblyIdentity("c9"),
                               null,
                               new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(varC_MTTestLib4)
                                                       });

                var asm9 = c9.SourceAssembly().BoundReferences();

                Assert.Same(asm9[0], asm2[0]);
                Assert.True(asm9[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm4[4]));

                var c10 = CreateCompilation(new AssemblyIdentity("c10"),
                               null,
                               new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           TestReferences.SymbolsTests.V3.MTTestLib1.dll,
                                                           new CSharpCompilationReference(varC_MTTestLib2),
                                                           new CSharpCompilationReference(varC_MTTestLib3),
                                                           new CSharpCompilationReference(varC_MTTestLib4)
                                                       });

                var asm10Prime = c10.SourceAssembly().BoundReferences();
                var asm10 = new AssemblySymbol[] { asm10Prime[0], asm10Prime[2], asm10Prime[1], asm10Prime[3], asm10Prime[4] };

                Assert.Same(asm10[0], asm2[0]);
                Assert.Same(asm10[1], asm4[1]);
                Assert.Same(asm10[2], asm4[2]);
                Assert.Same(asm10[3], asm4[3]);
                Assert.Same(asm10[4], asm4[4]);

                // Run the same tests again to make sure we didn't corrupt prior state by loading additional assemblies
                Assert.Same(asm2[0], asm_MTTestLib2[0]);

                Assert.Equal("MTTestLib2", asm2[1].Identity.Name);
                Assert.Equal(4, (from a in asm2[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());
                Assert.Equal(2, (from a in asm2[1].BoundReferences() where object.ReferenceEquals(a, asm2[2]) select a).Count());

                retval1 = asm2[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval1.Kind);
                Assert.Same(retval1, asm2[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm2[2].Identity.Name);
                Assert.Equal(1, asm2[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm2[2].BoundReferences() where object.ReferenceEquals(a, asm2[0]) select a).Count());

                Assert.Same(asm_MTTestLib3[0], asm_MTTestLib2[0]);
                Assert.NotSame(asm_MTTestLib3[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm_MTTestLib3[2], asm_MTTestLib2[1]);

                Assert.Same(asm3[0], asm_MTTestLib2[0]);
                Assert.Same(asm3[1], asm_MTTestLib3[1]);
                Assert.Same(asm3[2], asm_MTTestLib3[2]);
                Assert.Same(asm3[3], varC_MTTestLib3.SourceAssembly());

                Assert.Equal("MTTestLib2", asm3[1].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm3[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
                Assert.Equal(4, (from a in asm3[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(2, (from a in asm3[1].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                retval2 = asm3[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval2.Kind);
                Assert.Same(retval2, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm3[2].Identity.Name);
                Assert.NotSame(asm3[2], asm2[2]);
                Assert.NotSame(((PEAssemblySymbol)asm3[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
                Assert.Equal(2, asm3[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm3[2].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm3[3].Identity.Name);
                Assert.Equal(6, (from a in asm3[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[0]) select a).Count());
                Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[1]) select a).Count());
                Assert.Equal(2, (from a in asm3[3].BoundReferences() where object.ReferenceEquals(a, asm3[2]) select a).Count());

                type1 = asm3[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval3 = type1.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval3.Kind);
                Assert.Same(retval3, asm3[2].GlobalNamespace.GetMembers("Class1").Single());

                retval4 = type1.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval4.Kind);
                Assert.Same(retval4, asm3[2].GlobalNamespace.GetMembers("Class2").Single());

                retval5 = type1.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval5.Kind);
                Assert.Same(retval5, asm3[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Same(asm4[0], asm_MTTestLib2[0]);
                Assert.Same(asm4[1], asm_MTTestLib4[1]);
                Assert.Same(asm4[2], asm_MTTestLib4[2]);
                Assert.Same(asm4[3], asm_MTTestLib4[3]);
                Assert.Same(asm4[4], varC_MTTestLib4.SourceAssembly());

                Assert.Equal("MTTestLib2", asm4[1].Identity.Name);
                Assert.NotSame(asm4[1], varC_MTTestLib2.SourceAssembly());
                Assert.NotSame(asm4[1], asm2[1]);
                Assert.NotSame(asm4[1], asm3[1]);
                Assert.Same(((RetargetingAssemblySymbol)asm4[1]).UnderlyingAssembly, varC_MTTestLib2.SourceAssembly());
                Assert.Equal(4, (from a in asm4[1].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(2, (from a in asm4[1].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                retval6 = asm4[1].GlobalNamespace.GetTypeMembers("Class4").
                              Single().
                              GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval6.Kind);
                Assert.Same(retval6, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                Assert.Equal("MTTestLib1", asm4[2].Identity.Name);
                Assert.NotSame(asm4[2], asm2[2]);
                Assert.NotSame(asm4[2], asm3[2]);
                Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm2[2]).Assembly);
                Assert.NotSame(((PEAssemblySymbol)asm4[2]).Assembly, ((PEAssemblySymbol)asm3[2]).Assembly);
                Assert.Equal(3, asm4[2].Identity.Version.Major);
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(1, (from a in asm4[2].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());

                Assert.Equal("MTTestLib3", asm4[3].Identity.Name);
                Assert.NotSame(asm4[3], asm3[3]);
                Assert.Same(((RetargetingAssemblySymbol)asm4[3]).UnderlyingAssembly, asm3[3]);
                Assert.Equal(6, (from a in asm4[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(2, (from a in asm4[3].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());

                type2 = asm4[3].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval7 = type2.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval7.Kind);
                Assert.Same(retval7, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                retval8 = type2.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval8.Kind);
                Assert.Same(retval8, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                retval9 = type2.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval9.Kind);
                Assert.Same(retval9, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm4[4].Identity.Name);
                Assert.Equal(8, (from a in asm4[4].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[0]) select a).Count());
                Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[1]) select a).Count());
                Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[2]) select a).Count());
                Assert.Equal(2, (from a in asm4[4].BoundReferences() where object.ReferenceEquals(a, asm4[3]) select a).Count());

                type3 = asm4[4].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                retval10 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval10.Kind);
                Assert.Same(retval10, asm4[2].GlobalNamespace.GetMembers("Class1").Single());

                retval11 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval11.Kind);
                Assert.Same(retval11, asm4[2].GlobalNamespace.GetMembers("Class2").Single());

                retval12 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval12.Kind);
                Assert.Same(retval12, asm4[2].GlobalNamespace.GetMembers("Class3").Single());

                retval13 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval13.Kind);
                Assert.Same(retval13, asm4[1].GlobalNamespace.GetMembers("Class4").Single());

                retval14 = type3.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval14.Kind);
                Assert.Same(retval14, asm4[3].GlobalNamespace.GetMembers("Class5").Single());

                Assert.Same(asm5[0], asm2[0]);
                Assert.True(asm5[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(asm3[3]));

                Assert.Same(asm6[0], asm2[0]);
                Assert.True(asm6[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));

                Assert.Same(asm7[0], asm2[0]);
                Assert.True(asm7[1].RepresentsTheSameAssemblyButHasUnresolvedReferencesByComparisonTo(varC_MTTestLib2.SourceAssembly()));
                Assert.NotSame(asm7[2], asm3[3]);
                Assert.NotSame(asm7[2], asm4[3]);
                Assert.NotSame(asm7[3], asm4[4]);

                Assert.Equal("MTTestLib3", asm7[2].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm7[2]).UnderlyingAssembly, asm3[3]);
                Assert.Equal(4, (from a in asm7[2].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(2, (from a in asm7[2].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());

                type4 = asm7[2].GlobalNamespace.GetTypeMembers("Class5").
                              Single();

                retval15 = type4.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                missingAssembly = retval15.ContainingAssembly;

                Assert.True(missingAssembly.IsMissing);
                Assert.Equal("MTTestLib1", missingAssembly.Identity.Name);

                retval16 = type4.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Same(missingAssembly, retval16.ContainingAssembly);

                retval17 = type4.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval17.Kind);
                Assert.Same(retval17, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                Assert.Equal("MTTestLib4", asm7[3].Identity.Name);
                Assert.Same(((RetargetingAssemblySymbol)asm7[3]).UnderlyingAssembly, asm4[4]);
                Assert.Equal(6, (from a in asm7[3].BoundReferences() where !a.IsMissing select a).Count());
                Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[0]) select a).Count());
                Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[1]) select a).Count());
                Assert.Equal(2, (from a in asm7[3].BoundReferences() where object.ReferenceEquals(a, asm7[2]) select a).Count());

                type5 = asm7[3].GlobalNamespace.GetTypeMembers("Class6").
                              Single();

                retval18 = type5.GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("MTTestLib1", ((MissingMetadataTypeSymbol)retval18).ContainingAssembly.Identity.Name);

                retval19 = type5.GetMembers("Foo2").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Same(retval18.ContainingAssembly, retval19.ContainingAssembly);

                retval20 = type5.GetMembers("Foo3").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Same(retval18.ContainingAssembly, retval20.ContainingAssembly);

                retval21 = type5.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval21.Kind);
                Assert.Same(retval21, asm7[1].GlobalNamespace.GetMembers("Class4").Single());

                retval22 = type5.GetMembers("Foo5").OfType<MethodSymbol>().Single().ReturnType;

                Assert.NotEqual(SymbolKind.ErrorType, retval22.Kind);
                Assert.Same(retval22, asm7[2].GlobalNamespace.GetMembers("Class5").Single());
            }
        }

        [Fact]
        public void MultiTargeting4()
        {
            using (MetadataCache.LockAndClean())
            {
                var localC1_V1_Name = new AssemblyIdentity("c1", new Version("1.0.0.0"));

                var localC1_V1 = CreateCompilation(localC1_V1_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.MultiTargeting.Source1Module.netmodule
@"
public class C1<T>
{
    public class C2<S>
    {
        public C1<T>.C2<S> Foo()
        {
            return null;
        }
    }
}
"
                               },
                               new[] { TestReferences.NetFx.v4_0_30319.mscorlib });

                var asm1_V1 = localC1_V1.SourceAssembly();

                var localC1_V2_Name = new AssemblyIdentity("c1", new Version("2.0.0.0"));

                var localC1_V2 = CreateCompilation(localC1_V2_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.MultiTargeting.Source1Module.netmodule
@"
public class C1<T>
{
    public class C2<S>
    {
        public C1<T>.C2<S> Foo()
        {
            return null;
        }
    }
}
"
                               },
                               new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

                var asm1_V2 = localC1_V2.SourceAssembly();

                var localC4_V1_Name = new AssemblyIdentity("c4", new Version("1.0.0.0"));

                var localC4_V1 = CreateCompilation(localC4_V1_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.MultiTargeting.Source4Module.netmodule
@"
public class C4
{
}
"
                               },
                               new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

                var asm4_V1 = localC4_V1.SourceAssembly();

                var localC4_V2_Name = new AssemblyIdentity("c4", new Version("2.0.0.0"));

                var localC4_V2 = CreateCompilation(localC4_V2_Name,
                               new string[] {
                    // AssemblyPaths.SymbolsTests.MultiTargeting.Source4Module.netmodule
@"
public class C4
{
}
"
                               },
                               new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

                var asm4_V2 = localC4_V2.SourceAssembly();

                var c7 = CreateCompilation(new AssemblyIdentity("C7"),
                               new string[] {
                    // AssemblyPaths.SymbolsTests.MultiTargeting.Source7Module.netmodule
@"
public class C7
{}

public class C8<T>
{ }
"
                               },
                               new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib });

                var asm7 = c7.SourceAssembly();

                var c3 = CreateCompilation(new AssemblyIdentity("C3"),
                               new string[] {
                    // AssemblyPaths.SymbolsTests.MultiTargeting.Source3Module.netmodule
@"
public class C3
{
    public C1<C3>.C2<C4> Foo()
    {
        return null;
    }

    public static C6<C4> Bar()
    {
        return null;
    }

    public C8<C7> Foo1()
    {
        return null;
    }

    public void Foo2(ref C300[,] x1,
                    out C4 x2,
                    ref C7[] x3,
                    C4 x4 = null)
    {
        x2 = null;
    }

    internal virtual TFoo3 Foo3<TFoo3>() where TFoo3: C4
    {
        return null;
    }

    public C8<C4> Foo4()
    {
        return null;
    }

    public abstract class C301 :
        I1
    {
    }

    internal class C302
    {
    }

}

public class C6<T> where T: new ()
{}

public class C300
{}

public interface I1
{}

namespace ns1
{
    namespace ns2
    {
        public class C303
        {}

    }

    public class C304
    {
        public class C305
        {}

    }

}
"
                               },
                               new MetadataReference[] { TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(localC1_V1),
                                                           new CSharpCompilationReference(localC4_V1),
                                                           new CSharpCompilationReference(c7)
                                                       });

                var asm3 = c3.SourceAssembly();

                var localC3Foo2 = asm3.GlobalNamespace.GetTypeMembers("C3").
                              Single().GetMembers("Foo2").OfType<MethodSymbol>().Single();

                var c5 = CreateCompilation(new AssemblyIdentity("C5"),
                               new string[] {
                    // AssemblyPaths.SymbolsTests.MultiTargeting.Source5Module.netmodule
@"
public class C5 :
    ns1.C304.C305
{}
"
                               },
                               new MetadataReference[] {
                                                           TestReferences.NetFx.v4_0_30319.mscorlib,
                                                           new CSharpCompilationReference(c3),
                                                           new CSharpCompilationReference(localC1_V2),
                                                           new CSharpCompilationReference(localC4_V2),
                                                           new CSharpCompilationReference(c7)
                                                       });

                var asm5 = c5.SourceAssembly().BoundReferences();

                Assert.NotSame(asm5[1], asm3);
                Assert.Same(((RetargetingAssemblySymbol)asm5[1]).UnderlyingAssembly, asm3);
                Assert.Same(asm5[2], asm1_V2);
                Assert.Same(asm5[3], asm4_V2);
                Assert.Same(asm5[4], asm7);

                var type3 = asm5[1].GlobalNamespace.GetTypeMembers("C3").
                              Single();

                var type1 = asm1_V2.GlobalNamespace.GetTypeMembers("C1").
                              Single();

                var type2 = type1.GetTypeMembers("C2").
                              Single();

                var type4 = asm4_V2.GlobalNamespace.GetTypeMembers("C4").
                              Single();

                var retval1 = (NamedTypeSymbol)type3.GetMembers("Foo").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("C1<C3>.C2<C4>", retval1.ToTestDisplayString());

                Assert.Same(retval1.OriginalDefinition, type2);

                var args1 = retval1.ContainingType.TypeArguments.Concat(retval1.TypeArguments);
                var params1 = retval1.ContainingType.TypeParameters.Concat(retval1.TypeParameters);

                Assert.Same(params1[0], type1.TypeParameters[0]);
                Assert.Same(params1[1].OriginalDefinition, type2.TypeParameters[0].OriginalDefinition);

                Assert.Same(args1[0], type3);
                Assert.Same(args1[0].ContainingAssembly, asm5[1]);
                Assert.Same(args1[1], type4);

                var retval2 = retval1.ContainingType;

                Assert.Equal("C1<C3>", retval2.ToTestDisplayString());
                Assert.Same(retval2.OriginalDefinition, type1);

                var bar = type3.GetMembers("Bar").OfType<MethodSymbol>().Single();
                var retval3 = (NamedTypeSymbol)bar.ReturnType;
                var type6 = asm5[1].GlobalNamespace.GetTypeMembers("C6").
                              Single();

                Assert.Equal("C6<C4>", retval3.ToTestDisplayString());

                Assert.Same(retval3.OriginalDefinition, type6);
                Assert.Same(retval3.ContainingAssembly, asm5[1]);

                var args3 = retval3.TypeArguments;
                var params3 = retval3.TypeParameters;

                Assert.Same(params3[0], type6.TypeParameters[0]);
                Assert.Same(params3[0].ContainingAssembly, asm5[1]);
                Assert.Same(args3[0], type4);

                var foo1 = type3.GetMembers("Foo1").OfType<MethodSymbol>().Single();
                var retval4 = foo1.ReturnType;

                Assert.Equal("C8<C7>", retval4.ToTestDisplayString());

                Assert.Same(retval4,
                              asm3.GlobalNamespace.GetTypeMembers("C3").
                              Single().
                              GetMembers("Foo1").OfType<MethodSymbol>().Single().ReturnType);

                var foo1Params = foo1.Parameters;
                Assert.Equal(0, foo1Params.Length);

                var foo2 = type3.GetMembers("Foo2").OfType<MethodSymbol>().Single();
                Assert.NotEqual(localC3Foo2, foo2);
                Assert.Same(localC3Foo2, ((RetargetingMethodSymbol)foo2).UnderlyingMethod);                
                Assert.Equal(1, ((RetargetingMethodSymbol)foo2).Locations.Length);

                var foo2Params = foo2.Parameters;
                Assert.Equal(4, foo2Params.Length);
                Assert.Same(localC3Foo2.Parameters[0], ((RetargetingParameterSymbol)foo2Params[0]).UnderlyingParameter);
                Assert.Same(localC3Foo2.Parameters[1], ((RetargetingParameterSymbol)foo2Params[1]).UnderlyingParameter);
                Assert.Same(localC3Foo2.Parameters[2], ((RetargetingParameterSymbol)foo2Params[2]).UnderlyingParameter);
                Assert.Same(localC3Foo2.Parameters[3], ((RetargetingParameterSymbol)foo2Params[3]).UnderlyingParameter);

                var x1 = foo2Params[0];
                var x2 = foo2Params[1];
                var x3 = foo2Params[2];
                var x4 = foo2Params[3];

                Assert.Equal("x1", x1.Name);
                Assert.NotEqual(localC3Foo2.Parameters[0].Type, x1.Type);
                Assert.Equal(localC3Foo2.Parameters[0].ToTestDisplayString(), x1.ToTestDisplayString());
                Assert.Same(asm5[1], x1.ContainingAssembly);
                Assert.Same(foo2, x1.ContainingSymbol);
                Assert.False(x1.HasExplicitDefaultValue);
                Assert.False(x1.IsOptional);
                Assert.Equal(RefKind.Ref, x1.RefKind);
                Assert.Equal(2, ((ArrayTypeSymbol)x1.Type).Rank);

                Assert.Equal("x2", x2.Name);
                Assert.NotEqual(localC3Foo2.Parameters[1].Type, x2.Type);
                Assert.Equal(RefKind.Out, x2.RefKind);

                Assert.Equal("x3", x3.Name);
                Assert.Same(localC3Foo2.Parameters[2].Type, x3.Type);

                Assert.Equal("x4", x4.Name);
                Assert.True(x4.HasExplicitDefaultValue);
                Assert.True(x4.IsOptional);

                Assert.Equal("Foo2", foo2.Name);
                Assert.Equal(localC3Foo2.ToTestDisplayString(), foo2.ToTestDisplayString());
                Assert.Same(asm5[1], foo2.ContainingAssembly);
                Assert.Same(type3, foo2.ContainingSymbol);
                Assert.Equal(Accessibility.Public, foo2.DeclaredAccessibility);
                Assert.False(foo2.HidesBaseMethodsByName);
                Assert.False(foo2.IsAbstract);
                Assert.False(foo2.IsExtern);
                Assert.False(foo2.IsGenericMethod);
                Assert.False(foo2.IsOverride);
                Assert.False(foo2.IsSealed);
                Assert.False(foo2.IsStatic);
                Assert.False(foo2.IsVararg);
                Assert.False(foo2.IsVirtual);
                Assert.True(foo2.ReturnsVoid);
                Assert.Equal(0, foo2.TypeParameters.Length);
                Assert.Equal(0, foo2.TypeArguments.Length);

                Assert.True(bar.IsStatic);
                Assert.False(bar.ReturnsVoid);

                var foo3 = type3.GetMembers("Foo3").OfType<MethodSymbol>().Single();

                Assert.Equal(Accessibility.Internal, foo3.DeclaredAccessibility);
                Assert.True(foo3.IsGenericMethod);
                Assert.True(foo3.IsVirtual);

                var foo3TypeParams = foo3.TypeParameters;
                Assert.Equal(1, foo3TypeParams.Length);
                Assert.Equal(1, foo3.TypeArguments.Length);
                Assert.Same(foo3TypeParams[0], foo3.TypeArguments[0]);

                var typeC301 = type3.GetTypeMembers("C301").Single();
                var typeC302 = type3.GetTypeMembers("C302").Single();
                var typeC6 = asm5[1].GlobalNamespace.GetTypeMembers("C6").Single();

                Assert.Equal(typeC301.ToTestDisplayString(),
                    asm3.GlobalNamespace.GetTypeMembers("C3").Single().
                            GetTypeMembers("C301").Single().ToTestDisplayString());

                Assert.Equal(typeC6.ToTestDisplayString(),
                    asm3.GlobalNamespace.GetTypeMembers("C6").Single().ToTestDisplayString());

                Assert.Equal(typeC301.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat),
                    asm3.GlobalNamespace.GetTypeMembers("C3").Single().
                            GetTypeMembers("C301").Single().ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));

                Assert.Equal(typeC6.ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat),
                    asm3.GlobalNamespace.GetTypeMembers("C6").Single().ToDisplayString(SymbolDisplayFormat.QualifiedNameArityFormat));

                Assert.Equal(type3.GetMembers().Length,
                    asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetMembers().Length);

                Assert.Equal(type3.GetTypeMembers().Length,
                    asm3.GlobalNamespace.GetTypeMembers("C3").Single().GetTypeMembers().Length);

                Assert.Same(typeC301, type3.GetTypeMembers("C301", 0).Single());

                Assert.Equal(0, type3.Arity);
                Assert.Equal(1, typeC6.Arity);

                Assert.NotNull(type3.BaseType);
                Assert.Equal("System.Object", type3.BaseType.ToTestDisplayString());

                Assert.Equal(Accessibility.Public, type3.DeclaredAccessibility);
                Assert.Equal(Accessibility.Internal, typeC302.DeclaredAccessibility);

                Assert.Equal(0, type3.Interfaces.Length);
                Assert.Equal(1, typeC301.Interfaces.Length);
                Assert.Equal("I1", typeC301.Interfaces.Single().Name);

                Assert.False(type3.IsAbstract);
                Assert.True(typeC301.IsAbstract);

                Assert.False(type3.IsSealed);
                Assert.False(type3.IsStatic);

                Assert.Equal(0, type3.TypeArguments.Length);
                Assert.Equal(0, type3.TypeParameters.Length);

                var localC6Params = typeC6.TypeParameters;
                Assert.Equal(1, localC6Params.Length);
                Assert.Equal(1, typeC6.TypeArguments.Length);
                Assert.Same(localC6Params[0], typeC6.TypeArguments[0]);

                Assert.Same(((RetargetingNamedTypeSymbol)type3).UnderlyingNamedType,
                    asm3.GlobalNamespace.GetTypeMembers("C3").Single());
                Assert.Equal(1, ((RetargetingNamedTypeSymbol)type3).Locations.Length);                                  

                Assert.Equal(TypeKind.Class, type3.TypeKind);
                Assert.Equal(TypeKind.Interface, asm5[1].GlobalNamespace.GetTypeMembers("I1").Single().TypeKind);

                var localC6_T = localC6Params[0];
                var foo3TypeParam = foo3TypeParams[0];

                Assert.Equal(0, localC6_T.ConstraintTypes.Length);

                Assert.Equal(1, foo3TypeParam.ConstraintTypes.Length);
                Assert.Same(type4, foo3TypeParam.ConstraintTypes.Single());

                Assert.Same(typeC6, localC6_T.ContainingSymbol);
                Assert.False(foo3TypeParam.HasConstructorConstraint);

                Assert.True(localC6_T.HasConstructorConstraint);

                Assert.False(foo3TypeParam.HasReferenceTypeConstraint);
                Assert.False(foo3TypeParam.HasValueTypeConstraint);

                Assert.Equal("TFoo3", foo3TypeParam.Name);
                Assert.Equal("T", localC6_T.Name);

                Assert.Equal(0, foo3TypeParam.Ordinal);
                Assert.Equal(0, localC6_T.Ordinal);

                Assert.Equal(VarianceKind.None, foo3TypeParam.Variance);
                Assert.Same(((RetargetingTypeParameterSymbol)localC6_T).UnderlyingTypeParameter,
                    asm3.GlobalNamespace.GetTypeMembers("C6").Single().TypeParameters[0]);

                var ns1 = asm5[1].GlobalNamespace.GetMembers("ns1").OfType<NamespaceSymbol>().Single();
                var ns2 = ns1.GetMembers("ns2").OfType<NamespaceSymbol>().Single();

                Assert.Equal("ns1.ns2", ns2.ToTestDisplayString());
                Assert.Equal(2, ns1.GetMembers().Length);

                Assert.Equal(1, ns1.GetTypeMembers().Length);
                Assert.Same(ns1.GetTypeMembers("C304").Single(), ns1.GetTypeMembers("C304", 0).Single());

                Assert.Same(asm5[1].Modules[0], asm5[1].Modules[0].GlobalNamespace.ContainingSymbol);
                Assert.Same(asm5[1].Modules[0].GlobalNamespace, ns1.ContainingSymbol);
                Assert.Same(asm5[1].Modules[0], ns1.Extent.Module);
                Assert.Equal(1, ns1.ConstituentNamespaces.Length);
                Assert.Same(ns1, ns1.ConstituentNamespaces[0]);
                Assert.False(ns1.IsGlobalNamespace);
                Assert.True(asm5[1].Modules[0].GlobalNamespace.IsGlobalNamespace);

                Assert.Same(asm3.Modules[0].GlobalNamespace,
                    ((RetargetingNamespaceSymbol)asm5[1].Modules[0].GlobalNamespace).UnderlyingNamespace);
                Assert.Same(asm3.Modules[0].GlobalNamespace.GetMembers("ns1").Single(),
                    ((RetargetingNamespaceSymbol)ns1).UnderlyingNamespace);

                var module3 = (RetargetingModuleSymbol)asm5[1].Modules[0];

                Assert.Equal("C3.dll", module3.ToTestDisplayString());
                Assert.Equal("C3.dll", module3.Name);

                Assert.Same(asm5[1], module3.ContainingSymbol);
                Assert.Same(asm5[1], module3.ContainingAssembly);
                Assert.Null(module3.ContainingType);

                var retval5 = type3.GetMembers("Foo4").OfType<MethodSymbol>().Single().ReturnType;

                Assert.Equal("C8<C4>", retval5.ToTestDisplayString());

                var typeC5 = c5.Assembly.GlobalNamespace.GetTypeMembers("C5").Single();

                Assert.Same(asm5[1], typeC5.BaseType.ContainingAssembly);
                Assert.Equal("ns1.C304.C305", typeC5.BaseType.ToTestDisplayString());
                Assert.NotEqual(SymbolKind.ErrorType, typeC5.Kind);
            }
        }

        [Fact]
        public void MultiTargeting5()
        {
                var c1_Name = new AssemblyIdentity("c1");

                var text = @"
class Module1
{
    Class4 M1()
    {}

    Class4.Class4_1 M2()
    {}

    Class4 M3()
    {}
}
";
                var tree = Parse(text);

            var c1 = CreateCompilationWithMscorlib(tree, new MetadataReference[]
            {
                TestReferences.SymbolsTests.V1.MTTestLib1.dll,
                TestReferences.SymbolsTests.V1.MTTestModule2.netmodule
            });

                var c2_Name = new AssemblyIdentity("MTTestLib2");

            var c2 = CreateCompilation(c2_Name, null, new MetadataReference[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                TestReferences.SymbolsTests.V2.MTTestLib1.dll,
                new CSharpCompilationReference(c1)
            });

                SourceAssemblySymbol c1AsmSource = (SourceAssemblySymbol)c1.Assembly;
                PEAssemblySymbol Lib1_V1 = (PEAssemblySymbol)c1AsmSource.Modules[0].GetReferencedAssemblySymbols()[1];
                PEModuleSymbol module1 = (PEModuleSymbol)c1AsmSource.Modules[1];

                Assert.Equal(LocationKind.MetadataFile, ((MetadataLocation ) Lib1_V1.Locations[0]).Kind);                
                SourceAssemblySymbol c2AsmSource = (SourceAssemblySymbol)c2.Assembly;
                RetargetingAssemblySymbol c1AsmRef = (RetargetingAssemblySymbol)c2AsmSource.Modules[0].GetReferencedAssemblySymbols()[2];
                PEAssemblySymbol Lib1_V2 = (PEAssemblySymbol)c2AsmSource.Modules[0].GetReferencedAssemblySymbols()[1];
                PEModuleSymbol module2 = (PEModuleSymbol)c1AsmRef.Modules[1];

                Assert.Equal(1, Lib1_V1.Identity.Version.Major);
                Assert.Equal(2, Lib1_V2.Identity.Version.Major);

                Assert.NotEqual(module1, module2);
                Assert.Same(module1.Module, module2.Module);

                NamedTypeSymbol classModule1 = c1AsmRef.Modules[0].GlobalNamespace.GetTypeMembers("Module1").Single();
                MethodSymbol m1 = classModule1.GetMembers("M1").OfType<MethodSymbol>().Single();
                MethodSymbol m2 = classModule1.GetMembers("M2").OfType<MethodSymbol>().Single();
                MethodSymbol m3 = classModule1.GetMembers("M3").OfType<MethodSymbol>().Single();

                Assert.Same(module2, m1.ReturnType.ContainingModule);
                Assert.Same(module2, m2.ReturnType.ContainingModule);
                Assert.Same(module2, m3.ReturnType.ContainingModule);
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

            var mscorlibRef = new MetadataFileReference(mscorlibPath);
            var module1Ref = new MetadataFileReference(module1Path, MetadataImageKind.Module);

            using (var @lock = MetadataCache.LockAndClean())
            {
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

        // Very simplistic test if a compilation has a single type with the given full name. Does NOT handle generics.
        private bool HasSingleTypeOfKind(CSharpCompilation c, TypeKind kind, string fullName)
        {
            string[] names = fullName.Split('.');

            NamespaceOrTypeSymbol current = c.GlobalNamespace;
            foreach (string name in names)
            {
                var matchingSym = current.GetMembers(name);
                if (matchingSym.Length != 1)
                {
                    return false;
                }

                current = (NamespaceOrTypeSymbol)matchingSym.First();
            }

            return current is TypeSymbol && ((TypeSymbol)current).TypeKind == kind;
        }

        [Fact]
        public void AddRemoveReferences()
        {
            var mscorlibRef = TestReferences.NetFx.v4_0_30319.mscorlib;
            var systemCoreRef = TestReferences.NetFx.v4_0_30319.System_Core;
            var systemRef = TestReferences.NetFx.v4_0_30319.System;

            CSharpCompilation c = CSharpCompilation.Create("Test");
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Struct, "System.Int32"));
            c = c.AddReferences(mscorlibRef);
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Struct, "System.Int32"));
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"));
            c = c.AddReferences(systemCoreRef);
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"));
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"));
            c = c.ReplaceReference(systemCoreRef, systemRef);
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Linq.Enumerable"));
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"));
            c = c.RemoveReferences(systemRef);
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Class, "System.Uri"));
            Assert.True(HasSingleTypeOfKind(c, TypeKind.Struct, "System.Int32"));
            c = c.RemoveReferences(mscorlibRef);
            Assert.False(HasSingleTypeOfKind(c, TypeKind.Struct, "System.Int32"));
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
            var result = CSharpCompilation.Create(name, references: CreateMetadataReferences(dependencies));
            var asm = result.Assembly;
            GC.KeepAlive(asm);
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private ObjectReference CreateWeakCompilation(string name, params object[] dependencies)
        {
            // this compilation should only be reachable via the WeakReference we return
            var result = CSharpCompilation.Create(name, references: CreateMetadataReferences(dependencies));

            var asm = result.Assembly;
            GC.KeepAlive(asm);
            return new ObjectReference(result);
        }

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void CompactRetargetingCache1()
        {
            var mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;
            var V1MTTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path;
            var V2MTTestLib1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path;

            // Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
            var c1 = CSharpCompilation.Create("Test1", references: CreateMetadataReferences(
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

        [Fact]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void CompactRetargetingCache2()
        {
            var mscorlib2 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).Path;
            var mscorlib3 = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib).Path;
            var libV1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V1.MTTestLib1).Path;
            var libV2 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.V2.MTTestLib1).Path;

            // Note that metadata references have to be created outside of this method body to ensure that the objects are not kept alive by method temp variables:
            var c1 = CSharpCompilation.Create("Test1", references: CreateMetadataReferences(
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

            object assembly = mdTestLib1.Metadata.GetTarget().Assembly;
            Assert.NotNull(assembly);
            return new ObjectReference(assembly);
        }

        [Fact]
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
            }
        }

        [Fact]
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
                Assert.NotNull(mdTestLib1.Metadata.GetTarget().Assembly);
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
            }

            GC.KeepAlive(c1);
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

        [Fact]
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
            }
        }

        /// <summary>
        /// Compilation A depends on C Version 1.0.0.0, C Version 2.0.0.0, and B. B depends on C Version 2.0.0.0.
        /// Checks that the ReferenceManager compares the identities correctly and doesn't throw "Two metadata references found with the same identity" exception.
        /// </summary>
        [Fact]
        public void ReferencesVersioning()
        {
            var dir1 = Temp.CreateDirectory();
            var dir2 = Temp.CreateDirectory();
            var dir3 = Temp.CreateDirectory();
            var file1 = dir1.CreateFile("C.dll").WriteAllBytes(TestResources.SymbolsTests.General.C1);
            var file2 = dir2.CreateFile("C.dll").WriteAllBytes(TestResources.SymbolsTests.General.C2);
            var file3 = dir3.CreateFile("main.dll");

            var b = CreateCompilationWithMscorlib(
                @"public class B { public static int Main() { return C.Main(); } }",
                assemblyName: "b",
                references: new[] { new MetadataImageReference(TestResources.SymbolsTests.General.C2.AsImmutableOrNull()) },
                compOptions: TestOptions.Dll);

            using (MemoryStream output = new MemoryStream())
            {
                var emitResult = b.Emit(output);
                Assert.True(emitResult.Success);
                file3.WriteAllBytes(output.ToArray());
            }

            var a = CreateCompilationWithMscorlib(
                @"class A { public static void Main() { B.Main(); } }",
                assemblyName: "a",
                references: new[] { new MetadataFileReference(file1.Path), new MetadataFileReference(file2.Path), new MetadataFileReference(file3.Path) },
                compOptions: TestOptions.Dll);

            using (var stream = new MemoryStream())
            {
                a.Emit(stream);
            }
        }

        private sealed class Resolver : TestFileResolver
        {
            private readonly string data, core, system;

            public Resolver(string data, string core, string system)
            {
                this.data = data;
                this.core = core;
                this.system = system;
            }

            public override string ResolveAssemblyName(string assemblyName)
            {
                switch (assemblyName)
                {
                    case "System.Data":
                        return data;

                    case "System.Core":
                        return core;

                    case "System":
                        return system;

                    default:
                        return null;
                }
            }
        }

        [Fact]
        public void CompilationWithReferenceDirectives()
        {
            var data = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.System_Data).Path;
            var core = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.System_Core).Path;
            var system = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.System).Path;

            var trees = new[] {
                SyntaxFactory.ParseSyntaxTree(@"
#r ""System.Data""
#r """ + typeof(System.Xml.Serialization.IXmlSerializable).Assembly.Location + @"""
#r """ + typeof(System.Linq.Expressions.Expression).Assembly.Location + @"""
", options: TestOptions.Script),
                SyntaxFactory.ParseSyntaxTree(@"
#r ""System""
", options: TestOptions.Script),
            SyntaxFactory.ParseSyntaxTree(@"
new System.Data.DataSet();
System.Linq.Expressions.Expression.Constant(123);
System.Diagnostics.Process.GetCurrentProcess();
", options: TestOptions.Script)
            };

            var compilation = CSharpCompilation.Create("foo",
                syntaxTrees: trees,
                references: new[] { MscorlibRef },
                options: TestOptions.Dll.WithFileResolver(new Resolver(data, core, system)));

            var boundRefs = compilation.Assembly.BoundReferences();

            Assert.Equal(5, boundRefs.Length);
            Assert.NotNull(boundRefs.FirstOrDefault(sym => sym.Name == "mscorlib"));
            Assert.NotNull(boundRefs.FirstOrDefault(sym => sym.Name == "System"));
            Assert.NotNull(boundRefs.FirstOrDefault(sym => sym.Name == "System.Core"));
            Assert.NotNull(boundRefs.FirstOrDefault(sym => sym.Name == "System.Data"));
            Assert.NotNull(boundRefs.FirstOrDefault(sym => sym.Name == "System.Xml"));

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void CompilationWithReferenceDirectives_Errors()
        {
            var data = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.System_Data).Path;
            var core = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.System_Core).Path;
            var system = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_30319.System).Path;
            var mscorlibRef = new MetadataFileReference(typeof(object).Assembly.Location);

            var trees = new[] {
                SyntaxFactory.ParseSyntaxTree(@"
#r System
#r ""~!@#$%^&*():\?/""
#r ""non-existing-reference""
", options: TestOptions.Script),
                SyntaxFactory.ParseSyntaxTree(@"
#r ""System.Core""
")
            };

            var compilation = CSharpCompilation.Create("foo",
                syntaxTrees: trees,
                references: new[] { mscorlibRef },
                options: TestOptions.Dll.WithFileResolver(new Resolver(data, core, system)));

            compilation.VerifyDiagnostics(
                // (3,1): error CS0006: Metadata file '~!@#$%^&*():\?/' could not be found
                Diagnostic(ErrorCode.ERR_NoMetadataFile, @"#r ""~!@#$%^&*():\?/""").WithArguments(@"~!@#$%^&*():\?/"),
                // (4,1): error CS0006: Metadata file 'non-existing-reference' could not be found
                Diagnostic(ErrorCode.ERR_NoMetadataFile, @"#r ""non-existing-reference""").WithArguments("non-existing-reference"),
                // (2,4): error CS7010: Quoted file name expected
                Diagnostic(ErrorCode.ERR_ExpectedPPFile, "System"),
                // (2,1): error CS7011: #r is only allowed in scripts
                Diagnostic(ErrorCode.ERR_ReferenceDirectiveOnlyAllowedInScripts, @"#r ""System.Core"""));
        }

        private static readonly string ResolvedPath = Path.GetPathRoot(Environment.CurrentDirectory) + "RESOLVED";

        private class DummyFileProvider : MetadataReferenceProvider
        {
            readonly string targetDll;

            public DummyFileProvider(string targetDll)
            {
                this.targetDll = targetDll;
            }

            public override PortableExecutableReference GetReference(string fullPath, MetadataReferenceProperties properties = default(MetadataReferenceProperties))
            {
                var path = fullPath == ResolvedPath ? targetDll : fullPath;
                return new MetadataFileReference(path, properties);
            }

            public override void ClearCache()
            {
            }
        }

        private class DummyRelativePathResolver : TestFileResolver
        {
            public override string ResolveMetadataFile(string path, string basePath)
            {
                return path.EndsWith("-resolve") ? ResolvedPath : path;
            }
        }

        [Fact]
        public void MetadataReferenceProvider()
        {
            var csClasses01 = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).Path;
            var csInterfaces01 = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01).Path;

            var provider = new DummyFileProvider(csClasses01);
            var resolver = new DummyRelativePathResolver();

            var source = @"
#r """ + typeof(object).Assembly.Location + @"""
#r """ + "!@#$%^/&*-resolve" + @"""
#r """ + csInterfaces01 + @"""
class C : Metadata.ICSPropImpl { }";

            var compilation = CSharpCompilation.Create("foo",
                syntaxTrees: new[] 
                {
                    Parse(source, options: TestOptions.Script) 
                },
                options: TestOptions.Dll.WithFileResolver(resolver).WithMetadataReferenceProvider(provider));

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void CompilationWithReferenceDirective_RelativeToBaseDirectory()
        {
            string path = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).Path;
            string fileName = Path.GetFileName(path);
            string dir = Path.GetDirectoryName(path);

            var trees = new[] {
                SyntaxFactory.ParseSyntaxTree(@"
#r "".\" + fileName + @"""
", Path.Combine(dir, "a.csx"), options: TestOptions.Script),
            };

            var compilation = CSharpCompilation.Create("foo",
                syntaxTrees: trees,
                references: new[] { MscorlibRef });

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void CompilationWithReferenceDirective_RelativeToBaseParent()
        {
            string path = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).Path;
            string fileName = Path.GetFileName(path);
            string dir = Path.Combine(Path.GetDirectoryName(path), "subdir");

            var trees = new[] {
                SyntaxFactory.ParseSyntaxTree(@"
#r ""..\" + fileName + @"""
", Path.Combine(dir, "a.csx"), options: TestOptions.Script),
            };

            var compilation = CSharpCompilation.Create("foo",
                syntaxTrees: trees,
                references: new[] { MscorlibRef });

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void CompilationWithReferenceDirective_RelativeToBaseRoot()
        {
            string path = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).Path;
            string root = Path.GetPathRoot(path);
            string unrooted = path.Substring(root.Length);

            string dir = Path.Combine(root, "foo", "bar", "baz");

            var trees = new[] {
                SyntaxFactory.ParseSyntaxTree(@"
#r ""\" + unrooted + @"""
", Path.Combine(dir, "a.csx"), options: TestOptions.Script),
            };

            var compilation = CSharpCompilation.Create("foo",
                syntaxTrees: trees,
                references: new[] { MscorlibRef });

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void GlobalUsings1()
        {
            var trees = new[] {
                SyntaxFactory.ParseSyntaxTree(@"
WriteLine(1);
Console.WriteLine(2);
", options: TestOptions.Script),
                SyntaxFactory.ParseSyntaxTree(@"
class C 
{ 
    void Foo() { Console.WriteLine(3); }
}
")
            };

            var compilation = CSharpCompilation.Create(
                "foo",
                options: TestOptions.Dll.WithUsings(ImmutableArray.Create<string>("System.Console", "System")),
                syntaxTrees: trees,
                references: new[] { MscorlibRef });

            var diagnostics = compilation.GetDiagnostics().ToArray();

            // global usings are only visible in script code:
            DiagnosticsUtils.VerifyErrorCodes(diagnostics,
                // (4,18): error CS0103: The name 'Console' does not exist in the current context
                new ErrorDescription() { Code = (int)ErrorCode.ERR_NameNotInContext, Line = 4, Column = 18 });
        }

        [Fact]
        public void GlobalUsings_Errors()
        {
            var trees = new[] {
                SyntaxFactory.ParseSyntaxTree(@"
WriteLine(1);
Console.WriteLine(2);
", options: TestOptions.Script)
            };

            var compilation = CSharpCompilation.Create(
                "foo",
                options: TestOptions.Dll.WithUsings("System.Console!", "Blah"),
                syntaxTrees: trees,
                references: new[] { MscorlibRef });

            compilation.VerifyDiagnostics(
                // error CS0234: The type or namespace name 'Console!' does not exist in the namespace 'System' (are you missing an assembly reference?)
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS).WithArguments("Console!", "System"),
                // error CS0246: The type or namespace name 'Blah' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("Blah"),
                // (2,1): error CS0103: The name 'WriteLine' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "WriteLine").WithArguments("WriteLine"),
                // (3,1): error CS0103: The name 'Console' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Console").WithArguments("Console"));
        }

        [Fact]
        public void ReferenceToAssemblyWithSpecialCharactersInName()
        {
            var r = TestReferences.SymbolsTests.Metadata.InvalidCharactersInAssemblyName;

            var st = SyntaxFactory.ParseSyntaxTree("class C { static void Main() { new lib.Class1(); } }");
            var compilation = CSharpCompilation.Create("foo", references: new[] { MscorlibRef, r }, syntaxTrees: new[] { st });
            var diags = compilation.GetDiagnostics().ToArray();
            Assert.Equal(0, diags.Length);

            using (var stream = new MemoryStream())
            {
                compilation.Emit(stream);
            }
        }

        [Fact]
        public void SyntaxTreeOrderConstruct()
        {
            var tree1 = CreateSyntaxTree("A");
            var tree2 = CreateSyntaxTree("B");

            SyntaxTree[] treeOrder1 = new[] { tree1, tree2 };
            var compilation1 = CSharpCompilation.Create("Compilation1", syntaxTrees: treeOrder1);
            CheckCompilationSyntaxTrees(compilation1, treeOrder1);

            SyntaxTree[] treeOrder2 = new[] { tree2, tree1 };
            var compilation2 = CSharpCompilation.Create("Compilation2", syntaxTrees: treeOrder2);
            CheckCompilationSyntaxTrees(compilation2, treeOrder2);
        }

        [Fact]
        public void SyntaxTreeOrderAdd()
        {
            var tree1 = CreateSyntaxTree("A");
            var tree2 = CreateSyntaxTree("B");
            var tree3 = CreateSyntaxTree("C");
            var tree4 = CreateSyntaxTree("D");

            SyntaxTree[] treeList1 = new[] { tree1, tree2 };
            var compilation1 = CSharpCompilation.Create("Compilation1", syntaxTrees: treeList1);
            CheckCompilationSyntaxTrees(compilation1, treeList1);

            SyntaxTree[] treeList2 = new[] { tree3, tree4 };
            var compilation2 = compilation1.AddSyntaxTrees(treeList2);
            CheckCompilationSyntaxTrees(compilation1, treeList1); //compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, treeList1.Concat(treeList2).ToArray());

            SyntaxTree[] treeList3 = new[] { tree4, tree3 };
            var compilation3 = CSharpCompilation.Create("Compilation3", syntaxTrees: treeList3);
            CheckCompilationSyntaxTrees(compilation3, treeList3);

            SyntaxTree[] treeList4 = new[] { tree2, tree1 };
            var compilation4 = compilation3.AddSyntaxTrees(treeList4);
            CheckCompilationSyntaxTrees(compilation3, treeList3); //compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, treeList3.Concat(treeList4).ToArray());
        }

        [Fact]
        public void SyntaxTreeOrderRemove()
        {
            var tree1 = CreateSyntaxTree("A");
            var tree2 = CreateSyntaxTree("B");
            var tree3 = CreateSyntaxTree("C");
            var tree4 = CreateSyntaxTree("D");

            SyntaxTree[] treeList1 = new[] { tree1, tree2, tree3, tree4 };
            var compilation1 = CSharpCompilation.Create("Compilation1", syntaxTrees: treeList1);
            CheckCompilationSyntaxTrees(compilation1, treeList1);

            SyntaxTree[] treeList2 = new[] { tree3, tree1 };
            var compilation2 = compilation1.RemoveSyntaxTrees(treeList2);
            CheckCompilationSyntaxTrees(compilation1, treeList1); //compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, tree2, tree4);

            SyntaxTree[] treeList3 = new[] { tree4, tree3, tree2, tree1 };
            var compilation3 = CSharpCompilation.Create("Compilation3", syntaxTrees: treeList3);
            CheckCompilationSyntaxTrees(compilation3, treeList3);

            SyntaxTree[] treeList4 = new[] { tree3, tree1 };
            var compilation4 = compilation3.RemoveSyntaxTrees(treeList4);
            CheckCompilationSyntaxTrees(compilation3, treeList3); //compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, tree4, tree2);
        }

        [Fact]
        public void SyntaxTreeOrderReplace()
        {
            var tree1 = CreateSyntaxTree("A");
            var tree2 = CreateSyntaxTree("B");
            var tree3 = CreateSyntaxTree("C");

            SyntaxTree[] treeList1 = new[] { tree1, tree2 };
            var compilation1 = CSharpCompilation.Create("Compilation1", syntaxTrees: treeList1);
            CheckCompilationSyntaxTrees(compilation1, treeList1);

            var compilation2 = compilation1.ReplaceSyntaxTree(tree1, tree3);
            CheckCompilationSyntaxTrees(compilation1, treeList1); //compilation1 untouched
            CheckCompilationSyntaxTrees(compilation2, tree3, tree2);

            SyntaxTree[] treeList3 = new[] { tree2, tree1 };
            var compilation3 = CSharpCompilation.Create("Compilation3", syntaxTrees: treeList3);
            CheckCompilationSyntaxTrees(compilation3, treeList3);

            var compilation4 = compilation3.ReplaceSyntaxTree(tree1, tree3);
            CheckCompilationSyntaxTrees(compilation3, treeList3); //compilation3 untouched
            CheckCompilationSyntaxTrees(compilation4, tree2, tree3);
        }

        [WorkItem(578706)]
        [Fact]
        public void DeclaringCompilationOfAddedModule()
        {
            var source1 = "public class C1 { }";
            var source2 = "public class C2 { }";

            var lib1 = CreateCompilationWithMscorlib(source1, assemblyName: "Lib1", compOptions: TestOptions.NetModule);
            var ref1 = lib1.EmitToImageReference(); // NOTE: can't use a compilation reference for a module.

            var lib2 = CreateCompilationWithMscorlib(source2, new[] { ref1 }, assemblyName: "Lib2");
            lib2.VerifyDiagnostics();

            var sourceAssembly = lib2.Assembly;
            var sourceModule = sourceAssembly.Modules[0];
            var sourceType = sourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C2");

            Assert.IsType<SourceAssemblySymbol>(sourceAssembly);
            Assert.Equal(lib2, sourceAssembly.DeclaringCompilation);

            Assert.IsType<SourceModuleSymbol>(sourceModule);
            Assert.Equal(lib2, sourceModule.DeclaringCompilation);

            Assert.IsType<SourceNamedTypeSymbol>(sourceType);
            Assert.Equal(lib2, sourceType.DeclaringCompilation);


            var addedModule = sourceAssembly.Modules[1];
            var addedModuleAssembly = addedModule.ContainingAssembly;
            var addedModuleType = addedModule.GlobalNamespace.GetMember<NamedTypeSymbol>("C1");

            Assert.IsType<SourceAssemblySymbol>(addedModuleAssembly);
            Assert.Equal(lib2, addedModuleAssembly.DeclaringCompilation); //NB: not lib1, not null

            Assert.IsType<PEModuleSymbol>(addedModule);
            Assert.Null(addedModule.DeclaringCompilation);

            Assert.IsAssignableFrom<PENamedTypeSymbol>(addedModuleType);
            Assert.Null(addedModuleType.DeclaringCompilation);
        }

        private static SyntaxTree CreateSyntaxTree(string className)
        {
            var text = string.Format("public partial class {0} {{ }}", className);
            var path = string.Format("{0}.cs", className);
            return SyntaxFactory.ParseSyntaxTree(text, path);
        }

        private static void CheckCompilationSyntaxTrees(CSharpCompilation compilation, params SyntaxTree[] expectedSyntaxTrees)
        {
            ImmutableArray<SyntaxTree> actualSyntaxTrees = compilation.SyntaxTrees;

            int numTrees = expectedSyntaxTrees.Length;

            Assert.Equal(numTrees, actualSyntaxTrees.Length);
            for (int i = 0; i < numTrees; i++)
            {
                Assert.Equal(expectedSyntaxTrees[i], actualSyntaxTrees[i]);
            }

            for (int i = 0; i < numTrees; i++)
            {
                for (int j = 0; j < numTrees; j++)
                {
                    Assert.Equal(Math.Sign(compilation.CompareSyntaxTreeOrdering(expectedSyntaxTrees[i], expectedSyntaxTrees[j])), Math.Sign(i.CompareTo(j)));
                }
            }

            var types = expectedSyntaxTrees.Select(tree => compilation.GetSemanticModel(tree).GetDeclaredSymbol(tree.GetCompilationUnitRoot().Members.Single())).ToArray();
            for (int i = 0; i < numTrees; i++)
            {
                for (int j = 0; j < numTrees; j++)
                {
                    Assert.Equal(Math.Sign(compilation.CompareSourceLocations(types[i].Locations[0], types[j].Locations[0])), Math.Sign(i.CompareTo(j)));
                }
            }
        }
    }
}
