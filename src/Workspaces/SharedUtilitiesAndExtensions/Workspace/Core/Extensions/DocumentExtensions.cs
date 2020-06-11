// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.SemanticModelWorkspaceService;
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

        public static async Task<SemanticModel> GetRequiredSemanticModelAsync(this Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }

        public static async Task<SyntaxTree> GetRequiredSyntaxTreeAsync(this Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return syntaxTree ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }

#if !CODE_STYLE
        public static SyntaxTree GetRequiredSyntaxTreeSynchronously(this Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            return syntaxTree ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }
#endif

        public static async Task<SyntaxNode> GetRequiredSyntaxRootAsync(this Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root ?? throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }

        public static bool IsOpen(this Document document)
        {
            var workspace = document.Project.Solution.Workspace as Workspace;
            return workspace != null && workspace.IsDocumentOpen(document.Id);
        }

        /// <summary>
        /// this will return either regular semantic model or speculative semantic based on context. 
        /// any feature that is involved in typing or run on UI thread should use this to take advantage of speculative semantic model 
        /// whenever possible automatically.
        /// 
        /// when using this API, semantic model should only be used to ask node inside of the given span. 
        /// otherwise, it might throw if semantic model returned by this API is a speculative semantic model.
        /// 
        /// also, symbols from the semantic model returned by this API might have out of date location information. 
        /// if exact location (not relative location) is needed from symbol, regular GetSemanticModel should be used.
        /// </summary>
        public static async Task<SemanticModel> GetSemanticModelForSpanAsync(this Document document, TextSpan span, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document.SupportsSemanticModel);

            var syntaxFactService = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModelService = document.Project.Solution.Workspace.Services.GetService<ISemanticModelService>();
            if (semanticModelService == null || syntaxFactService == null)
            {
                return (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(root, "We shouldn't have a null root if the document supports semantic models");
            var token = root.FindToken(span.Start);
            if (token.Parent == null)
            {
                return (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
            }

            var node = token.Parent.AncestorsAndSelf().First(a => a.FullSpan.Contains(span));
            return await GetSemanticModelForNodeAsync(semanticModelService, syntaxFactService, document, node, span, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// this will return either regular semantic model or speculative semantic based on context. 
        /// any feature that is involved in typing or run on UI thread should use this to take advantage of speculative semantic model 
        /// whenever possible automatically.
        /// 
        /// when using this API, semantic model should only be used to ask node inside of the given node except ones that belong to 
        /// member signature. otherwise, it might throw if semantic model returned by this API is a speculative semantic model.
        /// 
        /// also, symbols from the semantic model returned by this API might have out of date location information. 
        /// if exact location (not relative location) is needed from symbol, regular GetSemanticModel should be used.
        /// </summary>
        public static Task<SemanticModel> GetSemanticModelForNodeAsync(this Document document, SyntaxNode? node, CancellationToken cancellationToken)
        {
            var syntaxFactService = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModelService = document.Project.Solution.Workspace.Services.GetService<ISemanticModelService>();
            if (semanticModelService == null || syntaxFactService == null || node == null)
            {
                return document.GetSemanticModelAsync(cancellationToken)!;
            }

            return GetSemanticModelForNodeAsync(semanticModelService, syntaxFactService, document, node, node.FullSpan, cancellationToken);
        }

        private static Task<SemanticModel> GetSemanticModelForNodeAsync(
            ISemanticModelService semanticModelService, ISyntaxFactsService syntaxFactService,
            Document document, SyntaxNode node, TextSpan span, CancellationToken cancellationToken)
        {
            // check whether given span is a valid span to do speculative binding
            var speculativeBindingSpan = syntaxFactService.GetMemberBodySpanForSpeculativeBinding(node);
            if (!speculativeBindingSpan.Contains(span))
            {
                return document.GetSemanticModelAsync(cancellationToken)!;
            }

            return semanticModelService.GetSemanticModelForNodeAsync(document, node, cancellationToken);
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
