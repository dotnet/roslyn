// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public partial class OverloadOperatorEqualsOnOverridingValueTypeEqualsTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new OverloadOperatorEqualsOnOverridingValueTypeEqualsAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new OverloadOperatorEqualsOnOverridingValueTypeEqualsAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231NoWarningCSharp()
        {
            VerifyCSharp(@"
    using System;

    // Non-value type
    public class A
    {    
        public override bool Equals(Object obj)
        {
            return true;
        }
    }

    // value type without overridding Equals
    public struct B
    {    
        public new bool Equals(Object obj)
        {
            return true;
        }
    }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231NoEqualsOperatorCSharp()
        {
            VerifyCSharp(@"
    using System;

    public struct A
    {
        public override bool Equals(Object obj)
        {
            return true;
        }
    }
",
            GetCA2231CSharpResultAt(4, 19));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231NoEqualsOperatorCSharpOutofScope()
        {
            VerifyCSharp(@"
    using System;

    public struct A
    {
        public override bool Equals(Object obj)
        {
            return true;
        }
    }

    [|// value type without overridding Equals
    public struct B
    {    
        public new bool Equals(Object obj)
        {
            return true;
        }
    }|]
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231CSharpInnerClassHasNoEqualsOperatorCSharp()
        {
            VerifyCSharp(@"
    using System;

    public struct A
    {
        public override bool Equals(Object obj)
        {
            return true;
        }

        public struct Aa
        {
            public override bool Equals(Object obj)
            {
                return true;
            }
        }
    }
",
            GetCA2231CSharpResultAt(4, 19),
            GetCA2231CSharpResultAt(11, 23));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231HasEqualsOperatorCSharp()
        {
            VerifyCSharp(@"
    using System;

    public struct A
    {
        public override bool Equals(Object obj)
        {
            return true;
        }

        public static bool operator ==(C c1, C c2)
        {
            return false;
        }

        public static bool operator !=(C c1, C c2)
        {
            return false;
        }
    }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231NoWarningBasic()
        {
            VerifyBasic(@"
Imports System

Public Class A
    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231NoEqualsOperatorBasic()
        {
            VerifyBasic(@"
Imports System

Public Structure A
    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function
End Structure
",
            GetCA2231BasicResultAt(4, 18));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231NoEqualsOperatorBasicWithScope()
        {
            VerifyBasic(@"
Imports System

[|Public Class A
    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function
End Class|]

Public Structure B
    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function
End Structure
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231BasicInnerClassHasNoEqualsOperatorBasic()
        {
            VerifyBasic(@"
Imports System

Public Structure A
    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Structure Aa
        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return True
        End Function
    End Structure
End Structure
",
            GetCA2231BasicResultAt(4, 18),
            GetCA2231BasicResultAt(9, 22));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2231HasEqualsOperatorBasic()
        {
            VerifyBasic(@"
Imports System

Public Structure A
    Public Overloads Overrides Function Equals(obj As Object) As Boolean
        Return True
    End Function

    Public Shared Operator =(left As A, right As A)
        Return True
    End Operator

    Public Shared Operator <>(left As A, right As A)
        Return True
    End Operator
End Structure
");
        }

        internal static string CA2231Name = "CA2231";
        internal static string CA2231Message = "Overload operator equals on overriding ValueType.Equals";

        private static DiagnosticResult GetCA2231CSharpResultAt(int line, int column)
        {
            return GetCSharpResultAt(line, column, CA2231Name, CA2231Message);
        }

        private static DiagnosticResult GetCA2231BasicResultAt(int line, int column)
        {
            return GetBasicResultAt(line, column, CA2231Name, CA2231Message);
        }
    }
}
