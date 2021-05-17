// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    [Serializable]
    public sealed class WpfTestSharedData
    {
        internal static readonly WpfTestSharedData Instance = new WpfTestSharedData();

        /// <summary>
        /// The name of a <see cref="Semaphore"/> used to ensure that only a single
        /// <see cref="IdeFactAttribute"/>-attributed test runs at once. This requirement must be made because,
        /// currently, <see cref="IdeTestCase"/>'s logic sets various static state before a method runs. If two tests
        /// run interleaved on the same scheduler (i.e. if one yields with an await) then all bets are off.
        /// </summary>
        internal static readonly Guid TestSerializationGateName = Guid.NewGuid();

        /// <summary>
        /// Holds the last 10 test cases executed: more recent test cases will occur later in the
        /// list. Useful for debugging deadlocks that occur because state leak between runs.
        /// </summary>
        private readonly List<string> _recentTestCases = new List<string>();

        private Semaphore _testSerializationGate = new Semaphore(1, 1, TestSerializationGateName.ToString("N"));

        private WpfTestSharedData()
        {
        }

        public Semaphore TestSerializationGate => _testSerializationGate;
    }
}
