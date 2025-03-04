// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if HAS_IOPERATION

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class OperationKinds
    {
        public static ImmutableArray<OperationKind> MemberReference { get; }
            = ImmutableArray.Create(
                OperationKind.EventReference,
                OperationKind.FieldReference,
                OperationKind.MethodReference,
                OperationKind.PropertyReference);
    }
}

#endif
