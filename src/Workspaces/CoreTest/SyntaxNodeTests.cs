// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public partial class SyntaxNodeTests : TestBase
    {
        [Fact]
        public async Task TestReplaceOneNodeAsync()
        {
            var text = @"public class C { public int X; }";
            var expected = @"public class C { public int Y; }";

            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetRoot();

            var node = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var newRoot = await root.ReplaceNodesAsync(new[] { node }, (o, n, c) =>
            {
                var decl = (VariableDeclaratorSyntax)n;
                return Task.FromResult<SyntaxNode>(decl.WithIdentifier(SyntaxFactory.Identifier("Y")));
            }, CancellationToken.None);

            var actual = newRoot.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestReplaceNestedNodesAsync()
        {
            var text = @"public class C { public int X; }";
            var expected = @"public class C1 { public int X1; }";

            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetRoot();

            var nodes = root.DescendantNodes().Where(n => n is VariableDeclaratorSyntax or ClassDeclarationSyntax).ToList();
            var computations = 0;
            var newRoot = await root.ReplaceNodesAsync(nodes, (o, n, c) =>
            {
                computations++;
                if (n is ClassDeclarationSyntax classDecl)
                {
                    var id = classDecl.Identifier;
                    return Task.FromResult<SyntaxNode>(classDecl.WithIdentifier(SyntaxFactory.Identifier(id.LeadingTrivia, id.ToString() + "1", id.TrailingTrivia)));
                }

                if (n is VariableDeclaratorSyntax varDecl)
                {
                    var id = varDecl.Identifier;
                    return Task.FromResult<SyntaxNode>(varDecl.WithIdentifier(SyntaxFactory.Identifier(id.LeadingTrivia, id.ToString() + "1", id.TrailingTrivia)));
                }

                return Task.FromResult(n);
            }, CancellationToken.None);

            var actual = newRoot.ToString();

            Assert.Equal(expected, actual);
            Assert.Equal(computations, nodes.Count);
        }

        [Fact]
        public async Task TestTrackNodesWithDocument()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var sourceText = @"public class C { void M() { } }";

            var sol = new AdhocWorkspace().CurrentSolution
                .AddProject(pid, "proj", "proj", LanguageNames.CSharp)
                .AddDocument(did, "doc", sourceText);

            var doc = sol.GetDocument(did);

            // find initial nodes of interest
            var root = await doc.GetSyntaxRootAsync();
            var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var methodDecl = classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>().First();

            // track these nodes
            var trackedRoot = root.TrackNodes(classDecl, methodDecl);

            // use some fancy document centric rewrites
            var gen = SyntaxGenerator.GetGenerator(doc);
            var cgenField = gen.FieldDeclaration("X", gen.TypeExpression(SpecialType.System_Int32), Accessibility.Private);

            var currentClassDecl = trackedRoot.GetCurrentNodes(classDecl).First();
            var classDeclWithField = gen.InsertMembers(currentClassDecl, 0, [cgenField]);

            // we can find related bits even from sub-tree fragments
            var latestMethod = classDeclWithField.GetCurrentNodes(methodDecl).First();
            Assert.NotNull(latestMethod);
            Assert.NotEqual(latestMethod, methodDecl);

            trackedRoot = trackedRoot.ReplaceNode(currentClassDecl, classDeclWithField);

            // put back into document (branch solution, etc)
            doc = doc.WithSyntaxRoot(trackedRoot);

            // re-get root of new document
            var root2 = await doc.GetSyntaxRootAsync();
            Assert.NotEqual(trackedRoot, root2);

            // we can still find the tracked node in the new document
            var finalClassDecl = root2.GetCurrentNodes(classDecl).First();
            Assert.Equal("public class C\r\n{\r\n    private int X;\r\n    void M()\r\n    {\r\n    }\r\n}", finalClassDecl.NormalizeWhitespace().ToString());

            // and other tracked nodes too
            var finalMethodDecl = root2.GetCurrentNodes(methodDecl).First();
            Assert.NotNull(finalMethodDecl);
            Assert.NotEqual(finalMethodDecl, methodDecl);
        }
    }
}
