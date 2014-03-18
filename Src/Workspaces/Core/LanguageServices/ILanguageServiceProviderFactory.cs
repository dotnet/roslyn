// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ILanguageServiceProviderFactory : IWorkspaceService
    {
        /// <summary>
        /// The workspace service provider that created this language service provider.
        /// </summary>
        IWorkspaceServiceProvider WorkspaceServices { get; }

        /// <summary>
        /// The list of languages supported.
        /// </summary>
        IEnumerable<string> SupportedLanguages { get; }

        /// <summary>
        /// Returns true if the language is supported.
        /// </summary>
        bool IsSupported(string language);

        /// <summary>
        /// Gets a language service provider for the language.
        /// If the language is not supported a NotSupportedException is thrown.
        /// </summary>
        ILanguageServiceProvider GetLanguageServiceProvider(string language);

        /// <summary>
        /// this will bring in all of our dlls. use it with caution.
        /// </summary>
        IEnumerable<T> GetServices<T>() where T : ILanguageService;
    }
}