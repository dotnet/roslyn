// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class PlaceholderLocalBinder : LocalScopeBinder
    {
        private readonly CSharpSyntaxNode _syntax;
        private readonly ImmutableArray<LocalSymbol> _aliasLocals;
        private readonly MethodSymbol _containingMethod;

        internal PlaceholderLocalBinder(
            CSharpSyntaxNode syntax,
            ImmutableArray<LocalSymbol> aliasLocals,
            MethodSymbol containingMethod,
            Binder next) :
            base(next)
        {
            _syntax = syntax;
            _aliasLocals = aliasLocals;
            _containingMethod = containingMethod;
        }

        internal sealed override void LookupSymbolsInSingleBinder(
            LookupResult result,
            string name,
            int arity,
            ConsList<Symbol> basesBeingResolved,
            LookupOptions options,
            Binder originalBinder,
            bool diagnose,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
                result.MergeEqual(this.CheckViability(local, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved));
            }
            else
            {
                base.LookupSymbolsInSingleBinder(result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
            }
        }

        protected sealed override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo info, LookupOptions options, Binder originalBinder)
        {
            throw new NotImplementedException();
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();
            builder.AddRange(_aliasLocals);
            var declaration = _syntax as LocalDeclarationStatementSyntax;
            if (declaration != null)
            {
                var kind = declaration.IsConst ? LocalDeclarationKind.Constant : LocalDeclarationKind.RegularVariable;
                foreach (var variable in declaration.Declaration.Variables)
                {
                    var local = SourceLocalSymbol.MakeLocal(_containingMethod, this, declaration.Declaration.Type, variable.Identifier, kind, variable.Initializer);
                    builder.Add(local);
                }
            }
            return builder.ToImmutableAndFree();
        }
    }
}
