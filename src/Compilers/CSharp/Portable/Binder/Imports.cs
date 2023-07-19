// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents symbols imported to the binding scope via using namespace, using alias, and extern alias.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class Imports
    {
        internal static readonly Imports Empty = new Imports(
            ImmutableDictionary<string, AliasAndUsingDirective>.Empty,
            ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty,
            ImmutableArray<AliasAndExternAliasDirective>.Empty);

        public readonly ImmutableDictionary<string, AliasAndUsingDirective> UsingAliases;
        public readonly ImmutableArray<NamespaceOrTypeAndUsingDirective> Usings;
        public readonly ImmutableArray<AliasAndExternAliasDirective> ExternAliases;

        private Imports(
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs)
        {
            Debug.Assert(usingAliases != null);
            Debug.Assert(!usings.IsDefault);
            Debug.Assert(!externs.IsDefault);

            this.UsingAliases = usingAliases;
            this.Usings = usings;
            this.ExternAliases = externs;
        }

        internal string GetDebuggerDisplay()
        {
            return string.Join("; ",
                UsingAliases.OrderBy(x => x.Value.UsingDirective.Location.SourceSpan.Start).Select(ua => $"{ua.Key} = {ua.Value.Alias.Target}").Concat(
                Usings.Select(u => u.NamespaceOrType.ToString())).Concat(
                ExternAliases.Select(ea => $"extern alias {ea.Alias.Name}")));

        }

        // TODO (https://github.com/dotnet/roslyn/issues/5517): skip namespace expansion if references haven't changed.
        internal static Imports ExpandPreviousSubmissionImports(Imports previousSubmissionImports, CSharpCompilation newSubmission)
        {
            if (previousSubmissionImports == Empty)
            {
                return Empty;
            }

            Debug.Assert(previousSubmissionImports != null);
            Debug.Assert(newSubmission.IsSubmission);

            var expandedAliases = ImmutableDictionary<string, AliasAndUsingDirective>.Empty;
            if (!previousSubmissionImports.UsingAliases.IsEmpty)
            {
                var expandedAliasesBuilder = ImmutableDictionary.CreateBuilder<string, AliasAndUsingDirective>();
                foreach (var pair in previousSubmissionImports.UsingAliases)
                {
                    var name = pair.Key;
                    var directive = pair.Value;
                    expandedAliasesBuilder.Add(name, new AliasAndUsingDirective(directive.Alias.ToNewSubmission(newSubmission), directive.UsingDirective));
                }
                expandedAliases = expandedAliasesBuilder.ToImmutable();
            }

            var expandedUsings = ExpandPreviousSubmissionImports(previousSubmissionImports.Usings, newSubmission);

            return Imports.Create(
                expandedAliases,
                expandedUsings,
                previousSubmissionImports.ExternAliases);
        }

        internal static ImmutableArray<NamespaceOrTypeAndUsingDirective> ExpandPreviousSubmissionImports(ImmutableArray<NamespaceOrTypeAndUsingDirective> previousSubmissionUsings, CSharpCompilation newSubmission)
        {
            Debug.Assert(newSubmission.IsSubmission);

            if (!previousSubmissionUsings.IsEmpty)
            {
                var expandedUsingsBuilder = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance(previousSubmissionUsings.Length);
                var expandedGlobalNamespace = newSubmission.GlobalNamespace;

                foreach (var previousUsing in previousSubmissionUsings)
                {
                    var previousTarget = previousUsing.NamespaceOrType;
                    if (previousTarget.IsType)
                    {
                        expandedUsingsBuilder.Add(previousUsing);
                    }
                    else
                    {
                        var expandedNamespace = ExpandPreviousSubmissionNamespace((NamespaceSymbol)previousTarget, expandedGlobalNamespace);
                        expandedUsingsBuilder.Add(new NamespaceOrTypeAndUsingDirective(expandedNamespace, previousUsing.UsingDirective, dependencies: default));
                    }
                }

                return expandedUsingsBuilder.ToImmutableAndFree();
            }

            return previousSubmissionUsings;
        }

        internal static NamespaceSymbol ExpandPreviousSubmissionNamespace(NamespaceSymbol originalNamespace, NamespaceSymbol expandedGlobalNamespace)
        {
            // Soft assert: we'll still do the right thing if it fails.
            Debug.Assert(!originalNamespace.IsGlobalNamespace, "Global using to global namespace");

            // Hard assert: we depend on this.
            Debug.Assert(expandedGlobalNamespace.IsGlobalNamespace, "Global namespace required");

            var nameParts = ArrayBuilder<string>.GetInstance();
            var curr = originalNamespace;
            while (!curr.IsGlobalNamespace)
            {
                nameParts.Add(curr.Name);
                curr = curr.ContainingNamespace;
            }

            var expandedNamespace = expandedGlobalNamespace;
            for (int i = nameParts.Count - 1; i >= 0; i--)
            {
                // Note, the name may have become ambiguous (e.g. if a type with the same name
                // is now in scope), but we're not rebinding - we're just expanding to the
                // current contents of the same namespace.
                expandedNamespace = expandedNamespace.GetMembers(nameParts[i]).OfType<NamespaceSymbol>().Single();
            }
            nameParts.Free();

            return expandedNamespace;
        }

        public bool IsEmpty => UsingAliases.IsEmpty && Usings.IsEmpty && ExternAliases.IsEmpty;

        public static Imports Create(
            ImmutableDictionary<string, AliasAndUsingDirective> usingAliases,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usings,
            ImmutableArray<AliasAndExternAliasDirective> externs)
        {
            Debug.Assert(usingAliases != null);
            Debug.Assert(!usings.IsDefault);
            Debug.Assert(!externs.IsDefault);

            if (usingAliases.IsEmpty && usings.IsEmpty && externs.IsEmpty)
            {
                return Empty;
            }

            return new Imports(usingAliases, usings, externs);
        }

        /// <remarks>
        /// Does not preserve diagnostics.
        /// </remarks>
        internal Imports Concat(Imports otherImports)
        {
            Debug.Assert(otherImports != null);

            if (this == Empty)
            {
                return otherImports;
            }

            if (otherImports == Empty)
            {
                return this;
            }

            var usingAliases = this.UsingAliases.SetItems(otherImports.UsingAliases); // NB: SetItems, rather than AddRange
            var usings = this.Usings.AddRange(otherImports.Usings).Distinct(UsingTargetComparer.Instance);
            var externAliases = ConcatExternAliases(this.ExternAliases, otherImports.ExternAliases);

            return Imports.Create(usingAliases, usings, externAliases);
        }

        private static ImmutableArray<AliasAndExternAliasDirective> ConcatExternAliases(ImmutableArray<AliasAndExternAliasDirective> externs1, ImmutableArray<AliasAndExternAliasDirective> externs2)
        {
            if (externs1.Length == 0)
            {
                return externs2;
            }

            if (externs2.Length == 0)
            {
                return externs1;
            }

            var replacedExternAliases = PooledHashSet<string>.GetInstance();
            replacedExternAliases.AddAll(externs2.Select(e => e.Alias.Name));
            return externs1.WhereAsArray((e, replacedExternAliases) => !replacedExternAliases.Contains(e.Alias.Name), replacedExternAliases).AddRange(externs2);
        }

        private class UsingTargetComparer : IEqualityComparer<NamespaceOrTypeAndUsingDirective>
        {
            public static readonly IEqualityComparer<NamespaceOrTypeAndUsingDirective> Instance = new UsingTargetComparer();

            private UsingTargetComparer() { }

            bool IEqualityComparer<NamespaceOrTypeAndUsingDirective>.Equals(NamespaceOrTypeAndUsingDirective x, NamespaceOrTypeAndUsingDirective y)
            {
                return x.NamespaceOrType.Equals(y.NamespaceOrType);
            }

            int IEqualityComparer<NamespaceOrTypeAndUsingDirective>.GetHashCode(NamespaceOrTypeAndUsingDirective obj)
            {
                return obj.NamespaceOrType.GetHashCode();
            }
        }
    }
}
