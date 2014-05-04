// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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

        // Cached delegates.
        private Func<ConsList<Imports>, ImmutableArray<Cci.NamespaceScope>> buildNamespaceScopes;
        private Func<NamespaceOrTypeSymbol, string> buildNamespaceOrTypeString;

        public NamespaceScopeBuilder(CSharpCompilation compilation)
        {
            this.compilation = compilation;

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
                        if (namespaceOrType.IsNamespace)
                        {
                            NamespaceSymbol @namespace = (NamespaceSymbol)namespaceOrType;
                            string namespaceString = GetNamespaceOrTypeString(@namespace);

                            // TODO: incorrect, see bug #913022
                            string externAlias = GetExternAliases(@namespace).FirstOrDefault();

                            usedNamespaces.Add(Cci.UsedNamespaceOrType.CreateCSharpNamespace(namespaceString, externAlias));
                        }
                        else
                        {
                            // This is possible in C# scripts, but the EE doesn't support the meaning intended by script files.
                            // Specifically, when a script includes "using System.Console;" the intended meaning is that the
                            // static methods of System.Console are available but System.Console itself is not.  Even if we output
                            // "TSystem.Console" - which the EE may or may not support - we would only see System.Console become 
                            // available.
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
                            // TODO: incorrect, see bug #913022
                            string externAlias = GetExternAliases((NamespaceSymbol)target).FirstOrDefault();

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

            return namespaceScopes.ToImmutableAndFree(); //NOTE: inner-to-outer order matches dev10
        }

        private ImmutableArray<string> GetExternAliases(NamespaceSymbol @namespace)
        {
            AssemblySymbol containingAssembly = @namespace.ContainingAssembly;
            if ((object)containingAssembly != null && containingAssembly != this.compilation.Assembly)
            {
                MetadataReference reference = this.compilation.GetMetadataReference(containingAssembly);
                if (reference != null)
                {
                    return reference.Properties.Aliases.NullToEmpty();
                }
            }

            return ImmutableArray<string>.Empty;
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
                return BuildTypeStringWithAssemblyName((TypeSymbol)symbol);
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

        /// <summary>
        /// Returns qualified name of type followed by full assembly name, with square brackets in place of
        /// angle brackets and around type arguments.
        /// e.g. "A.B.C`2[[D.E,assembly], [F.G,assembly]],assembly"
        /// </summary>
        private string BuildTypeStringWithAssemblyName(TypeSymbol symbol)
        {
            string typeArgumentsOpt;
            string assemblyNameSuffix;
            string typeName = BuildTypeString(symbol, out typeArgumentsOpt, out assemblyNameSuffix);
            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringBuilder builder = pooled.Builder;
            builder.Append(typeName);
            AppendTypeArguments(builder, typeArgumentsOpt);
            builder.Append(assemblyNameSuffix);
            return pooled.ToStringAndFree();
        }

        private static void AppendTypeArguments(StringBuilder builder, string typeArgumentsOpt)
        {
            if (typeArgumentsOpt != null)
            {
                builder.Append("[");
                builder.Append(typeArgumentsOpt);
                builder.Append("]");
            }
        }

        /// <summary>
        /// Returns qualified name of type (without the full assembly name), with square brackets in place of
        /// angle brackets and around type arguments.
        /// Full assembly name of the type is stored in <paramref name="assemblyNameSuffix"/>.
        /// </summary>
        private string BuildTypeString(TypeSymbol symbol, out string typeArgumentsOpt, out string assemblyNameSuffix)
        {
            Debug.Assert((object)symbol != null);
            Debug.Assert(!symbol.IsArray());

            if (symbol.TypeKind == TypeKind.DynamicType)
            {
                return BuildTypeString(this.compilation.GetSpecialType(SpecialType.System_Object), out typeArgumentsOpt, out assemblyNameSuffix);
            }

            Symbol containing = symbol.ContainingSymbol;
            Debug.Assert((object)containing != null);
            if (containing.Kind == SymbolKind.Namespace)
            {
                return BuildNamespaceString((NamespaceSymbol)containing, isContainer: true) + BuildTypeStringHelper(symbol, out typeArgumentsOpt, out assemblyNameSuffix);
            }
            else
            {
                Debug.Assert(containing is TypeSymbol);
                string outerTypeArgumentsOpt;
                string outerAssemblyNameSuffix;
                string outer = BuildTypeString((TypeSymbol)containing, out outerTypeArgumentsOpt, out outerAssemblyNameSuffix);
                string inner = BuildTypeStringHelper(symbol, out typeArgumentsOpt, out assemblyNameSuffix);
                Debug.Assert(outerAssemblyNameSuffix == assemblyNameSuffix);

                if (typeArgumentsOpt == null)
                {
                    typeArgumentsOpt = outerTypeArgumentsOpt;
                }
                else if (outerTypeArgumentsOpt != null)
                {
                    typeArgumentsOpt = outerTypeArgumentsOpt + "," + typeArgumentsOpt;
                }

                return outer + "+" + inner;
            }
        }

        /// <summary>
        /// Same as GetTypeString, but without containing type/namespace.
        /// </summary>
        private string BuildTypeStringHelper(TypeSymbol symbol, out string typeArgumentsOpt, out string assemblyNameSuffix)
        {
            if (symbol.GetMemberArity() > 0)
            {
                PooledStringBuilder pool = PooledStringBuilder.GetInstance();
                StringBuilder builder = pool.Builder;
                bool first = true;
                foreach (TypeSymbol typeArg in symbol.GetMemberTypeArgumentsNoUseSiteDiagnostics())
                {
                    if (!first)
                    {
                        builder.Append(",");
                    }
                    first = false;

                    builder.Append(BuildTypeArgumentString(typeArg));
                }
                typeArgumentsOpt = pool.ToStringAndFree();
            }
            else
            {
                typeArgumentsOpt = null;
            }

            assemblyNameSuffix = ", " + symbol.ContainingAssembly.Identity.GetDisplayName();
            return symbol.MetadataName; //should include backtick+arity if required
        }

        private string BuildTypeArgumentString(TypeSymbol typeArg)
        {
            Debug.Assert(typeArg.Kind != SymbolKind.TypeParameter); //must be a closed type

            string typeArgumentsOpt = null;
            string assemblyNameSuffix;
            string typeArgString = typeArg.IsArray() ?
                BuildArrayTypeString((ArrayTypeSymbol)typeArg, out assemblyNameSuffix) :
                BuildTypeString(typeArg, out typeArgumentsOpt, out assemblyNameSuffix);

            PooledStringBuilder pool = PooledStringBuilder.GetInstance();
            StringBuilder builder = pool.Builder;
            builder.Append("[");
            builder.Append(typeArgString);
            AppendTypeArguments(builder, typeArgumentsOpt);
            builder.Append(assemblyNameSuffix);
            builder.Append("]");
            return pool.ToStringAndFree();
        }

        private string BuildArrayTypeString(ArrayTypeSymbol arrayType, out string assemblyNameSuffix)
        {
            TypeSymbol elementType = arrayType.ElementType;

            string typeArgumentsOpt = null;
            string elementTypeString = elementType.IsArray() ?
                BuildArrayTypeString((ArrayTypeSymbol)elementType, out assemblyNameSuffix) :
                BuildTypeString(elementType, out typeArgumentsOpt, out assemblyNameSuffix);

            PooledStringBuilder pool = PooledStringBuilder.GetInstance();
            StringBuilder builder = pool.Builder;
            builder.Append(elementTypeString);
            AppendTypeArguments(builder, typeArgumentsOpt);
            builder.Append(BuildArrayShapeString(arrayType));
            return pool.ToStringAndFree();
        }

        private static string BuildArrayShapeString(ArrayTypeSymbol arrayType)
        {
            PooledStringBuilder pool = PooledStringBuilder.GetInstance();
            StringBuilder builder = pool.Builder;

            builder.Append("[");

            if (arrayType.Rank > 1)
            {
                builder.Append(',', arrayType.Rank - 1);
            }

            builder.Append("]");

            return pool.ToStringAndFree();
        }
    }
}
