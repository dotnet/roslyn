// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Used so we can mock out logging in unit tests.
    /// </summary>
    internal interface ISymbolSearchLogService
    {
        Task LogExceptionAsync(string exception, string text);
        Task LogInfoAsync(string text);
    }
}
