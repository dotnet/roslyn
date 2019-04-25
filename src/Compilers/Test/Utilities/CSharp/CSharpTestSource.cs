// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    /// <summary>
    /// Represents the source code used for a C# test. Allows us to have single helpers that enable all the different ways
    /// we typically provide source in testing.
    /// </summary>
    public readonly struct CSharpTestSource
    {
        public static CSharpTestSource None => new CSharpTestSource(null);

        public object Value { get; }

        private CSharpTestSource(object value)
        {
            Value = value;
        }

        public SyntaxTree[] GetSyntaxTrees(CSharpParseOptions parseOptions, string sourceFileName = "")
        {
            switch (Value)
            {
                case string source:
                    return new[] { CSharpTestBase.Parse(source, filename: sourceFileName, parseOptions) };
                case string[] sources:
                    Debug.Assert(string.IsNullOrEmpty(sourceFileName));
                    return CSharpTestBase.Parse(parseOptions, sources);
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
        public static implicit operator CSharpTestSource(SyntaxTree source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(SyntaxTree[] source) => new CSharpTestSource(source);
        public static implicit operator CSharpTestSource(List<SyntaxTree> source) => new CSharpTestSource(source.ToArray());
        public static implicit operator CSharpTestSource(ImmutableArray<SyntaxTree> source) => new CSharpTestSource(source.ToArray());
        public static implicit operator CSharpTestSource(CSharpTestSource[] source) => new CSharpTestSource(source);
    }
}
