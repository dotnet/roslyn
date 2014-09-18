// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Usage;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class CA2231FixerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new CA2231DiagnosticAnalyzer();
        }

        protected override ICodeFixProvider GetBasicCodeFixProvider()
        {
            return new CA2231BasicCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CA2231DiagnosticAnalyzer();
        }

        protected override ICodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CA2231CSharpCodeFixProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231CSharpCodeFixNoEqualsOperator()
        {
            VerifyCSharpFix(@"
using System;

// value type without overridding Equals
public struct A
{    
    public override bool Equals(Object obj)
    {
        return true;
    }
}
",
@"
using System;

// value type without overridding Equals
public struct A
{
    public override bool Equals(Object obj)
    {
        return true;
    }

    public static bool operator ==(A left, A right)
    {
        throw new NotImplementedException();
    }

    public static bool operator !=(A left, A right)
    {
        throw new NotImplementedException();
    }
}
",

                // This fix introduces the compiler warning:
                // Test0.cs(5,15): warning CS0661: 'A' defines operator == or operator != but does not override Object.GetHashCode()
                allowNewCompilerDiagnostics: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231BasicCodeFixNoEqualsOperator()
        {
            VerifyBasicFix(@"
Imports System

Public Structure A
    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function
End Structure
",
@"
Imports System

Public Structure A
    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator =(left As A, right As A) As Boolean
        Throw New NotImplementedException()
    End Operator

    Public Shared Operator <>(left As A, right As A) As Boolean
        Throw New NotImplementedException()
    End Operator
End Structure
");
        }
    }
}
