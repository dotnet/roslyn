' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.UseSystemHashCode

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseSystemHashCode
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseSystemHashCode)>
    Partial Public Class UseSystemHashCodeTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New UseSystemHashCodeDiagnosticAnalyzer(), New UseSystemHashCodeCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestDerivedClassWithFieldWithBase() As Task
            Await TestInRegularAndScriptAsync(
" namespace System
    public structure HashCode
    end structure
end namespace

class B
    public overrides function GetHashCode() as integer
        Return 0
    end function
end class

class C
    inherits B
    dim j as integer

    public overrides function $$GetHashCode() as integer
        dim hashCode = 339610899
        hashCode = hashCode * -1521134295 + MyBase.GetHashCode()
        hashCode = hashCode * -1521134295 + j.GetHashCode()
        Return hashCode
    end function
end class",
" namespace System
    public structure HashCode
    end structure
end namespace

class B
    public overrides function GetHashCode() as integer
        Return 0
    end function
end class

class C
    inherits B
    dim j as integer

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(MyBase.GetHashCode(), j)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestDerivedClassWithFieldWithNoBase() As Task
            Await TestInRegularAndScriptAsync(
" namespace System
    public structure HashCode
    end structure
end namespace

class B
    public overrides function GetHashCode() as integer
        Return 0
    end function
end class

class C
    inherits B
    dim j as integer

    public overrides function $$GetHashCode() as integer
        dim hashCode = 339610899
        hashCode = hashCode * -1521134295 + j.GetHashCode()
        Return hashCode
    end function
end class",
" namespace System
    public structure HashCode
    end structure
end namespace

class B
    public overrides function GetHashCode() as integer
        Return 0
    end function
end class

class C
    inherits B
    dim j as integer

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(j)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestDerivedClassWithNoFieldWithBase() As Task
            Await TestInRegularAndScriptAsync(
" namespace System
    public structure HashCode
    end structure
end namespace

class B
    public overrides function GetHashCode() as integer
        Return 0
    end function
end class

class C
    inherits B
    dim j as integer

    public overrides function $$GetHashCode() as integer
        dim hashCode = 339610899
        hashCode = hashCode * -1521134295 + MyBase.GetHashCode()
        Return hashCode
    end function
end class",
" namespace System
    public structure HashCode
    end structure
end namespace

class B
    public overrides function GetHashCode() as integer
        Return 0
    end function
end class

class C
    inherits B
    dim j as integer

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(MyBase.GetHashCode())
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestFieldAndProp() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = -538000506
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return hashCode
    end function
end class",
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(i, S)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestNotOnNonGetHashCode() As Task
            Await TestMissingAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode1() as integer
        dim hashCode = -538000506
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return hashCode
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestNotWithoutReturn() As Task
            Await TestMissingAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = -538000506
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestNotWithoutLocal() As Task
            Await TestMissingAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return hashCode
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestNotWithMultipleLocals() As Task
            Await TestMissingAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = -538000506, x
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return hashCode
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestNotWithoutInitializer() As Task
            Await TestMissingAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return hashCode
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestNotReturningAccumulator() As Task
            Await TestMissingAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = -538000506
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return 0
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestAcumulatorInitializedToField() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = i
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return hashCode
    end function
end class",
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(i, S)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestAcumulatorInitializedToHashedField() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = i.GetHashCode()
        hashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return hashCode
    end function
end class",
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(i, S)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMissingOnThisGetHashCode() As Task
            Await TestMissingAsync(
" namespace System
    public structure HashCode
    end structure
end namespace

class B
    public overrides function GetHashCode() as integer
        Return 0
    end function
end class

class C
    inherits B
    dim j as integer

    public overrides function $$GetHashCode() as integer
        dim hashCode = 339610899
        hashCode = hashCode * -1521134295 + me.GetHashCode()
        hashCode = hashCode * -1521134295 + j.GetHashCode()
        Return hashCode
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithNoSystemHashCode() As Task
            Await TestMissingAsync(
"
class B
    public overrides function GetHashCode() as integer
        Return 0
    end function
end class

class C
    inherits B
    dim j as integer

    public overrides function $$GetHashCode() as integer
        dim hashCode = 339610899
        hashCode = hashCode * -1521134295 + MyBase.GetHashCode()
        hashCode = hashCode * -1521134295 + j.GetHashCode()
        Return hashCode
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestDirectNullCheck1() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = -538000506
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + if(S isnot nothing, S.GetHashCode(), 0)
        Return hashCode
    end function
end class",
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(i, S)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestDirectNullCheck2() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = -538000506
        hashCode = hashCode * -1521134295 + i.GetHashCode()
        hashCode = hashCode * -1521134295 + if(S is nothing, 0, S.GetHashCode())
        Return hashCode
    end function
end class",
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(i, S)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestInt64Pattern() As Task
            Await TestInRegularAndScriptAsync(
" namespace System
    public structure HashCode
    end structure
end namespace

class C
    dim j as integer

    public overrides function $$GetHashCode() as integer
        dim hashCode as long = -468965076
        hashCode = (hashCode * -1521134295 + j.GetHashCode()).GetHashCode()
        Return hashCode
    end function
end class",
" namespace System
    public structure HashCode
    end structure
end namespace

class C
    dim j as integer

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(j)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestInt64Pattern2() As Task
            Await TestInRegularAndScriptAsync(
"namespace System
    public structure HashCode
    end structure
end namespace

class C
    dim j as integer

    public overrides function $$GetHashCode() as integer
        dim hashCode as long = -468965076
        hashCode = (hashCode * -1521134295 + j.GetHashCode()).GetHashCode()
        Return ctype(hashCode, integer)
    end function
end class",
"namespace System
    public structure HashCode
    end structure
end namespace

class C
    dim j as integer

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(j)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestTuple() As Task
            Await TestInRegularAndScriptAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
    public structure ValueTuple(Of T1, T2)
    end structure
end namespace

class C
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        Return (i, S).GetHashCode()
    end function
end class",
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
    public structure ValueTuple(Of T1, T2)
    end structure
end namespace

class C
    dim i as integer

    readonly property S as string

    public overrides function GetHashCode() as integer
        Return System.HashCode.Combine(i, S)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestAssignmentToImplicitMethodVariable() As Task
            Await TestMissingAsync(
"imports System.Collections.Generic
namespace System
    public structure HashCode
    end structure
end namespace

class C 
    dim i as integer

    readonly property S as string

    public overrides function $$GetHashCode() as integer
        dim hashCode = -538000506
        GetHashCode = hashCode * -1521134295 + i.GetHashCode()
        GetHashCode = hashCode * -1521134295 + EqualityComparer(of string).Default.GetHashCode(S)
        Return hashCode
    end function
end class")
        End Function
    End Class
End Namespace
