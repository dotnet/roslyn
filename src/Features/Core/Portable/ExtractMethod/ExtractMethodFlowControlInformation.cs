// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    private readonly Compilation _compilation;
    public readonly int BreakStatementCount;
    public readonly int ContinueStatementCount;
    public readonly int ReturnStatementCount;
    public readonly bool EndPointIsReachable;

    private readonly Dictionary<FlowControlKind, object?> _flowValues = [];

    public readonly ITypeSymbol ControlFlowValueType;

    public ExtractMethodFlowControlInformation(
        Compilation compilation,
        int breakStatementCount,
        int continueStatementCount,
        int returnStatementCount,
        bool endPointIsReachable)
    {
        _compilation = compilation;
        BreakStatementCount = breakStatementCount;
        ContinueStatementCount = continueStatementCount;
        ReturnStatementCount = returnStatementCount;
        EndPointIsReachable = endPointIsReachable;

        var controlFlowKindCount = GetControlFlowKindCount();
        if (controlFlowKindCount == 2)
        {
            ControlFlowValueType = _compilation.GetSpecialType(SpecialType.System_Boolean);
            AssignFlowValues([false, true]);
        }
        else if (controlFlowKindCount == 3)
        {
            ControlFlowValueType = _compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(_compilation.GetSpecialType(SpecialType.System_Boolean));
            AssignFlowValues([false, true, null]);
        }
        else if (controlFlowKindCount == 4)
        {
            ControlFlowValueType = _compilation.GetSpecialType(SpecialType.System_Int32);
            AssignFlowValues([0, 1, 2, 3]);
        }
        else
        {
            ControlFlowValueType = _compilation.GetSpecialType(SpecialType.System_Void);
        }
    }

    private void AssignFlowValues(object?[] values)
    {
        var valuesIndex = 0;
        if (BreakStatementCount > 0)
            _flowValues[FlowControlKind.Break] = values[valuesIndex++];
        if (ContinueStatementCount > 0)
            _flowValues[FlowControlKind.Continue] = values[valuesIndex++];
        if (ReturnStatementCount > 0)
            _flowValues[FlowControlKind.Return] = values[valuesIndex++];
        if (EndPointIsReachable)
            _flowValues[FlowControlKind.FallThrough] = values[valuesIndex++];
    }

    private int GetControlFlowKindCount()
    {
        var flowControlKinds =
            (BreakStatementCount > 0 ? 1 : 0) +
            (ContinueStatementCount > 0 ? 1 : 0) +
            (ReturnStatementCount > 0 ? 1 : 0) +
            (EndPointIsReachable ? 1 : 0);
        return flowControlKinds;
    }

    public bool NeedsControlFlowValue()
        => ControlFlowValueType.SpecialType != SpecialType.System_Void;

    public object? GetBreakFlowValue()
        => _flowValues[FlowControlKind.Break];

    public object? GetContinueFlowValue()
        => _flowValues[FlowControlKind.Continue];

    public object? GetReturnFlowValue()
        => _flowValues[FlowControlKind.Return];

    public object? GetFallThroughFlowValue()
        => _flowValues[FlowControlKind.FallThrough];
}
