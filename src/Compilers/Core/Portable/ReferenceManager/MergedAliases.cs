// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class MergedAliases
    {
        public ArrayBuilder<string>? AliasesOpt;
        public ArrayBuilder<string>? RecursiveAliasesOpt;
        public ArrayBuilder<MetadataReference>? MergedReferencesOpt;

        /// <summary>
        /// Adds aliases of a specified reference to the merged set of aliases.
        /// Consider the following special cases:
        /// 
        /// o {} + {} = {} 
        ///   If neither reference has any aliases then the result has no aliases.
        /// 
        /// o {A} + {} = {A, global}
        ///   {} + {A} = {A, global}
        ///   
        ///   If one and only one of the references has aliases we add the global alias since the 
        ///   referenced declarations should now be accessible both via existing aliases 
        ///   as well as unqualified.
        ///   
        /// o {A, A} + {A, B, B} = {A, A, B, B}
        ///   We preserve dups in each alias array, but avoid making more dups when merging.
        /// </summary>
        internal void Merge(MetadataReference reference)
        {
            ArrayBuilder<string> aliases;
            if (reference.Properties.HasRecursiveAliases)
            {
                if (RecursiveAliasesOpt == null)
                {
                    RecursiveAliasesOpt = ArrayBuilder<string>.GetInstance();
                    RecursiveAliasesOpt.AddRange(reference.Properties.Aliases);
                    return;
                }

                aliases = RecursiveAliasesOpt;
            }
            else
            {
                if (AliasesOpt == null)
                {
                    AliasesOpt = ArrayBuilder<string>.GetInstance();
                    AliasesOpt.AddRange(reference.Properties.Aliases);
                    return;
                }

                aliases = AliasesOpt;
            }

            Merge(
                aliases: aliases,
                newAliases: reference.Properties.Aliases);

            (MergedReferencesOpt ??= ArrayBuilder<MetadataReference>.GetInstance()).Add(reference);
        }

        internal static void Merge(ArrayBuilder<string> aliases, ImmutableArray<string> newAliases)
        {
            if (aliases.Count == 0 ^ newAliases.IsEmpty)
            {
                AddNonIncluded(aliases, MetadataReferenceProperties.GlobalAlias);
            }

            AddNonIncluded(aliases, newAliases);
        }

        internal static ImmutableArray<string> Merge(ImmutableArray<string> aliasesOpt, ImmutableArray<string> newAliases)
        {
            if (aliasesOpt.IsDefault)
            {
                return newAliases;
            }

            var result = ArrayBuilder<string>.GetInstance(aliasesOpt.Length);
            result.AddRange(aliasesOpt);
            Merge(result, newAliases);
            return result.ToImmutableAndFree();
        }

        private static void AddNonIncluded(ArrayBuilder<string> builder, string item)
        {
            if (!builder.Contains(item))
            {
                builder.Add(item);
            }
        }

        private static void AddNonIncluded(ArrayBuilder<string> builder, ImmutableArray<string> items)
        {
            int originalCount = builder.Count;

            foreach (var item in items)
            {
                if (builder.IndexOf(item, 0, originalCount) < 0)
                {
                    builder.Add(item);
                }
            }
        }
    }
}
