﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SemanticModelWorkspaceService;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class DocumentExtensions
    {
        // ⚠ Verify IVTs do not use this method before removing it.
        public static TLanguageService? GetLanguageService<TLanguageService>(this Document? document) where TLanguageService : class, ILanguageService
            => document?.Project?.LanguageServices?.GetService<TLanguageService>();

        public static TLanguageService GetRequiredLanguageService<TLanguageService>(this Document document) where TLanguageService : class, ILanguageService
            => document.Project.LanguageServices.GetRequiredService<TLanguageService>();

        public static async Task<SemanticModel> GetRequiredSemanticModelAsync(this Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return semanticModel ?? throw new InvalidOperationException(string.Format(WorkspacesResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }

        public static async Task<SyntaxTree> GetRequiredSyntaxTreeAsync(this Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return syntaxTree ?? throw new InvalidOperationException(string.Format(WorkspacesResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
        }

        public static async Task<SyntaxNode> GetRequiredSyntaxRootAsync(this Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return root ?? throw new InvalidOperationException(string.Format(WorkspacesResources.SyntaxTree_is_required_to_accomplish_the_task_but_is_not_supported_by_document_0, document.Name));
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

        public static bool IsFromPrimaryBranch(this Document document)
        {
            return document.Project.Solution.BranchId == document.Project.Solution.Workspace.PrimaryBranchId;
        }

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

        public static bool IsGeneratedCode(this Document document, CancellationToken cancellationToken)
        {
            var generatedCodeRecognitionService = document.GetLanguageService<IGeneratedCodeRecognitionService>();
            return generatedCodeRecognitionService?.IsGeneratedCode(document, cancellationToken) == true;
        }

        public static async Task<SemanticModel> RequireSemanticModelAsync(this Document document, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return model ?? throw new InvalidOperationException();
        }

        internal static Document WithSolutionOptions(this Document document, OptionSet options)
            => document.Project.Solution.WithOptions(options).GetDocument(document.Id)!;
    }
}
