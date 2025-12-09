// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit
{
    using Xunit.Threading;

    internal class Instances
    {
        /// <summary>
        /// This test method is manually injected into the discovery results to support easy launching and/or debugging
        /// of Visual Studio instances.
        /// </summary>
        /// <seealso cref="IdeInstanceTestCase"/>
        [Fact]
        public void VisualStudio()
        {
        }
    }
}
