// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class FindReferencesTests : ServicesTestBase
    {
        private Solution CreateSolution()
        {
            return new AdhocWorkspace().CurrentSolution;
        }

        private Solution GetSingleDocumentSolution(string sourceText)
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);
            return CreateSolution()
                    .AddProject(pid, "goo", "goo", LanguageNames.CSharp)
                    .AddMetadataReference(pid, MscorlibRef)
                    .AddDocument(did, "goo.cs", SourceText.From(sourceText));
        }

        private Solution GetMultipleDocumentSolution(string[] sourceTexts)
        {
            var pid = ProjectId.CreateNewId();

            var solution = CreateSolution()
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
            var text = @"
public class C {
   public int X;
   public int Y = X * X;
   public void M() {
     int x = 10;
     int y = x + X;
   }
}
";
            var solution = GetSingleDocumentSolution(text);
            var project = solution.Projects.First();
            var symbol = (await project.GetCompilationAsync()).GetTypeByMetadataName("C").GetMembers("X").First();

            var result = (await SymbolFinder.FindReferencesAsync(symbol, solution)).ToList();
            Assert.Equal(1, result.Count); // 1 symbol found
            Assert.Equal(3, result[0].Locations.Count()); // 3 locations found
        }

        [Fact]
        public async Task FindTypeReference_DuplicateMetadataReferences()
        {
            var text = @"
public class C {
   public string X;
}
";
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);
            var solution = CreateSolution()
                           .AddProject(pid, "goo", "goo.dll", LanguageNames.CSharp)
                           .AddMetadataReference(pid, MscorlibRef)
                           .AddMetadataReference(pid, ((PortableExecutableReference)MscorlibRef).WithAliases(new[] { "X" }))
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
                @"
