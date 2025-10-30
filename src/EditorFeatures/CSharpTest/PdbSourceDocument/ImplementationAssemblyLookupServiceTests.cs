// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PdbSourceDocument;

public sealed class ImplementationAssemblyLookupServiceTests : AbstractPdbSourceDocumentTests
{
    [Fact]
    public Task Net6SdkLayout_InvalidXml()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            public class C
            {
                // A change
                public event System.EventHandler [|E|] { add { } remove { } }
            }
            """, out var metadataSource, out var expectedSpan);

            var packDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "ref", "net6.0")).FullName;
            var dataDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "data")).FullName;
            var sharedDir = Directory.CreateDirectory(Path.Combine(path, "shared", "MyPack", "1.0")).FullName;

            // Create reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(packDir, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

            // Compile implementation assembly
            CompileTestSource(sharedDir, sourceText, project, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Create FrameworkList.xml
            File.WriteAllText(Path.Combine(dataDir, "FrameworkList.xml"), """
                FileList FrameworkName="MyPack">
                """);

            var workspace = (EditorTestWorkspace)project.Solution.Workspace;
            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            Assert.False(service.TryFindImplementationAssemblyPath(GetDllPath(packDir), out var implementationDll));
        });

    [Fact]
    public Task Net6SdkLayout()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            public class C
            {
                // A change
                public event System.EventHandler [|E|] { add { } remove { } }
            }
            """, out var metadataSource, out var expectedSpan);

            var packDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "ref", "net6.0")).FullName;
            var dataDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "data")).FullName;
            var sharedDir = Directory.CreateDirectory(Path.Combine(path, "shared", "MyPack", "1.0")).FullName;

            // Compile reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(packDir, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

            // Compile implementation assembly
            CompileTestSource(sharedDir, sourceText, project, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Create FrameworkList.xml
            File.WriteAllText(Path.Combine(dataDir, "FrameworkList.xml"), """
                <FileList FrameworkName="MyPack">
                </FileList>
                """);

            var workspace = (EditorTestWorkspace)project.Solution.Workspace;
            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            Assert.True(service.TryFindImplementationAssemblyPath(GetDllPath(packDir), out var implementationDll));
            Assert.Equal(GetDllPath(sharedDir), implementationDll);
        });

    [Fact]
    public Task Net6SdkLayout_PacksInPath()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            public class C
            {
                // A change
                public event System.EventHandler [|E|] { add { } remove { } }
            }
            """, out var metadataSource, out var expectedSpan);

            path = Path.Combine(path, "packs", "installed", "here");

            var packDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "ref", "net6.0")).FullName;
            var dataDir = Directory.CreateDirectory(Path.Combine(path, "packs", "MyPack.Ref", "1.0", "data")).FullName;
            var sharedDir = Directory.CreateDirectory(Path.Combine(path, "shared", "MyPack", "1.0")).FullName;

            // Compile reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(packDir, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

            // Compile implementation assembly
            CompileTestSource(sharedDir, sourceText, project, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Create FrameworkList.xml
            File.WriteAllText(Path.Combine(dataDir, "FrameworkList.xml"), """
                <FileList FrameworkName="MyPack">
                </FileList>
                """);

            var workspace = (EditorTestWorkspace)project.Solution.Workspace;
            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            Assert.True(service.TryFindImplementationAssemblyPath(GetDllPath(packDir), out var implementationDll));
            Assert.Equal(GetDllPath(sharedDir), implementationDll);
        });

    [Fact]
    public Task FollowTypeForwards()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            public class C
            {
                // A change
                public event System.EventHandler [|E|] { add { } remove { } }
            }
            """, out var metadataSource, out var expectedSpan);

            var sourceText = SourceText.From(metadataSource, Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

            var workspace = EditorTestWorkspace.Create($"""
                <Workspace>
                    <Project Language="{LanguageNames.CSharp}" CommonReferences="true" ReferencesOnDisk="true">
                    </Project>
                </Workspace>
                """, composition: GetTestComposition());

            var implProject = workspace.CurrentSolution.Projects.First();

            // Compile implementation assembly
            var dllFilePath = Path.Combine(path, "implementation.dll");
            var sourceCodePath = Path.Combine(path, "implementation.cs");
            var pdbFilePath = Path.Combine(path, "implementation.pdb");
            var assemblyName = "implementation";

            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Compile type forwarding implementation DLL
            var typeForwardDllFilePath = Path.Combine(path, "typeforward.dll");
            assemblyName = "typeforward";

            implProject = implProject.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));
            sourceText = SourceText.From("""
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(C))]
            """, Encoding.UTF8);
            CompileTestSource(typeForwardDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            Assert.Equal(dllFilePath, service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger()));
        });

    [Fact]
    public Task FollowTypeForwards_Namespace()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            namespace A
            {
                namespace B
                {
                    public class C
                    {
                        public class D
                        {
                            // A change
                            public event System.EventHandler [|E|] { add { } remove { } }
                        }
                    }
                }
            }
            """, out var metadataSource, out var expectedSpan);

            // Compile reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("A.B.C.D.E"), buildReferenceAssembly: true);

            // Compile implementation assembly to a different DLL
            var dllFilePath = Path.Combine(path, "implementation.dll");
            var sourceCodePath = Path.Combine(path, "implementation.cs");
            var pdbFilePath = Path.Combine(path, "implementation.pdb");
            var assemblyName = "implementation";

            var workspace = EditorTestWorkspace.Create($"""
                <Workspace>
                    <Project Language="{LanguageNames.CSharp}" CommonReferences="true" ReferencesOnDisk="true">
                    </Project>
                </Workspace>
                """, composition: GetTestComposition());

            var implProject = workspace.CurrentSolution.Projects.First();
            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Compile type forwarding implementation DLL
            var typeForwardDllFilePath = Path.Combine(path, "typeforward.dll");
            assemblyName = "typeforward";

            implProject = implProject.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));
            sourceText = SourceText.From("""
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(A.B.C))]
            """, Encoding.UTF8);
            CompileTestSource(typeForwardDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            var foundImplementationFilePath = service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger());
            Assert.Equal(dllFilePath, foundImplementationFilePath);
        });

    [Fact]
    public Task FollowTypeForwards_Generics()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            namespace A
            {
                namespace B
                {
                    public class C<T>
                    {
                        public class D
                        {
                            // A change
                            public event System.EventHandler [|E|] { add { } remove { } }
                        }
                    }
                }
            }
            """, out var metadataSource, out var expectedSpan);

            // Compile reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("A.B.C.D.E"), buildReferenceAssembly: true);

            // Compile implementation assembly to a different DLL
            var dllFilePath = Path.Combine(path, "implementation.dll");
            var sourceCodePath = Path.Combine(path, "implementation.cs");
            var pdbFilePath = Path.Combine(path, "implementation.pdb");
            var assemblyName = "implementation";

            var workspace = EditorTestWorkspace.Create($"""
                <Workspace>
                    <Project Language="{LanguageNames.CSharp}" CommonReferences="true" ReferencesOnDisk="true">
                    </Project>
                </Workspace>
                """, composition: GetTestComposition());

            var implProject = workspace.CurrentSolution.Projects.First();
            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Compile type forwarding implementation DLL
            var typeForwardDllFilePath = Path.Combine(path, "typeforward.dll");
            assemblyName = "typeforward";

            implProject = implProject.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));
            sourceText = SourceText.From("""
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(A.B.C<>))]
            """, Encoding.UTF8);
            CompileTestSource(typeForwardDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            var foundImplementationFilePath = service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger());
            Assert.Equal(dllFilePath, foundImplementationFilePath);
        });

    [Fact]
    public Task FollowTypeForwards_NestedType()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            public class C
            {
                public class D
                {
                    // A change
                    public event System.EventHandler [|E|] { add { } remove { } }
                }
            }
            """, out var metadataSource, out var expectedSpan);

            // Compile reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.D.E"), buildReferenceAssembly: true);

            // Compile implementation assembly to a different DLL
            var dllFilePath = Path.Combine(path, "implementation.dll");
            var sourceCodePath = Path.Combine(path, "implementation.cs");
            var pdbFilePath = Path.Combine(path, "implementation.pdb");
            var assemblyName = "implementation";

            var workspace = EditorTestWorkspace.Create($"""
                <Workspace>
                    <Project Language="{LanguageNames.CSharp}" CommonReferences="true" ReferencesOnDisk="true">
                    </Project>
                </Workspace>
                """, composition: GetTestComposition());

            var implProject = workspace.CurrentSolution.Projects.First();
            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Compile type forwarding implementation DLL
            var typeForwardDllFilePath = Path.Combine(path, "typeforward.dll");
            assemblyName = "typeforward";

            implProject = implProject.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));
            sourceText = SourceText.From("""
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(C))]
            """, Encoding.UTF8);
            CompileTestSource(typeForwardDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            Assert.Equal(dllFilePath, service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger()));
        });

    [Fact]
    public Task FollowTypeForwards_Cache()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            public class C
            {
                // A change
                public event System.EventHandler [|E|] { add { } remove { } }
            }
            """, out var metadataSource, out var expectedSpan);

            // Compile reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

            // Compile implementation assembly to a different DLL
            var dllFilePath = Path.Combine(path, "implementation.dll");
            var sourceCodePath = Path.Combine(path, "implementation.cs");
            var pdbFilePath = Path.Combine(path, "implementation.pdb");
            var assemblyName = "implementation";

            var workspace = EditorTestWorkspace.Create($"""
                <Workspace>
                    <Project Language="{LanguageNames.CSharp}" CommonReferences="true" ReferencesOnDisk="true">
                    </Project>
                </Workspace>
                """, composition: GetTestComposition());

            var implProject = workspace.CurrentSolution.Projects.First();
            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Compile type forwarding implementation DLL
            var typeForwardDllFilePath = Path.Combine(path, "typeforward.dll");
            assemblyName = "typeforward";

            implProject = implProject.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));
            sourceText = SourceText.From("""
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(C))]
            """, Encoding.UTF8);
            CompileTestSource(typeForwardDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            Assert.Equal(dllFilePath, service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger()));

            // We need the DLLs to exist, in order for some checks to pass correct, but to ensure
            // that the file isn't read, we just zero it out.
            File.WriteAllBytes(typeForwardDllFilePath, []);
            File.WriteAllBytes(dllFilePath, []);

            Assert.Equal(dllFilePath, service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger()));
        });

    [Fact]
    public Task FollowTypeForwards_MultipleTypes_Cache()
        => RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            public class C
            {
                // A change
                public event System.EventHandler [|E|] { add { } remove { } }
            }

            public class D { }
            public class E { }
            public class F { }
            """, out var metadataSource, out var expectedSpan);

            // Compile reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

            // Compile implementation assembly to a different DLL
            var dllFilePath = Path.Combine(path, "implementation.dll");
            var sourceCodePath = Path.Combine(path, "implementation.cs");
            var pdbFilePath = Path.Combine(path, "implementation.pdb");
            var assemblyName = "implementation";

            var workspace = EditorTestWorkspace.Create($"""
                <Workspace>
                    <Project Language="{LanguageNames.CSharp}" CommonReferences="true" ReferencesOnDisk="true">
                    </Project>
                </Workspace>
                """, composition: GetTestComposition());

            var implProject = workspace.CurrentSolution.Projects.First();
            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Compile type forwarding implementation DLL
            var typeForwardDllFilePath = Path.Combine(path, "typeforward.dll");
            assemblyName = "typeforward";

            implProject = implProject.AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));
            sourceText = SourceText.From("""
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(C))]
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(D))]
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(E))]
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(F))]
            """, Encoding.UTF8);
            CompileTestSource(typeForwardDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            Assert.Equal(dllFilePath, service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger()));

            // We need the DLLs to exist, in order for some checks to pass correct, but to ensure
            // that the file isn't read, we just zero it out.
            File.WriteAllBytes(typeForwardDllFilePath, []);
            File.WriteAllBytes(dllFilePath, []);

            Assert.Equal(dllFilePath, service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger()));
        });

    [Fact]
    public async Task FollowTypeForwards_MultipleHops_Cache()
    {
        var typeForwardSource = """
            [assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(C))]
            """;

        await RunTestAsync(async path =>
        {
            MarkupTestFile.GetSpan("""
            public class C
            {
                // A change
                public event System.EventHandler [|E|] { add { } remove { } }
            }
            """, out var metadataSource, out var expectedSpan);

            // Compile reference assembly
            var sourceText = SourceText.From(metadataSource, encoding: Encoding.UTF8);
            var (project, symbol) = await CompileAndFindSymbolAsync(path, Location.Embedded, Location.Embedded, sourceText, c => c.GetMember("C.E"), buildReferenceAssembly: true);

            // Compile implementation assembly to a different DLL
            var dllFilePath = Path.Combine(path, "implementation.dll");
            var sourceCodePath = Path.Combine(path, "implementation.cs");
            var pdbFilePath = Path.Combine(path, "implementation.pdb");
            var assemblyName = "implementation";

            var workspace = EditorTestWorkspace.Create($"""
                <Workspace>
                    <Project Language="{LanguageNames.CSharp}" CommonReferences="true" ReferencesOnDisk="true">
                    </Project>
                </Workspace>
                """, composition: GetTestComposition());

            var implProject = workspace.CurrentSolution.Projects.First();
            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Compile type forwarding implementation DLL
            var typeForwardDllFilePath = Path.Combine(path, "typeforward.dll");
            assemblyName = "typeforward";

            implProject = workspace.CurrentSolution.Projects.First().AddMetadataReference(MetadataReference.CreateFromFile(dllFilePath));
            var typeForwardSourceText = SourceText.From(typeForwardSource, Encoding.UTF8);
            CompileTestSource(typeForwardDllFilePath, sourceCodePath, pdbFilePath, assemblyName, typeForwardSourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Now compile a new implementation in realimplementation.dll
            var realImplementationDllFilePath = Path.Combine(path, "realimplementation.dll");
            assemblyName = "realimplementation";

            implProject = workspace.CurrentSolution.Projects.First();
            CompileTestSource(realImplementationDllFilePath, sourceCodePath, pdbFilePath, assemblyName, sourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            // Now compile a new implementation.dll that typeforwards to realimplementation.dll
            assemblyName = "implementation";

            implProject = workspace.CurrentSolution.Projects.First().AddMetadataReference(MetadataReference.CreateFromFile(realImplementationDllFilePath));
            CompileTestSource(dllFilePath, sourceCodePath, pdbFilePath, assemblyName, typeForwardSourceText, implProject, Location.Embedded, Location.Embedded, buildReferenceAssembly: false, windowsPdb: false);

            var service = workspace.GetService<IImplementationAssemblyLookupService>();

            var foundImplementationFilePath = service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger());
            Assert.Equal(realImplementationDllFilePath, foundImplementationFilePath);

            // We need the DLLs to exist, in order for some checks to pass correct, but to ensure
            // that the file isn't read, we just zero it out.
            File.WriteAllBytes(typeForwardDllFilePath, []);
            File.WriteAllBytes(realImplementationDllFilePath, []);
            File.WriteAllBytes(dllFilePath, []);

            foundImplementationFilePath = service.FollowTypeForwards(symbol, typeForwardDllFilePath, new NoDuplicatesLogger());
            Assert.Equal(realImplementationDllFilePath, foundImplementationFilePath);
        });
    }

    /// <summary>
    /// Test logger that ensures we don't log the same message twice.
    /// </summary>
    private sealed class NoDuplicatesLogger : IPdbSourceDocumentLogger
    {
        private readonly HashSet<string> _logs = [];
        public void Clear()
        {
            _logs.Clear();
        }

        public void Log(string message)
        {
            Assert.True(_logs.Add(message));
        }
    }
}
