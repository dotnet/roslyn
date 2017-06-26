using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    internal sealed class FirstChanceExceptionLogger : IDisposable
    {
        private readonly string _testName;
        private readonly Action<string> _logAction;

        public FirstChanceExceptionLogger(string testName, Action<string> logAction)
        {
            _testName = testName;
            _logAction = logAction;

            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionHandler;
        }

        private void FirstChanceExceptionHandler(object sender, FirstChanceExceptionEventArgs eventArgs)
        {
            try
            {
                var assemblyPath = typeof(FirstChanceExceptionLogger).Assembly.Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyPath);
                var fileName = $"{_testName}-{eventArgs.Exception.GetType().Name}-{DateTime.Now:HH.mm.ss}.png";

                _logAction?.Invoke($"First Chance Exception: {eventArgs.Exception.Message}");
                _logAction?.Invoke($"{eventArgs.Exception.StackTrace}");

                var fullPath = Path.Combine(assemblyDirectory, "xUnitResults", "Screenshots", fileName);

                ScreenshotService.TakeScreenshot(fullPath);

                _logAction?.Invoke($"Screenshot logged to {fullPath} for first chance exception above.");
            }
            catch (Exception)
            {
                // Per the AppDomain.FirstChanceException contract we must catch and deal with all exceptions that arise in the handler.
                // Otherwise, we are likely to end up with recursive calls into this method until we overflow the stack.
            }
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.FirstChanceException -= FirstChanceExceptionHandler;
        }
    }
}
