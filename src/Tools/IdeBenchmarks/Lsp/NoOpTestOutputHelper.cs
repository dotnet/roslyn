// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace IdeBenchmarks.Lsp
{
    internal class NoOpTestOutputHelper : ITestOutputHelper
    {
        public static readonly ITestOutputHelper Instance = new NoOpTestOutputHelper();
        private NoOpTestOutputHelper() { }
        public void WriteLine(string message) { }
        public void WriteLine(string format, params object[] args) { }
    }
}
