// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Feedback;

internal interface IVisualStudioFeedbackFileWatcherService
{
    event EventHandler RecordingStarted;

    event EventHandler RecordingEnded;

    bool IsWatching { get; }

    bool IsRecordingForCurrentVisualStudioInstance { get; }

    int VisualStudioProcessId { get; }

    string TempDirectory { get; }
}
