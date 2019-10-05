// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal enum EditAndContinueErrorCode
    {
        ErrorReadingFile = 1,
        CannotApplyChangesUnexpectedError = 2,
        ChangesNotAppliedWhileRunning = 3,
        ChangesDisallowedWhileStoppedAtException = 4,
        DocumentIsOutOfSyncWithDebuggee = 5,
    }
}
