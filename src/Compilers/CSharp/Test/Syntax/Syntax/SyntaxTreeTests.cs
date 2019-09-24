// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestHelpers;
using KeyValuePair = Roslyn.Utilities.KeyValuePairUtil;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxTreeTests
    {
        [Fact]
        public void CreateTreeWithDiagnostics()
        {
            var options = CreateImmutableDictionary(("CS0078", ReportDiagnostic.Suppress));
            var tree = CSharpSyntaxTree.Create(SyntaxFactory.ParseCompilationUnit(""), diagnosticOptions: options);
            Assert.Same(options, tree.DiagnosticOptions);
        }

        [Fact]
        public void ParseTreeWithChangesPreservesDiagnosticOptions()
        {
            var options = CreateImmutableDictionary(("CS0078", ReportDiagnostic.Suppress));
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(""),
                diagnosticOptions: options);
            Assert.Same(options, tree.DiagnosticOptions);
            var newTree = tree.WithChangedText(SourceText.From("class C { }"));
            Assert.Same(options, newTree.DiagnosticOptions);
        }

        [Fact]
        public void ParseTreeNullDiagnosticOptions()
        {
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(""),
                diagnosticOptions: null);
            Assert.NotNull(tree.DiagnosticOptions);
            Assert.True(tree.DiagnosticOptions.IsEmpty);
            // The default options are case insensitive but the default empty ImmutableDictionary is not
            Assert.NotSame(ImmutableDictionary<string, ReportDiagnostic>.Empty, tree.DiagnosticOptions);
        }

        [Fact]
        public void ParseTreeEmptyDiagnosticOptions()
        {
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(""),
                diagnosticOptions: ImmutableDictionary<string, ReportDiagnostic>.Empty);
            Assert.NotNull(tree.DiagnosticOptions);
            Assert.True(tree.DiagnosticOptions.IsEmpty);
            Assert.Same(ImmutableDictionary<string, ReportDiagnostic>.Empty, tree.DiagnosticOptions);
        }

        [Fact]
        public void ParseTreeCustomDiagnosticOptions()
        {
            var options = CreateImmutableDictionary(("CS0078", ReportDiagnostic.Suppress));
            var tree = CSharpSyntaxTree.ParseText(
                SourceText.From(""),
                diagnosticOptions: options);
            Assert.Same(options, tree.DiagnosticOptions);
        }

        [Fact]
        public void DefaultTreeDiagnosticOptions()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit());
            Assert.NotNull(tree.DiagnosticOptions);
            Assert.True(tree.DiagnosticOptions.IsEmpty);
        }

        [Fact]
        public void WithDiagnosticOptionsNull()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit());
            var newTree = tree.WithDiagnosticOptions(null);
            Assert.NotNull(newTree.DiagnosticOptions);
            Assert.True(newTree.DiagnosticOptions.IsEmpty);
            Assert.Same(tree, newTree);
        }

        [Fact]
        public void WithDiagnosticOptionsEmpty()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit());
            var newTree = tree.WithDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty);
            Assert.NotNull(tree.DiagnosticOptions);
            Assert.True(newTree.DiagnosticOptions.IsEmpty);
            // Default empty immutable dictionary is case sensitive
            Assert.NotSame(tree.DiagnosticOptions, newTree.DiagnosticOptions);
        }

        [Fact]
        public void PerTreeDiagnosticOptionsNewDict()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit());
            var map = ImmutableDictionary.CreateRange(
                new[] { KeyValuePair.Create("CS00778", ReportDiagnostic.Suppress) });
            var newTree = tree.WithDiagnosticOptions(map);
            Assert.NotNull(newTree.DiagnosticOptions);
            Assert.Same(map, newTree.DiagnosticOptions);
            Assert.NotEqual(tree, newTree);
        }

        [Fact]
        public void WithRootAndOptions_ParsedTree()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class B {}");
            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = oldTree.WithRootAndOptions(newRoot, newOptions);
            var newText = newTree.GetText();

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);

            Assert.Null(newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithRootAndOptions_ParsedTreeWithText()
        {
            var oldText = SourceText.From("class B {}", Encoding.UTF7, SourceHashAlgorithm.Sha256);
            var oldTree = SyntaxFactory.ParseSyntaxTree(oldText);

            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = oldTree.WithRootAndOptions(newRoot, newOptions);
            var newText = newTree.GetText();

            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);
            Assert.Same(Encoding.UTF7, newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithRootAndOptions_DummyTree()
        {
            var dummy = new CSharpSyntaxTree.DummySyntaxTree();
            var newRoot = SyntaxFactory.ParseCompilationUnit("class C {}");
            var newOptions = new CSharpParseOptions();
            var newTree = dummy.WithRootAndOptions(newRoot, newOptions);
            Assert.Equal(newRoot.ToString(), newTree.GetRoot().ToString());
            Assert.Same(newOptions, newTree.Options);
        }

        [Fact]
        public void WithFilePath_ParsedTree()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class B {}", path: "old.cs");
            var newTree = oldTree.WithFilePath("new.cs");
            var newText = newTree.GetText();

            Assert.Equal("new.cs", newTree.FilePath);
            Assert.Equal(oldTree.ToString(), newTree.ToString());

            Assert.Null(newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithFilePath_ParsedTreeWithText()
        {
            var oldText = SourceText.From("class B {}", Encoding.UTF7, SourceHashAlgorithm.Sha256);
            var oldTree = SyntaxFactory.ParseSyntaxTree(oldText, path: "old.cs");

            var newTree = oldTree.WithFilePath("new.cs");
            var newText = newTree.GetText();

            Assert.Equal("new.cs", newTree.FilePath);
            Assert.Equal(oldTree.ToString(), newTree.ToString());

            Assert.Same(Encoding.UTF7, newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithFilePath_DummyTree()
        {
            var oldTree = new CSharpSyntaxTree.DummySyntaxTree();
            var newTree = oldTree.WithFilePath("new.cs");

            Assert.Equal("new.cs", newTree.FilePath);
            Assert.Equal(oldTree.ToString(), newTree.ToString());
        }

        [Fact, WorkItem(12638, "https://github.com/dotnet/roslyn/issues/12638")]
        public void WithFilePath_Null()
        {
            SyntaxTree oldTree = new CSharpSyntaxTree.DummySyntaxTree();
            Assert.Equal(string.Empty, oldTree.WithFilePath(null).FilePath);
            oldTree = SyntaxFactory.ParseSyntaxTree("", path: "old.cs");
            Assert.Equal(string.Empty, oldTree.WithFilePath(null).FilePath);
            Assert.Equal(string.Empty, SyntaxFactory.ParseSyntaxTree("", path: null).FilePath);
            Assert.Equal(string.Empty, CSharpSyntaxTree.Create((CSharpSyntaxNode)oldTree.GetRoot(), path: null).FilePath);
        }
    }
}
