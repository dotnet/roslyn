// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class FindReferencesTests : TestBase
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
                    .AddProject(pid, "foo", "foo", LanguageNames.CSharp)
                    .AddMetadataReference(pid, MscorlibRef)
                    .AddDocument(did, "foo.cs", SourceText.From(sourceText));
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
                           .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                           .AddMetadataReference(pid, MscorlibRef)
                           .AddMetadataReference(pid, ((PortableExecutableReference)MscorlibRef).WithAliases(new[] { "X" }))
                           .AddDocument(did, "foo.cs", SourceText.From(text));

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

            ProjectId prj1Id = ProjectId.CreateNewId();
            DocumentId docId = DocumentId.CreateNewId(prj1Id);

            Solution sln = new AdhocWorkspace().CurrentSolution
                .AddProject(prj1Id, "testDeclareReferences", "testAssembly", LanguageNames.VisualBasic)
                .AddMetadataReference(prj1Id, MscorlibRef)
                .AddDocument(docId, "testFile", tree.GetText());

            Project prj = sln.GetProject(prj1Id).WithCompilationOptions(new VisualBasic.VisualBasicCompilationOptions(OutputKind.ConsoleApplication, embedVbCoreRuntime: true));
            tree = await prj.GetDocument(docId).GetSyntaxTreeAsync();
            Compilation comp = await prj.GetCompilationAsync();

            SemanticModel semanticModel = comp.GetSemanticModel(tree);

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

            ProjectId prj1Id = ProjectId.CreateNewId();
            DocumentId docId = DocumentId.CreateNewId(prj1Id);

            var sln = new AdhocWorkspace().CurrentSolution
                .AddProject(prj1Id, "testDeclareReferences", "testAssembly", LanguageNames.CSharp)
                .AddMetadataReference(prj1Id, MscorlibRef)
                .AddDocument(docId, "testFile", tree.GetText());

            Project prj = sln.GetProject(prj1Id).WithCompilationOptions(new CSharp.CSharpCompilationOptions(OutputKind.ConsoleApplication));
            tree = await prj.GetDocument(docId).GetSyntaxTreeAsync();
            Compilation comp = await prj.GetCompilationAsync();

            SemanticModel semanticModel = comp.GetSemanticModel(tree);

            List<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax> methodlist = tree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().ToList();
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

        [Fact, WorkItem(537936, "DevDiv")]
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

            HashSet<int> expectedMatchedLines = new HashSet<int> { 3, 13, 14 };
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

        private static void Verify(ReferencedSymbol reference, HashSet<int> expectedMatchedLines)
        {
            System.Action<Location> verifier = (location) => Assert.True(expectedMatchedLines.Remove(location.GetLineSpan().StartLinePosition.Line));

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
