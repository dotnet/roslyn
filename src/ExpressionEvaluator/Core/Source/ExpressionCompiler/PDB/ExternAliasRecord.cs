// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct ExternAliasRecord
    {
        public readonly string Alias;

        // IAssemblySymbol or AssemblyIdentity
        public readonly object TargetAssembly;

        public ExternAliasRecord(string alias, IAssemblySymbol targetAssembly)
        {
            Debug.Assert(alias != null);
            Debug.Assert(targetAssembly != null);

            Alias = alias;
            TargetAssembly = targetAssembly;
        }

        public ExternAliasRecord(string alias, AssemblyIdentity targetIdentity)
        {
            Debug.Assert(alias != null);
            Debug.Assert(targetIdentity != null);

            Alias = alias;
            TargetAssembly = targetIdentity;
        }
    }
}
