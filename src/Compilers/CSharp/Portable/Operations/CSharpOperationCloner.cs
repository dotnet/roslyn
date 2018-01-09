// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpOperationCloner : OperationCloner
    {
        public static OperationCloner Instance { get; } = new CSharpOperationCloner();
    }
}
