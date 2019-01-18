// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        ExcludeCustomDebugInformation = PdbToXmlOptions.ExcludeCustomDebugInformation
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
                PdbValidationOptions.ExcludeCustomDebugInformation;

            return PdbToXmlOptions.ResolveTokens | PdbToXmlOptions.ThrowOnError | PdbToXmlOptions.IncludeEmbeddedSources | (PdbToXmlOptions)(options & mask);
        }
    }
}
