// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    /// <summary>
    /// A per language service for constructing context dependent list of signatures that 
    /// can be presented to a user while typing in an editor.
    /// </summary>
    internal abstract class SignatureHelpService : ILanguageService
    {
        /// <summary>
        /// Gets the <see cref="SignatureHelpService"/> corresponding to the specified document.
        /// </summary>
        public static SignatureHelpService GetService(Document document)
        {
            return GetService(document.Project.Solution.Workspace, document.Project.Language);
        }

        /// <summary>
        /// Gets the <see cref="SignatureHelpService"/> corresponding to the specified workspace and language.
        /// </summary>
        public static SignatureHelpService GetService(Workspace workspace, string language)
        {
            return workspace.Services.GetLanguageServices(language)?.GetService<SignatureHelpService>() ?? NoOpService.Instance;
        }

        private class NoOpService : SignatureHelpService
        {
            public static readonly NoOpService Instance = new NoOpService();
        }

        /// <summary>
        /// Returns true if the character recently inserted in the text should trigger SignatureHelp.
        /// </summary>
        /// <param name="text">The document text to trigger completion within </param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The potential triggering action.</param>
        /// <param name="options">Optional options that override the default options.</param>
        /// <remarks>
        /// This API uses SourceText instead of Document so implementations can only be based on text, not syntax or semantics.
        /// </remarks>
        public virtual bool ShouldTriggerSignatureHelp(
            SourceText text,
            int caretPosition,
            SignatureHelpTrigger trigger,
            OptionSet options = null)
        {
            return false;
        }

        /// <summary>
        /// Gets the signatures available at the caret position.
        /// </summary>
        /// <param name="document">The document that signature help is occuring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="options">Optional options that override the default options.</param>
        /// <param name="cancellationToken"></param>
        public virtual Task<SignatureList> GetSignaturesAsync(
            Document document,
            int caretPosition,
            SignatureHelpTrigger trigger = default(SignatureHelpTrigger),
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return EmptyListTask;
        }

        public virtual Task<ImmutableArray<TaggedText>> GetItemDocumentationAsync(Document document, SignatureHelpItem item, CancellationToken cancellationToken = default(CancellationToken))
        {
            return EmptyTextTask;
        }

        public virtual Task<ImmutableArray<TaggedText>> GetParameterDocumentationAsync(Document document, SignatureHelpParameter parameter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return EmptyTextTask;
        }

        private static readonly Task<SignatureList> EmptyListTask = Task.FromResult(SignatureList.Empty);
        internal static readonly Task<ImmutableArray<TaggedText>> EmptyTextTask = Task.FromResult(ImmutableArray<TaggedText>.Empty);
    }
}
