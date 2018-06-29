// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    public class InProcessIdeTestAssemblyRunner : MarshalByRefObject, IDisposable
    {
        [ThreadStatic]
        private static bool s_inHandler;

        private readonly TestAssemblyRunner<IXunitTestCase> _testAssemblyRunner;

        public InProcessIdeTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            var reconstructedTestCases = testCases.Select(testCase =>
            {
                if (testCase is IdeTestCase ideTestCase)
                {
                    return new IdeTestCase(diagnosticMessageSink, ideTestCase.DefaultMethodDisplay, ideTestCase.TestMethod, ideTestCase.VisualStudioVersion, ideTestCase.TestMethodArguments);
                }

                return new IdeTestCase(diagnosticMessageSink, TestMethodDisplay.ClassAndMethod, testCase.TestMethod, VisualStudioVersion.VS2017, testCase.TestMethodArguments);
                //throw new InvalidOperationException($"{testCase.GetType().AssemblyQualifiedName} is not a supported test case type. Expected {typeof(IdeTestCase).AssemblyQualifiedName}.");
            });

            _testAssemblyRunner = new XunitTestAssemblyRunner(testAssembly, reconstructedTestCases.ToArray(), diagnosticMessageSink, executionMessageSink, executionOptions);
        }

        public Tuple<int, int, int, decimal> RunTestCollection(IMessageBus messageBus, ITestCollection testCollection, IXunitTestCase[] testCases)
        {
            try
            {
                AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler;
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var result = _testAssemblyRunner.RunAsync().GetAwaiter().GetResult();
                    return Tuple.Create(result.Total, result.Failed, result.Skipped, result.Time);
                }
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= FirstChanceExceptionHandler;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override object InitializeLifetimeService()
        {
            // This object can live forever
            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _testAssemblyRunner.Dispose();
            }
        }

        private static void FirstChanceExceptionHandler(object sender, FirstChanceExceptionEventArgs eventArgs)
        {
            if (s_inHandler)
            {
                // An exception was thrown from within the handler, resulting in a recursive call to the handler.
                // Bail out now we so don't recursively throw another exception and overflow the stack.
                return;
            }

            try
            {
                s_inHandler = true;

                if (!IsCapturedFirstChangeException(eventArgs.Exception))
                {
                    return;
                }

                SaveScreenshot(eventArgs.Exception);
            }
            finally
            {
                s_inHandler = false;
            }
        }

        internal static bool IsCapturedFirstChangeException(Exception ex)
        {
            switch (ex)
            {
                case XunitException _:
                case RemotingException _:
                case SerializationException _:
                case TargetInvocationException _:
                    return true;

                default:
                    return false;
            }
        }

        internal static void SaveScreenshot(Exception ex)
        {
            var assemblyDirectory = GetAssemblyDirectory();
            var testName = CaptureTestNameAttribute.CurrentName ?? "Unknown";
            var logDir = Path.Combine(assemblyDirectory, "xUnitResults", "Screenshots");
            var baseFileName = $"{DateTime.Now:HH.mm.ss}-{testName}-{ex.GetType().Name}";
            ScreenshotService.TakeScreenshot(Path.Combine(logDir, $"{baseFileName}.png"));

            File.WriteAllText(
                Path.Combine(logDir, $"{baseFileName}.log"),
                ex.ToString());
        }

        private static string GetAssemblyDirectory()
        {
            var assemblyPath = typeof(VisualStudioInstanceFactory).Assembly.Location;
            return Path.GetDirectoryName(assemblyPath);
        }
    }
}
