// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class InteractiveUsingsBinder : UsingsBinder
    {
        internal InteractiveUsingsBinder(Binder next)
            : base(next)
        {
        }

        protected override ImmutableArray<NamespaceOrTypeAndUsingDirective> GetConsolidatedUsings()
        {
            var currentSubmissionUsings = this.Compilation.GetSubmissionImports().Usings;

            // find the first preceding non-empty submission (has InteractiveUsingsBinder):
            CSharpCompilation previous = this.Compilation.PreviousSubmission;
            InteractiveUsingsBinder previousBinder = null;

            while (previous != null && previousBinder == null)
            {
                previousBinder = previous.GetInteractiveUsingsBinder();
                previous = previous.PreviousSubmission;
            }

            if (previousBinder != null)
            {
                // TODO (tomat): 
                // optimization: do this only if additional references are added to the submission:
                return RebindAndAddUsings(previousBinder.ConsolidatedUsings, currentSubmissionUsings);
            }

            return currentSubmissionUsings;
        }

        internal override TypeSymbol GetIteratorElementType(YieldStatementSyntax node, DiagnosticBag diagnostics)
        {
            diagnostics.Add(ErrorCode.ERR_IteratorInInteractive, node.Location);
            return CreateErrorType();
        }

        /// <summary>
        /// Returns a new list of usings with all namespace symbols replaced by namespace symbols updated from current compilation references.
        /// </summary>
        private ImmutableArray<NamespaceOrTypeAndUsingDirective> RebindAndAddUsings(
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usingsToRebind,
            ImmutableArray<NamespaceOrTypeAndUsingDirective> usingsToAdd)
        {
            if (usingsToRebind.Length == 0)
            {
                return usingsToAdd;
            }

            int rebindCount = usingsToRebind.Length;
            var result = new NamespaceOrTypeAndUsingDirective[rebindCount + usingsToAdd.Length];
            var reversedQualifiedName = new List<string>();

            int resultIndex = 0;
            for (int usingIndex = 0; usingIndex < rebindCount; usingIndex++)
            {
                var symbol = usingsToRebind[usingIndex];

                if (symbol.NamespaceOrType.Kind == SymbolKind.Namespace)
                {
                    var namespaceSymbol = (NamespaceSymbol)symbol.NamespaceOrType;
                    Debug.Assert(!namespaceSymbol.IsGlobalNamespace);
                    reversedQualifiedName.Clear();

                    do
                    {
                        reversedQualifiedName.Add(namespaceSymbol.Name);
                        namespaceSymbol = namespaceSymbol.ContainingNamespace;
                    }
                    while (!namespaceSymbol.IsGlobalNamespace);

                    NamespaceSymbol newNamespaceSymbol = Compilation.GlobalNamespace;
                    for (int i = reversedQualifiedName.Count - 1; i >= 0; i--)
                    {
                        newNamespaceSymbol = newNamespaceSymbol.GetNestedNamespace(reversedQualifiedName[i]);

                        // new submissions can only add more members to namespaces, not remove them
                        Debug.Assert((object)newNamespaceSymbol != null);
                    }

                    symbol = new NamespaceOrTypeAndUsingDirective(newNamespaceSymbol, null);
                }

                result[resultIndex++] = symbol;
            }

            // Don't add usings that are already present in rebound usings. An error has been
            // reported if there are duplicate usings within one submission, so we only need to
            // check for duplicates in previous submissions. Note that usings within each submission
            // are already distinct.
            foreach (var usingToAdd in usingsToAdd)
            {
                // The number of usings is small so use linear search rather than a hash set.
                if (!result.Any(n => Equals(n.NamespaceOrType, usingToAdd.NamespaceOrType)))
                {
                    result[resultIndex++] = usingToAdd;
                }
            }

            Array.Resize(ref result, resultIndex);
            return result.AsImmutableOrNull();
        }
    }
}