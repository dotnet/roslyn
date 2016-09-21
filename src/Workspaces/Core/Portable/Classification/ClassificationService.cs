// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// A language service that identifies classifications for ranges of a <see cref="Document"/>.
    /// </summary>
    internal abstract class ClassificationService : ILanguageService
    {
        /// <summary>
        /// Gets the appropriate <see cref="ClassificationService"/> for the specified <see cref="Document"/> .
        /// </summary>
        public static ClassificationService GetService(Document document)
        {
            return GetService(document.Project.Solution.Workspace, document.Project.Language);
        }

        /// <summary>
        /// Gets the appropriate <see cref="ClassificationService"/> for the specified <see cref="Workspace"/> and language.
        /// </summary>
        public static ClassificationService GetService(Workspace workspace, string language)
        {
            return workspace.Services.GetLanguageServices(language)?.GetService<ClassificationService>() ?? NoOpService.Instance;
        }

        private class NoOpService : ClassificationService
        {
            public static readonly ClassificationService Instance = new NoOpService();
        }

        /// <summary>
        /// Gets the lexical classifications for a range of a <see cref="SourceText"/>.
        /// This API intended to be a much faster classification than via <see cref="GetSyntacticClassificationsAsync(Document, TextSpan, CancellationToken)"/>
        /// as it does not require the entire <see cref="SyntaxTree"/> for the <see cref="Document"/> to have been parsed.
        /// </summary>
        public virtual ImmutableArray<ClassifiedSpan> GetLexicalClassifications(SourceText text, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ImmutableArray<ClassifiedSpan>.Empty;
        }

        /// <summary>
        /// Adjusts an existing classification after a text change.
        /// </summary>
        public virtual ClassifiedSpan AdjustClassification(SourceText changedText, ClassifiedSpan classifiedSpan)
        {
            return classifiedSpan;
        }

        /// <summary>
        /// Gets the syntax classficiations for a range of the specified <see cref="Document"/>.
        /// This API is intended to be a much faster classification than via <see cref="GetSemanticClassificationsAsync"/>
        /// as it should not require identifying symbol declarations and references.
        /// </summary>
        public virtual Task<ImmutableArray<ClassifiedSpan>> GetSyntacticClassificationsAsync(Document document, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.EmptyImmutableArray<ClassifiedSpan>();
        }

        /// <summary>
        /// Get the semantic classifcations for a range of the specified <see cref="Document"/>.
        /// This API typically identifies classifications for declarations and references to symbols such as types and members.
        /// </summary>
        public virtual Task<ImmutableArray<ClassifiedSpan>> GetSemanticClassificationsAsync(Document document, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.EmptyImmutableArray<ClassifiedSpan>();
        }
    }
}
