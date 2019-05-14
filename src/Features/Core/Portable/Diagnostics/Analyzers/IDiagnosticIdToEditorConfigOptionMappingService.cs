// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Language service to map <see cref="IDEDiagnosticIds"/> to an unique editorconfig code style option,
    /// if any such option exists.
    /// </summary>
    internal interface IDiagnosticIdToEditorConfigOptionMappingService : ILanguageService
    {
        /// <summary>
        /// Maps each diagnostic ID in <see cref="IDEDiagnosticIds"/> to an <see cref="IOption"/> as follows:
        ///   1. If the diagnostic has a unique code style option, such that diagnostic's
        ///      severity can be configured in .editorconfig with an entry such as
        ///          "%option_name% = %option_value%:%severity%
        ///      then this option is returned.
        ///   2. Otherwise, returns null.
        /// </summary>
        IOption GetMappedEditorConfigOption(string diagnosticId);
    }
}
