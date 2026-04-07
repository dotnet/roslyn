// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests
{
    using System;
    using global::Xunit;
    using global::Xunit.Harness;

    public class VisualStudioInstanceKeyTests
    {
        private static readonly Random _random = new Random(Environment.TickCount);

        private static int NextInt(int maxValue)
        {
            lock (_random)
            {
                return _random.Next(maxValue);
            }
        }

        [Theory]
        [InlineData(new object[] { new string[] { } })]
        [InlineData(new object[] { new string[] { "Key=Value" } })]
        [InlineData(new object[] { new string[] { "Key=" } })]
        [InlineData(new object[] { new string[] { "Key==", "Key=;" } })]
        [InlineData(new object[] { new string[] { "DOTNET_MULTILEVEL_LOOKUP=", "DOTNET_INSTALL_DIR=", "DotNetRoot=", "DotNetTool=" } })]
        public void TestSerializationWithEnvironmentVariables(string[] environmentVariables)
        {
            var versions = (VisualStudioVersion[])Enum.GetValues(typeof(VisualStudioVersion));
            var version = versions[NextInt(versions.Length)];

            var rootSuffix = NextInt(2) switch
            {
                0 => string.Empty,
                _ => "Exp",
            };

            var maxAttempts = NextInt(5);

            var key = new VisualStudioInstanceKey(version, rootSuffix, maxAttempts, environmentVariables);
            var serialized = key.SerializeToString();
            var recreatedKey = VisualStudioInstanceKey.DeserializeFromString(serialized);
            Assert.Equal(key, recreatedKey);
            Assert.Equal(serialized, recreatedKey.SerializeToString());
        }
    }
}
