// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    /// <summary>
    /// A per language service for constructing context dependent list of signatures that 
    /// can be presented to a user while typing in an editor.
    /// </summary>
    internal abstract class SignatureHelpService : ILanguageService
    {
        /// <summary>
        /// Gets the service corresponding to the specified document.
        /// </summary>
        public static SignatureHelpService GetService(Document document)
        {
            return document.Project.LanguageServices.GetService<SignatureHelpService>();
        }

        /// <summary>
        /// The language from <see cref="LanguageNames"/> this service corresponds to.
        /// </summary>
        public abstract string Language { get; }

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
        public abstract Task<SignatureList> GetSignaturesAsync(
            Document document,
            int caretPosition,
            SignatureHelpTrigger trigger = default(SignatureHelpTrigger),
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
