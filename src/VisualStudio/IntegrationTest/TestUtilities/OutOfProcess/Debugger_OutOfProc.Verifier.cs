// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class Debugger_OutOfProc : OutOfProcComponent
    {
        /// <summary>
        /// Provides a means of interacting with the Visual Studio debugger by remoting calls into Visual Studio.
        /// </summary>
        public class Verifier 
        {
            private readonly Debugger_OutOfProc _debugger;

            public Verifier(Debugger_OutOfProc debuigger)
            {
                _debugger = debuigger;
            }

            public void EvaluateExpression(string expression, string expectedResult)
            {
                string actualResult = _debugger.EvaluateExpression(expression);
                Assert.Equal(expectedResult, actualResult);
            }
        }
    }
}