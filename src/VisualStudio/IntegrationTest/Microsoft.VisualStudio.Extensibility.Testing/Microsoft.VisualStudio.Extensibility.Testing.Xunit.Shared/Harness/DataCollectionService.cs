// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public static class DataCollectionService
    {
        private static readonly ConditionalWeakTable<Exception, StrongBox<bool>> LoggedExceptions = new();
        private static ImmutableList<CustomLoggerData> _customInProcessLoggers = ImmutableList<CustomLoggerData>.Empty;
        private static bool _firstChanceExceptionHandlerInstalled;

        [ThreadStatic]
        private static bool _inHandler;

        internal static ITest? CurrentTest { get; set; }

        private static string CurrentTestName
        {
            get
            {
                if (CurrentTest is null)
                {
                    return "Unknown";
                }

                return GetTestName(CurrentTest.TestCase);
            }
        }

        /// <summary>
        /// Register a custom logger to collect data in the event of a test failure.
        /// </summary>
        /// <remarks>
        /// <para>The <paramref name="logId"/> and <paramref name="extension"/> should be chosen to avoid conflicts with
        /// other loggers. Otherwise, it is possible for logs to be overwritten during data collection. Built-in logs
        /// include:</para>
        ///
        /// <list type="table">
        ///   <listheader>
        ///     <description><strong>Log ID</strong></description>
        ///     <description><strong>Extension</strong></description>
        ///     <description><strong>Purpose</strong></description>
        ///   </listheader>
        ///   <item>
        ///     <description>None</description>
        ///     <description><c>log</c></description>
        ///     <description>Exception details</description>
        ///   </item>
        ///   <item>
        ///     <description>None</description>
        ///     <description><c>png</c></description>
        ///     <description>Screenshot</description>
        ///   </item>
        ///   <item>
        ///     <description><c>DotNet</c></description>
        ///     <description><c>log</c></description>
        ///     <description>.NET errors from the Windows Event Log (filtered to relevant processes)</description>
        ///   </item>
        ///   <item>
        ///     <description><c>Watson</c></description>
        ///     <description><c>log</c></description>
        ///     <description>Watson errors from the Windows Event Log (filtered to relevant processes)</description>
        ///   </item>
        ///   <item>
        ///     <description><c>Activity</c></description>
        ///     <description><c>xml</c></description>
        ///     <description>The in-memory activity log at the time of failure. This item is only collected when the error is handled by the harness inside the running Visual Studio process.</description>
        ///   </item>
        ///   <item>
        ///     <description><c>IDE</c></description>
        ///     <description><c>log</c></description>
        ///     <description>Information about the IDE state at the point of failure. This item is only collected when the error is handled by the harness inside the running Visual Studio process. See <see cref="IdeStateCollector"/>.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="callback">The callback to invoke to collect log information. The argument to the callback is the fully-qualified file path where the log data should be written.</param>
        /// <param name="logId">An optional log identifier to include in the resulting file name.</param>
        /// <param name="extension">The extension to give the resulting file.</param>
        public static void RegisterCustomLogger(Action<string> callback, string logId, string extension)
        {
            ImmutableInterlocked.Update(
                ref _customInProcessLoggers,
                (loggers, newLogger) => loggers.Add(newLogger),
                new CustomLoggerData(callback, logId, extension));
        }

        internal static string GetTestName(ITestCase testCase)
        {
            var testMethod = testCase.TestMethod.Method;
            var testClass = testMethod.Type.Name;
            var lastDot = testClass.LastIndexOf('.');
            testClass = testClass.Substring(lastDot + 1);
            return $"{testClass}.{testMethod.Name}";
        }

        internal static void InstallFirstChanceExceptionHandler()
        {
            if (!_firstChanceExceptionHandlerInstalled)
            {
                AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
                _firstChanceExceptionHandlerInstalled = true;
            }
        }

        internal static bool LogAndCatch(Exception ex)
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

        internal static bool LogAndPropagate(Exception ex)
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

        internal static bool TryLog(Exception ex)
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

        internal static void CaptureFailureState(string testName, Exception ex)
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
                ScreenshotService.TakeScreenshot(CreateLogFileName(logDir, timestamp, testName, errorId, string.Empty, $"png"));
                EventLogCollector.TryWriteDotNetEntriesToFile(CreateLogFileName(logDir, timestamp, testName, errorId, "DotNet", "log"));
                EventLogCollector.TryWriteWatsonEntriesToFile(CreateLogFileName(logDir, timestamp, testName, errorId, "Watson", "log"));

                if (Process.GetCurrentProcess().ProcessName == "devenv")
                {
                    ActivityLogCollector.TryWriteActivityLogToFile(CreateLogFileName(logDir, timestamp, testName, errorId, "Activity", "xml"));
                    IdeStateCollector.TryWriteIdeStateToFile(CreateLogFileName(logDir, timestamp, testName, errorId, "IDE", "log"));
                    foreach (var (callback, logId, extension) in _customInProcessLoggers)
                    {
                        callback(CreateLogFileName(logDir, timestamp, testName, errorId, logId, extension));
                    }
                }
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

        internal static string GetLogDirectory()
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

        internal record struct CustomLoggerData(Action<string> Callback, string LogId, string Extension);
    }
}
