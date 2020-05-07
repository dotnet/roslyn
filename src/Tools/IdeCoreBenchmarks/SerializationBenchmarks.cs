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

        [Benchmark]
        public void RoundTripSyntaxNode()
        {
            var stream = new MemoryStream();
            _root.SerializeTo(stream);

            stream.Position = 0;

            var droot = CSharpSyntaxNode.DeserializeFrom(stream);
            var nodes = droot.DescendantNodesAndSelf().ToImmutableArray();
        }
    }
}
