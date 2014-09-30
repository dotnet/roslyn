using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTemplate;

namespace AsyncPackage.Tests
{
    /// <summary>
    /// Unit Tests for the BlockingAsyncAnalyzer and BlockingAsyncCodeFix
    /// </summary>
    [TestClass]
    public class BlockingAsyncTests : CodeFixVerifier
    {
        private const string DefaultHeader = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {   ";

        private const string DefaultFooter = @"
    }
}";

        private DiagnosticResult expected = new DiagnosticResult
        {
            Id = "Async006",
            Message = "This method is blocking on async code",
            Severity = DiagnosticSeverity.Warning,

            // Location should be changed within test methods
            Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 10) }
        };

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new BlockingAsyncCodeFix();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new BlockingAsyncCodeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new BlockingAsyncAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BlockingAsyncAnalyzer();
        }

        // No methods - no diagnostic should show
        [TestMethod]
        public void BA001_None_NoMethodNoDiagnostics()
        {
            var body = @"";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Wait block on async code - 1 diagnostic - code fix
        [TestMethod]
        public void BA002_DACF_WaitBlockAsyncCode()
        {
            var body = @"
        async Task<int> fooAsync(int somenumber)
        {
            foo1Async(somenumber).Wait();
            return somenumber;
        }

        async Task<int> foo1Async(int value)
        {
            await Task.Yield();
            return value;
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) };

            VerifyCSharpDiagnostic(test, expected);

            var fixbody = @"
        async Task<int> fooAsync(int somenumber)
        {
            await foo1Async(somenumber);
            return somenumber;
        }

        async Task<int> foo1Async(int value)
        {
            await Task.Yield();
            return value;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Result block on async code - 1 diagnostic - code fix
        [TestMethod]
        public void BA003_DACF_ResultBlockAsyncCode()
        {
            var body = @"
        async Task foo2Async(int somenumber)
        {
            var temp = foo3Async(somenumber).Result;
        }

        async Task<int> foo3Async(int value)
        {
            await Task.Yield();
            return value;
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 24) };

            VerifyCSharpDiagnostic(test, expected);

            var fixbody = @"
        async Task foo2Async(int somenumber)
        {
            var temp = await foo3Async(somenumber);
        }

        async Task<int> foo3Async(int value)
        {
            await Task.Yield();
            return value;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Invocation expression assigned to variable - 1 diagnostic
        [TestMethod]
        public void BA004_DA_InvocationExpressionVariable()
        {
            var body = @"
        async Task<Task<int>> SomeMethodAsync()
        {
            var t = addAsync(25, 50);
            t.Wait();

            return t;
        }

        async Task<int> addAsync(int num1, int num2)
        {
            await Task.Delay(29292);
            return (num1 + num2);
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 13) };

            var fixbody = @"
        async Task<Task<int>> SomeMethodAsync()
        {
            var t = addAsync(25, 50);
            await t;

            return t;
        }

        async Task<int> addAsync(int num1, int num2)
        {
            await Task.Delay(29292);
            return (num1 + num2);
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpDiagnostic(test, expected);
            VerifyCSharpFix(test, fixtest);
        }

        // Async method using waitall on task - 1 diagnostic - code fix
        [TestMethod]
        public void BA005_DACF_WaitAllBlockAsyncCode()
        {
            var body = @"
        async Task WaitAllAsync()
        //Source: http://msdn.microsoft.com/en-us/library/dd270695(v=vs.110).aspx
        {
            Func<object, int> action = (object obj) =>
                {
                    int i = (int)obj;

                    // The tasks that receive an argument between 2 and 5 throw exceptions 
                    if (2 <= i && i <= 5)
                    {
                        throw new InvalidOperationException();
                    }

                    int tickCount = Environment.TickCount;

                    return tickCount;
                };

            const int n = 10;

            // Construct started tasks
            Task<int>[] tasks = new Task<int>[n];
            for (int i = 0; i<n; i++)
            {
                tasks[i] = Task<int>.Factory.StartNew(action, i);
            }

            // Exceptions thrown by tasks will be propagated to the main thread 
            // while it waits for the tasks. The actual exceptions will be wrapped in AggregateException. 
            try
            {
                // Wait for all the tasks to finish.
                Task.WaitAll(tasks);

                // We should never get to this point
            }
            catch (AggregateException e)
            {
                for (int j = 0; j<e.InnerExceptions.Count; j++)
                {
                    Console.WriteLine(e.InnerExceptions[j].ToString());
                }
            }
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 46, 17) };

            VerifyCSharpDiagnostic(test, expected);

            var fixbody = @"
        async Task WaitAllAsync()
        //Source: http://msdn.microsoft.com/en-us/library/dd270695(v=vs.110).aspx
        {
            Func<object, int> action = (object obj) =>
                {
                    int i = (int)obj;

                    // The tasks that receive an argument between 2 and 5 throw exceptions 
                    if (2 <= i && i <= 5)
                    {
                        throw new InvalidOperationException();
                    }

                    int tickCount = Environment.TickCount;

                    return tickCount;
                };

            const int n = 10;

            // Construct started tasks
            Task<int>[] tasks = new Task<int>[n];
            for (int i = 0; i<n; i++)
            {
                tasks[i] = Task<int>.Factory.StartNew(action, i);
            }

            // Exceptions thrown by tasks will be propagated to the main thread 
            // while it waits for the tasks. The actual exceptions will be wrapped in AggregateException. 
            try
            {
                // Wait for all the tasks to finish.
                await Task.WhenAll(tasks);

                // We should never get to this point
            }
            catch (AggregateException e)
            {
                for (int j = 0; j<e.InnerExceptions.Count; j++)
                {
                    Console.WriteLine(e.InnerExceptions[j].ToString());
                }
            }
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest, null, true);
        }

        // Async with thread.sleep - 1 diagnostic - code fix
        [TestMethod]
        public void BA006_DACF_AsyncWithThreadSleep()
        {
            var body = @"
        async Task<string> SleepHeadAsync(string phrase)
        {
            Thread.Sleep(9999);

            return phrase;
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) };

            VerifyCSharpDiagnostic(test, expected);

            var fixbody = @"
        async Task<string> SleepHeadAsync(string phrase)
        {
            await Task.Delay(9999);

            return phrase;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest, null, true);
        }

        // Thread.sleep within loop - 1 diagnostic - code fix
        [TestMethod]
        public void BA007_DACF_ThreadSleepInLoop()
        {
            var body = @"
        async Task SleepAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(3000);
            }
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 17) };

            VerifyCSharpDiagnostic(test, expected);

            var fixbody = @"
        async Task SleepAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(3000);
            }
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest, null, true);
        }

        // Thread.sleep(timespan) - 1 diagnostic - code fix
        [TestMethod]
        public void BA008_DACF_AsyncWithThreadSleepTimeSpan()
        {
            var body = @"
        async Task TimeSpanAsync()
        {
            TimeSpan time = new TimeSpan(1000);
            Thread.Sleep(time);
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 13) };

            VerifyCSharpDiagnostic(test, expected);

            var fixbody = @"
        async Task TimeSpanAsync()
        {
            TimeSpan time = new TimeSpan(1000);
            await Task.Delay(time);
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest, null, true);
        }

        // Thread.sleep(TimeSpan) within loop - 1 diagnostic - code fix
        [TestMethod]
        public void BA009_DACF_ThreadSleepTimeSpanInForLoop()
        {
            var body = @"
        async Task TimespaninForloopAsync()
        {
            TimeSpan interval = new TimeSpan(2, 20, 55, 33, 0);

            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(interval);
            }
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 17) };

            VerifyCSharpDiagnostic(test, expected);

            var fixbody = @"
        async Task TimespaninForloopAsync()
        {
            TimeSpan interval = new TimeSpan(2, 20, 55, 33, 0);

            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(interval);
            }
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest, null, true);
        }

        // Thread.sleep(timespan) within while loop - 1 diagnostic - code fix
        [TestMethod]
        public void BA010_DACF_ThreadSleepTimeSpanInWhileLoop()
        {
            var body = @"
        async Task TimeSpanWhileLoop()
        {
            bool trueorfalse = true;
            TimeSpan interval = new TimeSpan(0, 0, 2);
            while (trueorfalse)
            {
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(interval);
                }
                trueorfalse = false;
            }
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected.Id = "Async006";
            expected.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 22, 21) };

            VerifyCSharpDiagnostic(test, expected);

            var fixbody = @"
        async Task TimeSpanWhileLoop()
        {
            bool trueorfalse = true;
            TimeSpan interval = new TimeSpan(0, 0, 2);
            while (trueorfalse)
            {
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(interval);
                }
                trueorfalse = false;
            }
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest, null, true);
        }

        // Async with task.delay - no diagnostic
        [TestMethod]
        public void BA011_None_AsyncWithAsync()
        {
            var body = @"
        async Task SleepNowAsync()
        {
            await Task.Delay(1000);
        }";
            var test = DefaultHeader + body + DefaultFooter;
            VerifyCSharpDiagnostic(test);
        }

        // Async method calls a function with thread.sleep in it - out of scope, no diagnostic
        [TestMethod]
        public void BA012_None_HiddenSyncCode()
        {
            var body = @"
        async Task CallAnotherMethodAsync()
        {
            SleepAlittle(5888);
        }

        public void SleepAlittle(int value)
        {
            Thread.Sleep(value);
        }";
            var test = DefaultHeader + body + DefaultFooter;
            VerifyCSharpDiagnostic(test);
        }

        // Non async method calls thread.wait - no diagnostic
        [TestMethod]
        public void BA013_None_NonAsyncWait()
        {
            var body = @"
        Task Example()
        {
            Task.Wait();
        }";
            var test = DefaultHeader + body + DefaultFooter;
            VerifyCSharpDiagnostic(test);
        }

        // Async method calls a different Task method - no diagnostic
        [TestMethod]
        public void BA014_None_AsyncWithOtherMethod()
        {
            var body = @"
        async Task Example()
        {
            await Task.Delay(100);
        }";
            var test = DefaultHeader + body + DefaultFooter;
            VerifyCSharpDiagnostic(test);
        }

        // Async method calls a Wait method on a Task, but not the same Wait - no diagnostic
        [TestMethod]
        public void BA015_None_AsyncCallsOtherWait()
        {
            var test = @"
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        async Task Example(Action a, string s)
        {
            await Task.Delay(100);

            var t = new Task(a);
            t.Wait(s);
        }

    }

    public static class MyTaskExtensions
    {
        public static void Wait(this Task t, string s)
        {
            s = s + 1;
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        // Async method calls a WaitAll method on a Task, but not the same WaitAll - no diagnostic
        [TestMethod]
        public void BA016_None_AsyncCallsOtherWaitAll()
        {
            var test = @"
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        async Task Example(Action a, string s)
        {
            await Task.Delay(100);

            var t = new Task(a);
            t.WaitAll(s);
        }

    }

    public static class MyTaskExtensions
    {
        public static void WaitAll(this Task t, string s)
        {
            s = s + 1;
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        // Async method calls a WaitAny method on a Task, but not the same WaitAny - no diagnostic
        [TestMethod]
        public void BA017_None_AsyncCallsOtherWaitAny()
        {
            var test = @"
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        async Task Example(Action a, string s)
        {
            await Task.Delay(100);

            var t = new Task(a);
            t.WaitAny(s);
        }

    }

    public static class MyTaskExtensions
    {
        public static void WaitAny(this Task t, string s)
        {
            s = s + 1;
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        // Async method calls a Sleep method on a Task, but not the same Sleep - no diagnostic
        [TestMethod]
        public void BA018_None_AsyncCallsOtherSleep()
        {
            var test = @"
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        async Task Example(Action a, string s)
        {
            await Task.Delay(100);

            var t = new Task(a);
            t.Sleep(s);
        }

    }

    public static class MyTaskExtensions
    {
        public static void Sleep(this Task t, string s)
        {
            s = s + 1;
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        // Async method calls a GetResult method on a Task, but not the same GetResult - no diagnostic
        [TestMethod]
        public void BA019_None_AsyncCallsOtherGetResult()
        {
            var test = @"
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        async Task Example(Action a, string s)
        {
            await Task.Delay(100);

            var t = new Task(a);
            t.GetResult(s);
        }

    }

    public static class MyTaskExtensions
    {
        public static void GetResult(this Task t, string s)
        {
            s = s + 1;
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }
    }
}
