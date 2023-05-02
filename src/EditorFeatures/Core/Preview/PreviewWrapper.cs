// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    [NonDefaultable]
    internal readonly struct PreviewWrapper : IDisposable
    {
        private readonly object _preview;

        private PreviewWrapper(IReferenceCountedDisposable<IDisposable> preview)
        {
            _preview = preview.AddReference();
        }

        private PreviewWrapper(object preview)
        {
            _preview = preview;
        }

        public static PreviewWrapper FromReferenceCounted(IReferenceCountedDisposable<IDisposable> preview)
        {
            return new PreviewWrapper(preview);
        }

        public static PreviewWrapper FromNonReferenceCounted(object preview)
        {
            Debug.Assert(preview is not IReferenceCountedDisposable<IDisposable>);
            return new PreviewWrapper(preview);
        }

        public object Preview
        {
            get
            {
                if (_preview is IReferenceCountedDisposable<IDisposable> preview)
                    return preview.Target;

                return _preview;
            }
        }

        public void Dispose()
        {
            (Preview as IDisposable)?.Dispose();
        }
    }
}
