// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Analyzers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Analyzers;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
{
    public class UpgradeMSBuildWorkspaceAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer() => VisualBasicUpgradeMSBuildWorkspaceAnalyzer.CreateForTests();

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => CSharpUpgradeMSBuildWorkspaceAnalyzer.CreateForTests();

        [Fact]
        public void CSharp_VerifyWithMSBuildWorkspace()
        {
            const string source1 = @"
namespace Microsoft.CodeAnalysis.MSBuild
{
    public class MSBuildWorkspace
    {
        public static MSBuildWorkspace Create() => null;
    }
}";

            const string source2 = @"
using Microsoft.CodeAnalysis.MSBuild;

class Usage
{
    void M()
    {
        var workspace = MSBuildWorkspace.Create();
    }
}";

            VerifyCSharp(new[] { source1, source2 });
        }

        [Fact]
        public void CSharp_VerifyWithoutMSBuildWorkspace()
        {
            const string source = @"
using Microsoft.CodeAnalysis.MSBuild;

class Usage
{
    void M()
    {
        var workspace = MSBuildWorkspace.Create();
    }
}";

            var expected = new[] { GetCSharpExpectedDiagnostic(8, 25) };

            VerifyCSharp(source, TestValidationMode.AllowCompileErrors, expected);
        }

        [Fact]
        public void VisualBasic_VerifyWithMSBuildWorkspace()
        {
            const string source1 = @"
Namespace Microsoft.CodeAnalysis.MSBuild
    Public Class MSBuildWorkspace
        Public Shared Function Create() As MSBuildWorkspace
            Return Nothing
        End Function
    End Class
End Namespace";

            const string source2 = @"
Imports Microsoft.CodeAnalysis.MSBuild

Class Usage
    Sub M()
        Dim workspace = MSBuildWorkspace.Create()
    End Sub
End Class";

            VerifyBasic(new[] { source1, source2 });
        }

        [Fact]
        public void VisualBasic_VerifyWithoutMSBuildWorkspace()
        {
            const string source = @"
Imports Microsoft.CodeAnalysis.MSBuild

Class Usage
    Sub M()
        Dim workspace = MSBuildWorkspace.Create()
    End Sub
End Class";

            var expected = new[] { GetBasicExpectedDiagnostic(6, 25) };

            VerifyBasic(source, TestValidationMode.AllowCompileErrors, expected);
        }

        private static DiagnosticResult GetCSharpExpectedDiagnostic(int line, int column)
            => GetExpectedDiagnostic(line, column);

        private static DiagnosticResult GetBasicExpectedDiagnostic(int line, int column)
            => GetExpectedDiagnostic(line, column);

        private static DiagnosticResult GetExpectedDiagnostic(int line, int column)
        {
            return new DiagnosticResult(DiagnosticIds.UpgradeMSBuildWorkspaceRuleId, DiagnosticHelpers.DefaultDiagnosticSeverity)
               .WithLocation(line, column)
               .WithMessageFormat(CodeAnalysisDiagnosticsResources.UpgradeMSBuildWorkspaceMessage);
        }
    }
}
