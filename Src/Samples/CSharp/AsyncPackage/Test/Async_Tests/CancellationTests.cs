using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTemplate;

namespace AsyncPackage.Tests
{
    /// <summary>
    /// Unit Tests for the CancellationAnalyzer and CancellationCodeFix
    /// </summary>
    [TestClass]
    public class CancellationTests : CodeFixVerifier
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

        private DiagnosticResult expected005 = new DiagnosticResult
        {
            Id = "Async005",
            Message = "This method can take a CancellationToken",
            Severity = DiagnosticSeverity.Warning,

            // Location should be changed within test methods
            Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 10) }
        };

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CancellationCodeFix();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CancellationAnalyzer();
        }

        // No methods - no diagnostic should show
        [TestMethod]
        public void C001_None_NoMethodNoDiagnostics()
        {
            var body = @"";

            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // One method can pass a CT to another and does - no diagnostic
        [TestMethod]
        public void C002_None_BasicCasePass()
        {
            var body = @"
        public void BigMethod(CancellationToken cancellationToken)
        {
            SmallMethod(cancellationToken);
            return;
        }         

        public void SmallMethod(CancellationToken cancellationToken = default(CancellationToken)){
            return;
        }
        ";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // One method can pass a CT to another and does not
        [TestMethod]
        public void C003_DACF_BasicCaseNoPass()
        {
            var body = @"
        public void BigMethod(CancellationToken cancellationToken)
        {
            SmallMethod();
            return;
        }         

        public void SmallMethod(CancellationToken cancellationToken = default(CancellationToken))
        {
            return;
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void BigMethod(CancellationToken cancellationToken)
        {
            SmallMethod(cancellationToken);
            return;
        }         

        public void SmallMethod(CancellationToken cancellationToken = default(CancellationToken))
        {
            return;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // One method can pass a CT to another and instead passes CancellationToken.None - no diagnostic
        [TestMethod]
        public void C004_None_BasicCaseNone()
        {
            var body = @"
        public void BigMethod(CancellationToken cancellationToken)
        {
            SmallMethod(CancellationToken.None);
            return;
        }         

        public void SmallMethod(CancellationToken cancellationToken = default(CancellationToken)){
            return;
        }
        ";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Both methods have multiple parameters - no diagnostic
        [TestMethod]
        public void C005_None_ManyParamsPass()
        {
            var body = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            SmallMethod(b, cancellationToken, c);
            return;
        }         

        public void SmallMethod(string b, CancellationToken cancellationToken = default(CancellationToken), double c = 0.5){
            return;
        }
        ";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Both methods have multiple parameters - 1 diagnostic
        [TestMethod]
        public void C006_DACF_ManyParamsNoPass()
        {
            var body = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            SmallMethod(b, c);
            return;
        }         

        public void SmallMethod(string b, double c = 0.5, CancellationToken cancellationToken = default(CancellationToken)){
            return;
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            SmallMethod(b, c, cancellationToken);
            return;
        }         

        public void SmallMethod(string b, double c = 0.5, CancellationToken cancellationToken = default(CancellationToken)){
            return;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Containing method has many other things in it - 1 diagnostic
        [TestMethod]
        public void C007_DACF_OtherMiscInContainingMethodNoPass()
        {
            var body = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            for(int i = 0; i < a; i++){
                b = b + i;
            }

            double d = c + 10.5;
            
            //trivia here
            SmallMethod(b, d);

            string e = b.ToLower();

            return;
        }         

        public void SmallMethod(string b, double c = 0.5, CancellationToken cancellationToken = default(CancellationToken)){
                string f = (b+c).ToUpper();
                return;
        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 23, 13) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            for(int i = 0; i < a; i++){
                b = b + i;
            }

            double d = c + 10.5;
            
            //trivia here
            SmallMethod(b, d, cancellationToken);

            string e = b.ToLower();

            return;
        }         

        public void SmallMethod(string b, double c = 0.5, CancellationToken cancellationToken = default(CancellationToken)){
                string f = (b+c).ToUpper();
                return;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Containing methods have multiple invocations where a token can be passed - 1 diagnostic
        [TestMethod]
        public void C008_DACF_MultipleInvocationsInContainingMethod()
        {
            var body = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            SmallMethod(b, c, cancellationToken);
            SmallMethod(b, c);
            SmallMethod(b, c, CancellationToken.None);
            return;
        }         

        public void SmallMethod(string b, double c = 0.5, CancellationToken cancellationToken = default(CancellationToken)){
                return;
        }";
            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 13) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            SmallMethod(b, c, cancellationToken);
            SmallMethod(b, c, cancellationToken);
            SmallMethod(b, c, CancellationToken.None);
            return;
        }         

        public void SmallMethod(string b, double c = 0.5, CancellationToken cancellationToken = default(CancellationToken)){
                return;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Containing methods have multiple invocations where a token can be passed - 1 diagnostic
        public void C009_DACF_MultipleNoPassInContainingMethod()
        {
            var body = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            SmallMethod(b, c);
            SmallMethod(b, c, CancellationToken.None);
            SmallMethod(b, c);
            return;
        }         

        public void SmallMethod(string b, double c = 0.5, CancellationToken cancellationToken = default(CancellationToken)){
                return;
        }";
            var test = DefaultHeader + body + DefaultFooter;

            DiagnosticResult expected1 = new DiagnosticResult
            {
                Id = "Async005",
                Message = "This method can take a CancellationToken",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) }
            };

            DiagnosticResult expected2 = new DiagnosticResult
            {
                Id = "Async005",
                Message = "This method can take a CancellationToken",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 13) }
            };

            VerifyCSharpDiagnostic(test, expected1, expected2);

            var fixbody = @"
        public void BigMethod(int a, string b, CancellationToken cancellationToken, double c)
        {
            SmallMethod(b, c, cancellationToken);
            SmallMethod(b, c, CancellationToken.None);
            SmallMethod(b, c, cancellationToken);
            return;
        }         

        public void SmallMethod(string b, double c = 0.5, CancellationToken cancellationToken = default(CancellationToken)){
                return;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Nested invocations - 1 diagnostic
        [TestMethod]
        public void C010_DACF_NestedInvocation()
        {
            var body = @"
        public void OuterInvocation(CancellationToken c)
        {
            var x = MiddleInvocation(InnerInvocation(0));
        }
        public bool MiddleInvocation(bool t)
        {
            return t;
        }
        public bool InnerInvocation(int i, CancellationToken c = new CancellationToken())
        {
            return true;
        }";
            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 38) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void OuterInvocation(CancellationToken c)
        {
            var x = MiddleInvocation(InnerInvocation(0, c));
        }
        public bool MiddleInvocation(bool t)
        {
            return t;
        }
        public bool InnerInvocation(int i, CancellationToken c = new CancellationToken())
        {
            return true;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Multiple tokens in scope, both passed - no diagnostic
        [TestMethod]
        public void C011_None_MultipleTokensInScopePass()
        {
            var body = @"
        public void BigMethod (int s, CancellationToken ct1, CancellationToken ct2)
        {
            SmallMethod(s, ct1);
            SmallMethod(s, ct2);
        }

        public void SmallMethod (int s, CancellationToken ct = default(CancellationToken))
        {
            s = s == 123 ? 456 : 789;
        }";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Multiple tokens in scope - 1 diagnostic
        // Codefix should take the first token and put it in the first available slot
        [TestMethod]
        public void C012_DACF_MultipleTokensInScopeNoPass()
        {
            var body = @"
        public void BigMethod (int s, CancellationToken ct1, CancellationToken ct2)
        {
            SmallMethod(s);
            SmallMethod(s, ct1);
            SmallMethod(s, ct2);
        }

        public void SmallMethod (int s, CancellationToken ct = default(CancellationToken))
        {
            s = s == 123 ? 456 : 789;
        }";
            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void BigMethod (int s, CancellationToken ct1, CancellationToken ct2)
        {
            SmallMethod(s, ct1);
            SmallMethod(s, ct1);
            SmallMethod(s, ct2);
        }

        public void SmallMethod (int s, CancellationToken ct = default(CancellationToken))
        {
            s = s == 123 ? 456 : 789;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // No tokens to pass - no diagnostic
        [TestMethod]
        public void C013_None_NoTokensToPass()
        {
            var body = @"
        public void BigMethod (int s)
        {
            SmallMethod(s);
        }

        public void SmallMethod (int s, CancellationToken ct = default(CancellationToken))
        {
            s = s == 123 ? 456 : 789;
        }";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // Multiple default parameters - 1 diagnostic
        // Adding CancellationToken but not other default should not cause other diagnostics
        [TestMethod]
        public void C014_DACF_MultipleDefaultsNoPass()
        {
            var body = @"
        public void BigMethod(int s, CancellationToken ct)
        {
            SmallMethod(s);
        }

        public void SmallMethod(int s, CancellationToken ct = default(CancellationToken), int x = 0)
        {
            s = s + 1;
        }";
            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void BigMethod(int s, CancellationToken ct)
        {
            SmallMethod(s, ct);
        }

        public void SmallMethod(int s, CancellationToken ct = default(CancellationToken), int x = 0)
        {
            s = s + 1;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // Multiple tokens can be passed and multiple slots to pass them in - 1 diagnostic
        // Should just take the first token and put it in the first slot - anything else is out of scope
        [TestMethod]
        public void C015_DACF_MultipleTokensMultipleSlots()
        {
            var body = @"
        public void BigMethod(int s, CancellationToken ct1, CancellationToken ct2, CancellationToken ct3)
        {
            SmallMethod(s);
        }

        public void SmallMethod(int s, CancellationToken ct1 = default(CancellationToken), int x = 0, CancellationToken ct2 = default(CancellationToken))
        {
            s = s + 1;
        }";
            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void BigMethod(int s, CancellationToken ct1, CancellationToken ct2, CancellationToken ct3)
        {
            SmallMethod(s, ct1);
        }

        public void SmallMethod(int s, CancellationToken ct1 = default(CancellationToken), int x = 0, CancellationToken ct2 = default(CancellationToken))
        {
            s = s + 1;
        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }

        // CancellationToken can be passed, but it's a user made class called CancellationToken - no diagnostic
        [TestMethod]
        public void C016_None_NotSystemCancellationToken()
        {
            var test = @"
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
    {
        public void BigMethod(CancellationToken ct)
        {

            SmallMethod();

        }

        public void SmallMethod(CancellationToken ct = null)
        {
        }

    }

    public class CancellationToken
    {

    }
}";
            VerifyCSharpDiagnostic(test);
        }

        // An overload of the method takes a CancellationToken - no diagnostic, out of scope
        [TestMethod]
        public void C017_None_OverloadWithNoPass()
        {
            var body = @"
        public void BigMethod(CancellationToken ct)
        {
            SmallMethod(5);
        }

        public void SmallMethod(int n)
        {

        }

        public void SmallMethod(int n, CancellationToken ct)
        {

        }";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // An overload of the method uses default(CancellationToken) - no diagnostic, out of scope
        [TestMethod]
        public void C018_None_OverloadWithDefaultNoPass()
        {
            var body = @"
        public void BigMethod(CancellationToken ct)
        {
            SmallMethod(5);
        }

        public void SmallMethod(int n)
        {

        }

        public void SmallMethod(int n, CancellationToken ct = default(CancellationToken))
        {

        }";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // An overload of the method takes a CancellationToken, but the overload being used does not - no diagnostic
        [TestMethod]
        public void C019_None_OverloadWithTokenNotPossible()
        {
            var body = @"
        public void BigMethod(CancellationToken ct)
        {
            SmallMethod(5, 4);
        }

        public void SmallMethod(int n, int o)
        {

        }

        public void SmallMethod(int n, CancellationToken ct = default(CancellationToken))
        {

        }";
            var test = DefaultHeader + body + DefaultFooter;

            VerifyCSharpDiagnostic(test);
        }

        // The overload explicitly being used takes a CancellationToken- 1 diagnostic
        [TestMethod]
        public void C020_DACF_UsedOverloadTakesTokenNoPass()
        {
            var body = @"
        public void BigMethod(CancellationToken ct)
        {
            SmallMethod(5, 4);
        }

        public void SmallMethod(int n)
        {

        }

        public void SmallMethod(int n, int o, CancellationToken ct = default(CancellationToken))
        {

        }";

            var test = DefaultHeader + body + DefaultFooter;

            expected005.Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 13) };

            VerifyCSharpDiagnostic(test, expected005);

            var fixbody = @"
        public void BigMethod(CancellationToken ct)
        {
            SmallMethod(5, 4, ct);
        }

        public void SmallMethod(int n)
        {

        }

        public void SmallMethod(int n, int o, CancellationToken ct = default(CancellationToken))
        {

        }";

            var fixtest = DefaultHeader + fixbody + DefaultFooter;
            VerifyCSharpFix(test, fixtest);
        }
    }
}