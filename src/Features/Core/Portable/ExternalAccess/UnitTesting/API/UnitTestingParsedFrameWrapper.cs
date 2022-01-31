// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.StackTraceExplorer;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingParsedFrameWrapper
    {
        internal ParsedFrame UnderlyingObject { get; }

        public UnitTestingParsedFrameWrapper(ParsedFrame parsedFrame)
        {
            UnderlyingObject = parsedFrame;
        }
    }
}
