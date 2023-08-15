// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SyntaxParserBenchmarks
    {
        private Lexer _smallFileLexer;
        private Lexer _mediumFileLexer;
        private Lexer _largeFileLexer;

        [ParamsAllValues]
        public bool UsePooling { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
            var relativePath = Path.Combine(roslynRoot, @"src\Compilers\CSharp\Portable\Compiler");

            var path = Path.Combine(relativePath, "ModuleCompilationState.cs");
            _smallFileLexer = CreateLexer(path);

            path = Path.Combine(relativePath, "TypeCompilationState.cs");
            _mediumFileLexer = CreateLexer(path);

            path = Path.Combine(relativePath, "MethodCompiler.cs");
            _largeFileLexer = CreateLexer(path);
        }

        private Lexer CreateLexer(string filePath)
        {
            string fileContents = File.ReadAllText(filePath);

            var newText = SourceText.From(fileContents, Encoding.UTF8, SourceHashAlgorithms.Default);
            return new Lexer(newText, TestOptions.RegularDefault);
        }

        [Benchmark]
        public void SmallFile()
        {
            SyntaxParser.s_usePooling = UsePooling;
            using var parser = new LanguageParser(_smallFileLexer, oldTree: null, changes: null);
        }

        [Benchmark]
        public void MediumFile()
        {
            SyntaxParser.s_usePooling = UsePooling;
            using var parser = new LanguageParser(_mediumFileLexer, oldTree: null, changes: null);
        }

        [Benchmark]
        public void LargeFile()
        {
            SyntaxParser.s_usePooling = UsePooling;
            using var parser = new LanguageParser(_largeFileLexer, oldTree: null, changes: null);
        }
    }
}
