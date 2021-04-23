// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Tagging
{
    /// <summary>
    /// Information provided to the <see cref="AbstractAsynchronousTaggerProvider{TTag}"/> when 
    /// <see cref="ITaggerEventSource.Changed"/> fires.
    /// </summary>
    internal class TaggerEventArgs : EventArgs
    {
        public TaggerEventArgs()
        {
        }

        [Obsolete("Legacy api for TypeScript.  Can be removed once they move to the no-arg constructor.")]
        public TaggerEventArgs(TaggerDelay delay)
        {
        }
    }
}
