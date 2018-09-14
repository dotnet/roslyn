// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Experimentation
{
    internal static class KeybindingsResetLogger
    {
        private const string Name = "KeybindingsResetDetector";

        public static void Log(string action)
        {
            Logger.Log(FunctionId.Experiment_KeybindingsReset, KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m[nameof(Name)] = Name;
                m[nameof(action)] = action;
            }));
        }
    }
}
