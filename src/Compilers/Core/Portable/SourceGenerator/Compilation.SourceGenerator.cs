// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class Compilation
    {
        internal abstract SourceGeneratorTypeContext GetSourceGeneratorTypeContext(
            ArrayBuilder<SyntaxTree> builder,
            string attributeName,
            string path,
            bool writeToDisk);

        public ImmutableArray<SyntaxTree> GenerateSource(
            ImmutableArray<ITypeBasedSourceGenerator> generators,
            string attributeName,
            string path,
            bool writeToDisk)
        {
            if (generators.IsDefault)
            {
                throw new ArgumentException(nameof(generators));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var builder = ArrayBuilder<SyntaxTree>.GetInstance();
            var context = GetSourceGeneratorTypeContext(builder,
                                                        attributeName,
                                                        path,
                                                        writeToDisk);
            foreach (var generator in generators)
            {
                generator.Execute(context);
            }
            return builder.ToImmutableAndFree();
        }

        internal static ParseOptions CommonParseOptions(ImmutableArray<SyntaxNode> matchingTypes)
        {
            Debug.Assert(!matchingTypes.IsDefaultOrEmpty);

            ParseOptions common = null;
            foreach (var type in matchingTypes)
            {
                if (common == null)
                {
                    common = type.SyntaxTree.Options;
                }
                else if (common != type.SyntaxTree.Options)
                {
                    // TODO: Report diagnostic
                }
            }
            return common;
        }

    }
}