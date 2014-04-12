// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
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
            return new CustomWorkspace().CurrentSolution;
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
        public void FindFieldReferencesInSingleDocumentProject()
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
            var symbol = project.GetCompilationAsync().Result.GetTypeByMetadataName("C").GetMembers("X").First();

            var result = SymbolFinder.FindReferencesAsync(symbol, solution).Result.ToList();
            Assert.Equal(1, result.Count); // 1 symbol found
            Assert.Equal(3, result[0].Locations.Count()); // 3 locations found
        }

        [Fact]
        public void FindTypeReference_DuplicateMetadataReferences()
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
                           .AddMetadataReference(pid, ((MetadataImageReference)MscorlibRef).WithAliases(new[] { "X" }))
                           .AddDocument(did, "foo.cs", SourceText.From(text));

            var project = solution.Projects.First();
            var symbol = (IFieldSymbol)project.GetCompilationAsync().Result.GetTypeByMetadataName("C").GetMembers("X").First();

            var result = SymbolFinder.FindReferencesAsync(symbol.Type, solution).Result.ToList();
            Assert.Equal(9, result.Count);

            var typeSymbol = result.Where(@ref => @ref.Definition.Kind == SymbolKind.NamedType).Single();
            Assert.Equal(1, typeSymbol.Locations.Count());
        }

        [Fact, WorkItem(537936, "DevDiv")]
        public void FindReferences_InterfaceMapping()
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
            var comp = project.GetCompilationAsync().Result;

            // Find references on definition B.Boo()
            var typeB = comp.GetTypeByMetadataName("B");
            var boo = typeB.GetMembers("Boo").First();
            var result = SymbolFinder.FindReferencesAsync(boo, solution).Result.ToList();
            Assert.Equal(2, result.Count); // 2 symbols found

            HashSet<int> expectedMatchedLines = new HashSet<int> { 3, 13, 14 };
            result.ForEach((reference) => Verify(reference, expectedMatchedLines));

            Assert.Empty(expectedMatchedLines);

            // Find references on definition C.Boo()
            var typeC = comp.GetTypeByMetadataName("C");
            boo = typeC.GetMembers("Boo").First();
            result = SymbolFinder.FindReferencesAsync(boo, solution).Result.ToList();
            Assert.Equal(2, result.Count); // 2 symbols found

            expectedMatchedLines = new HashSet<int> { 3, 13, 14 };
            result.ForEach((reference) => Verify(reference, expectedMatchedLines));

            Assert.Empty(expectedMatchedLines);

            // Find references on definition A.Boo()
            var typeA = comp.GetTypeByMetadataName("A");
            boo = typeA.GetMembers("Boo").First();
            result = SymbolFinder.FindReferencesAsync(boo, solution).Result.ToList();
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