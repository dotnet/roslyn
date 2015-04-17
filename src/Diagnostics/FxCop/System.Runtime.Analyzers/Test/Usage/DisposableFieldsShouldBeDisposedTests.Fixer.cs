// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Xunit;

namespace System.Runtime.Analyzers.UnitTests
{
    public partial class DisposableFieldsShouldBeDisposedFixerTests : CodeFixTestBase
    {
        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new BasicCA2213DiagnosticAnalyzer();
        }

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new DisposableFieldsShouldBeDisposedFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CSharpCA2213DiagnosticAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new DisposableFieldsShouldBeDisposedFixer();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpCodeFixDisposableFiledShouldBeDisposed()
        {
            VerifyCSharpFix(@"
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
@"
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
        public void CA2213CSharpCodeFixDisposeMethodHasExplicitName()
        {
            VerifyCSharpFix(@"
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

    void IDisposable.Dispose()
    {
    }
}
",
@"
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

    void IDisposable.Dispose()
    {
        a.Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpCodeFixDisposeMethodCallsExplicitImplementation()
        {
            VerifyCSharpFix(@"
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

    void IDisposable.Dispose()
    {
    }
}
",
@"
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

    void IDisposable.Dispose()
    {
        ((IDisposable)a).Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpCodeFixDisposeMethodHasConflictNames()
        {
            VerifyCSharpFix(@"
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

    void IDisposable.Dispose()
    {
        bool a = true;
    }
}
",
@"
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

    void IDisposable.Dispose()
    {
        bool a = true;
        this.a.Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213CSharpCodeFixDisposeMethodHasConflictStaticNames()
        {
            VerifyCSharpFix(@"
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

    void IDisposable.Dispose()
    {
        bool a = true;
    }
}
",
@"
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

    void IDisposable.Dispose()
    {
        bool a = true;
        B.a.Dispose();
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicCodeFixDisposableFiledShouldBeDisposed()
        {
            VerifyBasicFix(@"
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
@"
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
End Class
",
codeFixIndex: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicCodeFixDisposableFiledHasConflictName()
        {
            VerifyBasicFix(@"
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
        Dim a = True
    End Sub 'Dispose
End Class
",
@"
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
        Dim a = True
        Me.a.Dispose()
    End Sub 'Dispose
End Class
",
codeFixIndex: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicCodeFixDisposableFiledHasConflictStaticName()
        {
            VerifyBasicFix(@"
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

    Shared Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        Dim a = True
    End Sub 'Dispose
End Class
",
@"
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

    Shared Dim a As A = New A()

    Public Overloads Sub Dispose() Implements IDisposable.Dispose
        Dim a = True
        B.a.Dispose()
    End Sub 'Dispose
End Class
",
codeFixIndex: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicCodeFixDisposedMethodHasCustomName()
        {
            VerifyBasicFix(@"
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

    Sub MyDispose() Implements IDisposable.Dispose

    End Sub
End Class
",
@"
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

    Sub MyDispose() Implements IDisposable.Dispose
        a.Dispose()
    End Sub
End Class
",
codeFixIndex: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicCodeFixDisposableFieldHasCustomName()
        {
            VerifyBasicFix(@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Sub Dispose() Implements IDisposable.Dispose

    End Sub
End Class
",
@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Sub Dispose() Implements IDisposable.Dispose
        a.MyDispose()
    End Sub
End Class
",
codeFixIndex: 0);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void CA2213BasicCodeFixDisposedMethodAndDisposableFieldHaveCustomNames()
        {
            VerifyBasicFix(@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Sub MyDispose() Implements IDisposable.Dispose

    End Sub
End Class
",
@"
Imports System
Imports System.IO

' This class violates the rule. 
Public Class A
    Implements IDisposable

    Public Overloads Sub MyDispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class B
    Implements IDisposable

    Dim a As A = New A()

    Sub MyDispose() Implements IDisposable.Dispose
        a.MyDispose()
    End Sub
End Class
",
codeFixIndex: 0);
        }
    }
}
