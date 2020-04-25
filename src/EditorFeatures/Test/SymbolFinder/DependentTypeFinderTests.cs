// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.RemoteHost;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public enum TestHost
    {
        InProcess,
        OutOfProcess,
    }

    [UseExportProvider]
    public class DependentTypeFinderTests : TestBase
    {
        private static Solution AddProjectWithMetadataReferences(Solution solution, string projectName, string languageName, string code, MetadataReference metadataReference, params ProjectId[] projectReferences)
        {
            var suffix = languageName == LanguageNames.CSharp ? "cs" : "vb";
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);
            var pi = ProjectInfo.Create(
                pid,
                VersionStamp.Default,
                projectName,
                projectName,
                languageName,
                metadataReferences: new[] { metadataReference },
                projectReferences: projectReferences.Select(p => new ProjectReference(p)));
            return solution.AddProject(pi).AddDocument(did, $"{projectName}.{suffix}", SourceText.From(code));
        }

        private static TestWorkspace GetWorkspace(TestHost host)
        {
            var workspace = TestWorkspace.CreateWorkspace(XElement.Parse("<Workspace></Workspace>"));

            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(
                workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, host == TestHost.OutOfProcess)));

            return workspace;
        }

        [Theory, CombinatorialData, WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        public async Task ImmediatelyDerivedTypes_CSharp(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class DerivedClass : BaseClass { }
}
", MscorlibRef, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [Theory, CombinatorialData, WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        public async Task ImmediatelyDerivedInterfaces_CSharp(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public interface BaseInterface { }
}
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public interface DerivedInterface : BaseInterface { }
}
", MscorlibRef, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseInterface");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedInterface");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.Interfaces[0]);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await SymbolFinder.FindDerivedInterfacesAsync(baseClassSymbol, solution, transitive: false);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        private static Project GetPortableProject(Solution solution)
        {
            return solution.Projects.Single(p => p.Name == "PortableProject");
        }

        [Theory, CombinatorialData]
        public async Task ImmediatelyDerivedTypes_CSharp_AliasedNames(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
using Alias1 = N.BaseClass;

namespace M
{
    using Alias2 = Alias1;

    public class DerivedClass : Alias2 { }
}
", MscorlibRef, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [Theory, CombinatorialData, WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        public async Task ImmediatelyDerivedTypes_CSharp_PortableProfile7(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class DerivedClass : BaseClass { }
}
", SystemRuntimePP7Ref, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [Theory, CombinatorialData, WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        public async Task ImmediatelyDerivedTypes_VisualBasic(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public MustInherit Class BaseClass
    End Class
End Namespace
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class DerivedClass
        Inherits BaseClass
    End Class
End Namespace
", MscorlibRef, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [Theory, CombinatorialData, WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        public async Task ImmediatelyDerivedTypes_CrossLanguage(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an abstract base class
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type derived from the portable abstract base
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class DerivedClass
        Inherits BaseClass
    End Class
End Namespace
", MscorlibRef, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

            // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
            var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
            var derivedDependentType = derivedFromBase.Single();
            Assert.Equal(derivedClassSymbol, derivedDependentType);
        }

        [Theory, CombinatorialData, WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        public async Task ImmediatelyDerivedAndImplementingInterfaces_CSharp(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an interface
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public interface IBaseInterface { }
}
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type implementing that interface
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class ImplementingClass : IBaseInterface { }
}
", MscorlibRef, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseInterfaceSymbol, implementingClassSymbol.Interfaces.Single());

            // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
            var typesThatImplementInterface = await SymbolFinder.FindImplementationsAsync(baseInterfaceSymbol, solution, transitive: false);
            Assert.Equal(implementingClassSymbol, typesThatImplementInterface.Single());
        }

        [Theory, CombinatorialData, WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        public async Task ImmediatelyDerivedInterfaces_VisualBasic(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an interface
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public Interface IBaseInterface
    End Interface
End Namespace
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type implementing that interface
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class ImplementingClass
        Implements IBaseInterface
    End Class
End Namespace
", MscorlibRef, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseInterfaceSymbol, implementingClassSymbol.Interfaces.Single());

            // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
            var typesThatImplementInterface = await SymbolFinder.FindImplementationsAsync(baseInterfaceSymbol, solution, transitive: false);
            Assert.Equal(implementingClassSymbol, typesThatImplementInterface.Single());
        }

        [Theory, CombinatorialData, WorkItem(4973, "https://github.com/dotnet/roslyn/issues/4973")]
        public async Task ImmediatelyDerivedAndImplementingInterfaces_CrossLanguage(TestHost host)
        {
            using var workspace = GetWorkspace(host);
            var solution = workspace.CurrentSolution;

            // create portable assembly with an interface
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public Interface IBaseInterface
    End Interface
End Namespace
", MscorlibRefPortable);

            var portableProject = GetPortableProject(solution);

            // create a normal assembly with a type implementing that interface
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class ImplementingClass : IBaseInterface { }
}
", MscorlibRef, portableProject.Id);

            // get symbols for types
            var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
            var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

            var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
            var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

            // verify that the symbols are different (due to retargeting)
            Assert.NotEqual(baseInterfaceSymbol, implementingClassSymbol.Interfaces.Single());

            // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
            var typesThatImplementInterface = await SymbolFinder.FindImplementationsAsync(baseInterfaceSymbol, solution, transitive: false);
            Assert.Equal(implementingClassSymbol, typesThatImplementInterface.Single());
        }
    }
}
