using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTemplate;

namespace AsyncPackage.Tests
{
    /// <summary>
    /// Unit Tests for the RenameAsyncAnalyzer and RenameAsyncCodeFix
    /// </summary>
    [TestClass]
    public class RenameAsyncTests : CodeFixVerifier
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

        private DiagnosticResult expected002 = new DiagnosticResult
        {
            Id = "Async002",
            Message = "This method is async but the method name does not end in Async",
            Severity = DiagnosticSeverity.Warning,

            // Location should be changed within test methods
            Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 10) }
        };

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new RenameAsyncCodeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new RenameAsyncAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new RenameAsyncAnalyzer();
        }

        // No methods - no diagnostic should show
        [TestMethod]
        public void RA001_None_NoMethodNoDiagnostics()
        {
            var body = @"";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<int> async method includes "Async" - no diagnostic
        [TestMethod]
        public void RA002_None_TaskIntReturnWithAsync()
        {
            var body = @"
            private async Task<int> MathAsync(int num1) 
            { 
            await Task.Delay(3000); 

            return (num1); 
            } ";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Task<bool> async method includes "async" - no diagnostic
        [TestMethod]
        public void RA003_None_TaskBoolReturnWithAsync()
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

        // Async method does not includes "Async" - 1 diagnostic
        [TestMethod]
        public void RA004_DACF_AsyncMethodWithoutAsync()
        {
            var body = @"
        private async Task<int> Math(int num1) 
        { 
            await Task.Delay(3000); 

            return (num1); 
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 33) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        private async Task<int> MathAsync(int num1) 
        { 
            await Task.Delay(3000); 

            return (num1); 
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Async method named "async" - no diagnostic 
        [TestMethod]
        public void RA005_None_AsyncMethodNamedAsync()
        {
            var body = @"
            private async Task<bool> Async(bool trueorfalse) 
            { 
            await Task.Delay(700); 

            return !(trueorfalse); 
            } ";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Non async method named "async" - no diagnostic
        [TestMethod]
        public void RA006_None_NonAsyncMethodnamedAsync()
        {
            var body = @"
            public string Async(int zipcode) 
            { 
            if (zipcode == 21206) 
            { 
                return ""Baltimore""; 
            } 
            else 
                return ""Unknown""; 
            } ";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Non-Async method that do not include async - no diagnostic 
        [TestMethod]
        public void RA007_None_NonAsyncWithoutAsync()
        {
            var body = @"
            private string RepeatingStrings(int num, string Message)
            {
                while (num > 0)
                {
                    return RepeatingStrings(num - 1, Message);
                }
            return Message;
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Adding Async would cause conflict 
        [TestMethod]
        public void RA008_DACF_MethodNameConflict1()
        {
            var body = @"
        private async Task<int> DoSomeMath(int number1, int number2) 
        { 
            await Task.Delay(100); 

            return (number1 + number2); 
        } 
    
        private async Task<int> DoSomeMathAsync(int number1, int number2) 
        { 
            await Task.Delay(500); 

            return (number1 * number2); 
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 33) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        private async Task<int> DoSomeMath(int number1, int number2) 
        { 
            await Task.Delay(100); 

            return (number1 + number2); 
        } 
    
        private async Task<int> DoSomeMathAsync(int number1, int number2) 
        { 
            await Task.Delay(500); 

            return (number1 * number2); 
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Method name conflict - 1 diagnostic
        [TestMethod]
        public void RA009_DACF_MethodNameConflict2()
        {
            var body = @"
        private int Math(int num1, int num2) 
        { 
            return (num1 - num2); 
        } 

        private async Task<int> Math(int num1) 
        { 
            await Task.Delay(3000); 

            return (num1); 
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 33) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        private int Math(int num1, int num2) 
        { 
            return (num1 - num2); 
        } 

        private async Task<int> MathAsync(int num1) 
        { 
            await Task.Delay(3000); 

            return (num1); 
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Async keyword is at beginning - should show 1 diagnostic 
        [TestMethod]
        public void RA010_DACF_AsyncKeywordInBeginning()
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

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 36) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        public async void AsyncVoidReturnTaskT()
        {
            Console.WriteLine(""Begin Program"");
            Console.WriteLine(await AsyncTaskReturnTAsync());
            Console.WriteLine(""End Program"");
        }

        private async Task<string> AsyncTaskReturnTAsync()
        {
            await Task.Delay(65656565);

            return ""Program is processing...."";
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Misspelled async in method name - 1 diagnostic
        [TestMethod]
        public void RA011_DACF_MisspelledAsync()
        {
            var body = @"
        private async Task<int> DoSomeMathAsnyc(int number1, int number2) 
        { 
            await Task.Delay(100); 

            return (number1 + number2); 
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 33) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        private async Task<int> DoSomeMathAsync(int number1, int number2) 
        { 
            await Task.Delay(100); 

            return (number1 + number2); 
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // No capital A in Async method name - 1 diagnostic
        [TestMethod]
        public void RA012_DACF_NoCapitalAinAsync()
        {
            var body = @"
        public static Task<String> ReturnAAAasync()
        {
            return Task.Run(() =>
            { 
                return (""AAA""); 
            });
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 36) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        public static Task<String> ReturnAAAAsync()
        {
            return Task.Run(() =>
            { 
                return (""AAA""); 
            });
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Method name contains async but misspelled and no capitol A - 1 diagnostic
        [TestMethod]
        public void RA013_DACF_MisspelledAndNoCapitolA()
        {
            var body = @"
        public static async Task<int> Multiplyanysc(int factor1, int factor2)
        {
            //delay three times
            await Task.Delay(100);
            await Task.Delay(100);
            await Task.Delay(100);

            return (factor1 * factor2);
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 39) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        public static async Task<int> MultiplyAsync(int factor1, int factor2)
        {
            //delay three times
            await Task.Delay(100);
            await Task.Delay(100);
            await Task.Delay(100);

            return (factor1 * factor2);
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Misspelled Async named Async method - diagnostic should rename to Async
        [TestMethod]
        public void RA014_DACF_AsyncnamedAsyncMethodMisspelled()
        {
            var body = @"
        private async Task<bool> Anysc(bool trueorfalse) 
        { 
            await Task.Delay(700); 

            return !(trueorfalse); 
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 34) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        private async Task<bool> Async(bool trueorfalse) 
        { 
            await Task.Delay(700); 

            return !(trueorfalse); 
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Very close spelling of async in name but wrong letters - 1 diagnostic
        [TestMethod]
        public void RA015_DACF_CloseSpellingOfAsyncbutNotEnough()
        {
            var body = @"
        private async Task<int> MathAzync(int num1) 
        { 
            await Task.Delay(3000); 

            return (num1); 
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 33) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        private async Task<int> MathAzyncAsync(int num1) 
        { 
            await Task.Delay(3000); 

            return (num1); 
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Very close spelling of async in name but wrong letters - 1 diagnostic
        [TestMethod]
        public void RA016_DACF_CloseSpellingOfAsyncbutNotEnough2()
        {
            var body = @"
        private async Task<int> MathAsytnc(int num1) 
        { 
            await Task.Delay(3000); 

            return (num1); 
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected002.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 33) };

            VerifyCSharpDiagnostic(test, expected002);

            var fixbody = @"
        private async Task<int> MathAsytncAsync(int num1) 
        { 
            await Task.Delay(3000); 

            return (num1); 
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Test multiple fixes in a row - should have 4 diagnostics and fixes
        [TestMethod]
        public void RA017_DACF_MultipleMisspellings()
        {
            var body = @"
        private async Task Wait10Asycn()
        {
            await Task.Delay(10);
        }
        private async Task Wait50Aysnc()
        {
            await Task.Delay(50);
        }
        private async Task Wait100Ayscn()
        {
            await Task.Delay(100);
        }
        private async Task Wait200asycn()
        {
            await Task.Delay(200);
        }";

            var test = DefaultHeader + body + DefaultFooter;

            DiagnosticResult expected1 = new DiagnosticResult
            {
                Id = "Async002",
                Message = "This method is async but the method name does not end in Async",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 28) }
            };

            DiagnosticResult expected2 = new DiagnosticResult
            {
                Id = "Async002",
                Message = "This method is async but the method name does not end in Async",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 28) }
            };

            DiagnosticResult expected3 = new DiagnosticResult
            {
                Id = "Async002",
                Message = "This method is async but the method name does not end in Async",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 28) }
            };

            DiagnosticResult expected4 = new DiagnosticResult
            {
                Id = "Async002",
                Message = "This method is async but the method name does not end in Async",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 25, 28) }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2, expected3, expected4);

            var fixbody = @"
        private async Task Wait10Async()
        {
            await Task.Delay(10);
        }
        private async Task Wait50Async()
        {
            await Task.Delay(50);
        }
        private async Task Wait100Async()
        {
            await Task.Delay(100);
        }
        private async Task Wait200Async()
        {
            await Task.Delay(200);
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        #region VB Tests

        // Simple missing "Async" test for VB - 1 diagnostic
        // Codefix is out of scope
        [TestMethod]
        public void RA018_DA_VBRenameSimple()
        {
            var body = @"
imports System
imports System.Threading.Tasks

Module Module1

    Async Function example() As Task
    End Function

End Module
";

            var test = body;

            DiagnosticResult expectedvb = new DiagnosticResult
            {
                Id = "Async002",
                Message = "This method is async but the method name does not end in Async",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.vb", 7, 20) }
            };

            VerifyBasicDiagnostic(test, expectedvb);
        }

        // Misspelled "Async" test for VB - 1 diagnostic
        // Codefix is out of scope
        [TestMethod]
        public void RA019_DA_VBRenameMisspelledAsync()
        {
            var body = @"
imports System
imports System.Threading.Tasks

Module Module1

    Async Function exampleasycn() As Task
    End Function

End Module
";
            var test = body;

            DiagnosticResult expectedvb = new DiagnosticResult
            {
                Id = "Async002",
                Message = "This method is async but the method name does not end in Async",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.vb", 7, 20) }
            };

            VerifyBasicDiagnostic(test, expectedvb);
        }

        #endregion
    }
}