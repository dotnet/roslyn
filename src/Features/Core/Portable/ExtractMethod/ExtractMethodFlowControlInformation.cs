// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal sealed class ExtractMethodFlowControlInformation
{
    private enum FlowControlKind
    {
        Break,
        Continue,
        Return,
        FallThrough,
    }

    public readonly int BreakStatementCount;
    public readonly int ContinueStatementCount;
    public readonly int ReturnStatementCount;
    public readonly bool EndPointIsReachable;

    private readonly Dictionary<FlowControlKind, object?> _flowValues = [];

    public readonly ITypeSymbol ControlFlowValueType;

    private ExtractMethodFlowControlInformation(
        int breakStatementCount,
        int continueStatementCount,
        int returnStatementCount,
        bool endPointIsReachable,
        ITypeSymbol controlFlowValueType,
        Dictionary<FlowControlKind, object?> flowValues)
    {
        BreakStatementCount = breakStatementCount;
        ContinueStatementCount = continueStatementCount;
        ReturnStatementCount = returnStatementCount;
        EndPointIsReachable = endPointIsReachable;
        ControlFlowValueType = controlFlowValueType;
        _flowValues = flowValues;
    }

    public static ExtractMethodFlowControlInformation Create(
        Compilation compilation,
        bool supportsComplexFlowControl,
        int breakStatementCount,
        int continueStatementCount,
        int returnStatementCount,
        bool endPointIsReachable)
    {
        var controlFlowValueType = compilation.GetSpecialType(SpecialType.System_Void);
        var flowValues = new Dictionary<FlowControlKind, object?>();
        if (supportsComplexFlowControl)
        {
            var controlFlowKindCount = GetControlFlowKindCount();
            if (controlFlowKindCount == 2)
            {
                controlFlowValueType = compilation.GetSpecialType(SpecialType.System_Boolean);
                AssignFlowValues([false, true]);
            }
            else if (controlFlowKindCount == 3)
            {
                controlFlowValueType = compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(compilation.GetSpecialType(SpecialType.System_Boolean));
                AssignFlowValues([false, true, null]);
            }
            else if (controlFlowKindCount == 4)
            {
                controlFlowValueType = compilation.GetSpecialType(SpecialType.System_Int32);
                AssignFlowValues([0, 1, 2, 3]);
            }
        }

        return new(
            breakStatementCount,
            continueStatementCount,
            returnStatementCount,
            endPointIsReachable,
            controlFlowValueType,
            flowValues);

        void AssignFlowValues(object?[] values)
        {
            var valuesIndex = 0;
            if (breakStatementCount > 0)
                flowValues[FlowControlKind.Break] = values[valuesIndex++];
            if (continueStatementCount > 0)
                flowValues[FlowControlKind.Continue] = values[valuesIndex++];
            if (returnStatementCount > 0)
                flowValues[FlowControlKind.Return] = values[valuesIndex++];
            if (endPointIsReachable)
                flowValues[FlowControlKind.FallThrough] = values[valuesIndex++];

            Contract.ThrowIfFalse(valuesIndex == values.Length);
        }

        int GetControlFlowKindCount()
        {
            var flowControlKinds =
                (breakStatementCount > 0 ? 1 : 0) +
                (continueStatementCount > 0 ? 1 : 0) +
                (returnStatementCount > 0 ? 1 : 0) +
                (endPointIsReachable ? 1 : 0);
            return flowControlKinds;
        }
    }

    public bool NeedsControlFlowValue()
        => ControlFlowValueType.SpecialType != SpecialType.System_Void;

    public bool HasUniformControlFlow()
        => (BreakStatementCount > 0, ContinueStatementCount > 0, ReturnStatementCount > 0, EndPointIsReachable) switch
        {
            // All breaks on all paths.
            (true, false, false, false) => true,
            // All continues on all paths.
            (false, true, false, false) => true,
            // All returns on all paths.
            (false, false, true, false) => true,
            _ => false,
        };

    public bool TryGetBreakFlowValue(out object? value)
        => _flowValues.TryGetValue(FlowControlKind.Break, out value);

    public bool TryGetContinueFlowValue(out object? value)
        => _flowValues.TryGetValue(FlowControlKind.Continue, out value);

    public bool TryGetReturnFlowValue(out object? value)
        => _flowValues.TryGetValue(FlowControlKind.Return, out value);

    public bool TryGetFallThroughFlowValue(out object? value)
        => _flowValues.TryGetValue(FlowControlKind.FallThrough, out value);
}
