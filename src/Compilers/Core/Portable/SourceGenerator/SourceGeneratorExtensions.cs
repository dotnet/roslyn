// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class SourceGeneratorExtensions
    {
        /// <summary>
        /// Invoke each <see cref="SourceGenerator"/> passing the <paramref name="compilation"/>
        /// and return the collection of <see cref="SyntaxTree"/>s generated. Diagnostics reported
        /// by the generators are returned in <paramref name="diagnostics"/>. Exceptions thrown
        /// by the generators are also returned as diagnostics in the <paramref name="diagnostics"/>
        /// collection.
        /// </summary>
        /// <param name="compilation">The compilation to pass to each generator.</param>
        /// <param name="generators">The collection of source generators.</param>
        /// <param name="path">The path to persist generated source if <paramref name="writeToDisk"/> is true.</param>
        /// <param name="writeToDisk">Persist generated source to <paramref name="path"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="diagnostics">Diagnostics and exceptions from the generators.</param>
        /// <returns></returns>
        public static ImmutableArray<SyntaxTree> GenerateSource(
            this Compilation compilation,
            ImmutableArray<SourceGenerator> generators,
            string path,
            bool writeToDisk,
            CancellationToken cancellationToken,
            out ImmutableArray<Diagnostic> diagnostics)
        {
            if (generators.IsDefault)
            {
                throw new ArgumentException(nameof(generators));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var treeBuilder = ArrayBuilder<SyntaxTree>.GetInstance();
            var diagnosticBuilder = ArrayBuilder<Diagnostic>.GetInstance();
            var context = new Context(treeBuilder, diagnosticBuilder, compilation, path, writeToDisk);
            foreach (var generator in generators)
            {
                try
                {
                    generator.Execute(context);
                }
                catch (Exception e) when (ExceptionFilter(e))
                {
                    var descriptor = CreateExceptionDiagnosticDescriptor();
                    var diagnostic = Diagnostic.Create(descriptor, Location.None, generator, e.GetType().ToString(), e.Message);
                    diagnosticBuilder.Add(diagnostic);
                }
            }
            diagnostics = diagnosticBuilder.ToImmutableAndFree();
            return treeBuilder.ToImmutableAndFree();
        }

        private static bool ExceptionFilter(Exception e)
        {
            return !(e is OperationCanceledException);
        }

        private static DiagnosticDescriptor CreateExceptionDiagnosticDescriptor()
        {
            return new DiagnosticDescriptor(
                id: "SG0001",
                title: CodeAnalysisResources.SourceGeneratorFailure,
                messageFormat: CodeAnalysisResources.SourceGeneratorThrows,
                category: "Compiler",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: CodeAnalysisResources.SourceGeneratorThrowsDescription);
        }

        private sealed class Context : SourceGeneratorContext
        {
            private readonly ArrayBuilder<SyntaxTree> _trees;
            private readonly ArrayBuilder<Diagnostic> _diagnostics;
            private readonly Compilation _compilation;
            private readonly string _path;
            private readonly bool _writeToDisk;

            internal Context(ArrayBuilder<SyntaxTree> trees, ArrayBuilder<Diagnostic> diagnostics, Compilation compilation, string path, bool writeToDisk)
            {
                _trees = trees;
                _diagnostics = diagnostics;
                _compilation = compilation;
                _path = path;
                _writeToDisk = writeToDisk;
            }

            public override Compilation Compilation
            {
                get { return _compilation; }
            }

            public override void ReportDiagnostic(Diagnostic diagnostic)
            {
                _diagnostics.Add(diagnostic);
            }

            public override void AddCompilationUnit(string name, SyntaxTree tree)
            {
                _trees.Add(ToResult(_compilation, name, tree, _path, _writeToDisk));
            }

            private static SyntaxTree ToResult(Compilation compilation, string name, SyntaxTree tree, string path, bool writeToDisk)
            {
                var ext = (compilation.Language == LanguageNames.VisualBasic) ? ".vb" : ".cs";
                var fileName = $"{FixUpName(name)}{ext}";
                path = PathUtilities.CombinePossiblyRelativeAndRelativePaths(path, fileName);

                if (writeToDisk)
                {
                    var sourceText = tree.GetText();
                    var encoding = sourceText.Encoding ?? Encoding.UTF8;
                    PortableShim.File.WriteAllText(path, sourceText.ToString(), encoding);
                }

                return tree.WithFilePath(path);
            }

            // Remove any characters from name other than [0-9a-zA-Z_]
            // so the name can be used as a file name. It's possible
            // the resulting name is the empty string.
            private static string FixUpName(string name)
            {
                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                foreach (var c in name)
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        builder.Append(c);
                    }
                }
                return pooledBuilder.ToStringAndFree();
            }
        }
    }
}
