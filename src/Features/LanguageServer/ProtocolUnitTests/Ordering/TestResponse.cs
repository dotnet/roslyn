// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable disable
using System;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    internal class TestResponse
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public bool ContextHasSolution { get; set; }
    }
}
