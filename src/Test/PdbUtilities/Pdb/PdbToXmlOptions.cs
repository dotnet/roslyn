// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Test.PdbUtilities
{
    [Flags]
    public enum PdbToXmlOptions
    {
        Default = 0,
        ResolveTokens = 1 << 1,
        IncludeTokens = 1 << 2,
        IncludeMethodSpans = 1 << 3,
        ThrowOnError = 1 << 4,
    }
}
