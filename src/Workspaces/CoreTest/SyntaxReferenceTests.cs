// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Shared.Extensions;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public sealed class SyntaxReferenceTests : TestBase
    {
        private static Workspace CreateWorkspace()
            => new AdhocWorkspace(FeaturesTestCompositions.Features.GetHostServices());

        private static Solution AddSingleFileCSharpProject(Solution solution, string source)
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            return solution
                .AddProject(pid, "Test", "Test.dll", LanguageNames.CSharp)
                .AddDocument(did, "Test.cs", SourceText.From(source));
        }

        private static Solution AddSingleFileVisualBasicProject(Solution solution, string source)
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            return solution
                .AddProject(pid, "Test", "Test.dll", LanguageNames.VisualBasic)
                .AddDocument(did, "Test.vb", SourceText.From(source));
        }

        [Fact]
        public async Task TestCSharpReferenceToZeroWidthNode()
        {
            using var workspace = CreateWorkspace();
            var solution = AddSingleFileCSharpProject(workspace.CurrentSolution, @"
public class C<> 
{
}
");

            var tree = await solution.Projects.First().Documents.First().GetRequiredSyntaxTreeAsync(CancellationToken.None);

            // this is an expected TypeParameterSyntax with a missing identifier token (it is zero-length w/ an error attached to it)
            var node = tree.GetRoot().DescendantNodes().OfType<CS.Syntax.TypeParameterSyntax>().Single();
            Assert.Equal(0, node.FullSpan.Length);

            var syntaxRef = tree.GetReference(node);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact]
        public async Task TestVisualBasicReferenceToZeroWidthNode()
        {
            using var workspace = CreateWorkspace();
            var solution = AddSingleFileVisualBasicProject(workspace.CurrentSolution, @"
Public Class C(Of )
End Class
");

            var tree = await solution.Projects.First().Documents.First().GetRequiredSyntaxTreeAsync(CancellationToken.None);

            // this is an expected TypeParameterSyntax with a missing identifier token (it is zero-length w/ an error attached to it)
            var node = tree.GetRoot().DescendantNodes().OfType<VB.Syntax.TypeParameterSyntax>().Single();
            Assert.Equal(0, node.FullSpan.Length);

            var syntaxRef = tree.GetReference(node);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact]
        public async Task TestCSharpReferenceToNodeInStructuredTrivia()
        {
            using var workspace = CreateWorkspace();
            var solution = AddSingleFileCSharpProject(workspace.CurrentSolution, @"
#if true || true
public class C 
{
}
#endif
");
            var tree = await solution.Projects.First().Documents.First().GetRequiredSyntaxTreeAsync(CancellationToken.None);

            // find binary node that is part of #if directive
            var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<CS.Syntax.BinaryExpressionSyntax>().First();

            var syntaxRef = tree.GetReference(node);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact]
        public async Task TestVisualBasicReferenceToNodeInStructuredTrivia()
        {
            using var workspace = CreateWorkspace();
            var solution = AddSingleFileVisualBasicProject(workspace.CurrentSolution, @"
#If True Or True Then
Public Class C
End Class
#End If
");

            var tree = await solution.Projects.First().Documents.First().GetRequiredSyntaxTreeAsync(CancellationToken.None);

            // find binary node that is part of #if directive
            var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<VB.Syntax.BinaryExpressionSyntax>().First();

            var syntaxRef = tree.GetReference(node);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact]
        public async Task TestCSharpReferenceToZeroWidthNodeInStructuredTrivia()
        {
            using var workspace = CreateWorkspace();
            var solution = AddSingleFileCSharpProject(workspace.CurrentSolution, @"
#if true ||
public class C 
{
}
#endif
");

            var tree = await solution.Projects.First().Documents.First().GetRequiredSyntaxTreeAsync(CancellationToken.None);

            // find binary node that is part of #if directive
            var binary = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<CS.Syntax.BinaryExpressionSyntax>().First();

            // right side should be missing identifier name syntax
            var node = binary.Right;
            Assert.Equal(0, node.FullSpan.Length);

            var syntaxRef = tree.GetReference(node);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact]
        public async System.Threading.Tasks.Task TestVisualBasicReferenceToZeroWidthNodeInStructuredTriviaAsync()
        {
            using var workspace = CreateWorkspace();
            var solution = AddSingleFileVisualBasicProject(workspace.CurrentSolution, @"
#If (True Or ) Then
Public Class C
End Class
#End If
");

            var tree = await solution.Projects.First().Documents.First().GetRequiredSyntaxTreeAsync(CancellationToken.None);

            // find binary node that is part of #if directive
            var binary = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<VB.Syntax.BinaryExpressionSyntax>().First();

            // right side should be missing identifier name syntax
            var node = binary.Right;
            Assert.True(node.IsMissing);
            Assert.Equal(0, node.Span.Length);

            var syntaxRef = tree.GetReference(node);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }
    }
}
