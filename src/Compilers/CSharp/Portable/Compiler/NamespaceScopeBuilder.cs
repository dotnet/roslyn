// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Class to cache and build namespace scopes. Should be released and collected once all namespace scopes
    /// are built, since it contains caches that won't be needed anymore.
    /// </summary>
    internal sealed class NamespaceScopeBuilder
    {
        // Cache to map from ConsList<Imports> to ImmutableArray<NamespaceScope>. Currently we just use
        // identity comparison on the key. We could implement our own comparer to detect equivalent ConsList<Imports>,
        // but that only provides benefit when many file share the exactly same set of Imports in the same order. This would
        // be a complex comparer to implement, and the benefit wouldn't be very high.
        private readonly ConcurrentDictionary<ConsList<Imports>, ImmutableArray<Cci.NamespaceScope>> cache = new ConcurrentDictionary<ConsList<Imports>, ImmutableArray<Cci.NamespaceScope>>();

        // Cache to map from namespace or type to the string used to represent that namespace/type in the debug info.
        private readonly ConcurrentDictionary<NamespaceOrTypeSymbol, string> stringCache = new ConcurrentDictionary<NamespaceOrTypeSymbol, string>();

        private readonly CSharpCompilation compilation;

        private readonly EmitContext context;

        // Cached delegates.
        private Func<ConsList<Imports>, ImmutableArray<Cci.NamespaceScope>> buildNamespaceScopes;
        private Func<NamespaceOrTypeSymbol, string> buildNamespaceOrTypeString;

        public NamespaceScopeBuilder(CSharpCompilation compilation, EmitContext context)
        {
            this.compilation = compilation;
            this.context = context;

            buildNamespaceScopes = BuildNamespaceScopes;
            buildNamespaceOrTypeString = BuildNamespaceOrTypeString;
        }

        /// <remarks>
        /// CONSIDER: in the case of field initializers, it is possible that different parts of a method could have different
        /// namespace scopes (i.e. if they come from different parts of a partial type).  Currently, we're following Dev10's
        /// approach of using the context of the (possibly synthesized) constructor into which the field initializers are
        /// inserted.  It might be possible to give field initializers their own scopes, assuming the EE supports it.
        /// </remarks>
        public ImmutableArray<Cci.NamespaceScope> GetNamespaceScopes(ConsList<Imports> debugImports)
        {
            if (debugImports == null)
            {
                return ImmutableArray<Cci.NamespaceScope>.Empty;
            }
            else
            {
                return cache.GetOrAdd(debugImports, buildNamespaceScopes);
            }
        }

        private ImmutableArray<Cci.NamespaceScope> BuildNamespaceScopes(ConsList<Imports> debugImports)
        {
            var namespaceScopes = ArrayBuilder<Cci.NamespaceScope>.GetInstance();

            // NOTE: All extern aliases are stored on the outermost Imports object.
            var validExternAliases = PooledHashSet<string>.GetInstance();
            foreach (AliasAndExternAliasDirective externAlias in debugImports.Last().ExternAliases.NullToEmpty())
            {
                validExternAliases.Add(externAlias.Alias.Name);
            }

            foreach (Imports imports in debugImports)
            {
                var usedNamespaces = ArrayBuilder<Cci.UsedNamespaceOrType>.GetInstance();

                // NOTE: order based on dev10: extern aliases, then usings, then aliases namespaces and types

                ImmutableArray<AliasAndExternAliasDirective> externAliases = imports.ExternAliases;
                if (!externAliases.IsDefault)
                {
                    foreach (var alias in externAliases)
                    {
                        usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateCSharpExternNamespace(alias.Alias.Name));
                    }
                }

                ImmutableArray<NamespaceOrTypeAndUsingDirective> usings = imports.Usings;
                if (!usings.IsDefault)
                {
                    foreach (var nsOrType in usings)
                    {
                        NamespaceOrTypeSymbol namespaceOrType = nsOrType.NamespaceOrType;
                        string namespaceOrTypeString = GetNamespaceOrTypeString(namespaceOrType);
                        if (namespaceOrType.IsNamespace)
                        {
                            string externAlias = GuessExternAlias((NamespaceSymbol)namespaceOrType, validExternAliases);
                            usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateCSharpNamespace(namespaceOrTypeString, externAlias));
                        }
                        else
                        {
                            Debug.Assert(namespaceOrType is TypeSymbol);
                            usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateCSharpType(namespaceOrTypeString));
                        }
                    }
                }

                Dictionary<string, AliasAndUsingDirective> aliasSymbols = imports.UsingAliases;
                if (aliasSymbols != null)
                {
                    foreach (var pair in aliasSymbols)
                    {
                        var alias = pair.Key;
                        var symbol = pair.Value.Alias;
                        Debug.Assert(!symbol.IsExtern);

                        var target = symbol.Target;
                        var targetString = GetNamespaceOrTypeString(target);
                        if (target.Kind == SymbolKind.Namespace)
                        {
                            string externAlias = GuessExternAlias((NamespaceSymbol)target, validExternAliases);

                            usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateCSharpNamespaceAlias(targetString, alias, externAlias));
                        }
                        else
                        {
                            Debug.Assert(target is TypeSymbol);
                            usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateCSharpTypeAlias(targetString, alias));
                        }
                    }
                }

                namespaceScopes.Add(new Cci.NamespaceScope(usedNamespaces.ToImmutableAndFree()));
            }

            validExternAliases.Free();

            return namespaceScopes.ToImmutableAndFree(); //NOTE: inner-to-outer order matches dev10
        }

        private string GuessExternAlias(NamespaceSymbol @namespace, HashSet<string> validAliases)
        {
            AssemblySymbol containingAssembly = @namespace.ContainingAssembly;
            if ((object)containingAssembly != null && containingAssembly != this.compilation.Assembly)
            {
                MetadataReference reference = this.compilation.GetMetadataReference(containingAssembly);
                if (reference != null)
                {
                    ImmutableArray<string> aliases = reference.Properties.Aliases;
                    if (!aliases.IsDefaultOrEmpty)
                    {
                        foreach (string alias in aliases)
                        {
                            if (alias == MetadataReferenceProperties.GlobalAlias)
                            {
                                // Don't bother explicitly emitting "global".
                                return null;
                            }
                            else if (validAliases.Contains(alias))
                            {
                                // CONSIDER: Dev12 uses the one that appeared in source, whereas we use
                                // the first one that COULD have appeared in source.  (DevDiv #913022)
                                // NOTE: The reason we're not just using the alias from the syntax is that
                                // it is non-trivial to locate.  In particular, since "." may be used in
                                // place of "::", determining whether the first identifier in the name is
                                // the alias requires binding.  For example, "using A.B;" could refer to
                                // either "A::B" or "global::A.B".
                                return alias;
                            }
                        }

                        Debug.Assert(false, $"None of the aliases of {@namespace} is valid in this scope");
                    }
                }
            }

            return null;
        }

        private string GetNamespaceOrTypeString(NamespaceOrTypeSymbol symbol)
        {
            return stringCache.GetOrAdd(symbol, buildNamespaceOrTypeString);
        }

        private string BuildNamespaceOrTypeString(NamespaceOrTypeSymbol symbol)
        {
            if (symbol.IsNamespace)
            {
                return BuildNamespaceString((NamespaceSymbol)symbol, isContainer: false);
            }
            else
            {
                var context = this.context;
                return context.ModuleBuilder.Translate((ITypeSymbol)symbol, context.SyntaxNodeOpt, context.Diagnostics).GetSerializedTypeName(context);
            }
        }

        /// <summary>
        /// Qualified name of namespace.
        /// e.g. "A.B.C"
        /// </summary>
        private static string BuildNamespaceString(NamespaceSymbol symbol, bool isContainer)
        {
            Debug.Assert((object)symbol != null);

            if (symbol.IsGlobalNamespace)
            {
                return "";
            }

            ArrayBuilder<string> parts = ArrayBuilder<string>.GetInstance();
            for (NamespaceSymbol curr = symbol; !curr.IsGlobalNamespace; curr = curr.ContainingNamespace)
            {
                parts.Add(curr.Name);
            }
            parts.ReverseContents();

            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooled.Builder;
            bool first = true;
            foreach (string part in parts)
            {
                if (!first)
                {
                    builder.Append(".");
                }
                first = false;

                builder.Append(part);
            }

            if (isContainer)
            {
                builder.Append(".");
            }

            parts.Free();
            return pooled.ToStringAndFree();
        }
    }
}
