// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class DocumentExtensions
    {
        public static bool IsFromPrimaryBranch(this Document document)
            => document.Project.Solution.BranchId == document.Project.Solution.Workspace.PrimaryBranchId;

        public static async Task<bool> IsForkedDocumentWithSyntaxChangesAsync(this Document document, CancellationToken cancellationToken)
        {
            try
            {
                if (document.IsFromPrimaryBranch())
                {
                    return false;
                }

                var currentSolution = document.Project.Solution.Workspace.CurrentSolution;
                var currentDocument = currentSolution.GetDocument(document.Id);
                if (currentDocument == null)
                {
                    return true;
                }

                var documentVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
                var currentDocumentVersion = await currentDocument.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
                return !documentVersion.Equals(currentDocumentVersion);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public static Task<SyntaxTreeIndex> GetSyntaxTreeIndexAsync(this Document document, CancellationToken cancellationToken)
            => SyntaxTreeIndex.GetIndexAsync(document, loadOnly: false, cancellationToken);

        public static Task<SyntaxTreeIndex> GetSyntaxTreeIndexAsync(this Document document, bool loadOnly, CancellationToken cancellationToken)
            => SyntaxTreeIndex.GetIndexAsync(document, loadOnly, cancellationToken);

        /// <summary>
        /// Returns the semantic model for this document that may be produced from partial semantics. The semantic model
        /// is only guaranteed to contain the syntax tree for <paramref name="document"/> and nothing else.
        /// </summary>
        public static async Task<SemanticModel?> GetPartialSemanticModelAsync(this Document document, CancellationToken cancellationToken)
        {
            if (document.Project.TryGetCompilation(out var compilation))
            {
                // We already have a compilation, so at this point it's fastest to just get a SemanticModel
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // Make sure the compilation is kept alive so that GetSemanticModelAsync() doesn't become expensive
                GC.KeepAlive(compilation);
                return semanticModel;
            }
            else
            {
                var frozenDocument = document.WithFrozenPartialSemantics(cancellationToken);
                return await frozenDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        internal static Document WithSolutionOptions(this Document document, OptionSet options)
            => document.Project.Solution.WithOptions(options).GetDocument(document.Id)!;
    }
}
