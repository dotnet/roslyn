// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class ErrorList_OutOfProc : OutOfProcComponent
    {
        public class Verifier
        {
            private readonly ErrorList_OutOfProc _errorList;
            private readonly VisualStudioInstance _instance;

            public Verifier(ErrorList_OutOfProc errorList, VisualStudioInstance instance)
            {
                _errorList = errorList;
                _instance = instance;
            }

            public void NoBuildErrors()
            {
                _instance.SolutionExplorer.BuildSolution();
                NoErrors();
            }

            public void NoErrors()
            {
                Assert.Equal(0, _errorList.GetErrorListErrorCount());
            }
        }
    }
}
