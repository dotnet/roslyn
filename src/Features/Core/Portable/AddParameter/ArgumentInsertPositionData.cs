// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal struct ArgumentInsertPositionData<TArgumentSyntax> where TArgumentSyntax : SyntaxNode
    {
        public ArgumentInsertPositionData(IMethodSymbol methodToUpdate, TArgumentSyntax argumentToInsert, int argumentInsertionIndex)
        {
            MethodToUpdate = methodToUpdate;
            ArgumentToInsert = argumentToInsert;
            ArgumentInsertionIndex = argumentInsertionIndex;
        }

        public IMethodSymbol MethodToUpdate { get; }
        public TArgumentSyntax ArgumentToInsert { get; }
        public int ArgumentInsertionIndex { get; }
    }
}
