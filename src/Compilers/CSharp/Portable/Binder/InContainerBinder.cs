// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that places the members of a symbol in scope.
    /// </summary>
    internal class InContainerBinder : Binder
    {
        private readonly NamespaceOrTypeSymbol _container;

        /// <summary>
        /// Creates a binder for a container.
        /// </summary>
        internal InContainerBinder(NamespaceOrTypeSymbol container, Binder next)
            : base(next)
        {
            Debug.Assert((object)container != null);
            _container = container;
        }

        internal NamespaceOrTypeSymbol Container
        {
            get
            {
                return _container;
            }
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                var merged = _container as MergedNamespaceSymbol;
                return ((object)merged != null) ? merged.GetConstituentForCompilation(this.Compilation) : _container;
            }
        }

        private bool IsScriptClass
        {
            get { return (_container.Kind == SymbolKind.NamedType) && ((NamedTypeSymbol)_container).IsScriptClass; }
        }

        internal override bool IsAccessibleHelper(Symbol symbol, TypeSymbol accessThroughType, out bool failedThroughTypeCheck, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, ConsList<TypeSymbol> basesBeingResolved)
        {
            var type = _container as NamedTypeSymbol;
            if ((object)type != null)
            {
                return this.IsSymbolAccessibleConditional(symbol, type, accessThroughType, out failedThroughTypeCheck, ref useSiteInfo);
            }
            else
            {
                return Next.IsAccessibleHelper(symbol, accessThroughType, out failedThroughTypeCheck, ref useSiteInfo, basesBeingResolved);  // delegate to containing Binder, eventually checking assembly.
            }
        }

        internal override bool SupportsExtensions
        {
            get { return true; }
        }

#nullable enable
        internal override void GetAllExtensionCandidatesInSingleBinder(ArrayBuilder<Symbol> members, string? name, string? alternativeName, int arity, LookupOptions options, Binder originalBinder)
        {
            if (_container is NamespaceSymbol ns)
            {
                ns.GetAllExtensionMembers(members, name, alternativeName, arity, options, originalBinder.FieldsBeingBound);
            }
        }
#nullable disable

        internal override TypeWithAnnotations GetIteratorElementType()
        {
            if (IsScriptClass)
            {
                // This is the scenario where a `yield return` exists in the script file as a global statement.
                // This method is to guard against hitting `BuckStopsHereBinder` and crash. 
                return TypeWithAnnotations.Create(this.Compilation.GetSpecialType(SpecialType.System_Object));
            }
            else
            {
                // This path would eventually throw, if we didn't have the case above.
                return Next.GetIteratorElementType();
            }
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(result.IsClear);

            // first lookup members of the namespace
            if ((options & LookupOptions.NamespaceAliasesOnly) == 0)
            {
                this.LookupMembersInternal(result, _container, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);

                if (result.IsMultiViable)
                {
                    if (arity == 0)
                    {
                        // symbols cannot conflict with using alias names
                        if (Next is WithExternAndUsingAliasesBinder withUsingAliases && withUsingAliases.IsUsingAlias(name, originalBinder.IsSemanticModelBinder, basesBeingResolved))
                        {
                            CSDiagnosticInfo diagInfo = new CSDiagnosticInfo(ErrorCode.ERR_ConflictAliasAndMember, name, _container);
                            var error = new ExtendedErrorTypeSymbol((NamespaceOrTypeSymbol)null, name, arity, diagInfo, unreported: true);
                            result.SetFrom(LookupResult.Good(error)); // force lookup to be done w/ error symbol as result
                        }
                    }

                    return;
                }
            }
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            this.AddMemberLookupSymbolsInfo(result, _container, options, originalBinder);
        }

        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            return null;
        }

        protected override LocalFunctionSymbol LookupLocalFunction(SyntaxToken nameToken)
        {
            return null;
        }
    }
}
