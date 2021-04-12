// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Remote.Testing
{
    public enum TestHost
    {
        /// <summary>
        /// Features that optionally dispatch to a remote implementation service will
        /// not do so and instead directly call their local implementation.
        /// </summary>
        InProcess,

        /// <summary>
        /// Features that optionally dispatch to a remote implementation service will do so.
        /// This remote implementation will execute in the same process to simplify debugging
        /// and avoid cost of process management.
        /// </summary>
        OutOfProcess,
    }
}
