// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.KeybindingReset;

internal static class KeybindingsResetLogger
{
    private const string Name = "KeybindingsResetDetector";

    public static void Log(string action)
    {
        Logger.Log(FunctionId.Experiment_KeybindingsReset, KeyValueLogMessage.Create(LogType.UserAction, static (m, action) =>
        {
            m[nameof(Name)] = Name;
            m[nameof(action)] = action;
        }, action));
    }
}
