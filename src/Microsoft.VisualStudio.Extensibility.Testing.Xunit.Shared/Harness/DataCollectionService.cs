// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal static class DataCollectionService
    {
        private static readonly ConditionalWeakTable<Exception, StrongBox<bool>> LoggedExceptions = new();
        private static bool _firstChanceExceptionHandlerInstalled;

        [ThreadStatic]
        private static bool _inHandler;

        public static ITest CurrentTest { get; set; }

        private static string CurrentTestName
        {
            get
            {
                if (CurrentTest is null)
                {
                    return "Unknown";
                }

                var testMethod = CurrentTest.TestCase.TestMethod.Method;
                var testClass = testMethod.Type.Name;
                var lastDot = testClass.LastIndexOf('.');
                testClass = testClass.Substring(lastDot + 1);
                return $"{testClass}.{testMethod.Name}";
            }
        }

        public static void InstallFirstChanceExceptionHandler()
        {
            if (!_firstChanceExceptionHandlerInstalled)
            {
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
                _firstChanceExceptionHandlerInstalled = true;
            }
        }

        public static bool LogAndCatch(Exception ex)
        {
            try
            {
                TryLog(ex);
            }
            catch
            {
                // Make sure exceptions do not escape the exception filter
            }

            return true;
        }

        public static bool LogAndPropagate(Exception ex)
        {
            try
            {
                TryLog(ex);
            }
            catch
            {
                // Make sure exceptions do not escape the exception filter
            }

            return false;
        }

        public static bool TryLog(Exception ex)
        {
            if (ex is null)
            {
                return false;
            }

            var logged = LoggedExceptions.GetOrCreateValue(ex);
            if (logged.Value)
            {
                // Only log the first time an exception is thrown
                return false;
            }

            logged.Value = true;
            CaptureFailureState(CurrentTestName, ex);
            return true;
        }

        public static void CaptureFailureState(string testName, Exception ex)
        {
            if (_inHandler)
            {
                // Avoid stack overflow which could occur by recursively trying to capture failure states
                return;
            }

            try
            {
                _inHandler = true;

                var logDir = GetLogDirectory();
                var timestamp = DateTimeOffset.UtcNow;
                testName ??= "Unknown";
                var errorId = ex.GetType().Name;

                Directory.CreateDirectory(logDir);

                File.WriteAllText(CreateLogFileName(logDir, timestamp, testName, errorId, logId: string.Empty, "log"), ex.ToString());

                ActivityLogCollector.TryWriteActivityLogToFile(CreateLogFileName(logDir, timestamp, testName, errorId, "Activity", "xml"));
                EventLogCollector.TryWriteDotNetEntriesToFile(CreateLogFileName(logDir, timestamp, testName, errorId, "DotNet", "log"));
                EventLogCollector.TryWriteWatsonEntriesToFile(CreateLogFileName(logDir, timestamp, testName, errorId, "Watson", "log"));
                IdeStateCollector.TryWriteIdeStateToFile(CreateLogFileName(logDir, timestamp, testName, errorId, "IDE", "log"));

                ScreenshotService.TakeScreenshot(CreateLogFileName(logDir, timestamp, testName, errorId, string.Empty, $"png"));
            }
            finally
            {
                _inHandler = false;
            }
        }

        private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is not XunitException)
            {
                // Only xunit exceptions are logged in this handler
                return;
            }

            TryLog(e.Exception);
        }

        /// <summary>
        /// Computes a full log file name.
        /// </summary>
        /// <param name="logDirectory">The location where logs are saved.</param>
        /// <param name="timestamp">The timestamp of the failure.</param>
        /// <param name="testName">The current test name, or <c>Unknown</c> if the test is not known.</param>
        /// <param name="errorId">The error ID, e.g. the name of the exception instance.</param>
        /// <param name="logId">The log ID (e.g. <c>DotNet</c> or <c>Watson</c>). This may be an empty string for one log output of a particular <paramref name="extension"/>.</param>
        /// <param name="extension">The log file extension, without a dot (e.g. <c>log</c>).</param>
        /// <returns>The fully qualified log file name.</returns>
        private static string CreateLogFileName(string logDirectory, DateTimeOffset timestamp, string testName, string errorId, string logId, string extension)
        {
            const int MaxPath = 260;

            var path = CombineElements(logDirectory, timestamp, testName, errorId, logId, extension);
            if (path.Length > MaxPath)
            {
                testName = testName.Substring(0, Math.Max(0, testName.Length - (path.Length - MaxPath)));
                path = CombineElements(logDirectory, timestamp, testName, errorId, logId, extension);
            }

            return path;

            static string CombineElements(string logDirectory, DateTimeOffset timestamp, string testName, string errorId, string logId, string extension)
            {
                if (!string.IsNullOrEmpty(logId))
                {
                    logId = $".{logId}";
                }

                var sanitizedTestName = new string(testName.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
                var sanitizedErrorId = new string(errorId.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

                return Path.Combine(Path.GetFullPath(logDirectory), $"{timestamp:HH.mm.ss}-{testName}-{errorId}{logId}.{extension}");
            }
        }

        private static string GetLogDirectory()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("XUNIT_LOGS")))
            {
                return Path.GetFullPath(Path.Combine(Environment.GetEnvironmentVariable("XUNIT_LOGS"), "Screenshots"));
            }

            var assemblyDirectory = GetAssemblyDirectory();
            return Path.Combine(assemblyDirectory, "xUnitResults", "Screenshots");
        }

        private static string GetAssemblyDirectory()
        {
            var assemblyPath = typeof(DataCollectionService).Assembly.Location;
            return Path.GetDirectoryName(assemblyPath);
        }
    }
}
