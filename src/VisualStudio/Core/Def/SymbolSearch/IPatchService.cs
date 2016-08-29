// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    /// <summary>
    /// Used so we can mock out patching in unit tests.
    /// </summary>
    internal interface IPatchService
    {
        byte[] ApplyPatch(byte[] databaseBytes, byte[] patchBytes);
    }
}
