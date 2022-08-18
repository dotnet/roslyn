// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class OperationKindEx
    {
        public const OperationKind UsingDeclaration = (OperationKind)0x6c;
        public const OperationKind FunctionPointerInvocation = (OperationKind)0x78;
        public const OperationKind ImplicitIndexerReference = (OperationKind)0x7b;
        public const OperationKind Attribute = (OperationKind)0x7d;
    }
}

#endif
