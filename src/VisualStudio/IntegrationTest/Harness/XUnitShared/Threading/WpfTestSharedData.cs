// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    [Serializable]
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public sealed class WpfTestSharedData
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        internal static readonly WpfTestSharedData Instance = new WpfTestSharedData();

        /// <summary>
        /// The name of a <see cref="Semaphore"/> used to ensure that only a single
        /// <see cref="IdeFactAttribute"/>-attributed test runs at once. This requirement must be made because,
        /// currently, <see cref="IdeTestCase"/>'s logic sets various static state before a method runs. If two tests
        /// run interleaved on the same scheduler (i.e. if one yields with an await) then all bets are off.
        /// </summary>
        internal static readonly Guid TestSerializationGateName = Guid.NewGuid();

#pragma warning disable CA2235 // Mark all non-serializable fields
        private readonly Semaphore _testSerializationGate = new Semaphore(1, 1, TestSerializationGateName.ToString("N"));
#pragma warning restore CA2235 // Mark all non-serializable fields

        private WpfTestSharedData()
        {
        }

        public Semaphore TestSerializationGate => _testSerializationGate;

        public Exception? Exception { get; set; }
    }
}
