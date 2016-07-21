// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public abstract class SourceGeneratorTypeContext
    {
        private readonly ArrayBuilder<SyntaxTree> _builder;
        private readonly string _path;
        private readonly bool _writeToDisk;
        private readonly ParseOptions _parseOptions;

        internal SourceGeneratorTypeContext(ArrayBuilder<SyntaxTree> builder,
                                            ImmutableArray<SyntaxNode> matchingTypes,
                                            Compilation compilation,
                                            string path,
                                            ParseOptions parseOptions,
                                            bool writeToDisk)
        {
            _builder = builder;
            MatchingTypes = matchingTypes;
            Compilation = compilation;
            _path = path;
            _parseOptions = parseOptions;
            _writeToDisk = writeToDisk;
        }

        internal abstract SyntaxTree CreateSyntaxTree(string source, ParseOptions options, string path);

        public Compilation Compilation { get; }

        public ImmutableArray<SyntaxNode> MatchingTypes { get; }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add the generated source.
        /// </summary>
        /// <param name="name">
        /// Name of the generated source. This name must be unique across
        /// all source generated from this <see cref="ITypeBasedSourceGenerator"/> and
        /// <see cref="CodeAnalysis.Compilation"/>. If the host persists the source to disk,
        /// the file will have this name, with a location determined by the host.
        /// (<see cref="SyntaxTree.FilePath"/> is ignored.)
        /// </param>
        /// <param name="source">Generated source.</param>
        public void AddCompilationUnit(string name, string source)
        {
            var ext = (Compilation.Language == LanguageNames.VisualBasic) ? ".vb" : ".cs";
            var fileName = $"{FixUpName(name)}{ext}";
            var path = PathUtilities.CombinePossiblyRelativeAndRelativePaths(_path, fileName);

            if (_writeToDisk)
            {
                PortableShim.File.WriteAllText(path, source, Encoding.UTF8);
            }

            var tree = CreateSyntaxTree(source, _parseOptions, path);
            _builder.Add(tree);
        }

        // Remove any characters from name other than [0-9a-zA-Z._]
        // so the name can be used as a file name. It's possible
        // the resulting name is the empty string.
        private static string FixUpName(string name)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '_')
                {
                    builder.Append(c);
                }
            }
            return pooledBuilder.ToStringAndFree();
        }
    }
}
