// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    internal static class TestUtils
    {
        public static readonly string HostRootPath = Path.Combine(Path.GetDirectoryName(typeof(TestUtils).Assembly.Location)!, "Host");
    }
}
