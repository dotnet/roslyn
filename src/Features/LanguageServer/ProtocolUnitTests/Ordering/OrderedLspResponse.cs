// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    internal class OrderedLspResponse
    {
        public int RequestOrder { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime TimeAfterFirstAwait { get; set; }
    }
}
