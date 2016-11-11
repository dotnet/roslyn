// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private partial class SymbolReference
        {
            /// <summary>
            /// Code action we use when just adding a using, possibly with a project or
            /// metadata reference.  We don't use the standard code action types because
            /// we want to do things like show a glyph if this will do more than just add
            /// an import.
            /// </summary>
            private class SymbolReferenceCodeAction : CodeAction
            {
                private readonly string _title;
                private readonly Glyph? _glyph;
                private readonly CodeActionPriority _priority;
                private readonly AsyncLazy<CodeActionOperation> _getOperation;
                private readonly Func<Workspace, bool> _isApplicable;

                public override string Title => _title;
                internal override int? Glyph => _glyph.HasValue ? (int)_glyph.Value : (int?)null;
                public override string EquivalenceKey => _title;
                internal override CodeActionPriority Priority => _priority;

                public SymbolReferenceCodeAction(
                    string title, Glyph? glyph, CodeActionPriority priority,
                    AsyncLazy<CodeActionOperation> getOperation,
                    Func<Workspace, bool> isApplicable)
                {
                    _title = title;
                    _glyph = glyph;
                    _priority = priority;
                    _getOperation = getOperation;
                    _isApplicable = isApplicable;
                }

                protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                    => ImmutableArray.Create(await _getOperation.GetValueAsync(cancellationToken).ConfigureAwait(false));

                internal override bool PerformFinalApplicabilityCheck
                    => _isApplicable != null;

                internal override bool IsApplicable(Workspace workspace)
                    => _isApplicable == null ? true : _isApplicable(workspace);
            }
        }
    }
}