// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.AnalyzerPowerPack;
using Microsoft.AnalyzerPowerPack.Design;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests
{
    // Some of the CA1052Tests tests hold true here as CA1052 and CA1053 are mutually exclusive
    public class CA1053Tests : DiagnosticAnalyzerTestBase
    {
        #region Verifiers

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new StaticTypeRulesDiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new StaticTypeRulesDiagnosticAnalyzer();
        }

        internal static string RuleCA1053Name = "CA1053";
        internal static string RuleCA1053Text = "Type '{0}' is a static holder type and should not contain Instance Constructors";

        private static DiagnosticResult CSharpResult(int line, int column, string objectName)
        {
            return GetCSharpResultAt(line, column, StaticTypeRulesDiagnosticAnalyzer.CA1053RuleId, string.Format(AnalyzerPowerPackRulesResources.StaticHolderTypesShouldNotHaveConstructorsMessage, objectName));
        }

        private static DiagnosticResult BasicResult(int line, int column, string objectName)
        {
            return GetBasicResultAt(line, column, StaticTypeRulesDiagnosticAnalyzer.CA1053RuleId, string.Format(AnalyzerPowerPackRulesResources.StaticHolderTypesShouldNotHaveConstructorsMessage, objectName));
        }

        #endregion

        #region CSharp 
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1053EmptyNestedClassCSharp()
        {
            VerifyCSharp(@"
public class C
{
    protected class D
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1053NonDefaultInstanceConstructorCSharp()
        {
            VerifyCSharp(@"
public class Program
{
    static void Main(string[] args)
    {
    }
    
    static Program()
    {
    }
    
    public Program(int x)
    {
    }

    private Program(int x , int y)
    {
    }
}
",
    CSharpResult(2, 14, "Program"));
        }

        #endregion

        #region VisualBasic
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1053EmptyNestedClassBasic()
        {
            VerifyBasic(@"
Public Class C
    Protected Class A
    End Class
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1053NonDefaultInstanceConstructorBasic()
        {
            VerifyBasic(@"
Public Class C
    Shared Sub Main(args As String())
    End Sub

    Shared Sub New()
    End Sub

    Public Sub New(x As Integer)
    End Sub

    Private Sub New(x As Integer, y As Integer)
    End Sub
End Class
",
    BasicResult(2, 14, "C"));
        }
        #endregion 
    }
}
