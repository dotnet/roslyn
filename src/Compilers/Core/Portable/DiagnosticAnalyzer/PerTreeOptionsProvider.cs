// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Provide options keyed on <see cref="SyntaxTree"/>.
    /// </summary>
    public abstract class PerTreeOptionsProvider
    {
        /// <summary>
        /// Get options for a given <paramref name="tree"/>.
        /// Null if there are no options registered for the given
        /// tree.
        /// </summary>
        public abstract OptionSet TryGetOptions(SyntaxTree tree);
    }

    internal sealed class CompilerPerTreeOptionsProvider : PerTreeOptionsProvider
    {
        public static OptionSet EmptyCompilerOptionSet { get; }
            = new CompilerOptionSet();

        public const string OptionFeatureName = "analyzer-config";

        internal class CompilerOptionSet : OptionSet
        {
            // internal for testing
            internal readonly ImmutableDictionary<Option<string>, string> _options;

            private CompilerOptionSet(ImmutableDictionary<Option<string>, string> options)
            {
                _options = options;
            }

            public CompilerOptionSet()
                : this(ImmutableDictionary.Create(
                    CompilerOptionSetKeyComparer.Instance,
                    StringOrdinalComparer.Instance))
            { }

            public override object GetOption(OptionKey optionKey)
                => _options.TryGetValue((Option<string>)optionKey.Option, out string value)
                    ? value
                    : optionKey.Option.DefaultValue;

            public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
            {
                if (!StringOrdinalComparer.Equals(optionAndLanguage.Option.Feature, OptionFeatureName))
                {
                    throw new ArgumentException($"CompilerOptionSet only supports Feature {OptionFeatureName}");
                }

                return new CompilerOptionSet(_options.SetItem((Option<string>)optionAndLanguage.Option, (string)value));
            }

            internal class CompilerOptionSetKeyComparer : IEqualityComparer<Option<string>>
            {
                public static readonly CompilerOptionSetKeyComparer Instance = new CompilerOptionSetKeyComparer();

                private CompilerOptionSetKeyComparer() { }

                public bool Equals(Option<string> x, Option<string> y)
                    => CaseInsensitiveComparison.Equals(x.Name, y.Name);

                public int GetHashCode(Option<string> obj)
                    => CaseInsensitiveComparison.GetHashCode(obj.Name);
            }
        }

        // internal for testing
        internal readonly ImmutableDictionary<SyntaxTree, OptionSet> _treeDict;

        public static CompilerPerTreeOptionsProvider Empty { get; }
            = new CompilerPerTreeOptionsProvider(ImmutableDictionary<SyntaxTree, OptionSet>.Empty);

        public CompilerPerTreeOptionsProvider(ImmutableDictionary<SyntaxTree, OptionSet> treeDict)
        {
            _treeDict = treeDict;
        }

        public override OptionSet TryGetOptions(SyntaxTree tree)
            => _treeDict.TryGetValue(tree, out var options) ? options : null;
    }
}
