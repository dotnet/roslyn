// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

using Xunit.Sdk;

namespace Roslyn.Test.Utilities.Parallel
{
    internal class AsyncTestClassCommand : ITestClassCommand
    {
        private readonly Dictionary<MethodInfo, object> _fixtures;

        public AsyncTestClassCommand(Dictionary<MethodInfo, object> fixtures)
        {
            _fixtures = fixtures;
        }

        public int ChooseNextTest(ICollection<IMethodInfo> testsLeftToRun)
        {
            throw new NotSupportedException("AsyncTestClassCommand can only be used to enumerate Test Commands.");
        }

        public Exception ClassFinish()
        {
            throw new NotSupportedException("AsyncTestClassCommand can only be used to enumerate Test Commands.");
        }

        public Exception ClassStart()
        {
            throw new NotSupportedException("AsyncTestClassCommand can only be used to enumerate Test Commands.");
        }

        public IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo testMethod)
        {
            string skipReason = MethodUtility.GetSkipReason(testMethod);

            if (skipReason != null)
            {
                yield return new SkipCommand(testMethod, MethodUtility.GetDisplayName(testMethod), skipReason);
            }
            else
            {
                yield return new FixtureCommand(new FactCommand(testMethod), _fixtures);
            }
        }

        public IEnumerable<IMethodInfo> EnumerateTestMethods()
        {
            throw new NotSupportedException("AsyncTestClassCommand can only be used to enumerate Test Commands.");
        }

        public bool IsTestMethod(IMethodInfo testMethod)
        {
            throw new NotSupportedException("AsyncTestClassCommand can only be used to enumerate Test Commands.");
        }

        public object ObjectUnderTest
        {
            get { return null; }
        }

        public ITypeInfo TypeUnderTest
        {
            get
            {
                throw new NotSupportedException("AsyncTestClassCommand can only be used to enumerate Test Commands.");
            }

            set
            {
                throw new NotSupportedException("AsyncTestClassCommand can only be used to enumerate Test Commands.");
            }
        }
    }
}
