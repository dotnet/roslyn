// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    /// <summary>
    /// Represents the source code used for a C# test. Allows us to have single helpers that enable all the different ways
    /// we typically provide source in testing.
    /// </summary>
    [System.Runtime.CompilerServices.CollectionBuilder(typeof(CSharpTestSourceBuilder), nameof(CSharpTestSourceBuilder.Create))]
    public readonly struct CSharpTestSource : IEnumerable<CSharpTestSource>
    {
        public static CSharpTestSource None => new CSharpTestSource(null);

        public object Value { get; }

        private CSharpTestSource(object value)
        {
            Value = value;
        }

        public static SyntaxTree Parse(
            string text,
            string path = "",
            CSharpParseOptions options = null,
            Encoding encoding = null,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithms.Default)
        {
            var stringText = SourceText.From(text, encoding ?? Encoding.UTF8, checksumAlgorithm);
            var tree = SyntaxFactory.ParseSyntaxTree(stringText, options ?? TestOptions.RegularPreview, path);
            return tree;
        }

        public SyntaxTree[] GetSyntaxTrees(CSharpParseOptions parseOptions, string sourceFileName = "")
        {
            switch (Value)
            {
                case string source:
                    return new[] { Parse(source, path: sourceFileName, parseOptions) };
                case string[] sources:
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return CSharpTestBase.Parse(parseOptions, sources);
                case (string source, string fileName):
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return new[] { CSharpTestBase.Parse(source, fileName, parseOptions) };
                case (string Source, string FileName)[] sources:
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return sources.Select(source => Parse(source.Source, source.FileName, parseOptions)).ToArray();
                case SyntaxTree tree:
                    Debug.Assert(parseOptions == null);
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return new[] { tree };
                case SyntaxTree[] trees:
                    Debug.Assert(parseOptions == null);
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return trees;
                case CSharpTestSource[] testSources:
                    return testSources.SelectMany(s => s.GetSyntaxTrees(parseOptions, sourceFileName)).ToArray();
                case null:
                    return Array.Empty<SyntaxTree>();
                default:
                    throw new Exception($"Unexpected value: {Value}");
            }
        }

        public static implicit operator CSharpTestSource(string source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(string[] source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource((string Source, string FileName) source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource((string Source, string FileName)[] source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(SyntaxTree source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(SyntaxTree[] source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(List<SyntaxTree> source) => new CSharpTestSource(source.ToArray());
        public static implicit operator CSharpTestSource(ImmutableArray<SyntaxTree> source) => new CSharpTestSource(source.ToArray());
        public static implicit operator CSharpTestSource(CSharpTestSource[] source) => new CSharpTestSource(source);

        // Dummy IEnumerable support to satisfy the collection expression and CollectionBuilder requirements
        IEnumerator<CSharpTestSource> IEnumerable<CSharpTestSource>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        internal static class CSharpTestSourceBuilder
        {
            public static CSharpTestSource Create(ReadOnlySpan<CSharpTestSource> source)
            {
                return source.ToArray();
            }
        }
    }
}
