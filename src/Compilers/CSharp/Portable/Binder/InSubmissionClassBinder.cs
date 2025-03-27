// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that places the members of a submission class and aliases in scope.
    /// </summary>
    internal sealed class InSubmissionClassBinder : InContainerBinder
    {
        private readonly CompilationUnitSyntax _declarationSyntax;
        private readonly bool _inUsings;
        private QuickAttributeChecker? _lazyQuickAttributeChecker;

        internal InSubmissionClassBinder(NamedTypeSymbol submissionClass, Binder next, CompilationUnitSyntax declarationSyntax, bool inUsings)
            : base(submissionClass, next)
        {
            Debug.Assert(submissionClass.IsSubmissionClass);
            _declarationSyntax = declarationSyntax;
            _inUsings = inUsings;
        }

        internal override void GetCandidateExtensionMethods(
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            Binder originalBinder)
        {
            for (var submission = this.Compilation; submission != null; submission = submission.PreviousSubmission)
            {
                submission.ScriptClass?.GetExtensionMethods(methods, name, arity, options);
            }
        }

        internal override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<TypeSymbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            Debug.Assert(result.IsClear);

            this.LookupMembersInSubmissions(result, (NamedTypeSymbol)Container, _declarationSyntax, _inUsings, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteInfo);
        }

        internal override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            this.AddMemberLookupSymbolsInfoInSubmissions(result, (NamedTypeSymbol)Container, _inUsings, options, originalBinder);
        }

        internal override ImmutableArray<AliasAndExternAliasDirective> ExternAliases => ((SourceNamespaceSymbol)Compilation.SourceModule.GlobalNamespace).GetExternAliases(_declarationSyntax);

        internal override ImmutableArray<AliasAndUsingDirective> UsingAliases => ((SourceNamespaceSymbol)Compilation.SourceModule.GlobalNamespace).GetUsingAliases(_declarationSyntax, basesBeingResolved: null);

        /// <summary>
        /// Get <see cref="QuickAttributeChecker"/> that can be used to quickly
        /// check for certain attribute applications in context of this binder.
        /// </summary>
        internal override QuickAttributeChecker QuickAttributeChecker
        {
            get
            {
                if (_lazyQuickAttributeChecker == null)
                {
                    QuickAttributeChecker result = this.Next!.QuickAttributeChecker;
                    result = result.AddAliasesIfAny(_declarationSyntax.Usings);
                    _lazyQuickAttributeChecker = result;
                }

                return _lazyQuickAttributeChecker;
            }
        }
    }
}
