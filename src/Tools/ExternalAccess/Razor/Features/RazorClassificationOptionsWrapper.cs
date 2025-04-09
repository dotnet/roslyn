// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal readonly struct RazorClassificationOptionsWrapper
    {
        public static RazorClassificationOptionsWrapper Default = new(ClassificationOptions.Default);

        internal readonly ClassificationOptions UnderlyingObject;

        public RazorClassificationOptionsWrapper(ClassificationOptions underlyingObject)
            => UnderlyingObject = underlyingObject;
    }
}
