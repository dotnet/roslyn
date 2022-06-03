// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct ExternAliasRecord
    {
        public readonly string Alias;

        /// <summary>
        /// IAssemblySymbolInternal or AssemblyIdentity
        /// </summary>
        public readonly object TargetAssembly;

        public ExternAliasRecord(string alias, IAssemblySymbolInternal targetAssembly)
        {
            RoslynDebug.AssertNotNull(alias);
            RoslynDebug.AssertNotNull(targetAssembly);

            Alias = alias;
            TargetAssembly = targetAssembly;
        }

        public ExternAliasRecord(string alias, AssemblyIdentity targetIdentity)
        {
            RoslynDebug.AssertNotNull(alias);
            RoslynDebug.AssertNotNull(targetIdentity);

            Alias = alias;
            TargetAssembly = targetIdentity;
        }
    }
}
