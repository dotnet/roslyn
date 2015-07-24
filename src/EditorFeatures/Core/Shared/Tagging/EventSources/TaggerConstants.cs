// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        internal static int ComputeTimeDelayMS(this TaggerDelay behavior, ITextBuffer textBufferOpt)
        {
            if (TextBufferAssociatedViewService.AnyAssociatedViewHasFocus(textBufferOpt))
            {
                // TODO : should we remove TaggerBehavior enum all together and put NearImmediateDelay
                // const in Interaction?
                return ComputeTimeDelayMS(behavior);
            }

            return NonFocusDelay;
        }

        internal static int ComputeTimeDelayMS(this TaggerDelay behavior)
        {
            switch (behavior)
            {
                case TaggerDelay.NearImmediate:
                    return NearImmediateDelay;
                case TaggerDelay.Short:
                    return ShortDelay;
                case TaggerDelay.Medium:
                    return MediumDelay;
                case TaggerDelay.OnIdle:
                default:
                    return IdleDelay;
            }
        }
    }
}
