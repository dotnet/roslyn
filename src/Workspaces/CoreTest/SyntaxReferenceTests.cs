// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SyntaxReferenceTests : TestBase
    {
        private Solution CreateSingleFileCSharpSolution(string source)
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            return CreateEmptySolutionUsingRecoverableSyntaxTrees()
                           .AddProject(pid, "Test", "Test.dll", LanguageNames.CSharp)
                           .AddDocument(did, "Test.cs", SourceText.From(source));
        }

        private Solution CreateSingleFileVisualBasicSolution(string source)
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            return CreateEmptySolutionUsingRecoverableSyntaxTrees()
                           .AddProject(pid, "Test", "Test.dll", LanguageNames.VisualBasic)
                           .AddDocument(did, "Test.vb", SourceText.From(source));
        }

        private static Solution CreateEmptySolutionUsingRecoverableSyntaxTrees()
        {
            var workspace = new AdhocWorkspace(MefHostServices.Create(TestHost.Assemblies), workspaceKind: "NotKeptAlive");
            workspace.Options = workspace.Options.WithChangedOption(Host.CacheOptions.RecoverableTreeLengthThreshold, 0);
            return workspace.CurrentSolution;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestCSharpReferenceToZeroWidthNode()
        {
            var solution = CreateSingleFileCSharpSolution(@"
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
            var solution = CreateSingleFileVisualBasicSolution(@"
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
            var solution = CreateSingleFileCSharpSolution(@"
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
            var solution = CreateSingleFileVisualBasicSolution(@"
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
            var solution = CreateSingleFileCSharpSolution(@"
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
            var solution = CreateSingleFileVisualBasicSolution(@"
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
