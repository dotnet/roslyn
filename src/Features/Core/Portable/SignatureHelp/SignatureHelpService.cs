// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

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
        /// Gets the signatures available at the caret position.
        /// </summary>
        /// <param name="providers">An array of <see cref="ISignatureHelpProvider"/>s from which to retrieve signatures.</param>
        /// <param name="document">The document that signature help is occuring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="options">Optional options that override the default options.</param>
        /// <param name="cancellationToken"></param>
        public abstract Task<SignatureHelpItems> GetSignaturesAsync(
            ImmutableArray<ISignatureHelpProvider> providers,
            Document document,
            int caretPosition,
            SignatureHelpTriggerInfo trigger = default(SignatureHelpTriggerInfo),
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
