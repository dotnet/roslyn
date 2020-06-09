// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal enum EditAndContinueErrorCode
    {
        ErrorReadingFile = 1,
        CannotApplyChangesUnexpectedError = 2,
        ChangesNotAppliedWhileRunning = 3,
        ChangesDisallowedWhileStoppedAtException = 4,
        DocumentIsOutOfSyncWithDebuggee = 5,
        UnableToReadSourceFileOrPdb = 6,
    }
}
