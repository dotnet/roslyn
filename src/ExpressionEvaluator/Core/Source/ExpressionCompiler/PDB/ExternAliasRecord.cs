// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
