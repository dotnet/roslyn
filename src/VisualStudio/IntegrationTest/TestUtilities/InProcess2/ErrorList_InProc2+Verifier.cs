// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    partial class ErrorList_InProc2
    {
        public class Verifier
        {
            private readonly ErrorList_InProc2 _errorList;

            public Verifier(ErrorList_InProc2 errorList)
            {
                _errorList = errorList;
            }

            public async Task NoBuildErrorsAsync()
            {
                await _errorList.TestServices.SolutionExplorer.BuildSolutionAsync(waitForBuildToFinish: true);
                await NoErrorsAsync();
            }

            public async Task NoErrorsAsync()
            {
                Assert.Equal(0, await _errorList.GetErrorCountAsync());
            }
        }
    }
}
