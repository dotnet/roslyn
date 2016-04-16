// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
        ShowValueRaw = 128,
        HideNonPublicMembers = 512,
        NoToString = 1024,
        NoFormatting = 2048,
        NoRawView = 4096, // Not used in managed debugging
        NoQuotes = 8192,
        DynamicView = 16384,
        ResultsOnly = 32768,
        NoExpansion = 65536,
    }
}
