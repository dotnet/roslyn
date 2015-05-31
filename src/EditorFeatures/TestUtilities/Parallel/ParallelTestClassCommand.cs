// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using Xunit;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities.Parallel
{
    internal class ParallelTestClassCommand : ITestClassCommand
    {
        private readonly Dictionary<MethodInfo, object> _fixtures = new Dictionary<MethodInfo, object>();
        private readonly IDictionary<IMethodInfo, Task<MethodResult>> _testMethodTasks = new Dictionary<IMethodInfo, Task<MethodResult>>();
        private ITypeInfo _typeUnderTest;
        private bool _classStarted;
        private bool _invokingSingleMethod = true;
        private List<TraceListener> _oldListeners;

        public ParallelTestClassCommand()
            : this((ITypeInfo)null)
        { }

        public ParallelTestClassCommand(Type typeUnderTest)
            : this(Reflector.Wrap(typeUnderTest))
        { }

        public ParallelTestClassCommand(ITypeInfo typeUnderTest)
        {
            _typeUnderTest = typeUnderTest;
        }

        public object ObjectUnderTest
        {
            get { return null; }
        }

        public ITypeInfo TypeUnderTest
        {
            get { return _typeUnderTest; }

            set { _typeUnderTest = value; }
        }

        public int ChooseNextTest(ICollection<IMethodInfo> testsLeftToRun)
        {
            return 0;
        }

        public Exception ClassFinish()
        {
            foreach (object fixtureData in _fixtures.Values)
            {
                try
                {
                    if (fixtureData is IDisposable)
                    {
                        ((IDisposable)fixtureData).Dispose();
                    }

                    Trace.Listeners.Clear();
                    Trace.Listeners.AddRange(_oldListeners.ToArray());
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            return null;
        }

        public Exception ClassStart()
        {
            try
            {
                foreach (Type @interface in _typeUnderTest.Type.GetInterfaces())
                {
                    if (@interface.IsGenericType)
                    {
                        Type genericDefinition = @interface.GetGenericTypeDefinition();

                        if (genericDefinition == typeof(IUseFixture<>))
                        {
                            Type dataType = @interface.GetGenericArguments()[0];
                            if (dataType == _typeUnderTest.Type)
                            {
                                throw new InvalidOperationException("Cannot use a test class as its own fixture data");
                            }

                            object fixtureData = null;

                            try
                            {
                                fixtureData = Activator.CreateInstance(dataType);
                            }
                            catch (TargetInvocationException ex)
                            {
                                return ex.InnerException;
                            }

                            MethodInfo method = @interface.GetMethod("SetFixture", new Type[] { dataType });
                            _fixtures[method] = fixtureData;
                        }
                    }
                }

                _oldListeners = new List<TraceListener>();
                foreach (TraceListener oldListener in Trace.Listeners)
                {
                    _oldListeners.Add(oldListener);
                }

                Trace.Listeners.Clear();
                Trace.Listeners.Add(new AssertTraceListener());

                if (!_invokingSingleMethod)
                {
                    StartMethodsAsync(TestMethodTasks);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                _classStarted = true;
            }
        }

        public IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo testMethod)
        {
            string skipReason = MethodUtility.GetSkipReason(testMethod);

            if (skipReason != null)
            {
                yield return new SkipCommand(testMethod, MethodUtility.GetDisplayName(testMethod), skipReason);
            }
            else if (!_invokingSingleMethod)
            {
                yield return new FixtureCommand(new AsyncFactCommand(testMethod, TestMethodTasks), _fixtures);
            }
            else
            {
                yield return new FixtureCommand(new FactCommand(testMethod), _fixtures);
            }
        }

        public IEnumerable<IMethodInfo> EnumerateTestMethods()
        {
            if (!_classStarted && _testMethodTasks.Count == 0)
            {
                _invokingSingleMethod = false;
            }

            return TestMethodTasks.Keys;
        }

        public static IEnumerable<ITestCommand> Make(ITestClassCommand classCommand,
                                                     IMethodInfo method)
        {
            foreach (var testCommand in classCommand.EnumerateTestCommands(method))
            {
                ITestCommand wrappedCommand = testCommand;

                // Timeout (if they have one) -> Capture -> Timed -> Lifetime (if we need an instance) -> BeforeAfter

                wrappedCommand = new BeforeAfterCommand(wrappedCommand, method.MethodInfo);

                if (testCommand.ShouldCreateInstance)
                {
                    wrappedCommand = new LifetimeCommand(wrappedCommand, method);
                }

                wrappedCommand = new TimedCommand(wrappedCommand);
                wrappedCommand = new ExceptionCaptureCommand(wrappedCommand, method);

                if (wrappedCommand.Timeout > 0)
                {
                    wrappedCommand = new TimeoutCommand(wrappedCommand, wrappedCommand.Timeout, method);
                }

                yield return wrappedCommand;
            }
        }

        private IDictionary<IMethodInfo, Task<MethodResult>> TestMethodTasks
        {
            get
            {
                if (_testMethodTasks.Count == 0)
                {
                    foreach (IMethodInfo testMethod in TypeUtility.GetTestMethods(_typeUnderTest))
                    {
                        foreach (ITestCommand command in Make(new AsyncTestClassCommand(_fixtures), testMethod))
                        {
                            // looks like sometimes compiler generated code will reuse "command".
                            // and that can cause closure given to a task to be changed underneath.
                            // make sure to use local copy of command so that closure doesn't get
                            // changed.
                            var localCopy = command;
                            _testMethodTasks.Add(testMethod, new Task<MethodResult>(() => localCopy.Execute(null)));
                        }
                    }
                }

                return _testMethodTasks;
            }
        }

        public bool IsTestMethod(IMethodInfo testMethod)
        {
            return MethodUtility.IsTest(testMethod);
        }

        private static void StartMethodsAsync(IDictionary<IMethodInfo, Task<MethodResult>> testMethodInvokeInfo)
        {
            foreach (var testMethod in testMethodInvokeInfo)
            {
                testMethod.Value.Start();
            }
        }

        private class AssertTraceListener : TraceListener
        {
            public override void Fail(string message,
                                      string detailMessage)
            {
                throw new TraceAssertException(message, detailMessage);
            }

            public override void Write(string message) { }

            public override void WriteLine(string message) { }
        }
    }
}
