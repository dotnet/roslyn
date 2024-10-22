// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public class SymbolFinderTests : TestBase
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
            metadataReferences: [metadataReference],
            projectReferences: projectReferences.Select(p => new ProjectReference(p)));
        return solution.AddProject(pi).AddDocument(did, $"{projectName}.{suffix}", SourceText.From(code));
    }

    private static TestWorkspace CreateWorkspace(TestHost host)
    {
        var composition = EditorTestCompositions.EditorFeatures.WithTestHostParts(host);
        return TestWorkspace.CreateWorkspace(XElement.Parse("<Workspace></Workspace>"), composition: composition);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/4973")]
    public async Task ImmediatelyDerivedTypes_CSharp(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an abstract base class
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", Net40.References.mscorlib);

        var portableProject = GetPortableProject(solution);

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class DerivedClass : BaseClass { }
}
", Net461.References.mscorlib, portableProject.Id);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

        // verify that the symbols are different (due to retargeting)
        Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

        // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
        var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
        var derivedDependentType = Assert.Single(derivedFromBase);
        Assert.Equal(derivedClassSymbol, derivedDependentType);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/4973")]
    public async Task ImmediatelyDerivedInterfaces_CSharp(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an abstract base class
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public interface BaseInterface { }
}
", Net40.References.mscorlib);

        var portableProject = GetPortableProject(solution);

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public interface DerivedInterface : BaseInterface { }
}
", Net461.References.mscorlib, portableProject.Id);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseInterface");

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedInterface");

        // verify that the symbols are different (due to retargeting)
        Assert.NotEqual(baseClassSymbol, derivedClassSymbol.Interfaces[0]);

        // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
        var derivedFromBase = await SymbolFinder.FindDerivedInterfacesAsync(baseClassSymbol, solution, transitive: false);
        var derivedDependentType = Assert.Single(derivedFromBase);
        Assert.Equal(derivedClassSymbol, derivedDependentType);
    }

    private static Project GetPortableProject(Solution solution)
        => solution.Projects.Single(p => p.Name == "PortableProject");

    private static Project GetNormalProject(Solution solution)
        => solution.Projects.Single(p => p.Name == "NormalProject");

    [Theory, CombinatorialData]
    public async Task ImmediatelyDerivedTypes_CSharp_AliasedNames(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an abstract base class
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", Net40.References.mscorlib);

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
", Net461.References.mscorlib, portableProject.Id);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

        // verify that the symbols are different (due to retargeting)
        Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

        // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
        var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
        var derivedDependentType = Assert.Single(derivedFromBase);
        Assert.Equal(derivedClassSymbol, derivedDependentType);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/4973")]
    public async Task ImmediatelyDerivedTypes_CSharp_PortableProfile7(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an abstract base class
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", Net40.References.mscorlib);

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
        var derivedDependentType = Assert.Single(derivedFromBase);
        Assert.Equal(derivedClassSymbol, derivedDependentType);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/4973")]
    public async Task ImmediatelyDerivedTypes_VisualBasic(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an abstract base class
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public MustInherit Class BaseClass
    End Class
End Namespace
", Net40.References.mscorlib);

        var portableProject = GetPortableProject(solution);

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class DerivedClass
        Inherits BaseClass
    End Class
End Namespace
", Net461.References.mscorlib, portableProject.Id);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

        // verify that the symbols are different (due to retargeting)
        Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

        // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
        var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
        var derivedDependentType = Assert.Single(derivedFromBase);
        Assert.Equal(derivedClassSymbol, derivedDependentType);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/4973")]
    public async Task ImmediatelyDerivedTypes_CrossLanguage(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an abstract base class
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public abstract class BaseClass { }
}
", Net40.References.mscorlib);

        var portableProject = GetPortableProject(solution);

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class DerivedClass
        Inherits BaseClass
    End Class
End Namespace
", Net40.References.mscorlib, portableProject.Id);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseClassSymbol = portableCompilation.GetTypeByMetadataName("N.BaseClass");

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var derivedClassSymbol = normalCompilation.GetTypeByMetadataName("M.DerivedClass");

        // verify that the symbols are different (due to retargeting)
        Assert.NotEqual(baseClassSymbol, derivedClassSymbol.BaseType);

        // verify that the dependent types of `N.BaseClass` correctly resolve to `M.DerivedCLass`
        var derivedFromBase = await SymbolFinder.FindDerivedClassesAsync(baseClassSymbol, solution, transitive: false);
        var derivedDependentType = Assert.Single(derivedFromBase);
        Assert.Equal(derivedClassSymbol, derivedDependentType);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/4973")]
    public async Task ImmediatelyDerivedAndImplementingInterfaces_CSharp(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an interface
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public interface IBaseInterface { }
}
", Net40.References.mscorlib);

        var portableProject = GetPortableProject(solution);

        // create a normal assembly with a type implementing that interface
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class ImplementingClass : IBaseInterface { }
}
", Net461.References.mscorlib, portableProject.Id);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

        // verify that the symbols are different (due to retargeting)
        Assert.NotEqual(baseInterfaceSymbol, Assert.Single(implementingClassSymbol.Interfaces));

        // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
        var typesThatImplementInterface = await SymbolFinder.FindImplementationsAsync(baseInterfaceSymbol, solution, transitive: false);
        Assert.Equal(implementingClassSymbol, Assert.Single(typesThatImplementInterface));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/4973")]
    public async Task ImmediatelyDerivedInterfaces_VisualBasic(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an interface
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public Interface IBaseInterface
    End Interface
End Namespace
", Net40.References.mscorlib);

        var portableProject = GetPortableProject(solution);

        // create a normal assembly with a type implementing that interface
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.VisualBasic, @"
Imports N
Namespace M
    Public Class ImplementingClass
        Implements IBaseInterface
    End Class
End Namespace
", Net461.References.mscorlib, portableProject.Id);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

        // verify that the symbols are different (due to retargeting)
        Assert.NotEqual(baseInterfaceSymbol, Assert.Single(implementingClassSymbol.Interfaces));

        // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
        var typesThatImplementInterface = await SymbolFinder.FindImplementationsAsync(baseInterfaceSymbol, solution, transitive: false);
        Assert.Equal(implementingClassSymbol, Assert.Single(typesThatImplementInterface));
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/4973")]
    public async Task ImmediatelyDerivedAndImplementingInterfaces_CrossLanguage(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an interface
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.VisualBasic, @"
Namespace N
    Public Interface IBaseInterface
    End Interface
End Namespace
", Net40.References.mscorlib);

        var portableProject = GetPortableProject(solution);

        // create a normal assembly with a type implementing that interface
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class ImplementingClass : IBaseInterface { }
}
", Net40.References.mscorlib, portableProject.Id);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.IBaseInterface");

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var implementingClassSymbol = normalCompilation.GetTypeByMetadataName("M.ImplementingClass");

        // verify that the symbols are different (due to retargeting)
        Assert.NotEqual(baseInterfaceSymbol, Assert.Single(implementingClassSymbol.Interfaces));

        // verify that the implementing types of `N.IBaseInterface` correctly resolve to `M.ImplementingClass`
        var typesThatImplementInterface = await SymbolFinder.FindImplementationsAsync(baseInterfaceSymbol, solution, transitive: false);
        Assert.Equal(implementingClassSymbol, Assert.Single(typesThatImplementInterface));
    }

    [Theory, CombinatorialData]
    public async Task DerivedMetadataClasses(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"", Net40.References.mscorlib);

        // get symbols for types
        var compilation = await GetNormalProject(solution).GetCompilationAsync();
        var rootType = compilation.GetTypeByMetadataName("System.IO.Stream");

        Assert.NotNull(rootType);

        var immediateDerived = await SymbolFinder.FindDerivedClassesAsync(
            rootType, solution, transitive: false);

        Assert.NotEmpty(immediateDerived);
        Assert.True(immediateDerived.All(d => d.BaseType.Equals(rootType)));

        var transitiveDerived = await SymbolFinder.FindDerivedClassesAsync(
            rootType, solution, transitive: true);

        Assert.NotEmpty(transitiveDerived);
        Assert.True(transitiveDerived.All(d => d.GetBaseTypes().Contains(rootType)), "All results must transitively derive from the type");
        Assert.True(transitiveDerived.Any(d => !Equals(d.BaseType, rootType)), "At least one result must not immediately derive from the type");

        Assert.True(transitiveDerived.Count() > immediateDerived.Count());
    }

    [Theory, CombinatorialData]
    public async Task DerivedSourceInterfaces(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
interface IA { }

interface IB1 : IA { }
interface IB2 : IA { }
interface IB3 : IEquatable<IB3>, IA { }

interface IC1 : IB1 { }
interface IC2 : IA, IB2 { }
interface IC3 : IB3 { }

interface ID1 : IC1 { }

interface IOther { }
", Net40.References.mscorlib);

        // get symbols for types
        var compilation = await GetNormalProject(solution).GetCompilationAsync();
        var rootType = compilation.GetTypeByMetadataName("IA");

        Assert.NotNull(rootType);

        var immediateDerived = await SymbolFinder.FindDerivedInterfacesAsync(
            rootType, solution, transitive: false);

        Assert.NotEmpty(immediateDerived);
        AssertEx.SetEqual(immediateDerived.Select(d => d.Name),
            ["IB1", "IB2", "IB3", "IC2"]);
        Assert.True(immediateDerived.All(d => d.Interfaces.Contains(rootType)));

        var transitiveDerived = await SymbolFinder.FindDerivedInterfacesAsync(
            rootType, solution, transitive: true);

        Assert.NotEmpty(transitiveDerived);
        AssertEx.SetEqual(transitiveDerived.Select(d => d.Name),
            ["IB1", "IB2", "IB3", "IC1", "IC2", "IC3", "ID1"]);
        Assert.True(transitiveDerived.All(d => d.AllInterfaces.Contains(rootType)), "All results must transitively derive from the type");
        Assert.True(transitiveDerived.Any(d => !d.Interfaces.Contains(rootType)), "At least one result must not immediately derive from the type");

        Assert.True(transitiveDerived.Count() > immediateDerived.Count());
    }

    [Theory, CombinatorialData]
    public async Task ImplementingSourceTypes(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
interface IA { }

class B1 : IA { }
class B2 : IA { }
class B3 : IEquatable<B3>, IA { }

class C1 : B1 { }
class C2 : B2, IA { }
class C3 : B3 { }

struct S1 : IA { }

class D1 : C1 { }

class OtherClass { }
struct OtherStruct { }
", Net40.References.mscorlib);

        // get symbols for types
        var compilation = await GetNormalProject(solution).GetCompilationAsync();
        var rootType = compilation.GetTypeByMetadataName("IA");

        Assert.NotNull(rootType);

        var immediateImpls = await SymbolFinder.FindImplementationsAsync(
            rootType, solution, transitive: false);

        Assert.NotEmpty(immediateImpls);
        Assert.True(immediateImpls.All(d => d.Interfaces.Contains(rootType)));
        AssertEx.SetEqual(immediateImpls.Select(d => d.Name),
            ["B1", "B2", "B3", "C2", "S1"]);

        var transitiveImpls = await SymbolFinder.FindImplementationsAsync(
            rootType, solution, transitive: true);

        Assert.NotEmpty(transitiveImpls);
        AssertEx.SetEqual(transitiveImpls.Select(d => d.Name),
            ["B1", "B2", "B3", "C1", "C2", "C3", "D1", "S1"]);
        Assert.True(transitiveImpls.All(d => d.AllInterfaces.Contains(rootType)), "All results must transitively derive from the type");
        Assert.True(transitiveImpls.Any(d => !d.Interfaces.Contains(rootType)), "At least one result must not immediately derive from the type");

        Assert.True(transitiveImpls.Count() > immediateImpls.Count());
    }

    [Theory, CombinatorialData]
    public async Task ImplementingTypesDoesProduceDelegates(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
delegate void D();
", Net40.References.mscorlib);

        // get symbols for types
        var compilation = await GetNormalProject(solution).GetCompilationAsync();
        var rootType = compilation.GetTypeByMetadataName("System.ICloneable");

        Assert.NotNull(rootType);

        var transitiveImpls = await SymbolFinder.FindImplementationsAsync(
            rootType, solution, transitive: true);

        var delegates = transitiveImpls.Where(i => i.TypeKind == TypeKind.Delegate);

        Assert.NotEmpty(delegates); // We should find delegates when looking for implementations
        Assert.True(delegates.Any(i => i.Locations.Any(loc => loc.IsInMetadata)), "We should find a metadata delegate");
        Assert.Single(delegates.Where(i => i.Locations.Any(loc => loc.IsInSource))); // We should find a single source delegate
    }

    [Theory, CombinatorialData]
    public async Task ImplementingTypesDoesProduceEnums(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create a normal assembly with a type derived from the portable abstract base
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
enum E
{
    A, B, C,
}
", Net40.References.mscorlib);

        // get symbols for types
        var compilation = await GetNormalProject(solution).GetCompilationAsync();
        var rootType = compilation.GetTypeByMetadataName("System.IComparable");

        Assert.NotNull(rootType);

        var transitiveImpls = await SymbolFinder.FindImplementationsAsync(
            rootType, solution, transitive: true);

        var enums = transitiveImpls.Where(i => i.TypeKind == TypeKind.Enum);

        Assert.NotEmpty(enums); // We should find enums when looking for implementations
        Assert.True(enums.Any(i => i.Locations.Any(loc => loc.IsInMetadata)), "We should find a metadata enum");
        Assert.Single(enums.Where(i => i.Locations.Any(loc => loc.IsInSource))); // We should find a single source type
    }

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1464142")]
    public async Task DependentTypeFinderSkipsNoCompilationLanguages(TestHost host)
    {
        var composition = EditorTestCompositions.EditorFeatures.WithTestHostParts(host);

        using var workspace = TestWorkspace.Create(
@"
<Workspace>
    <Project Name=""TSProject"" Language=""TypeScript"">
        <Document>
            Dummy ts content
        </Document>
    </Project>
    <Project Name=""CSProject"" Language=""C#"">
        <Document>
            public class Base { }
            public class Derived : Base { } 
        </Document>
    </Project>
</Workspace>", composition: composition);
        var solution = workspace.CurrentSolution;

        var csProject = solution.Projects.Single(p => p.Language == LanguageNames.CSharp);
        var otherProject = solution.Projects.Single(p => p != csProject);
        var csDoc = csProject.Documents.Single();

        var semanticModel = await csDoc.GetSemanticModelAsync();
        var csRoot = await csDoc.GetSyntaxRootAsync();

        var firstDecl = csRoot.DescendantNodes().First(d => d is CSharp.Syntax.TypeDeclarationSyntax);
        var firstType = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(firstDecl);

        // Should find one result in the c# project.
        var results = await SymbolFinder.FindDerivedClassesArrayAsync(firstType, solution, transitive: true, ImmutableHashSet.Create(csProject), CancellationToken.None);
        Assert.Single(results);
        Assert.Equal("Derived", results[0].Name);

        // Should find zero results in the TS project (and should not crash).
        results = await SymbolFinder.FindDerivedClassesArrayAsync(firstType, solution, transitive: true, ImmutableHashSet.Create(otherProject), CancellationToken.None);
        Assert.Empty(results);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1555496")]
    public async Task TestDerivedTypesWithIntermediaryType1(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an interface
        solution = AddProjectWithMetadataReferences(solution, "NormalProject1", LanguageNames.CSharp, @"
namespace N_Main
{
    // All these types should find T_DependProject_Class as a derived class,
    // even if only searching the second project.

    abstract public class T_BaseProject_BaseClass
    {
    }
    public abstract class T_BaseProject_DerivedClass1 : T_BaseProject_BaseClass
    {
    }
    public abstract class T_BaseProject_DerivedClass2 : T_BaseProject_DerivedClass1
    {
    }
}
", Net40.References.mscorlib);

        var normalProject1 = solution.Projects.Single();

        // create a normal assembly with a type implementing that interface
        solution = AddProjectWithMetadataReferences(solution, "NormalProject2", LanguageNames.CSharp, @"
namespace N_Main
{
    public class T_DependProject_Class : T_BaseProject_DerivedClass2
    {
    }
}
", Net40.References.mscorlib, normalProject1.Id);

        normalProject1 = solution.GetProject(normalProject1.Id);
        var normalProject2 = solution.Projects.Single(p => p != normalProject1);

        var compilation = await normalProject1.GetCompilationAsync();

        {
            var baseClass = compilation.GetTypeByMetadataName("N_Main.T_BaseProject_BaseClass");
            var typesThatDerive = await SymbolFinder.FindDerivedClassesArrayAsync(
                baseClass, solution, transitive: true, ImmutableHashSet.Create(normalProject2));
            Assert.True(typesThatDerive.Any(t => t.Name == "T_DependProject_Class"));
        }

        {
            var baseClass = compilation.GetTypeByMetadataName("N_Main.T_BaseProject_DerivedClass1");
            var typesThatDerive = await SymbolFinder.FindDerivedClassesArrayAsync(
                baseClass, solution, transitive: true, ImmutableHashSet.Create(normalProject2));
            Assert.True(typesThatDerive.Any(t => t.Name == "T_DependProject_Class"));
        }

        {
            var baseClass = compilation.GetTypeByMetadataName("N_Main.T_BaseProject_DerivedClass2");
            var typesThatDerive = await SymbolFinder.FindDerivedClassesArrayAsync(
                baseClass, solution, transitive: true, ImmutableHashSet.Create(normalProject2));
            Assert.True(typesThatDerive.Any(t => t.Name == "T_DependProject_Class"));
        }
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1598801")]
    public async Task ImplementedInterface_CSharp1(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        solution = AddProjectWithMetadataReferences(solution, "PortableProject1", LanguageNames.CSharp, @"
namespace N
{
    public interface I
    {
        void M();
    }
}
", Net40.References.mscorlib);

        var portableProject1 = solution.Projects.Single(p => p.Name == "PortableProject1");

        solution = AddProjectWithMetadataReferences(solution, "PortableProject2", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class C : I
    {
        public void M() { }
    }
}
", Net40.References.mscorlib, portableProject1.Id);

        // get symbols for types
        var compilation1 = await solution.Projects.Single(p => p.Name == "PortableProject1").GetCompilationAsync();
        var compilation2 = await solution.Projects.Single(p => p.Name == "PortableProject2").GetCompilationAsync();

        var classSymbol = compilation2.GetTypeByMetadataName("M.C");
        var methodSymbol = classSymbol.GetMembers("M").Single();

        var interfaceSymbol = compilation1.GetTypeByMetadataName("N.I");

        var interfaceMembers = await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(methodSymbol, solution, CancellationToken.None);
        var interfaceMember = Assert.Single(interfaceMembers);
        Assert.Equal(interfaceSymbol, interfaceMember.ContainingType);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_queries/edit/1598801")]
    public async Task ImplementedInterface_CSharp2(TestHost host)
    {
        using var workspace = CreateWorkspace(host);
        var solution = workspace.CurrentSolution;

        // create portable assembly with an abstract base class
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public interface I
    {
        void M();
    }
}
", Net40.References.mscorlib);

        var portableProject1 = GetPortableProject(solution);

        // get symbols for types
        var portableCompilation = await GetPortableProject(solution).GetCompilationAsync();
        var baseInterfaceSymbol = portableCompilation.GetTypeByMetadataName("N.I");
        var namespaceSymbol = baseInterfaceSymbol.ContainingNamespace;

        // verify that we don't crash here.
        var implementedMembers = await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(namespaceSymbol, solution, CancellationToken.None);
    }
}
