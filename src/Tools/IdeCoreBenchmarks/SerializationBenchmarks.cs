// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable CS0618 // Type or member is obsolete

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SerializationBenchmarks
    {
        private List<CompilationUnitSyntax> _rootList;
        private MemoryStream _stream;

        private readonly int _iterationCount = 10;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var csFilePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Parser");

            var files = Directory.GetFiles(csFilePath);
            _rootList = new List<CompilationUnitSyntax>();

            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    throw new ArgumentException();
                }

                var text = File.ReadAllText(file);
                var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(text, encoding: null, SourceHashAlgorithms.Default));
                _rootList.Add(tree.GetCompilationUnitRoot());
            }
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
            foreach (var root in _rootList)
            {
                for (var i = 0; i < _iterationCount; ++i)
                {
                    root.SerializeTo(_stream);
                }
            }
        }

        [IterationSetup(Target = nameof(DeserializeSyntaxNode))]
        public void DeserializationSetup()
        {
            _stream = new MemoryStream();

            foreach (var root in _rootList)
            {
                for (var i = 0; i < _iterationCount; ++i)
                {
                    root.SerializeTo(_stream);
                }
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

#pragma warning restore CS0618 // Type or member is obsolete
