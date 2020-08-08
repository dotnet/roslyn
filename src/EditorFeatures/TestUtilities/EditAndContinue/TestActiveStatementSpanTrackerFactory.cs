// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [ExportWorkspaceService(typeof(IActiveStatementSpanTrackerFactory), ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal class TestActiveStatementSpanTrackerFactory : IActiveStatementSpanTrackerFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestActiveStatementSpanTrackerFactory()
        {
            ActiveStatementSpanTracker = new TestActiveStatementSpanTracker();
        }

        public TestActiveStatementSpanTracker ActiveStatementSpanTracker { get; }

        IActiveStatementSpanTracker IActiveStatementSpanTrackerFactory.GetOrCreateActiveStatementSpanTracker()
            => ActiveStatementSpanTracker;
    }
}
