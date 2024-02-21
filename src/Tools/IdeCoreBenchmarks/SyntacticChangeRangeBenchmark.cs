// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SyntacticChangeRangeBenchmark
    {
        private int _index;
        private SourceText _text;
        private SyntaxTree _tree;
        private SyntaxNode _root;

        private SyntaxNode _rootWithSimpleEdit;
        private SyntaxNode _rootWithComplexEdit;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\BoundNodes.xml.Generated.cs");

            if (!File.Exists(csFilePath))
                throw new FileNotFoundException(csFilePath);

            var text = File.ReadAllText(csFilePath);
            _index = text.IndexOf("switch (node.Kind)");
            if (_index < 0)
                throw new ArgumentException("Code location not found");

            _text = SourceText.From(text);
            _tree = SyntaxFactory.ParseSyntaxTree(_text);
            _root = _tree.GetCompilationUnitRoot();
            _rootWithSimpleEdit = WithSimpleEditAtMiddle();
            _rootWithComplexEdit = WithDestabalizingEditAtMiddle();
        }

        private SyntaxNode WithSimpleEditAtMiddle()
        {
            // this will change the switch statement to `mode.kind` instead of `node.kind`.  This should be reuse most
            // of the tree and should result in a very small diff.
            var newText = _text.WithChanges(new TextChange(new TextSpan(_index + 8, 1), "m"));
            var newTree = _tree.WithChangedText(newText);
            var newRoot = newTree.GetRoot();
            return newRoot;
        }

        private SyntaxNode WithDestabalizingEditAtMiddle()
        {
            // this will change the switch statement to a switch expression.  This may have large cascading changes.
            var newText = _text.WithChanges(new TextChange(new TextSpan(_index, 0), "var v = x "));
            var newTree = _tree.WithChangedText(newText);
            var newRoot = newTree.GetRoot();
            return newRoot;
        }

        private SyntaxNode WithSimpleEditAtBeginning()
        {
            var addedText = "using System.IO;";
            var newText = _text.WithChanges(new TextChange(new TextSpan(0, addedText.Length), addedText));
            var newTree = _tree.WithChangedText(newText);
            var newRoot = newTree.GetRoot();
            return newRoot;
        }

        private SyntaxNode WithSimpleEditInTheEnd()
        {
            var addedText = "class HelloWorld { }";
            var newText = _text.WithChanges(new TextChange(new TextSpan(_text.Length - addedText.Length, addedText.Length), addedText));
            var newTree = _tree.WithChangedText(newText);
            var newRoot = newTree.GetRoot();
            return newRoot;
        }

        [Benchmark]
        public void SimpleEditAtMiddle()
        {
            var newRoot = WithSimpleEditAtMiddle();
            SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(_root, newRoot, TimeSpan.MaxValue, CancellationToken.None);
        }

        [Benchmark]
        public void DestabalizingEditAtMiddle()
        {
            var newRoot = WithDestabalizingEditAtMiddle();
            SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(_root, newRoot, TimeSpan.MaxValue, CancellationToken.None);
        }

        [Benchmark]
        public void SimpleEditAtMiddle_NoParse()
        {
            SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(_root, _rootWithSimpleEdit, TimeSpan.MaxValue, CancellationToken.None);
        }

        [Benchmark]
        public void DestabalizingEditAtMiddle_NoParse()
        {
            SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(_root, _rootWithComplexEdit, TimeSpan.MaxValue, CancellationToken.None);
        }

        [Benchmark]
        public void SimpleEditAtTheBeginning()
        {
            var newRoot = WithSimpleEditAtBeginning();
            SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(_root, newRoot, TimeSpan.MaxValue, CancellationToken.None);
        }

        [Benchmark]
        public void SimpleEditorInTheEnd()
        {
            var newRoot = WithSimpleEditInTheEnd();
            SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(_root, newRoot, TimeSpan.MaxValue, CancellationToken.None);
        }
    }
}
