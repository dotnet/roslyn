// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class SourceGeneratorExtensions
    {
        public static ImmutableArray<SyntaxTree> GenerateSource(
            this Compilation compilation,
            ImmutableArray<SourceGenerator> generators,
            string path,
            bool writeToDisk,
            CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<SyntaxTree>.GetInstance();
            var context = new Context(builder, compilation, path, writeToDisk);
            foreach (var generator in generators)
            {
                generator.Execute(context);
            }
            return builder.ToImmutableAndFree();
        }

        private sealed class Context : SourceGeneratorContext
        {
            private readonly ArrayBuilder<SyntaxTree> _builder;
            private readonly Compilation _compilation;
            private readonly string _path;
            private readonly bool _writeToDisk;

            internal Context(ArrayBuilder<SyntaxTree> builder, Compilation compilation, string path, bool writeToDisk)
            {
                _builder = builder;
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
                throw new NotImplementedException();
            }

            public override void AddCompilationUnit(string name, SyntaxTree tree)
            {
                _builder.Add(ToResult(_compilation, name, tree, _path, _writeToDisk));
            }

            private static SyntaxTree ToResult(Compilation compilation, string name, SyntaxTree tree, string path, bool writeToDisk)
            {
                var ext = (compilation.Language == LanguageNames.VisualBasic) ? ".vb" : ".cs";
                var fileName = $"{FixUpName(name)}{ext}";
                path = PathUtilities.CombineAbsoluteAndRelativePaths(path, fileName);

                if (writeToDisk)
                {
                    var sourceText = tree.GetText();
                    var bytes = sourceText.Encoding.GetBytes(sourceText.ToString());
                    PortableShim.File.WriteAllBytes(path, bytes);
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
                    if (char.IsLetterOrDigit(c))
                    {
                        builder.Append(c);
                    }
                }
                return pooledBuilder.ToStringAndFree();
            }
        }
    }
}
