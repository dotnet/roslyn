// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class FindReferencesTests : TestBase
{
    private static Workspace CreateWorkspace(Type[] additionalParts = null)
        => new AdhocWorkspace(FeaturesTestCompositions.Features.AddParts(additionalParts).GetHostServices());

    private static Solution AddProjectWithMetadataReferences(Solution solution, string projectName, string languageName, string code, IEnumerable<MetadataReference> metadataReference, params ProjectId[] projectReferences)
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
            metadataReferences: metadataReference,
            projectReferences: projectReferences.Select(p => new ProjectReference(p)));
        return solution.AddProject(pi).AddDocument(did, $"{projectName}.{suffix}", SourceText.From(code));
    }

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

    private static Solution GetSingleDocumentSolution(Workspace workspace, string sourceText, string languageName = LanguageNames.CSharp)
    {
        var pid = ProjectId.CreateNewId();
        var did = DocumentId.CreateNewId(pid);
        return workspace.CurrentSolution
                .AddProject(pid, "goo", "goo", languageName)
                .AddMetadataReference(pid, MscorlibRef)
                .AddDocument(did, "goo.cs", SourceText.From(sourceText));
    }

    private static Solution GetMultipleDocumentSolution(Workspace workspace, string[] sourceTexts)
    {
        var pid = ProjectId.CreateNewId();

        var solution = workspace.CurrentSolution
                .AddProject(pid, "goo", "goo", LanguageNames.CSharp)
                .AddMetadataReference(pid, MscorlibRef);

        var docCounter = 1;

        foreach (var sourceText in sourceTexts)
        {
            var did = DocumentId.CreateNewId(pid);
            solution = solution.AddDocument(did, $"goo{docCounter++}.cs", SourceText.From(sourceText));
        }

        return solution;
    }

    [Fact]
    public async Task FindFieldReferencesInSingleDocumentProject()
    {
        var text = """

            public class C {
               public int X;
               public int Y = X * X;
               public void M() {
                 int x = 10;
                 int y = x + X;
               }
            }

            """;
        using var workspace = CreateWorkspace();
        var solution = GetSingleDocumentSolution(workspace, text);
        var project = solution.Projects.First();
        var symbol = (await project.GetCompilationAsync()).GetTypeByMetadataName("C").GetMembers("X").First();

        var result = (await SymbolFinder.FindReferencesAsync(symbol, solution)).ToList();
        Assert.Equal(1, result.Count); // 1 symbol found
        Assert.Equal(3, result[0].Locations.Count()); // 3 locations found
    }

    [Fact]
    public async Task FindTypeReference_DuplicateMetadataReferences()
    {
        var text = """

            public class C {
               public string X;
            }

            """;
        using var workspace = CreateWorkspace();
        var pid = ProjectId.CreateNewId();
        var did = DocumentId.CreateNewId(pid);
        var solution = workspace.CurrentSolution
                       .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                       .AddMetadataReference(pid, MscorlibRef)
                       .AddMetadataReference(pid, ((PortableExecutableReference)MscorlibRef).WithAliases(["X"]))
                       .AddDocument(did, "goo.cs", SourceText.From(text));

        var project = solution.Projects.First();
        var symbol = (IFieldSymbol)(await project.GetCompilationAsync()).GetTypeByMetadataName("C").GetMembers("X").First();

        var result = (await SymbolFinder.FindReferencesAsync(symbol.Type, solution)).ToList();
        Assert.Equal(9, result.Count);

        var typeSymbol = result.Where(@ref => @ref.Definition.Kind == SymbolKind.NamedType).Single();
        Assert.Equal(1, typeSymbol.Locations.Count());
    }

    [Fact]
    public async Task PinvokeMethodReferences_VB()
    {
        var tree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText(
            """

            Module Module1
                    Declare Function CreateDirectory Lib "kernel32" Alias "CreateDirectoryA" (ByVal lpPathName As String) As Integer
             
                    Private prop As Integer
                    Property Prop1 As Integer
                        Get
                            Return prop
                        End Get
                        Set(value As Integer)
                            CreateDirectory("T")  ' Method Call 1
                            prop = value
                            prop = Nothing
                        End Set
                    End Property

                    Sub Main()
                      CreateDirectory("T") 'Method Call 2            
                      NormalMethod() ' Method Call 1
                      NormalMethod() ' Method Call 2
                   End Sub

                   Sub NormalMethod()
                   End Sub
             End Module
                        
            """);

        var prj1Id = ProjectId.CreateNewId();
        var docId = DocumentId.CreateNewId(prj1Id);

        var sln = CreateWorkspace().CurrentSolution
            .AddProject(prj1Id, "testDeclareReferences", "testAssembly", LanguageNames.VisualBasic)
            .AddMetadataReference(prj1Id, MscorlibRef)
            .AddDocument(docId, "testFile", tree.GetText());

        var prj = sln.GetProject(prj1Id).WithCompilationOptions(new VisualBasic.VisualBasicCompilationOptions(OutputKind.ConsoleApplication, embedVbCoreRuntime: true));
        tree = await prj.GetDocument(docId).GetSyntaxTreeAsync();
        var comp = await prj.GetCompilationAsync();

        var semanticModel = comp.GetSemanticModel(tree);

        SyntaxNode declareMethod = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.DeclareStatementSyntax>().FirstOrDefault();
        SyntaxNode normalMethod = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax>().ToList()[1];

        // declared method calls
        var symbol = semanticModel.GetDeclaredSymbol(declareMethod);
        var references = await SymbolFinder.FindReferencesAsync(symbol, prj.Solution);
        Assert.Equal(expected: 2, actual: references.ElementAt(0).Locations.Count());

        // normal method calls
        symbol = semanticModel.GetDeclaredSymbol(normalMethod);
        references = await SymbolFinder.FindReferencesAsync(symbol, prj.Solution);
        Assert.Equal(expected: 2, actual: references.ElementAt(0).Locations.Count());
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1744118")]
    public async Task TestSymbolWithEmptyIdentifier()
    {
        var tree = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxTree.ParseText(
            """

            Imports System
            Public Class C
                private readonly property
            End Class
                        
            """);

        var prj1Id = ProjectId.CreateNewId();
        var docId = DocumentId.CreateNewId(prj1Id);

        var sln = CreateWorkspace().CurrentSolution
            .AddProject(prj1Id, "testDeclareReferences", "testAssembly", LanguageNames.VisualBasic)
            .AddMetadataReference(prj1Id, MscorlibRef)
            .AddDocument(docId, "testFile", tree.GetText());

        var prj = sln.GetProject(prj1Id).WithCompilationOptions(new VisualBasic.VisualBasicCompilationOptions(OutputKind.ConsoleApplication, embedVbCoreRuntime: true));
        tree = await prj.GetDocument(docId).GetSyntaxTreeAsync();
        var comp = await prj.GetCompilationAsync();

        var semanticModel = comp.GetSemanticModel(tree);

        var propertyStatement = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyStatementSyntax>().FirstOrDefault();
        var symbol = semanticModel.GetDeclaredSymbol(propertyStatement);
        var references = await SymbolFinder.FindReferencesAsync(symbol, prj.Solution);

        Assert.Equal(expected: 0, actual: references.ElementAt(0).Locations.Count());
    }

    [Fact]
    public async Task PinvokeMethodReferences_CS()
    {
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            """


            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Data;
            using System.Diagnostics;
            using System.Runtime.InteropServices;
            static class Module1
            {
            	[DllImport("kernel32", EntryPoint = "CreateDirectoryA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
                public static extern int CreateDirectory(string lpPathName);

                    private static int prop;
                    public static int Prop1
                    {
                        get { return prop; }
                        set
                        {
                            CreateDirectory("T");
                            // Method Call 1
                            prop = value;
                            prop = null;
                        }
                    }

                    public static void Main()
                    {
                        CreateDirectory("T"); // Method Call 2            
                        NormalMethod(); // Method Call 1
                        NormalMethod(); // Method Call 2
                    }

                    public static void NormalMethod()
                    {
                    }
                }
                            
            """);

        var prj1Id = ProjectId.CreateNewId();
        var docId = DocumentId.CreateNewId(prj1Id);

        var sln = CreateWorkspace().CurrentSolution
            .AddProject(prj1Id, "testDeclareReferences", "testAssembly", LanguageNames.CSharp)
            .AddMetadataReference(prj1Id, MscorlibRef)
            .AddDocument(docId, "testFile", tree.GetText());

        var prj = sln.GetProject(prj1Id).WithCompilationOptions(new CSharp.CSharpCompilationOptions(OutputKind.ConsoleApplication));
        tree = await prj.GetDocument(docId).GetSyntaxTreeAsync();
        var comp = await prj.GetCompilationAsync();

        var semanticModel = comp.GetSemanticModel(tree);

        var methodlist = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().ToList();
        SyntaxNode declareMethod = methodlist.ElementAt(0);
        SyntaxNode normalMethod = methodlist.ElementAt(2);

        // pinvoke method calls
        var symbol = semanticModel.GetDeclaredSymbol(declareMethod);
        var references = await SymbolFinder.FindReferencesAsync(symbol, prj.Solution);
        Assert.Equal(2, references.ElementAt(0).Locations.Count());

        // normal method calls
        symbol = semanticModel.GetDeclaredSymbol(normalMethod);
        references = await SymbolFinder.FindReferencesAsync(symbol, prj.Solution);
        Assert.Equal(2, references.ElementAt(0).Locations.Count());
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537936")]
    public async Task FindReferences_InterfaceMapping()
    {
        var text = """

            abstract class C
            {
                public abstract void Boo(); // Line 3
            }
            interface A
            {
                void Boo(); // Line 7
            }
             
            class B : C, A
            {
               void A.Boo() { } // Line 12
               public override void Boo() { } // Line 13
               public void Bar() { Boo(); } // Line 14
            }

            """;
        using var workspace = CreateWorkspace();
        var solution = GetSingleDocumentSolution(workspace, text);
        var project = solution.Projects.First();
        var comp = await project.GetCompilationAsync();

        // Find references on definition B.Boo()
        var typeB = comp.GetTypeByMetadataName("B");
        var boo = typeB.GetMembers("Boo").First();
        var result = (await SymbolFinder.FindReferencesAsync(boo, solution)).ToList();
        Assert.Equal(2, result.Count); // 2 symbols found

        var expectedMatchedLines = new HashSet<int> { 3, 13, 14 };
        result.ForEach((reference) => Verify(reference, expectedMatchedLines));

        Assert.Empty(expectedMatchedLines);

        // Find references on definition C.Boo()
        var typeC = comp.GetTypeByMetadataName("C");
        boo = typeC.GetMembers("Boo").First();
        result = [.. (await SymbolFinder.FindReferencesAsync(boo, solution))];
        Assert.Equal(2, result.Count); // 2 symbols found

        expectedMatchedLines = [3, 13, 14];
        result.ForEach((reference) => Verify(reference, expectedMatchedLines));

        Assert.Empty(expectedMatchedLines);

        // Find references on definition A.Boo()
        var typeA = comp.GetTypeByMetadataName("A");
        boo = typeA.GetMembers("Boo").First();
        result = [.. (await SymbolFinder.FindReferencesAsync(boo, solution))];
        Assert.Equal(2, result.Count); // 2 symbols found

        expectedMatchedLines = [7, 12];
        result.ForEach((reference) => Verify(reference, expectedMatchedLines));

        Assert.Empty(expectedMatchedLines);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28827")]
    public async Task FindReferences_DifferingAssemblies()
    {
        var solution = CreateWorkspace().CurrentSolution;

        solution = AddProjectWithMetadataReferences(solution, "NetStandardProject", LanguageNames.CSharp, """

            namespace N
            {
                public interface I
                {
                    System.Uri Get();
                }
            }
            """, NetStandard20.References.All);

        solution = AddProjectWithMetadataReferences(solution, "NetCoreProject", LanguageNames.CSharp, """

            using N;

            namespace N2 
            {
                public class Impl : I
                {
                    public System.Uri Get()
                    {
                        return null;
                    }
                }
            }
            """, NetCoreApp.References, solution.Projects.Single(pid => pid.Name == "NetStandardProject").Id);

        var netCoreProject = solution.Projects.First(p => p.Name == "NetCoreProject");
        var netStandardProject = solution.Projects.First(p => p.Name == "NetStandardProject");

        var interfaceMethod = (IMethodSymbol)(await netStandardProject.GetCompilationAsync()).GetTypeByMetadataName("N.I").GetMembers("Get").First();

        var references = (await SymbolFinder.FindReferencesAsync(interfaceMethod, solution)).ToList();
        Assert.Equal(2, references.Count);

        var projectIds = new HashSet<ProjectId>();
        foreach (var r in references)
            projectIds.Add(solution.GetOriginatingProjectId(r.Definition));

        Assert.True(projectIds.Contains(netCoreProject.Id));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35786")]
    public async Task FindReferences_MultipleInterfaceInheritence()
    {
        var implText = """
            namespace A
            {
                class C : ITest
                {
                    public string Name { get; }
                    public System.Uri Uri { get; }
                }
            }
            """;

        var interface1Text = """
            namespace A
            {
                interface ITest : ITestBase
                {
                    string Name { get; }
                }
            }
            """;

        var interface2Text = """
            namespace A
            {
                interface ITestBase
                {
                    System.Uri Uri { get; }
                }
            }
            """;

        using var workspace = CreateWorkspace();
        var solution = GetMultipleDocumentSolution(workspace, [implText, interface1Text, interface2Text]);
        solution = solution.AddMetadataReferences(solution.ProjectIds.Single(), NetFramework.References);

        var project = solution.Projects.Single();
        var compilation = await project.GetCompilationAsync();
        var nameProperty = compilation.GetTypeByMetadataName("A.C").GetMembers("Uri").Single();

        var references = await SymbolFinder.FindReferencesAsync(nameProperty, solution);

        // References are: 
        // A.C.Uri
        // A.ITestBase.Uri
        // k__backingField
        // A.C.get_Uri
        // A.ITestBase.get_Uri
        Assert.Equal(5, references.Count());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4936")]
    public async Task OverriddenMethodsFromPortableToDesktop()
    {
        var solution = CreateWorkspace().CurrentSolution;

        // create portable assembly with a virtual method
        solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, """

            namespace N
            {
                public class BaseClass
                {
                    public virtual void SomeMethod() { }
                }
            }

            """, MscorlibPP7Ref);

        // create a normal assembly with a type derived from the portable base and overriding the method
        solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, """

            using N;
            namespace M
            {
                public class DerivedClass : BaseClass
                {
                    public override void SomeMethod() { }
                }
            }

            """, Net40.References.mscorlib, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

        // get symbols for methods
        var portableCompilation = await solution.Projects.Single(p => p.Name == "PortableProject").GetCompilationAsync();
        var baseType = portableCompilation.GetTypeByMetadataName("N.BaseClass");
        var baseVirtualMethodSymbol = baseType.GetMembers("SomeMethod").Single();

        var normalCompilation = await solution.Projects.Single(p => p.Name == "NormalProject").GetCompilationAsync();
        var derivedType = normalCompilation.GetTypeByMetadataName("M.DerivedClass");
        var overriddenMethodSymbol = derivedType.GetMembers("SomeMethod").Single();

        // FAR from the virtual method should find both methods
        var refsFromVirtual = await SymbolFinder.FindReferencesAsync(baseVirtualMethodSymbol, solution);
        Assert.Equal(2, refsFromVirtual.Count());

        // FAR from the overridden method should find both methods
        var refsFromOverride = await SymbolFinder.FindReferencesAsync(overriddenMethodSymbol, solution);
        Assert.Equal(2, refsFromOverride.Count());

        // all methods returned should be equal
        var refsFromVirtualSorted = refsFromVirtual.Select(r => r.Definition).OrderBy(r => r.ContainingType.Name).ToArray();
        var refsFromOverrideSorted = refsFromOverride.Select(r => r.Definition).OrderBy(r => r.ContainingType.Name).ToArray();
        Assert.Equal(refsFromVirtualSorted, refsFromOverrideSorted);
    }

    [Fact]
    public async Task FindRefereceToUnmanagedConstraint_Type()
    {
        var text = """

            interface unmanaged                             // Line 1
            {
            }
            abstract class C<T> where T : unmanaged         // Line 4
            {
            }
            """;
        using var workspace = CreateWorkspace();
        var solution = GetSingleDocumentSolution(workspace, text);
        var project = solution.Projects.First();
        var comp = await project.GetCompilationAsync();

        var constraint = comp.GetTypeByMetadataName("C`1").TypeParameters.Single().ConstraintTypes.Single();
        var result = (await SymbolFinder.FindReferencesAsync(constraint, solution)).Single();

        Verify(result, [1, 4]);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1177764")]
    public async Task DoNotIncludeConstructorReferenceInTypeList_CSharp()
    {
        var text = """

            class C
            {
            }

            class Test
            {
                void M()
                {
                    C c = new C();
                }
            }

            """;
        using var workspace = CreateWorkspace();
        var solution = GetSingleDocumentSolution(workspace, text);
        var project = solution.Projects.First();
        var compilation = await project.GetCompilationAsync();
        var symbol = compilation.GetTypeByMetadataName("C");

        var result = (await SymbolFinder.FindReferencesAsync(symbol, solution)).ToList();
        Assert.Equal(2, result.Count);

        var typeResult = result.Single(r => r.Definition.Kind == SymbolKind.NamedType);
        var constructorResult = result.Single(r => r.Definition.Kind == SymbolKind.Method);

        // Should be one hit for the type and one for the constructor.
        Assert.Equal(1, typeResult.Locations.Count());
        Assert.Equal(1, constructorResult.Locations.Count());

        // those locations should not be the same
        Assert.NotEqual(typeResult.Locations.Single().Location.SourceSpan, constructorResult.Locations.Single().Location.SourceSpan);
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1177764")]
    public async Task DoNotIncludeConstructorReferenceInTypeList_VisualBasic()
    {
        var text = """

            class C
            end class

            class Test
                sub M()
                    dim c as C = new C()
                end sub
            end class

            """;
        using var workspace = CreateWorkspace();
        var solution = GetSingleDocumentSolution(workspace, text, LanguageNames.VisualBasic);
        var project = solution.Projects.First();
        var compilation = await project.GetCompilationAsync();
        var symbol = compilation.GetTypeByMetadataName("C");

        var result = (await SymbolFinder.FindReferencesAsync(symbol, solution)).ToList();
        Assert.Equal(2, result.Count);

        var typeResult = result.Single(r => r.Definition.Kind == SymbolKind.NamedType);
        var constructorResult = result.Single(r => r.Definition.Kind == SymbolKind.Method);

        // Should be one hit for the type and one for the constructor.
        Assert.Equal(1, typeResult.Locations.Count());
        Assert.Equal(1, constructorResult.Locations.Count());

        // those locations should not be the same
        Assert.NotEqual(typeResult.Locations.Single().Location.SourceSpan, constructorResult.Locations.Single().Location.SourceSpan);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49624")]
    public async Task DoNotIncludeSameNamedAlias()
    {
        var text = """

            using NestedDummy = Test.Dummy.NestedDummy;

            namespace Test
            {
                public class DummyFactory
                {
                    public NestedDummy Create() => new NestedDummy();
                }
            }

            namespace Test
            {
            	public class Dummy
            	{
            		public class NestedDummy { }
            	}
            }

            """;
        using var workspace = CreateWorkspace();
        var solution = GetSingleDocumentSolution(workspace, text, LanguageNames.CSharp);
        var project = solution.Projects.First();
        var compilation = await project.GetCompilationAsync();
        var symbol = compilation.GetTypeByMetadataName("Test.Dummy+NestedDummy");

        var result = (await SymbolFinder.FindReferencesAsync(symbol, solution)).ToList();
        Assert.Equal(2, result.Count);

        var typeResult = result.Single(r => r.Definition.Kind == SymbolKind.NamedType);
        var constructorResult = result.Single(r => r.Definition.Kind == SymbolKind.Method);

        // Should be one hit for the type and one for the constructor.
        Assert.Equal(2, typeResult.Locations.Count());
        Assert.Equal(1, constructorResult.Locations.Count());

        // those locations should not be the same
        Assert.True(typeResult.Locations.All(loc => loc.Location.SourceSpan != constructorResult.Locations.Single().Location.SourceSpan));

        // Constructor still binds to the alias.
        Assert.NotNull(constructorResult.Locations.Single().Alias);

        // One type reference is to the type itself, and one is through the alias.
        Assert.True(typeResult.Locations.Count(loc => loc.Alias == null) == 1);
        Assert.True(typeResult.Locations.Count(loc => loc.Alias != null) == 1);
    }

    [Fact]
    public async Task MemberAccessReadWriteSemantics_NotAffectedByWalkUp()
    {
        var text = """

            class C
            {
                public C InnerC;
                public int Field;
                
                void M()
                {
                    // Test the critical case: a.b.c = 5
                    // InnerC (b) should be READ, Field (c) should be WRITTEN
                    InnerC.Field = 5;
                }
            }

            """;
        using var workspace = CreateWorkspace();
        var solution = GetSingleDocumentSolution(workspace, text);
        var project = solution.Projects.First();
        var compilation = await project.GetCompilationAsync();

        var typeC = compilation.GetTypeByMetadataName("C");
        var innerCSymbol = typeC.GetMembers("InnerC").OfType<IFieldSymbol>().First();
        var fieldSymbol = typeC.GetMembers("Field").OfType<IFieldSymbol>().First();

        // Find references to InnerC
        var innerCReferences = (await SymbolFinder.FindReferencesAsync(innerCSymbol, solution)).ToList();
        Assert.Equal(1, innerCReferences.Count);

        var innerCLocations = innerCReferences[0].Locations.Where(loc => !loc.IsImplicit).ToList();
        Assert.Equal(1, innerCLocations.Count); // Should only have the assignment usage, not declaration

        var innerCUsage = innerCLocations.Single();

        // CRITICAL TEST: In "InnerC.Field = 5", InnerC should be READ (not written)
        // This is the key assertion - in a.b.c = 5, 'b' (InnerC) is read to access 'c' (Field)
        Assert.True(innerCUsage.SymbolUsageInfo.IsReadFrom(),
            "InnerC should be READ in 'InnerC.Field = 5'. " +
            $"The walk-up logic should not affect this. UsageInfo: {innerCUsage.SymbolUsageInfo}");

        Assert.False(innerCUsage.SymbolUsageInfo.IsWrittenTo(),
            "InnerC should NOT be WRITTEN in 'InnerC.Field = 5'. " +
            $"Only Field should be written. UsageInfo: {innerCUsage.SymbolUsageInfo}");

        // Verify Field is written as expected
        var fieldReferences = (await SymbolFinder.FindReferencesAsync(fieldSymbol, solution)).ToList();
        Assert.Equal(1, fieldReferences.Count);

        var fieldLocations = fieldReferences[0].Locations.Where(loc => !loc.IsImplicit).ToList();
        Assert.Equal(1, fieldLocations.Count); // Should only have the assignment usage, not declaration

        var fieldUsage = fieldLocations.Single();

        Assert.True(fieldUsage.SymbolUsageInfo.IsWrittenTo(),
            "Field should be WRITTEN in 'InnerC.Field = 5'. " +
            $"UsageInfo: {fieldUsage.SymbolUsageInfo}");
    }

    private static void Verify(ReferencedSymbol reference, HashSet<int> expectedMatchedLines)
    {
        void verifier(Location location)
        {
            var line = location.GetLineSpan().StartLinePosition.Line;
            Assert.True(expectedMatchedLines.Remove(line), $"An unexpected reference was found on line number {line}.");
        }

        foreach (var location in reference.Locations)
        {
            verifier(location.Location);
        }

        foreach (var location in reference.Definition.Locations)
        {
            verifier(location);
        }
    }
}
