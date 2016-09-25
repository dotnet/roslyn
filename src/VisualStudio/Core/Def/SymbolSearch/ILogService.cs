// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    /// <summary>
    /// Used so we can mock out logging in unit tests.
    /// </summary>
    internal interface ILogService
    {
        Task LogExceptionAsync(Exception e, string text);
        Task LogInfoAsync(string text);
    }
}