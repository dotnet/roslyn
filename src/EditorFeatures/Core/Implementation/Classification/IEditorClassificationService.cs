﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    [Obsolete("Use Microsoft.CodeAnalysis.Classification.IClassificationService instead")]
    internal interface IEditorClassificationService : ILanguageService
    {
        /// <summary>
        /// Produce the classifications for the span of text specified.  Classification should be
        /// performed as quickly as possible, and should process the text in a lexical fashion.
        /// This allows classification results to be shown to the user when a file is opened before
        /// any additional compiler information is available for the text.
        /// 
        /// Important: The classification should not consider the context the text exists in, and how
        /// that may affect the final classifications.  This may result in incorrect classification
        /// (i.e. identifiers being classified as keywords).  These incorrect results will be patched
        /// up when the lexical results are superseded by the calls to AddSyntacticClassifications.
        /// </summary>
        void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// Produce the classifications for the span of text specified.  The syntax of the document 
        /// can be accessed to provide more correct classifications.  For example, the syntax can
        /// be used to determine if a piece of text that looks like a keyword should actually be
        /// considered an identifier in its current context.
        /// </summary>
        Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// Produce the classifications for the span of text specified.  Semantics of the language
        /// can be used to provide richer information for constructs where syntax is insufficient.
        /// For example, semantic information can be used to determine if an identifier should be
        /// classified as a type, structure, or something else entirely. 
        /// </summary>
        Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);

        /// <summary>
        /// Adjust a classification from a previous version of text accordingly based on the current
        /// text.  For example, if a piece of text was classified as an identifier in a previous version,
        /// but a character was added that would make it into a keyword, then indicate that here.
        /// 
        /// This allows the classified to quickly fix up old classifications as the user types.  These
        /// adjustments are allowed to be incorrect as they will be superseded by calls to get the
        /// syntactic and semantic classifications for this version later.
        /// </summary>
        ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);
    }

    /// <summary>
    /// Exists to bridge between <see cref="IEditorClassificationService"/> and <see cref="IClassificationService"/>
    /// </summary>
    internal interface IClassificationDelegationService<TClassificationService>
    {
        void AddLexicalClassifications(TClassificationService service, SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);
        Task AddSyntacticClassificationsAsync(TClassificationService service, Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);
        Task AddSemanticClassificationsAsync(TClassificationService service, Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);
        ClassifiedSpan AdjustStaleClassification(TClassificationService service, SourceText text, ClassifiedSpan classifiedSpan);
    }

    internal class WorkspaceClassificationDelegationService : IClassificationDelegationService<IClassificationService>
    {
        public static readonly IClassificationDelegationService<IClassificationService> Instance = new WorkspaceClassificationDelegationService();

        private WorkspaceClassificationDelegationService()
        {
        }

        public void AddLexicalClassifications(IClassificationService service, SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
            => service.AddLexicalClassifications(text, textSpan, result, cancellationToken);

        public Task AddSyntacticClassificationsAsync(IClassificationService service, Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
            => service.AddSyntacticClassificationsAsync(document, textSpan, result, cancellationToken);

        public Task AddSemanticClassificationsAsync(IClassificationService service, Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
            => service.AddSemanticClassificationsAsync(document, textSpan, result, cancellationToken);

        public ClassifiedSpan AdjustStaleClassification(IClassificationService service, SourceText text, ClassifiedSpan classifiedSpan)
            => service.AdjustStaleClassification(text, classifiedSpan);
    }

#pragma warning disable CS0618 // Type or member is obsolete
    internal class EditorClassificationDelegationService : IClassificationDelegationService<IEditorClassificationService>
    {
        public static readonly IClassificationDelegationService<IEditorClassificationService> Instance = new EditorClassificationDelegationService();

        private EditorClassificationDelegationService()
        {
        }

        public void AddLexicalClassifications(IEditorClassificationService service, SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
            => service.AddLexicalClassifications(text, textSpan, result, cancellationToken);

        public Task AddSyntacticClassificationsAsync(IEditorClassificationService service, Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
            => service.AddSyntacticClassificationsAsync(document, textSpan, result, cancellationToken);

        public Task AddSemanticClassificationsAsync(IEditorClassificationService service, Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
            => service.AddSemanticClassificationsAsync(document, textSpan, result, cancellationToken);

        public ClassifiedSpan AdjustStaleClassification(IEditorClassificationService service, SourceText text, ClassifiedSpan classifiedSpan)
            => service.AdjustStaleClassification(text, classifiedSpan);
    }
#pragma warning restore CS0612 // Type or member is obsolete
}
