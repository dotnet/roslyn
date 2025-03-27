// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    [Flags]
    public enum DkmEvaluationResultFlags
    {
        None = 0x0,
        SideEffect = 0x1,
        Expandable = 0x2,
        Boolean = 0x4,
        BooleanTrue = 0x8,
        RawString = 0x10,
        Address = 0x20,
        ReadOnly = 0x40,
        ILInterpreter = 0x80,
        UnflushedSideEffects = 0x100,
        HasObjectId = 0x200,
        CanHaveObjectId = 0x400,
        CrossThreadDependency = 0x800,
        Invalid = 0x1000,
        Visualized = 0x2000,
        ExpandableError = 0x4000,
        ExceptionThrown = 0x8000,
        ReturnValue = 0x10000,
        IsBuiltInType = 0x20000,
        CanEvaluateNow = 0x40000,
        EnableExtendedSideEffectsUponRefresh = 0x80000,
        MemoryFuture = 0x100000,
        MemoryPast = 0x200000,
        MemoryGap = 0x400000,
        HasDataBreakpoint = 0x800000,
        CanFavorite = 0x1000000,
        IsFavorite = 0x2000000,
        HasFavorites = 0x4000000,
        IsObjectReplaceable = 0x8000000,
        ExpansionHasSideEffects = 0x10000000,
        CanEvaluateWithoutOptimization = 0x20000000,
        TruncatedString = 0x40000000
    }
}
