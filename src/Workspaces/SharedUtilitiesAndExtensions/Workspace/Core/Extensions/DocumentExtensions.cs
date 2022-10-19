// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SemanticModelReuse;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if DEBUG
using System.Collections.Immutable;
using System.Diagnostics;
#endif

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class DocumentExtensions
    {
        // ⚠ Verify IVTs do not use this method before removing it.
        public static TLanguageService? GetLanguageService<TLanguageService>(this Document? document) where TLanguageService : class, ILanguageService
            => document?.Project?.GetLanguageService<TLanguageService>();

        public static TLanguageService GetRequiredLanguageService<TLanguageService>(this Document document) where TLanguageService : class, ILanguageService
            => document.Project.GetRequiredLanguageService<TLanguageService>();

        public static async ValueTask<SemanticModel> GetRequiredSemanticModelAsync(this Document document, CancellationToken cancellationToken)
        {
            if (document.TryGetSemanticModel(out var semanticModel))
                return semanticModel;

            semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }

        public static async ValueTask<SyntaxTree> GetRequiredSyntaxTreeAsync(this Document document, CancellationToken cancellationToken)
        {
            if (document.TryGetSyntaxTree(out var syntaxTree))
                return syntaxTree;

            syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return syntaxTree ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }

#if !CODE_STYLE
        public static SyntaxTree GetRequiredSyntaxTreeSynchronously(this Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            return syntaxTree ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }
#endif

        public static async ValueTask<SyntaxNode> GetRequiredSyntaxRootAsync(this Document document, CancellationToken cancellationToken)
        {
            if (document.TryGetSyntaxRoot(out var root))
                return root;

            root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }

#if !CODE_STYLE
        public static SyntaxNode GetRequiredSyntaxRootSynchronously(this Document document, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            return root ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }
#endif

        public static bool IsOpen(this TextDocument document)
        {
            var workspace = document.Project.Solution.Workspace;
            return workspace != null && workspace.IsDocumentOpen(document.Id);
        }

        /// <summary>
        /// Attempts to return an speculative semantic model for <paramref name="document"/> if possible if <paramref
        /// name="position"/> is contained within a method body in the tree.  Specifically, this will attempt to get an
        /// existing cached semantic model for <paramref name="document"/>.  If it can find one, and the top-level semantic
        /// version for this project matches the cached version, and the position is within a method body, then it will 
        /// be returned, just with the previous corresponding method body swapped out with the current method body.
        /// <para/>
        /// If this is not possible, the regular semantic model for <paramref name="document"/> will be returned.
        /// <para/>
        /// When using this API, semantic model should only be used to ask questions about nodes inside of the member
        /// that contains the given <paramref name="position"/>.
        /// <para/>
        /// As a speculative semantic model may be returned, location based information provided by it may be innacurate.
        /// </summary>
        public static ValueTask<SemanticModel> ReuseExistingSpeculativeModelAsync(this Document document, int position, CancellationToken cancellationToken)
            => ReuseExistingSpeculativeModelAsync(document, new TextSpan(position, 0), cancellationToken);

        /// <summary>
        /// Attempts to return an speculative semantic model for <paramref name="document"/> if possible if <paramref
        /// name="span"/> is contained within a method body in the tree.  Specifically, this will attempt to get an
        /// existing cached semantic model <paramref name="document"/>.  If it can find one, and the top-level semantic
        /// version for this project matches the cached version, and the position is within a method body, then it will 
        /// be returned, just with the previous corresponding method body swapped out with the current method body.
        /// <para/>
        /// If this is not possible, the regular semantic model for <paramref name="document"/> will be returned.
        /// <para/>
        /// When using this API, semantic model should only be used to ask questions about nodes inside of the
        /// member that contains the given <paramref name="span"/>.
        /// <para/>
        /// As a speculative semantic model may be returned, location based information provided by it may be innacurate.
        /// </summary>
        public static async ValueTask<SemanticModel> ReuseExistingSpeculativeModelAsync(this Document document, TextSpan span, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.SupportsSemanticModel);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);
            var node = token.Parent!.AncestorsAndSelf().First(a => a.FullSpan.Contains(span));

            return await ReuseExistingSpeculativeModelAsync(document, node, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to return an speculative semantic model for <paramref name="document"/> if possible if <paramref
        /// name="node"/> is contained within a method body in the tree.  Specifically, this will attempt to get an
        /// existing cached semantic model <paramref name="document"/>.  If it can find one, and the top-level semantic
        /// version for this project matches the cached version, and the position is within a method body, then it will 
        /// be returned, just with the previous corresponding method body swapped out with the current method body.
        /// <para/>
        /// If this is not possible, the regular semantic model for <paramref name="document"/> will be returned.
        /// <para/>
        /// When using this API, semantic model should only be used to ask questions about nodes inside of the
        /// member that contains the given <paramref name="node"/>.
        /// <para/>
        /// As a speculative semantic model may be returned, location based information provided by it may be innacurate.
        /// </summary>
        public static ValueTask<SemanticModel> ReuseExistingSpeculativeModelAsync(this Document document, SyntaxNode? node, CancellationToken cancellationToken)
        {
            if (node == null)
                return document.GetRequiredSemanticModelAsync(cancellationToken);

            var workspace = document.Project.Solution.Workspace;
            var semanticModelService = workspace.Services.GetRequiredService<ISemanticModelReuseWorkspaceService>();

            return semanticModelService.ReuseExistingSpeculativeModelAsync(document, node, cancellationToken);
        }

#if DEBUG
        public static async Task<bool> HasAnyErrorsAsync(this Document document, CancellationToken cancellationToken, List<string>? ignoreErrorCode = null)
        {
            var errors = await GetErrorsAsync(document, cancellationToken, ignoreErrorCode).ConfigureAwait(false);
            return errors.Length > 0;
        }

        public static async Task<ImmutableArray<Diagnostic>> GetErrorsAsync(this Document document, CancellationToken cancellationToken, IList<string>? ignoreErrorCode = null)
        {
            if (!document.SupportsSemanticModel)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            ignoreErrorCode ??= SpecializedCollections.EmptyList<string>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel!.GetDiagnostics(cancellationToken: cancellationToken).WhereAsArray(
                diag => diag.Severity == DiagnosticSeverity.Error && !ignoreErrorCode.Contains(diag.Id));
        }

        /// <summary>
        /// Debug only extension method to verify no errors were introduced by formatting, pretty listing and other related document altering service in error-free code.
        /// </summary>
        public static async Task VerifyNoErrorsAsync(this Document newDocument, string message, CancellationToken cancellationToken, List<string>? ignoreErrorCodes = null)
        {
            var errors = await newDocument.GetErrorsAsync(cancellationToken, ignoreErrorCodes).ConfigureAwait(false);
            if (errors.Length > 0)
            {
                var diagnostics = string.Join(", ", errors.Select(d => d.ToString()));
                Debug.Assert(false, message + ". " + diagnostics);
            }
        }
#endif

#if !CODE_STYLE
        public static bool IsGeneratedCode(this Document document, CancellationToken cancellationToken)
        {
            var generatedCodeRecognitionService = document.GetLanguageService<IGeneratedCodeRecognitionService>();
            return generatedCodeRecognitionService?.IsGeneratedCode(document, cancellationToken) == true;
        }
#endif

        public static async Task<bool> IsGeneratedCodeAsync(this Document document, CancellationToken cancellationToken)
        {
            var generatedCodeRecognitionService = document.GetLanguageService<IGeneratedCodeRecognitionService>();
            return generatedCodeRecognitionService != null &&
                await generatedCodeRecognitionService.IsGeneratedCodeAsync(document, cancellationToken).ConfigureAwait(false);
        }

        public static IEnumerable<Document> GetLinkedDocuments(this Document document)
        {
            var solution = document.Project.Solution;

            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
            {
                yield return solution.GetRequiredDocument(linkedDocumentId);
            }
        }
    }
}
