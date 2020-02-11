﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public enum CompilerFeature
    {
        Async,
        Dynamic,
        ExpressionBody,
        Determinism,
        Iterator,
        LocalFunctions,
        Params,
        Var,
        Tuples,
        RefLocalsReturns,
        ReadOnlyReferences,
        OutVar,
        Patterns,
        DefaultLiteral,
        AsyncMain,
        IOperation,
        Dataflow,
        NonTrailingNamedArgs,
        PrivateProtected,
        PEVerifyCompat,
        RefConditionalOperator,
        TupleEquality,
        StackAllocInitializer,
        NullCoalescingAssignment,
        AsyncStreams,
        NullableReferenceTypes,
        DefaultInterfaceImplementation,
        LambdaDiscardParameters,
    }
}
