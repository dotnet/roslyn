// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Rename
{
    public enum RenameEntityKind
    {
        /// <summary>
        /// mentions that the result is for the base symbol of the rename
        /// </summary>
        BaseSymbol = 0,

        /// <summary>
        /// mentions that the result is for the overloaded symbols of the rename
        /// </summary>
        OverloadedSymbols = 1
    }
}