Module Module1
        Declare Function CreateDirectory Lib ""kernel32"" Alias ""CreateDirectoryA"" (ByVal lpPathName As String) As Integer
 
        Private prop As Integer
        Property Prop1 As Integer
            Get
                Return prop
            End Get
            Set(value As Integer)
                CreateDirectory(""T"")  ' Method Call 1
                prop = value
                prop = Nothing
            End Set
        End Property

        Sub Main()
          CreateDirectory(""T"") 'Method Call 2            
          NormalMethod() ' Method Call 1
          NormalMethod() ' Method Call 2
       End Sub

       Sub NormalMethod()
       End Sub
 End Module
            ");

            var prj1Id = ProjectId.CreateNewId();
            var docId = DocumentId.CreateNewId(prj1Id);

            var sln = new AdhocWorkspace().CurrentSolution
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

        [Fact]
        public async Task PinvokeMethodReferences_CS()
        {
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
                @"

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
static class Module1
{
	[DllImport(""kernel32"", EntryPoint = ""CreateDirectoryA"", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
    public static extern int CreateDirectory(string lpPathName);

        private static int prop;
        public static int Prop1
        {
            get { return prop; }
            set
            {
                CreateDirectory(""T"");
                // Method Call 1
                prop = value;
                prop = null;
            }
        }

        public static void Main()
        {
            CreateDirectory(""T""); // Method Call 2            
            NormalMethod(); // Method Call 1
            NormalMethod(); // Method Call 2
        }

        public static void NormalMethod()
        {
        }
    }
                ");

            var prj1Id = ProjectId.CreateNewId();
            var docId = DocumentId.CreateNewId(prj1Id);

            var sln = new AdhocWorkspace().CurrentSolution
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

        [Fact, WorkItem(537936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537936")]
        public async Task FindReferences_InterfaceMapping()
        {
            var text = @"
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
";
            var solution = GetSingleDocumentSolution(text);
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
            result = (await SymbolFinder.FindReferencesAsync(boo, solution)).ToList();
            Assert.Equal(2, result.Count); // 2 symbols found

            expectedMatchedLines = new HashSet<int> { 3, 13, 14 };
            result.ForEach((reference) => Verify(reference, expectedMatchedLines));

            Assert.Empty(expectedMatchedLines);

            // Find references on definition A.Boo()
            var typeA = comp.GetTypeByMetadataName("A");
            boo = typeA.GetMembers("Boo").First();
            result = (await SymbolFinder.FindReferencesAsync(boo, solution)).ToList();
            Assert.Equal(2, result.Count); // 2 symbols found

            expectedMatchedLines = new HashSet<int> { 7, 12 };
            result.ForEach((reference) => Verify(reference, expectedMatchedLines));

            Assert.Empty(expectedMatchedLines);
        }

        [Fact, WorkItem(28827, "https://github.com/dotnet/roslyn/issues/28827")]
        public async Task FindReferences_DifferingAssemblies()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            solution = AddProjectWithMetadataReferences(solution, "NetStandardProject", LanguageNames.CSharp, @"
namespace N
{
    public interface I
    {
        System.Uri Get();
    }
}", NetStandard20Ref);

            solution = AddProjectWithMetadataReferences(solution, "DesktopProject", LanguageNames.CSharp, @"
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
}", SystemRef_v46, solution.Projects.Single(pid => pid.Name == "NetStandardProject").Id);

            var desktopProject = solution.Projects.First(p => p.Name == "DesktopProject");
            solution = solution.AddMetadataReferences(desktopProject.Id, new[] { MscorlibRef_v46, Net46StandardFacade });

            desktopProject = solution.GetProject(desktopProject.Id);
            var netStandardProject = solution.Projects.First(p => p.Name == "NetStandardProject");

            var interfaceMethod = (IMethodSymbol)(await netStandardProject.GetCompilationAsync()).GetTypeByMetadataName("N.I").GetMembers("Get").First();

            var references = (await SymbolFinder.FindReferencesAsync(interfaceMethod, solution)).ToList();
            Assert.Equal(2, references.Count);
            Assert.True(references.Any(r => r.DefinitionAndProjectId.ProjectId == desktopProject.Id));
        }

        [Fact, WorkItem(35786, "https://github.com/dotnet/roslyn/issues/35786")]
        public async Task FindReferences_MultipleInterfaceInheritence()
        {
            var implText = @"namespace A
{
    class C : ITest
    {
        public string Name { get; }
        public System.Uri Uri { get; }
    }
}";

            var interface1Text = @"namespace A
{
    interface ITest : ITestBase
    {
        string Name { get; }
    }
}";

            var interface2Text = @"namespace A
{
    interface ITestBase
    {
        System.Uri Uri { get; }
    }
}";

            var solution = GetMultipleDocumentSolution(new[] { implText, interface1Text, interface2Text });
            solution = solution.AddMetadataReferences(solution.ProjectIds.Single(), new[] { MscorlibRef_v46, Net46StandardFacade, SystemRef_v46, NetStandard20Ref });

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

        [WorkItem(4936, "https://github.com/dotnet/roslyn/issues/4936")]
        [Fact]
        public async Task OverriddenMethodsFromPortableToDesktop()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            // create portable assembly with a virtual method
            solution = AddProjectWithMetadataReferences(solution, "PortableProject", LanguageNames.CSharp, @"
namespace N
{
    public class BaseClass
    {
        public virtual void SomeMethod() { }
    }
}
", MscorlibRefPortable);

            // create a normal assembly with a type derived from the portable base and overriding the method
            solution = AddProjectWithMetadataReferences(solution, "NormalProject", LanguageNames.CSharp, @"
using N;
namespace M
{
    public class DerivedClass : BaseClass
    {
        public override void SomeMethod() { }
    }
}
", MscorlibRef, solution.Projects.Single(pid => pid.Name == "PortableProject").Id);

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
            var text = @"
interface unmanaged                             // Line 1
{
}
abstract class C<T> where T : unmanaged         // Line 4
{
}";
            var solution = GetSingleDocumentSolution(text);
            var project = solution.Projects.First();
            var comp = await project.GetCompilationAsync();

            var constraint = comp.GetTypeByMetadataName("C`1").TypeParameters.Single().ConstraintTypes.Single();
            var result = (await SymbolFinder.FindReferencesAsync(constraint, solution)).Single();

            Verify(result, new HashSet<int> { 1, 4 });
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
}
