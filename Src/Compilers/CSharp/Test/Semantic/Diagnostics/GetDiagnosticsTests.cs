// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

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
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"C : Abracadabr");
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"Abracadabra[\r\n]+", ErrorId);
            DiagnosticsHelper.VerifyDiagnostics(model, source, @"bracadabra[\r\n]+");
        }
    }
}
