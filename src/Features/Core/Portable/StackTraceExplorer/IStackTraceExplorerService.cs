// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.StackTraceExplorer
{
    internal interface IStackTraceExplorerService : ILanguageService
    {
        /// <summary>
        /// Given the type name from <see cref="StackFrameCompilationUnit.MethodDeclaration"/>, get the equivalent name
        /// in metadata that can be used to look up the type
        /// </summary>
        string GetTypeMetadataName(string className);

        /// <summary>
        /// Given the method name from <see cref="StackFrameCompilationUnit.MethodDeclaration"/>, get the symbol name
        /// for to match <see cref="IMethodSymbol"/> of a given type
        /// </summary>
        string GetMethodSymbolName(string methodName);
    }
}
