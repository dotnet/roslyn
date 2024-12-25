// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging;

internal static class TaggerConstants
{
    internal static TimeSpan ComputeTimeDelay(this TaggerDelay behavior, ITextBuffer textBufferOpt)
    {
        if (TextBufferAssociatedViewService.AnyAssociatedViewHasFocus(textBufferOpt))
        {
            // TODO : should we remove TaggerBehavior enum all together and put NearImmediateDelay
            // const in Interaction?
            return ComputeTimeDelay(behavior);
        }

        return DelayTimeSpan.NonFocus;
    }

    internal static TimeSpan ComputeTimeDelay(this TaggerDelay behavior)
        => behavior switch
        {
            TaggerDelay.NearImmediate => DelayTimeSpan.NearImmediate,
            TaggerDelay.Short => DelayTimeSpan.Short,
            TaggerDelay.Medium => DelayTimeSpan.Medium,
            TaggerDelay.OnIdle => DelayTimeSpan.Idle,
            TaggerDelay.OnIdleWithLongDelay => DelayTimeSpan.IdleWithLongDelay,
            _ => DelayTimeSpan.NonFocus,
        };
}
