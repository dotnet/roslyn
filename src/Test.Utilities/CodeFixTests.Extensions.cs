// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;

namespace Test.Utilities
{
    public static class DiagnosticFixerTestsExtensions
    {
        internal static Solution Apply(CodeAction action)
        {
            System.Collections.Immutable.ImmutableArray<CodeActionOperation> operations = action.GetOperationsAsync(CancellationToken.None).Result;
            return operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        }
    }
}