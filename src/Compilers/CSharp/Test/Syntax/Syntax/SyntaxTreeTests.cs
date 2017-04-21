// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxTreeTests
    {
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

            Assert.Equal(newTree.FilePath, "new.cs");
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

            Assert.Equal(newTree.FilePath, "new.cs");
            Assert.Equal(oldTree.ToString(), newTree.ToString());

            Assert.Same(Encoding.UTF7, newText.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha256, newText.ChecksumAlgorithm);
        }

        [Fact]
        public void WithFilePath_DummyTree()
        {
            var oldTree = new CSharpSyntaxTree.DummySyntaxTree();
            var newTree = oldTree.WithFilePath("new.cs");

            Assert.Equal(newTree.FilePath, "new.cs");
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
