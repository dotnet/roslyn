// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit.Sdk;

namespace Roslyn.Test.Utilities.Parallel
{
    internal class AsyncFactCommand : FactCommand
    {
        private readonly IDictionary<IMethodInfo, Task<MethodResult>> _testMethodInvokeInfo;

        public AsyncFactCommand(IMethodInfo method, IDictionary<IMethodInfo, Task<MethodResult>> testMethodInvokeInfo)
            : base(method)
        {
            _testMethodInvokeInfo = testMethodInvokeInfo;
        }

        public override MethodResult Execute(object testClass)
        {
            Task<MethodResult> task = _testMethodInvokeInfo[testMethod];
            task.Wait();
            return task.Result;
        }
    }
}
