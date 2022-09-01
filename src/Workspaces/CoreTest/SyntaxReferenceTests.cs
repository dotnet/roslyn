// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Persistence;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SyntaxReferenceTests : TestBase
    {
        private static Workspace CreateWorkspace(Type[] additionalParts = null)
            => new AdhocWorkspace(FeaturesTestCompositions.Features.AddParts(additionalParts).GetHostServices());

        private static Workspace CreateWorkspaceWithRecoverableSyntaxTrees()
            => CreateWorkspace(new[]
            {
                typeof(TestProjectCacheService),
                typeof(TestTemporaryStorageServiceFactory)
            });

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

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestCSharpReferenceToZeroWidthNode()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTrees();
            var solution = AddSingleFileCSharpProject(workspace.CurrentSolution, @"
public class C<> 
{
}
");

            var tree = solution.Projects.First().Documents.First().GetSyntaxTreeAsync().Result;

            // this is an expected TypeParameterSyntax with a missing identifier token (it is zero-length w/ an error attached to it)
            var node = tree.GetRoot().DescendantNodes().OfType<CS.Syntax.TypeParameterSyntax>().Single();
            Assert.Equal(0, node.FullSpan.Length);

            var syntaxRef = tree.GetReference(node);
            Assert.Equal("PathSyntaxReference", syntaxRef.GetType().Name);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestVisualBasicReferenceToZeroWidthNode()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTrees();
            var solution = AddSingleFileVisualBasicProject(workspace.CurrentSolution, @"
Public Class C(Of )
End Class
");

            var tree = solution.Projects.First().Documents.First().GetSyntaxTreeAsync().Result;

            // this is an expected TypeParameterSyntax with a missing identifier token (it is zero-length w/ an error attached to it)
            var node = tree.GetRoot().DescendantNodes().OfType<VB.Syntax.TypeParameterSyntax>().Single();
            Assert.Equal(0, node.FullSpan.Length);

            var syntaxRef = tree.GetReference(node);
            Assert.Equal("PathSyntaxReference", syntaxRef.GetType().Name);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestCSharpReferenceToNodeInStructuredTrivia()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTrees();
            var solution = AddSingleFileCSharpProject(workspace.CurrentSolution, @"
#if true || true
public class C 
{
}
#endif
");
            var tree = solution.Projects.First().Documents.First().GetSyntaxTreeAsync().Result;

            // find binary node that is part of #if directive
            var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<CS.Syntax.BinaryExpressionSyntax>().First();

            var syntaxRef = tree.GetReference(node);
            Assert.Equal("PositionalSyntaxReference", syntaxRef.GetType().Name);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestVisualBasicReferenceToNodeInStructuredTrivia()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTrees();
            var solution = AddSingleFileVisualBasicProject(workspace.CurrentSolution, @"
#If True Or True Then
Public Class C
End Class
#End If
");

            var tree = solution.Projects.First().Documents.First().GetSyntaxTreeAsync().Result;

            // find binary node that is part of #if directive
            var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<VB.Syntax.BinaryExpressionSyntax>().First();

            var syntaxRef = tree.GetReference(node);
            Assert.Equal("PositionalSyntaxReference", syntaxRef.GetType().Name);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestCSharpReferenceToZeroWidthNodeInStructuredTrivia()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTrees();
            var solution = AddSingleFileCSharpProject(workspace.CurrentSolution, @"
#if true ||
public class C 
{
}
#endif
");

            var tree = solution.Projects.First().Documents.First().GetSyntaxTreeAsync().Result;

            // find binary node that is part of #if directive
            var binary = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<CS.Syntax.BinaryExpressionSyntax>().First();

            // right side should be missing identifier name syntax
            var node = binary.Right;
            Assert.Equal(0, node.FullSpan.Length);

            var syntaxRef = tree.GetReference(node);
            Assert.Equal("PathSyntaxReference", syntaxRef.GetType().Name);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public async System.Threading.Tasks.Task TestVisualBasicReferenceToZeroWidthNodeInStructuredTriviaAsync()
        {
            using var workspace = CreateWorkspaceWithRecoverableSyntaxTrees();
            var solution = AddSingleFileVisualBasicProject(workspace.CurrentSolution, @"
#If (True Or ) Then
Public Class C
End Class
#End If
");

            var tree = await solution.Projects.First().Documents.First().GetSyntaxTreeAsync();

            // find binary node that is part of #if directive
            var binary = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<VB.Syntax.BinaryExpressionSyntax>().First();

            // right side should be missing identifier name syntax
            var node = binary.Right;
            Assert.True(node.IsMissing);
            Assert.Equal(0, node.Span.Length);

            var syntaxRef = tree.GetReference(node);
            Assert.Equal("PathSyntaxReference", syntaxRef.GetType().Name);

            var refNode = syntaxRef.GetSyntax();

            Assert.Equal(node, refNode);
        }
    }
}
