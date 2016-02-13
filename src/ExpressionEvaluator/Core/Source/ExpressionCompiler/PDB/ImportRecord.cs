// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct ImportRecord
    {
        public readonly ImportTargetKind TargetKind;
        public readonly string Alias;

        // target type of a type import (C#)
        public readonly ITypeSymbol TargetType;

        // target of an import (type, namespace or XML namespace) that needs to be bound (C#, VB)
        public readonly string TargetString;

        // target assembly of a namespace import (C#, Portable)
        public readonly IAssemblySymbol TargetAssembly;

        // target assembly of a namespace import is identified by an extern alias which needs to be bound in the context (C#, native PDB)
        public readonly string TargetAssemblyAlias;

        public ImportRecord(
            ImportTargetKind targetKind,
            string alias = null,
            ITypeSymbol targetType = null,
            string targetString = null,
            IAssemblySymbol targetAssembly = null,
            string targetAssemblyAlias = null)
        {
            TargetKind = targetKind;
            Alias = alias;
            TargetType = targetType;
            TargetString = targetString;
            TargetAssembly = targetAssembly;
            TargetAssemblyAlias = targetAssemblyAlias;
        }
    }
}
