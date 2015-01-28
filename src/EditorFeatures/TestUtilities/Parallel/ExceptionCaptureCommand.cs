// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

using Xunit.Sdk;

namespace Roslyn.Test.Utilities.Parallel
{
    internal class ExceptionCaptureCommand : DelegatingTestCommand
    {
        private readonly IMethodInfo _method;

        public ExceptionCaptureCommand(ITestCommand innerCommand, IMethodInfo method)
            : base(innerCommand)
        {
            _method = method;
        }

        public override MethodResult Execute(object testClass)
        {
            MethodResult result = null;

            try
            {
                result = InnerCommand.Execute(testClass);
            }
            catch (Exception ex)
            {
                result = new FailedResult(_method, ex, DisplayName);
            }

            return result;
        }
    }
}
