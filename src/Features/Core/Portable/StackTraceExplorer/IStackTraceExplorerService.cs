// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal interface IStackTraceExplorerService : ILanguageService
    {
        /// <summary>
        /// Given the <see cref="ParsedStackFrame.TypeSpan"/>, get the equivalent name
        /// in metadata that can be used to look up the type
        /// </summary>
        string GetTypeMetadataName(string className);

        /// <summary>
        /// Given the <see cref="ParsedStackFrame.MethodSpan"/>, get the symbol name
        /// for to match <see cref="IMethodSymbol"/> of a given type
        /// </summary>
        string GetMethodSymbolName(string methodName);
    }
}
