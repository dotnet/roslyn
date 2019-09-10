// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Telemetry;
using StreamJsonRpc;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.Watson
{
    public class WatsonTests
    {
        [Fact]
        public void TestRegularException()
        {
            try
            {
                // throw one
                throw new Exception("test");
            }
            catch (Exception exception)
            {
                var mockFault = new MockFault();
                mockFault.SetExtraParameters(exception, emptyCallstack: false);

                // there should be no extra bucket info
                // for regular exception
                Assert.False(mockFault.Map.ContainsKey(7));
            }
        }

        [Fact]
        public void TestRegularWithInnerexception()
        {
            try
            {
                try
                {
                    // throw one
                    throw new Exception("inner");
                }
                catch (Exception inner)
                {
                    throw new Exception("outter", inner);
                }
            }
            catch (Exception exception)
            {
                var mockFault = new MockFault();
                mockFault.SetExtraParameters(exception, emptyCallstack: false);

                Assert.Equal(exception.InnerException.GetParameterString(), mockFault.Map[7]);
            }
        }

        [Fact]
        public void TestRemoteInvocationException()
        {
            var mockFault = new MockFault();

            var exception = new RemoteInvocationException("test", errorCode: 100, "remoteErrorData");
            mockFault.SetExtraParameters(exception, emptyCallstack: false);

            Assert.Equal(exception.GetParameterString(), mockFault.Map[7]);
        }

        [Fact]
        public void TestRemoteInvocationExceptionNull()
        {
            var mockFault = new MockFault();

            var exception = new RemoteInvocationException(message: null, errorCode: -1, errorData: null);
            mockFault.SetExtraParameters(exception, emptyCallstack: false);

            Assert.Equal(exception.GetParameterString(), mockFault.Map[7]);
        }

        [Fact]
        public void TestAggregateException()
        {
            try
            {
                // throw one
                throw new AggregateException("no inner");
            }
            catch (Exception exception)
            {
                var mockFault = new MockFault();
                mockFault.SetExtraParameters(exception, emptyCallstack: false);

                // there should be no extra bucket info
                // for regular exception
                Assert.False(mockFault.Map.ContainsKey(7));
            }
        }

        [Fact]
        public void TestAggregateWithInnerexception()
        {
            try
            {
                try
                {
                    // throw one
                    throw new Exception("inner");
                }
                catch (Exception inner)
                {
                    throw new AggregateException(inner);
                }
            }
            catch (Exception exception)
            {
                var mockFault = new MockFault();
                mockFault.SetExtraParameters(exception, emptyCallstack: false);

                Assert.Equal(exception.GetParameterString(), mockFault.Map[7]);
            }
        }

        [Fact]
        public void TestAggregateWithMultipleInnerexceptions()
        {
            try
            {
                var inners = new List<Exception>();

                try
                {
                    // throw one
                    throw new Exception("inner1");
                }
                catch (Exception inner)
                {
                    inners.Add(inner);
                }

                try
                {
                    // throw one
                    throw new Exception("inner2");
                }
                catch (Exception inner)
                {
                    inners.Add(inner);
                }

                throw new AggregateException(inners);
            }
            catch (AggregateException exception)
            {
                var mockFault = new MockFault();
                mockFault.SetExtraParameters(exception, emptyCallstack: false);

                var flatten = exception.Flatten();
                Assert.Equal(flatten.CalculateHash(), mockFault.Map[6]);
                Assert.Equal(flatten.InnerException.GetParameterString(), mockFault.Map[7]);
            }
        }

        [Fact]
        public void TestEmptyCallstack()
        {
            var mockFault = new MockFault();

            var exception = new Exception("not thrown");
            mockFault.SetExtraParameters(exception, emptyCallstack: true);

            Assert.NotNull(mockFault.Map[3]);
        }

        public class MockFault : IFaultUtility
        {
            public readonly Dictionary<int, string> Map = new Dictionary<int, string>();

            public void SetBucketParameter(int bucketNumber, string value)
            {
                Map.Add(bucketNumber, value);
            }

            #region not used
            public void AddErrorInformation(string information)
            {
                throw new NotImplementedException();
            }

            public void AddFile(string fullpathname)
            {
                throw new NotImplementedException();
            }

            public void AddProcessDump(int pid)
            {
                throw new NotImplementedException();
            }

            public string GetBucketParameter(int bucketNumber)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
    }
}
