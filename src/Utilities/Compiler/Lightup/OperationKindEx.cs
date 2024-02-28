// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class OperationKindEx
    {
        public const OperationKind FunctionPointerInvocation = (OperationKind)0x78;
        public const OperationKind ImplicitIndexerReference = (OperationKind)0x7b;
        public const OperationKind Utf8String = (OperationKind)0x7c;
        public const OperationKind Attribute = (OperationKind)0x7d;
        public const OperationKind CollectionExpression = (OperationKind)0x7f;
    }
}

#endif
