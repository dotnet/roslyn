// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.ErrorList
{
    public static partial class ErrorListExtensions
    {
        public static void VerifyNoBuildErrors(this AbstractIntegrationTest test)
        {
            test.BuildSolution(waitForBuildToFinish: true);
            Assert.Equal(0, test.GetErrorListErrorCount());
        }
    }
}
