// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Test.PdbUtilities
{
    [Flags]
    public enum PdbToXmlOptions
    {
        Default = 0,
        ThrowOnError = 1 << 1,
        ResolveTokens = 1 << 2,
        IncludeTokens = 1 << 3,
        IncludeMethodSpans = 1 << 4,
        ExcludeDocuments = 1 << 5,
        ExcludeMethods = 1 << 6,
        ExcludeSequencePoints = 1 << 7,
        ExcludeScopes = 1 << 8,
        ExcludeNamespaces = 1 << 9,
        ExcludeAsyncInfo = 1 << 10,
        ExcludeCustomDebugInformation = 1 << 11,
    }
}
