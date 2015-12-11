// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    internal enum CommandIds : uint
    {
        // TODO (crwilcox): should all of these be in the editoroperations?
        SmartExecute = 0x103,
        AbortExecution = 0x104,
        Reset = 0x105,
        HistoryNext = 0x0106,
        HistoryPrevious = 0x0107,
        ClearScreen = 0x0108,
        BreakLine = 0x0109,
        SearchHistoryNext = 0x010A,
        SearchHistoryPrevious = 0x010B,
        ExecuteInInteractiveWindow = 0x010C,
        CopyToInteractiveWindow = 0x010D,
        CopyInputs = 0x010E,
    }
}
