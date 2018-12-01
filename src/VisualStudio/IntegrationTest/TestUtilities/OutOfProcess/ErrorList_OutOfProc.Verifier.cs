// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class ErrorList_OutOfProc : OutOfProcComponent
    {
        public class Verifier
        {
            private ErrorList_OutOfProc _errorList;
            private readonly VisualStudioInstance _instance;

            public Verifier(ErrorList_OutOfProc errorList, VisualStudioInstance instance)
            {
                _errorList = errorList;
                _instance = instance;
            }

            public void NoBuildErrors()
            {
                _instance.SolutionExplorer.BuildSolution(waitForBuildToFinish: true);
                NoErrors();
            }

            public void NoErrors()
            {
                Assert.AreEqual(0, _errorList.GetErrorListErrorCount());
            }
        }
    }
}
