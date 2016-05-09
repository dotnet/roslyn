// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

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
    }
}
