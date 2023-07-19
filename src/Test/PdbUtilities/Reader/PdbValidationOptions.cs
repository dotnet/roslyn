// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.DiaSymReader.Tools;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Flags]
    public enum PdbValidationOptions
    {
        Default = 0,
        SkipConversionValidation = 1,
        ExcludeDocuments = PdbToXmlOptions.ExcludeDocuments,
        ExcludeMethods = PdbToXmlOptions.ExcludeMethods,
        ExcludeSequencePoints = PdbToXmlOptions.ExcludeSequencePoints,
        ExcludeScopes = PdbToXmlOptions.ExcludeScopes,
        ExcludeNamespaces = PdbToXmlOptions.ExcludeNamespaces,
        ExcludeAsyncInfo = PdbToXmlOptions.ExcludeAsyncInfo,
        ExcludeCustomDebugInformation = PdbToXmlOptions.ExcludeCustomDebugInformation,
        IncludeModuleDebugInfo = PdbToXmlOptions.IncludeModuleDebugInfo
    }

    public static class PdbValidationOptionsExtensions
    {
        public static PdbToXmlOptions ToPdbToXmlOptions(this PdbValidationOptions options)
        {
            const PdbValidationOptions mask =
                PdbValidationOptions.ExcludeDocuments |
                PdbValidationOptions.ExcludeMethods |
                PdbValidationOptions.ExcludeSequencePoints |
                PdbValidationOptions.ExcludeScopes |
                PdbValidationOptions.ExcludeNamespaces |
                PdbValidationOptions.ExcludeAsyncInfo |
                PdbValidationOptions.ExcludeCustomDebugInformation |
                PdbValidationOptions.IncludeModuleDebugInfo;

            return PdbToXmlOptions.ResolveTokens | PdbToXmlOptions.ThrowOnError | PdbToXmlOptions.IncludeEmbeddedSources | (PdbToXmlOptions)(options & mask);
        }
    }
}
