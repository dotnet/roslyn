// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        RefPartialModOrdering,
    }
}
