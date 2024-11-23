// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

internal enum EditAndContinueResult
{
    NoChanges = 0,

    RudeEdits = 1,

    BuildError = 2,

    Succeeded = 3,

    ApplyUpdatesFailure = 4,

    // This gets returned when the result has already been handled by the provider,
    // and no further action is required.
    NoReport = 5,

    NotSupported = 6,
}
