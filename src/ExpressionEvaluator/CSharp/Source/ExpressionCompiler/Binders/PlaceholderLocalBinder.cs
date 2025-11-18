// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Clr;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class PlaceholderLocalBinder : LocalScopeBinder
    {
        private readonly CSharpSyntaxNode _syntax;
        private readonly ImmutableArray<LocalSymbol> _aliases;
        private readonly MethodSymbol _containingMethod;
        private readonly ImmutableDictionary<string, LocalSymbol> _lowercaseReturnValueAliases;

        internal PlaceholderLocalBinder(
            CSharpSyntaxNode syntax,
            ImmutableArray<Alias> aliases,
            MethodSymbol containingMethod,
            EETypeNameDecoder typeNameDecoder,
            Binder next) :
            base(next)
        {
            _syntax = syntax;
            _containingMethod = containingMethod;

            var compilation = next.Compilation;
            var sourceAssembly = compilation.SourceAssembly;

            var aliasesBuilder = ArrayBuilder<LocalSymbol>.GetInstance(aliases.Length);
            var lowercaseBuilder = ImmutableDictionary.CreateBuilder<string, LocalSymbol>();
            foreach (Alias alias in aliases)
            {
                var local = PlaceholderLocalSymbol.Create(
                    typeNameDecoder,
                    containingMethod,
                    sourceAssembly,
                    alias);
                aliasesBuilder.Add(local);

                if (alias.Kind == DkmClrAliasKind.ReturnValue)
                {
                    lowercaseBuilder.Add(local.Name.ToLower(), local);
                }
            }
            _lowercaseReturnValueAliases = lowercaseBuilder.ToImmutableDictionary();
            _aliases = aliasesBuilder.ToImmutableAndFree();
        }

        internal sealed override void LookupSymbolsInSingleBinder(
            LookupResult result,
            string name,
            int arity,
            ConsList<TypeSymbol> basesBeingResolved,
            LookupOptions options,
            Binder originalBinder,
            bool diagnose,
            ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            if ((options & (LookupOptions.NamespaceAliasesOnly | LookupOptions.NamespacesOrTypesOnly | LookupOptions.LabelsOnly)) != 0)
            {
                return;
            }

            if (name.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var valueText = name.Substring(2);
                ulong address;
                if (!ulong.TryParse(valueText, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address))
                {
                    // Invalid value should have been caught by Lexer.
                    throw ExceptionUtilities.UnexpectedValue(valueText);
                }
                var local = new ObjectAddressLocalSymbol(_containingMethod, name, this.Compilation.GetSpecialType(SpecialType.System_Object), address);
                result.MergeEqual(this.CheckViability(local, arity, options, null, diagnose, ref useSiteInfo, basesBeingResolved));
            }
            else
            {
                LocalSymbol lowercaseReturnValueAlias;
                if (_lowercaseReturnValueAliases.TryGetValue(name, out lowercaseReturnValueAlias))
                {
                    result.MergeEqual(this.CheckViability(lowercaseReturnValueAlias, arity, options, null, diagnose, ref useSiteInfo, basesBeingResolved));
                }
                else
                {
                    base.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);
                }
            }
        }

        internal sealed override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo info, LookupOptions options, Binder originalBinder)
        {
            throw new NotImplementedException();
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return _aliases;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
