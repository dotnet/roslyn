// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.AnalyzerPowerPack.Design;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace Microsoft.AnalyzerPowerPack.UnitTests
{
    public class CA1052Tests : DiagnosticAnalyzerTestBase
    {
        #region Verifiers

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CA1052DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new CA1052DiagnosticAnalyzer();
        }

        private static DiagnosticResult CSharpResult(int line, int column, string objectName)
        {
            return GetCSharpResultAt(line, column, CA1052DiagnosticAnalyzer.DiagnosticId, string.Format(AnalyzerPowerPackRulesResources.StaticHolderTypeIsNotStatic, objectName));
        }

        private static DiagnosticResult BasicResult(int line, int column, string objectName)
        {
            return GetBasicResultAt(line, column, CA1052DiagnosticAnalyzer.DiagnosticId, string.Format(AnalyzerPowerPackRulesResources.StaticHolderTypeIsNotStatic, objectName));
        }

        #endregion

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForEmptyNonStaticClassCSharp()
        {
            VerifyCSharp(@"
public class C1
{
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForEmptyInheritableClassBasic()
        {
            VerifyBasic(@"
Public Class B1
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]

        public void CA1052NoDiagnosticForStaticClassWithOnlyStaticDeclaredMembersCSharp()
        {
            VerifyCSharp(@"
public static class C2
{
    public static void Foo() { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForSealedClassWithOnlyStaticDeclaredMembersCSharp()
        {
            VerifyCSharp(@"
public sealed class C3
{
    public static void Foo() { }
}
",
                CSharpResult(2, 21, "C3"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonInheritableClassWithOnlySharedDeclaredMembersBasic()
        {
            VerifyBasic(@"
Public NotInheritable Class B3
    Public Shared Sub Foo()
    End Sub
End Class
",
                BasicResult(2, 29, "B3"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithOnlyStaticDeclaredMembersCSharp()
        {
            VerifyCSharp(@"
public class C4
{
    public static void Foo() { }
}
",
                CSharpResult(2, 14, "C4"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithOnlySharedDeclaredMembersBasic()
        {
            VerifyBasic(@"
Public Class B4
    Public Shared Sub Foo()
    End Sub
EndClass
",
                BasicResult(2, 14, "B4"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithBothStaticAndInstanceDeclaredMembersCSharp()
        {
            VerifyCSharp(@"
public class C5
{
    public void Moo() { }
    public static void Foo() { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithBothSharedAndInstanceDeclaredMembersBasic()
        {
            VerifyBasic(@"
Public Class B5
    Public Sub Moo()
    End Sub

    Public Shared Sub Foo()
    End Sub
EndClass
");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForInternalClassWithOnlyStaticDeclaredMembersCSharp()
        {
            VerifyCSharp(@"
internal class C6
{
    public static void Foo() { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForFriendClassWithOnlySharedDeclaredMembersBasic()
        {
            VerifyBasic(@"
Friend Class B6
    Public Shared Sub Foo()
    End Sub
EndClass
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithOperatorOverloadCSharp()
        {
            VerifyCSharp(@"
public class C7
{
    public static int operator +(C7 a, C7 b)
    {
        return 0;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithOperatorOverloadBasic()
        {
            VerifyBasic(@"
Public Class B7
    Public Shared Operator +(a As B7, b As B7) As Integer
        Return 0
    End Operator
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithStaticMethodAndOperatorOverloadCSharp()
        {
            VerifyCSharp(@"
public class C8
{
    public static void Foo() { }

    public static int operator +(C8 a, C8 b)
    {
        return 0;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithSharedMethodAndOperatorOverloadBasic()
        {
            VerifyBasic(@"
Public Class B8
    Public Shared Sub Foo()
    End Sub

    Public Shared Operator +(a As B8, b As B8) As Integer
        Return 0
    End Operator
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithPublicDefaultConstructorAndStaticMethodCSharp()
        {
            VerifyCSharp(@"
public class C9
{
    public C9() { }

    public static void Foo() { }
}
",
            CSharpResult(2, 14, "C9"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithPublicDefaultConstructorAndSharedMethodBasic()
        {
            VerifyBasic(@"
Public Class B9
    Public Sub New()
    End Sub

    Public Shared Sub Foo()
    End Sub
End Class
",
            BasicResult(2, 14, "B9"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithProtectedDefaultConstructorAndStaticMethodCSharp()
        {
            VerifyCSharp(@"
public class C10
{
    protected C10() { }

    public static void Foo() { }
}
",
            CSharpResult(2, 14, "C10"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithProtectedDefaultConstructorAndSharedMethodBasic()
        {
            VerifyBasic(@"
Public Class B10
    Protected Sub New()
    End Sub

    Public Shared Sub Foo()
    End Sub
End Class
",
            BasicResult(2, 14, "B10"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithPrivateDefaultConstructorAndStaticMethodCSharp()
        {
            VerifyCSharp(@"
public class C11
{
    private C11() { }

    public static void Foo() { }
}
",
            CSharpResult(2, 14, "C11"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithPrivateDefaultConstructorAndSharedMethodBasic()
        {
            VerifyBasic(@"
Public Class B11
    Private Sub New()
    End Sub

    Public Shared Sub Foo()
    End Sub
End Class
",
            BasicResult(2, 14, "B11"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithPublicNonDefaultConstructorAndStaticMethodCSharp()
        {
            VerifyCSharp(@"
public class C12
{
    public C12(int i) { }

    public static void Foo() { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithPublicNonDefaultConstructorAndSharedMethodBasic()
        {
            VerifyBasic(@"
Public Class B12
    Public Sub New(i as Integer)
    End Sub

    Public Shared Sub Foo()
    End Sub
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithPublicNonDefaultConstructorWithDefaultedParametersAndStaticMethodCSharp()
        {
            VerifyCSharp(@"
public class C13
{
    public C13(int i = 0, string s = "") { }

    public static void Foo() { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForNonStaticClassWithPublicNonDefaultConstructorWithOptionalParametersAndSharedMethodBasic()
        {
            VerifyBasic(@"
Public Class B13
    Public Sub New(Optional i as Integer = 0, Optional s as String = "")
    End Sub

    Public Shared Sub Foo()
    End Sub
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNestedPublicNonStaticClassWithPublicDefaultConstructorAndStaticMethodCSharp()
        {
            VerifyCSharp(@"
public class C14
{
    public void Moo() { }

    public class C14Inner
    {
        public C14Inner() { }
        public static void Foo() { }
    }
}
",
                CSharpResult(6, 18, "C14Inner"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNestedPublicNonStaticClassWithPublicDefaultConstructorAndSharedMethodBasic()
        {
            VerifyBasic(@"
Public Class B14
    Public Sub Moo()
    End Sub

    Public Class B14Inner
        Public Sub New()
        End Sub

        Public Shared Sub Foo()
        End Sub
    End Class
End Class
",
                BasicResult(6, 18, "B14Inner"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForEmptyStaticClassCSharp()
        {
            VerifyCSharp(@"
public static class C15
{
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithStaticConstructorCSharp()
        {
            VerifyCSharp(@"
public class C16
{
    static C16() { }
}
",
                CSharpResult(2, 14, "C16"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithStaticConstructorBasic()
        {
            VerifyBasic(@"
Public Class B16
    Shared Sub New()
    End Sub
End Class
",
                BasicResult(2, 14, "B16"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticForStaticClassWithStaticConstructorCSharp()
        {
            VerifyCSharp(@"
public static class C17
{
    static C17() { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithStaticConstructorAndInstanceConstructorCSharp()
        {
            VerifyCSharp(@"
public class C18
{
    public C18() { }
    static C18() { }
}
",
                CSharpResult(2, 14, "C18"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNonStaticClassWithStaticConstructorAndInstanceConstructorBasic()
        {
            VerifyBasic(@"
Public Class B18
    Sub New()
    End Sub

    Shared Sub New()
    End Sub
End Class
",
                BasicResult(2, 14, "B18"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNestedPublicClassInOtherwiseEmptyNonStaticClassCSharp()
        {
            VerifyCSharp(@"
public class C19
{
    public class C19Inner
    {
    }
}
",
                CSharpResult(2, 14, "C19"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052DiagnosticForNestedPublicClassInOtherwiseEmptyNonStaticClassBasic()
        {
            VerifyBasic(@"
Public Class B19
    Public Class B19Inner
    End Class
End Class
",
                BasicResult(2, 14, "B19"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticAnEnumCSharp()
        {
            VerifyCSharp(@"
public enum E20
{
    Unknown = 0
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA1052NoDiagnosticAnEnumBasic()
        {
            VerifyBasic(@"
Public Enum EB20
    Unknown = 0
End Enum
");
        }
    }
}
