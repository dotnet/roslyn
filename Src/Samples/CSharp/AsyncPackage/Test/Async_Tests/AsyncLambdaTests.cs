using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTemplate;

namespace AsyncPackage.Tests
{
    /// <summary>
    /// Unit Tests for the AsyncLambdaDiagnosticAnalzer and AsyncLambdaVariableCodeFix
    /// </summary>
    [TestClass]
    public class AsyncLambdaTests : CodeFixVerifier
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

        private DiagnosticResult expected003 = new DiagnosticResult
        {
            Id = "Async003",
            Message = "This async lambda is passed as a void-returning delegate type",
            Severity = DiagnosticSeverity.Warning,

            // Location should be changed within test methods
            Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 10) }
        };

        private DiagnosticResult expected004 = new DiagnosticResult
        {
            Id = "Async004",
            Message = "This async lambda is stored as a void-returning delegate type",
            Severity = DiagnosticSeverity.Warning,

            // Location should be changed within test methods
            Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 10) }
        };

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AsyncLambdaVariableCodeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AsyncLambdaAnalyzer();
        }

        [TestMethod]
        public void AL001_None_NoMethodNoDiagnostics()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void AL002_DACF_AssignVoidableVariable()
        {
            var body = @"
            public void method()
            {
                //assign to voidable variable
                Action thing = async () =>
                {
                await Task.Delay(500);
                };
            }";

            var test = DefaultHeader + body + DefaultFooter;

            expected004.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 17) };

            VerifyCSharpDiagnostic(test, expected004);

            var fix = @"
            public void method()
            {
            //assign to voidable variable
            Func<Task> thing = async () =>
                {
                await Task.Delay(500);
                };
            }";

            var testfix = DefaultHeader + fix + DefaultFooter;

            // Causes new unused using directory error because the code is not using System.Action anymore
            VerifyCSharpFix(test, testfix, null, true);
        }

        // Task function assigned to lambda - no diagnostic should show
        [TestMethod]
        public void AL003_None_TaskReturningDelegateType()
        {
            var body = @"
            static void Main(string[] args)
            {
                //assign as correct variable
                Func<Task> thing2 = async () =>
                {
                await Task.Delay(666);
                };
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Method with void-returning delegate type - diagnostic should show on the lambda
        [TestMethod]
        public void AL004_DA_PassAsVoidReturningDelagateType()
        {
            var body = @"
        //bad implementation
        public static double BadMethod(Action action)
        {
            return 0;
        }           

        //pass as void-returning delegate type
        double num = BadMethod(async () =>
        {
            await Task.Delay(1000);
        });
        ";
            var test = DefaultHeader + body + DefaultFooter;

            expected003.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 32) };

            VerifyCSharpDiagnostic(test, expected003);
        }

        // Method with correct delegate type - no diagnostic should show
        [TestMethod]
        public void AL005_None_PassAsTaskReturningDelagateType()
        {
            var body = @"
        //good implementation
        public static double GoodMethod(Func<Task> func)
        {
            return 0;
        }         

        //pass as void-returning delegate type
        double num = GoodMethod(async () =>
        {
            await Task.Delay(1000);
        });
        ";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Lambda overrides a Task function - no diagnostic should show
        [TestMethod]
        public void AL006_None_TaskOverrideDelegateType()
        {
            var body = @"
            static void Main(string[] args)
            {
                //use correct override
                Task.Factory.StartNew(async () =>
                {
                await Task.Delay(12345);
                });
            }";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Test delegate vs => correct - no diagnostic should show
        [TestMethod]
        public void AL007_None_DelegateLambdaTaskReturn()
        {
            var body = @"
        //good implementation
        public static double GoodMethod(Func<Task> func)
        {
            return 0;
        }         

        //pass as void-returning delegate type
        double num = GoodMethod(async delegate ()
        {
            await Task.Delay(222);
        });
        ";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Test delegate vs => incorrect - 1 diagnostic should show
        [TestMethod]
        public void AL008_DA_DelegateLambdaVoidReturn()
        {
            var body = @"
        //good implementation
        public static double BadMethod(Action a)
        {
            return 0;
        }         

        //pass as void-returning delegate type
        double num = BadMethod(async delegate ()
        {
            await Task.Delay(100);
        });
        ";

            var test = DefaultHeader + body + DefaultFooter;

            expected003.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 32) };

            VerifyCSharpDiagnostic(test, expected003);
        }

        // Test simple lambda correct - no diagnostic should show
        [TestMethod]
        public void AL009_None_SimpleLambdaTaskReturn()
        {
            var body = @"
        //good implementation
        public static double GoodMethod(Func<Task> func)
        {
            return 0;
        }         

        //pass as void-returning delegate type
        double num = GoodMethod(async () => await Task.Delay(10));
        ";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Test simple lambda incorrect - 1 diagnostic should show
        [TestMethod]
        public void AL010_DA_SimpleLambdaVoidReturn()
        {
            var body = @"
        //good implementation
        public static double BadMethod(Action a)
        {
            return 0;
        }         

        //pass as void-returning delegate type
        double num = BadMethod(async () => await Task.Delay(10));
        ";
            var test = DefaultHeader + body + DefaultFooter;

            expected003.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 32) };

            VerifyCSharpDiagnostic(test, expected003);
        }

        // Missing necessary using directives - 1 diagnostic
        [TestMethod]
        public void AL011_DA_MissingUsings()
        {
            var body = @"
        using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class Program
    {
        //bad implementation
        public static double BadMethod(System.Action action)
        {
            return 0;
        }

        //pass as void-returning delegate type
        double num = BadMethod(async () =>
        {
            await System.Threading.Tasks.Task.Delay(1000);
        });

    }
}";
            var test = body;

            expected003.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 32) };

            VerifyCSharpDiagnostic(test, expected003);
        }
    }
}