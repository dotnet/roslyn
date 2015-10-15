// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class GetDiagnosticsTests : CSharpTestBase
    {
        [Fact]
        public void DiagnosticsFilteredInMethodBody()
        {
            var source = @"
class C
{
    void M()
    {
        @
        #
        !
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            DiagnosticsHelper.VerifyDiagnostics(model, source, @"(?s)^.*$", "CS1646", "CS1024", "CS1525", "CS1002");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"\s*(?=@)", "CS1646");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"#", "CS1024");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"(?<=\!)", "CS1525", "CS1002");
        }

        [Fact]
        public void DiagnosticsFilteredInMethodBodyInsideNamespace()
        {
            var source = @"
namespace N
{
    class C
    {
        void S()
        {
            var x = X;
        }
    }
}

class D
{
    int P
    {
        get
        {
            return Y;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            DiagnosticsHelper.VerifyDiagnostics(model, source, @"var x = X;", "CS0103");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"return Y;", "CS0103");
        }

        [Fact]
        public void DiagnosticsFilteredForInsersectingIntervals()
        {
            var source = @"
class C : Abracadabra
{
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

            const string ErrorId = "CS0246";
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"(?s)^.*$", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"Abracadabra", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"C : Abracadabra", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"C : Abracadabr", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"Abracadabra[\r\n]+", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"bracadabra[\r\n]+", ErrorId);
        }

        [Fact, WorkItem(1066483)]
        public void TestDiagnosticWithSeverity()
        {
            var source = @"
class C
{
    public void Foo()
    {
        int x;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source);
            var diag = compilation.GetDiagnostics().Single();

            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal(3, diag.WarningLevel);

            var error = diag.WithSeverity(DiagnosticSeverity.Error);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, error.DefaultSeverity);
            Assert.Equal(0, error.WarningLevel);

            var warning = error.WithSeverity(DiagnosticSeverity.Warning);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, warning.DefaultSeverity);
            Assert.Equal(3, warning.WarningLevel);

            var hidden = diag.WithSeverity(DiagnosticSeverity.Hidden);
            Assert.Equal(DiagnosticSeverity.Hidden, hidden.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, hidden.DefaultSeverity);
            Assert.Equal(4, hidden.WarningLevel);

            var info = diag.WithSeverity(DiagnosticSeverity.Info);
            Assert.Equal(DiagnosticSeverity.Info, info.Severity);
            Assert.Equal(DiagnosticSeverity.Warning, info.DefaultSeverity);
            Assert.Equal(4, info.WarningLevel);
        }
    }
}
