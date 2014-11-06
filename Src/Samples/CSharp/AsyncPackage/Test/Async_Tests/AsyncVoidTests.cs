// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTemplate;

namespace AsyncPackage.Tests
{
    /// <summary>
    /// Unit Tests for the AsyncVoidAnalyzer and AsyncVoidCodeFix
    /// </summary>
    [TestClass]
    public class AsyncVoidTests : CodeFixVerifier
    {
        private const string DefaultHeader = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class Program
    {   ";

        private const string DefaultFooter = @"
    }
}";

        private DiagnosticResult expected001 = new DiagnosticResult
        {
            Id = "Async001",
            Message = "This method has the async keyword but it returns void",
            Severity = DiagnosticSeverity.Warning,

            // Location should be changed within test methods
            Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 10) }
        };

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AsyncVoidCodeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AsyncVoidAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new AsyncVoidAnalyzer();
        }

        // No methods - no diagnostic should show
        [TestMethod]
        public void AV001_None_NoMethodNoDiagnostics()
        {
            var body = @"";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task Return Async Method - single request - should show no diagnostic
        [TestMethod]
        public void AV002_None_TaskReturnAsyncSingleRequest()
        {
            var body = @"
            public static async Task DelayAsync()
            {
            await Task.Delay(1000);
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task Return Async Method - serial requests - should show no diagnostic
        [TestMethod]
        public void AV003_None_TaskReturnAsyncSerialRequests()
        {
            var body = @"
            public static async Task DelayMultipleTimesAsync()
            {
            await Task.Delay(1000);
            await Task.Delay(1000);
            await Task.Delay(1000);
            await Task.Delay(1000);
           }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task Return Async Method - parallel requests - should show no diagnostic
        [TestMethod]
        public void AV004_None_TaskReturnAsyncParallelRequests()
        {
            var body = @"
            public async Task SomeAsyncTask()
            { int value = 708;
            await ValuePlusOne(value);
            await ValuePlusTwo(value);
            await ValuePlusThree(value);
            }
    
            public async Task<int> ValuePlusOne(int num)
            {
            await Task.Delay(90);

            return num;
             }

            public async Task<int> ValuePlusTwo(int num)
            {
            await Task.Delay(990);

            return num;
            }

            public async Task<int> ValuePlusThree(int num)
            {
            await Task.Delay(9990);

            return num;
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<int> Return Async Method - Single request - should show no diagnostic
        [TestMethod]
        public void AV005_None_TaskIntReturnAsyncSingleRequest()
        {
            var body = @"
            public static async Task<int> MultiplyfactorsAsync(int factor1, int factor2)
        {
            await Task.Delay(5000);
            //delay once
            
            return (factor1 * factor2);
        }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<int> Return Async Method - serial requests - should show no diagnostic
        [TestMethod]
        public void AV006_None_TaskIntReturnWithAsync()
        {
            var body = @"
            private async Task<int> MathAsync(int num1) 
            { 
            //delays
            await Task.Delay(3000); 
            await Task.Delay(1000);
            await Task.Delay(num1 * 15);

            return (num1); 
            } ";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<string> Return Async Method - single request - should show no diagnostic
        [TestMethod]
        public void AV007_None_TaskStringReturnAsyncSingleRequest()
        {
            var body = @"
            public static Task<String> ReturnAAAAsync()
            {
            return Task.Run(() =>
            { return (""AAA""); });
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<string> Return Async Method - serial requests - should show no diagnostic
        [TestMethod]
        public void AV008_None_TaskStringReturnAsyncSerialRequests()
        {
            var body = @"
            public async static Task<String> LongTaskAAsync()
            {
            await Task.Delay(2000);
            await Task.Delay(2000);
            await Task.Delay(2000);
            return await Task.Run(() =>
            {
                return (""AAA"");
            });
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<int> Return Async Method - serial requests - should show no diagnostic
        [TestMethod]
        public void AV009_None_TaskIntReturnAsyncSerialRequests()
        {
            var body = @"
            public static async Task<int> MultiplyAsync(int factor1, int factor2)
            {
            //delay three times
            await Task.Delay(100);
            await Task.Delay(100);
            await Task.Delay(100);

            return (factor1 * factor2);
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task Return Async Method - single request - should show no diagnostic
        [TestMethod]
        public void AV010_None_TaskReturnNoAsyncSingleRequest()
        {
            var body = @"
            public static async Task Delay()
            {
            await Task.Delay(1000);
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<int> Return Async Method - serial requests - should show no diagnostic
        [TestMethod]
        public void AV011_None_TaskIntReturnNoAsyncSerialRequests()
        {
            var body = @"
            public static async Task<int> Multiply(int factor1, int factor2)
            {
            //delay three times
            await Task.Delay(100);
            await Task.Delay(100);
            await Task.Delay(100);

            return (factor1 * factor2);
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<bool> async method - no diagnostic
        [TestMethod]
        public void AV012_None_TaskBoolReturnAsync()
        {
            var body = @"
            private async Task<bool> TrueorFalseAsync(bool trueorfalse) 
            { 
            await Task.Delay(700); 

            return !(trueorfalse); 
            } ";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Void Return Async Method with Event handling - should show no diagnostic
        [TestMethod]
        public void AV013_None_AsyncVoidWithEventHandling()
        {
            var body = @"
            private async void button1_Click(object sender, EventArgs e) 
            { 
            await Button1ClickAsync(); 
            } 
            public async Task Button1ClickAsync() 
            { 
            // Do asynchronous work. 
            await Task.Delay(1000); 
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Void Return Async Method - parallel requests - should show 1 diagnostic
        [TestMethod]
        public void AV014_DACF_VoidReturnAsyncParallelRequests()
        {
            var body = @"         
        public async void SomeAsyncTask()
        { 
            int value = 708;
            await ValuePlusOne(value);
            await ValuePlusTwo(value);
            await ValuePlusThree(value);
        }
    
        public async Task<int> ValuePlusOne(int num)
        {
            await Task.Delay(90);

            return num;
        }

        public async Task<int> ValuePlusTwo(int num)
        {
            await Task.Delay(990);

            return num;
        }

        public async Task<int> ValuePlusThree(int num)
        {
            await Task.Delay(9990);

            return num;
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected001.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 27) };

            VerifyCSharpDiagnostic(test, expected001);

            var fixbody = @"         
        public async Task SomeAsyncTask()
        { 
            int value = 708;
            await ValuePlusOne(value);
            await ValuePlusTwo(value);
            await ValuePlusThree(value);
        }
    
        public async Task<int> ValuePlusOne(int num)
        {
            await Task.Delay(90);

            return num;
        }

        public async Task<int> ValuePlusTwo(int num)
        {
            await Task.Delay(990);

            return num;
        }

        public async Task<int> ValuePlusThree(int num)
        {
            await Task.Delay(9990);

            return num;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // No diagnostic shown during async void return when asynchronous event handler is used - parallel requests
        [TestMethod]
        public void AV015_None_VoidReturnaSyncWithEventParallel()
        {
            var body = @"
            private async void button1_Click(object sender, EventArgs e) 
            { 
            await Button1ClickAsync(); 
            await Button2ClickAsync();
            } 
            public async Task Button1ClickAsync() 
            { 
            // Do asynchronous work. 
            await Task.Delay(1000); 
            }
            public async Task Button2ClickAsync()
            {
            //Do asynchronous work.
            await Task.Delay(3000);
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // No diagnostic shown during async void return when asynchronous event handler is used - serial requests
        [TestMethod]
        public void AV016_None_VoidReturnaSyncWithEventSerial()
        {
            var body = @"
            private async void button1_Click(object sender, EventArgs e) 
            { 
            await Button1ClickAsync(); 
            await Button1ClickAsync();
            await Button1ClickAsync();
            } 
            public async Task Button1ClickAsync() 
            { 
            // Do asynchronous work. 
            await Task.Delay(1000); 
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Void Return Async Method with no event handlers - 1 diagnostic should show 
        [TestMethod]
        public void AV017_DACF_AsyncVoidWithOutEvent1()
        {
            var body = @"
        public async void AsyncVoidReturnTaskT()
        {
            Console.WriteLine(""Begin Program"");
            Console.WriteLine(await AsyncTaskReturnT());
            Console.WriteLine(""End Program"");
        }

        private async Task<string> AsyncTaskReturnT()
        {
            await Task.Delay(65656565);

            return ""Program is processing...."";
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected001.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 27) };

            VerifyCSharpDiagnostic(test, expected001);

            var fixbody = @"
        public async Task AsyncVoidReturnTaskT()
        {
            Console.WriteLine(""Begin Program"");
            Console.WriteLine(await AsyncTaskReturnT());
            Console.WriteLine(""End Program"");
        }

        private async Task<string> AsyncTaskReturnT()
        {
            await Task.Delay(65656565);

            return ""Program is processing...."";
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Void Return Async Method with no event handlers - 1 diagnostic should show 
        [TestMethod]
        public void AV018_DACF_AsyncVoidWithOutEvent2()
        {
            var body = @"
        private async void WaitForIt(int value)
        {
            await Task.Delay(value);
        }
        private void DoSomethingSilent(int value)
        {
            WaitForIt(value);
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected001.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 28) };

            VerifyCSharpDiagnostic(test, expected001);

            var fixbody = @"
        private async Task WaitForIt(int value)
        {
            await Task.Delay(value);
        }
        private void DoSomethingSilent(int value)
        {
            WaitForIt(value);
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest, null, true);
        }

        // Check to make sure the diagnostic still shows for methods with two parameters that are not Event Handlers
        [TestMethod]
        public void AV019_DACF_AsyncVoidTwoParams()
        {
            var body = @"
        private async void WaitForIt(int value, int value2)
        {
            await Task.Delay(value+value2);
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected001.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 28) };

            VerifyCSharpDiagnostic(test, expected001);

            var fixbody = @"
        private async Task WaitForIt(int value, int value2)
        {
            await Task.Delay(value+value2);
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Check to make sure the diagnostic still shows for methods with a parameter that implements Event Handler
        [TestMethod]
        public void AV020_DA_AsyncVoidImplementsEvent()
        {
            var body = @"
        using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace TestingTemplate
{
    [TestClass]
    public class UnitTest1
    {
        //Show no diagnostic for a param that implements an EventArgs
        public async void EventHandlerImplementAsync(object sender, ImplementsEvents e)
        {
            await Task.Delay(1000);
        }
    }
    public class ImplementsEvents : EventArgs
    {
        public ImplementsEvents() { }
    }
}";
            var test = body;
            VerifyCSharpDiagnostic(test);
        }

        // Check to make sure the diagnostic still shows for methods with a parameter that implements Event Handler
        [TestMethod]
        public void AV021_DA_AsyncVoidParentImplementsEvent()
        {
            var body = @"
        using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace TestingTemplate
{
    [TestClass]
    public class UnitTest1
    {
        //Show no diagnostic for a param that implements an EventArgs
        public async void EventHandlerImplementAsync(object sender, ParentImplementsEvents e)
        {
            await Task.Delay(1000);
        }
    }
    public class ImplementsEvents : EventArgs
    {
        public ImplementsEvents() { }
    }

    public class ParentImplementsEvents : ImplementsEvents
    {
        public ParentImplementsEvents() { }
    }
}
";
            var test = body;
            VerifyCSharpDiagnostic(test);
        }

        // Check to make sure the diagnostic still shows for methods with a parameter that implements Event Handler
        [TestMethod]
        public void AV022_DA_AsyncVoidGrandfatherImplementsEvent()
        {
            var body = @"
        using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace TestingTemplate
{
    [TestClass]
    public class UnitTest1
    {
        //Show no diagnostic for a param that implements an EventArgs
        public async void EventHandlerImplementAsync(object sender, ParentParentImplementsEvents e)
        {
            await Task.Delay(1000);
        }
    }
    public class ImplementsEvents : EventArgs
    {
        public ImplementsEvents() { }
    }

    public class ParentImplementsEvents : ImplementsEvents
    {
        public ParentImplementsEvents() { }
    }
    public class ParentParentImplementsEvents : ParentImplementsEvents
    {
        public ParentParentImplementsEvents() { }
    }
}";
            var test = body;
            VerifyCSharpDiagnostic(test);
        }

        #region VB Tests

        // Check to make sure the diagnostic appears for VB
        [TestMethod]
        public void AV023_DA_VBDiagnosticSimple()
        {
            var body = @"
Module Module1

    Async Sub example()
        Await Task.Delay(100)
    End Sub

End Module
";
            var test = body;

            DiagnosticResult expected001vb = new DiagnosticResult
            {
                Id = "Async001",
                Message = "This method has the async keyword but it returns void",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.vb", 4, 15) }
            };

            VerifyBasicDiagnostic(test, expected001vb);
        }

        // Check to make sure the diagnostic does not appear for the case of VB event handlers
        [TestMethod]
        public void AV024_None_VBEventHandler()
        {
            var body = @"
imports System

Module Module1

    Async Sub event_method(sender As Object, e As EventArgs)
        Await Task.Delay(100)
    End Sub

End Module
";
            var test = body;
            VerifyBasicDiagnostic(test);
        }

        #endregion
    }
}