// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SerializationBenchmarks
    {
        private CompilationUnitSyntax _root;
        private MemoryStream _stream;

        private readonly int _iterationCount = 10;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Generated\Syntax.xml.Syntax.Generated.cs");

            if (!File.Exists(csFilePath))
            {
                throw new ArgumentException();
            }

            var text = File.ReadAllText(csFilePath);
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            _root = tree.GetCompilationUnitRoot();
        }

        [IterationCleanup]
        public void SerializationCleanup()
        {
            _stream?.Dispose();
        }

        [IterationSetup(Target = nameof(SerializeSyntaxNode))]
        public void SerializationSetup()
        {
            _stream = new MemoryStream();
        }

        [Benchmark]
        public void SerializeSyntaxNode()
        {
            for (var i = 0; i < _iterationCount; ++i)
            {
                _root.SerializeTo(_stream);
            }
        }

        [IterationSetup(Target = nameof(DeserializeSyntaxNode))]
        public void DeserializationSetup()
        {
            _stream = new MemoryStream();

            for (var i = 0; i < _iterationCount; ++i)
            {
                _root.SerializeTo(_stream);
            }

            _stream.Position = 0;
        }

        [Benchmark]
        public void DeserializeSyntaxNode()
        {
            for (var i = 0; i < _iterationCount; ++i)
            {
                var droot = CSharpSyntaxNode.DeserializeFrom(_stream);
                _ = droot.DescendantNodesAndSelf().ToImmutableArray();
            }
        }
    }
}
