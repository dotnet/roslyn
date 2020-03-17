// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal static class TaggerConstants
    {
        internal const int NearImmediateDelay = 50;
        internal const int ShortDelay = 250;
        internal const int MediumDelay = 500;
        internal const int IdleDelay = 1500;
        internal const int NonFocusDelay = 3000;

        internal static TimeSpan ComputeTimeDelay(this TaggerDelay behavior, ITextBuffer textBufferOpt)
        {
            if (TextBufferAssociatedViewService.AnyAssociatedViewHasFocus(textBufferOpt))
            {
                // TODO : should we remove TaggerBehavior enum all together and put NearImmediateDelay
                // const in Interaction?
                return ComputeTimeDelay(behavior);
            }

            return TimeSpan.FromMilliseconds(NonFocusDelay);
        }

        internal static TimeSpan ComputeTimeDelay(this TaggerDelay behavior)
        {
            switch (behavior)
            {
                case TaggerDelay.NearImmediate:
                    return TimeSpan.FromMilliseconds(NearImmediateDelay);
                case TaggerDelay.Short:
                    return TimeSpan.FromMilliseconds(ShortDelay);
                case TaggerDelay.Medium:
                    return TimeSpan.FromMilliseconds(MediumDelay);
                case TaggerDelay.OnIdle:
                default:
                    return TimeSpan.FromMilliseconds(IdleDelay);
            }
        }
    }
}
