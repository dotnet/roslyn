// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public partial class DisposableFieldsShouldBeDisposedTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicCA2213DiagnosticAnalyzer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpCA2213DiagnosticAnalyzer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestDisposableFieldsShouldBeDisposed()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    A a = new A();

    public void Dispose()
    {
    }
}
",
            GetCA2213CSharpResultAt(13, 7));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestDisposableFieldsIsDisposed()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        a.Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestDisposableFieldsIsDisposedWithThis()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        this.a.Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestStaticDisposableFieldsIsDisposed()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    static A a = new A();

    public void Dispose()
    {
        B.a.Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestStaticDisposableFieldsIsDisposedAndParenthesized()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    static A a = new A();

    public void Dispose()
    {
        (B.a).Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestStaticDisposableFieldsWithLongPathsIsDisposed()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    public class C : IDisposable
    {
        static A a = new A();

        public void Dispose()
        {
            B.C.a.Dispose();
        }
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestInnerClassHasNotDisposedDisposableFields()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        a.Dispose();
    }

    public class Ba : IDisposable
    {
        A a = new A();

        public void Dispose()
        {
        }
    }
}
",
            GetCA2213CSharpResultAt(22, 11));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestExplicitInterfaceImplementation()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    void IDisposable.Dispose()
    {
    }
}

public class B : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        ((IDisposable)a).Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestExplicitInterfaceImplementationAs()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    void IDisposable.Dispose()
    {
    }
}

public class B : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        (a as IDisposable).Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestNonDisposableDispose()
        {
            VerifyCSharp(@"
using System;

public class A
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        a.Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpTestUsingDisposeDisposableFields()
        {
            VerifyCSharp(@"
using System;

public class A : IDisposable
{
    public void Dispose()
    {
    }
}

public class B : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        // B.a is disposed here
        using (a)
        {
        }
    }
}

public class C : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        A a = new A();
        // C.a is not disposed
        using (a)
        {
        }
    }
}

public class D : IDisposable
{
    A a = new A();

    public void Dispose()
    {
        // D.a is not disposed
        using (A a = new A())
        {
        }
    }
}
",
            GetCA2213CSharpResultAt(26, 7),
            GetCA2213CSharpResultAt(40, 7));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestDisposableFieldsIsNotDisposed()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub 'Dispose
End Class
",
            GetCA2213BasicResultAt(16, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestDisposableFieldsIsNotDisposedInMyDispose()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
    End Sub 'Dispose
End Class
",
            GetCA2213BasicResultAt(16, 9));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestDisposableFieldsIsDisposedInMyDispose()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestDisposableFieldsIsDisposedInMyDisposeWithMe()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
        Me.a.Dispose()
    End Sub 'Dispose
End Class

Public Class C
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
        Call Me.a.Dispose()
    End Sub 'Dispose
End Class

Public Class D
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
        Call (Me.a).Dispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestTest()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Shared Dim a As A = New A()

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
        Call (Me.a).Dispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestStaticDisposableFieldsIsDisposedInMyDispose()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Shared Dim a As A = New A()

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
        B.a.Dispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestDisposableFieldsWithMyDisposeMethodIsDisposedInMyDispose()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
        a.MyDispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestDisposableFieldsWithMyDisposeMethodIsDisposed()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        a.MyDispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestDisposableFieldsIsDisposed()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestExplicitInterfaceDispatchDirectCast()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose1() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        DirectCast(a, IDisposable).Dispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestExplicitInterfaceDispatchTryCast()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose1() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        TryCast(a, IDisposable).Dispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestExplicitInterfaceDispatchCType()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose1() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        CType(a, IDisposable).Dispose()
    End Sub 'Dispose
End Class
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestInnerClassHasNotDisposedDisposableFields()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub 'Dispose

    Public Class Ba
        Implements IDisposable

        Dim a As A = New A()

        Public Overloads Sub Dispose() Implements IDisposable.Dispose
        End Sub 'Dispose
    End Class
End Class
",
            GetCA2213BasicResultAt(25, 13));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestUsingDisposeDisposableFields()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        ' B.a is disposed here
        Using a

        End Using
    End Sub 'Dispose
End Class

Public Class C
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        ' C.a is not disposed here
        Using a As A = New A()

        End Using
    End Sub 'Dispose
End Class

Public Class D
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        ' D.a is not disposed here
        Dim a As A = New A()
        Using a

        End Using
    End Sub 'Dispose
End Class
",
            GetCA2213BasicResultAt(29, 9),
            GetCA2213BasicResultAt(42, 9));
        }

        [WorkItem(858613, "DevDiv")]
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicTestUsingDisposeDisposableFieldsWithScope()
        {
            VerifyBasic(@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        ' B.a is disposed here
        Using a

        End Using
    End Sub 'Dispose
End Class

Public Class C
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        ' C.a is not disposed here
        Using a As A = New A()

        End Using
    End Sub 'Dispose
End Class

[|Public Class D
    Implements IDisposable

    Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        ' D.a is not disposed here
        Dim a As A = New A()
        Using a

        End Using
    End Sub 'Dispose
End Class|]
",
            GetCA2213BasicResultAt(42, 9));
        }

        internal static string CA2213Name = "CA2213";
        internal static string CA2213Message = "Disposable fields should be disposed";

        private static DiagnosticResult GetCA2213CSharpResultAt(int line, int column)
        {
            return GetCSharpResultAt(line, column, CA2213Name, CA2213Message);
        }

        private static DiagnosticResult GetCA2213BasicResultAt(int line, int column)
        {
            return GetBasicResultAt(line, column, CA2213Name, CA2213Message);
        }
    }
}
