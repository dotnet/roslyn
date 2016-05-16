// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace ProcessWatchdog
{
    internal enum ErrorCode
    {
        None = 0,
        ProcessTimedOut = 1,
        InvalidTimeLimit = 2,
        InvalidPollingInterval = 3,
        ProcDumpNotFound = 4,
        CannotTakeScreenShotNoConsoleSession = 5,
        CannotTakeScreenShotUnexpectedError = 6
    }
}
