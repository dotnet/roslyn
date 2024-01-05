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
    public enum DkmEvaluationFlags
    {
        None,
        NoSideEffects = 4,
        ShowValueRaw = 128,
        HideNonPublicMembers = 512,
        NoToString = 1024,
        NoFormatting = 2048,
        NoRawView = 4096, // Not used in managed debugging
        NoQuotes = 8192,
        DynamicView = 16384,
        ResultsOnly = 32768,
        NoExpansion = 65536,
        FilterToFavorites = 0x40000,
        UseSimpleDisplayString = 0x80000,
        IncreaseMaxStringSize = 0x100000
    }
}
