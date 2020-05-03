// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    [Flags]
    public enum DkmEvaluationResultFlags
    {
        None,
        SideEffect,
        Expandable,
        Boolean = 4,
        BooleanTrue = 8,
        RawString = 16,
        Address = 32,
        ReadOnly = 64,
        ILInterpreter = 128,
        UnflushedSideEffects = 256,
        HasObjectId = 512,
        CanHaveObjectId = 1024,
        CrossThreadDependency = 2048,
        Invalid = 4096,
        Visualized = 8192,
        ExpandableError = 16384,
        ExceptionThrown = 32768,
        CanFavorite = 0x1000000,
        IsFavorite = 0x2000000,
        HasFavorites = 0x4000000,
    }
}
