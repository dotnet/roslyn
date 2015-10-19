' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class EventCollectorTests
        Inherits AbstractEventCollectorTests

#Region "Imports statements"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Add1()
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Imports System
</Code>

            Test(code, changedCode,
                 Add("System"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Add2()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Dim changedCode =
<Code>
Imports System.Linq
Imports System
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Add("System.Linq"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Add3()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Dim changedCode =
<Code>
Imports System.Linq
Imports System
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Add("System.Linq"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Add4()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Dim changedCode =
<Code>
Imports System.Linq
Imports System
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Add("System.Linq"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Add5()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Dim changedCode =
<Code>
Imports System.Linq, System
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Add("System.Linq"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Add6()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Dim changedCode =
<Code>
Imports System, System.Linq
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Add("System.Linq"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Add7()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Dim changedCode =
<Code>
Imports System, S = System.Linq
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Add("System.Linq"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Remove1()
            Dim code =
<Code>
Imports System
</Code>

            Dim changedCode =
<Code>
</Code>

            Test(code, changedCode,
                 Remove("System", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Remove2()
            Dim code =
<Code>
Imports System.Linq
Imports System
Imports System.Collections.Generic
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Remove("System.Linq", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Remove3()
            Dim code =
<Code>
Imports System
Imports System.Linq
Imports System.Collections.Generic
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Remove("System.Linq", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Remove4()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Collections.Generic
</Code>

            Test(code, changedCode,
                 Remove("System.Linq", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Remove5()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Class C
End Class
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Collections.Generic
End Class
</Code>

            Test(code, changedCode,
                 Remove("System.Linq", Nothing),
                 Remove("C", Nothing))
        End Sub
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Remove6()
            Dim code =
<Code>
Imports System
Imports System.Collections.Generic
Imports &lt;xmlns:aw="http://www.adventure-works.com"&gt;
Class C
End Class
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Collections.Generic
Class C
End Class
</Code>

            ' Note: XML namespaces aren't supported by VB code model, so there should be no events when they're removed.
            Test(code, changedCode)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestImportsStatement_Rename1()
            Dim code =
<Code>
Imports System
</Code>

            Dim changedCode =
<Code>
Imports System.Linq
</Code>

            Test(code, changedCode,
                 Rename("System.Linq"))
        End Sub

#End Region

#Region "Option statements"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Add1()
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Option Strict On
</Code>

            Test(code, changedCode,
                 Add("Option Strict On"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Add2()
            Dim code =
<Code>
Option Strict On
Option Infer On
</Code>

            Dim changedCode =
<Code>
Option Explicit On
Option Strict On
Option Infer On
</Code>

            Test(code, changedCode,
                 Add("Option Explicit On"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Add3()
            Dim code =
<Code>
Option Strict On
Option Infer On
</Code>

            Dim changedCode =
<Code>
Option Strict On
Option Explicit On
Option Infer On
</Code>

            Test(code, changedCode,
                 Add("Option Explicit On"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Add4()
            Dim code =
<Code>
Option Strict On
Option Infer On
</Code>

            Dim changedCode =
<Code>
Option Strict On
Option Infer On
Option Explicit On
</Code>

            Test(code, changedCode,
                 Add("Option Explicit On"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Remove1()
            Dim code =
<Code>
Option Strict On
</Code>

            Dim changedCode =
<Code>
</Code>

            Test(code, changedCode,
                 Remove("Option Strict On", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Remove2()
            Dim code =
<Code>
Option Explicit On
Option Strict On
Option Infer On
</Code>

            Dim changedCode =
<Code>
Option Strict On
Option Infer On
</Code>

            Test(code, changedCode,
                 Remove("Option Explicit On", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Remove3()
            Dim code =
<Code>
Option Strict On
Option Explicit On
Option Infer On
</Code>

            Dim changedCode =
<Code>
Option Strict On
Option Infer On
</Code>

            Test(code, changedCode,
                 Remove("Option Explicit On", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Remove4()
            Dim code =
<Code>
Option Strict On
Option Infer On
Option Explicit On
</Code>

            Dim changedCode =
<Code>
Option Strict On
Option Infer On
</Code>

            Test(code, changedCode,
                 Remove("Option Explicit On", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Rename1()
            Dim code =
<Code>
Option Strict On
</Code>

            Dim changedCode =
<Code>
Option Strict Off
</Code>

            Test(code, changedCode,
                 Rename("Option Strict Off"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Rename2()
            Dim code =
<Code>
Option Strict On
</Code>

            Dim changedCode =
<Code>
Option Explicit On
</Code>

            Test(code, changedCode,
                 Rename("Option Explicit On"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestOptionsStatement_Rename3()
            ' Note: This represents a change from the legacy VB code model where
            ' the following test would result in Remove event being fired for "Option Strict On"
            ' rather than a resolve. However, this should be more expected in the Roslyn code model
            ' because it doesn't fire on commit as it did in the legacy code model.

            Dim code =
<Code>
Option Strict On
</Code>

            Dim changedCode =
<Code>
Option Strict Foo
</Code>

            Test(code, changedCode,
                 Rename("Option Strict Foo"))
        End Sub

#End Region

#Region "File-level attributes"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_Add1()
            Dim code =
<Code>
Imports System
</Code>

            Dim changedCode =
<Code>
Imports System
&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Test(code, changedCode,
                 Add("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_Add2()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: AssemblyTitle("")&gt;
&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Test(code, changedCode,
                 Add("AssemblyTitle"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_Add3()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
&lt;Assembly: AssemblyTitle("")&gt;
</Code>

            Test(code, changedCode,
                 Add("AssemblyTitle"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_Add4()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: AssemblyTitle(""), Assembly: CLSCompliant(True)&gt;
</Code>

            Test(code, changedCode,
                 Add("AssemblyTitle"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_Add5()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True), Assembly: AssemblyTitle("")&gt;
</Code>

            Test(code, changedCode,
                 Add("AssemblyTitle"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_ChangeSpecifier1()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Module: CLSCompliant(True)&gt;
</Code>

            Test(code, changedCode,
                 Unknown("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_AddArgument1()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Test(code, changedCode,
                 Add("", "CLSCompliant"),
                 ArgChange("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_RemoveArgument1()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant&gt;
</Code>

            Test(code, changedCode,
                 Remove("", "CLSCompliant"),
                 ArgChange("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_OmitArgument1()
            Dim code =
<Code>
&lt;Foo("hello", Baz:=True)&gt;
Class FooAttribute
    Inherits Attribute

    Sub New(Optional bar As String = Nothing)

    End Sub

    Public Property Baz As Boolean
        Get

        End Get
        Set(value As Boolean)

        End Set
    End Property
End Class
</Code>

            Dim changedCode =
<Code>
&lt;Foo(, Baz:=True)&gt;
Class FooAttribute
    Inherits Attribute

    Sub New(Optional bar As String = Nothing)

    End Sub

    Public Property Baz As Boolean
        Get

        End Get
        Set(value As Boolean)

        End Set
    End Property
End Class
</Code>

            Test(code, changedCode,
                 Change(CodeModelEventType.Rename Or CodeModelEventType.Unknown, ""),
                 ArgChange("Foo"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_RenameArgument1()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(IsCompliant:=True)&gt;
</Code>

            Test(code, changedCode,
                 Rename("IsCompliant"),
                 ArgChange("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_ChangeArgument1()
            Dim code =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Dim changedCode =
<Code>
Imports System
Imports System.Reflection

&lt;Assembly: CLSCompliant(False)&gt;
</Code>

            Test(code, changedCode,
                 Unknown(""),
                 ArgChange("CLSCompliant"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestFileLevelAttribute_ChangeArgument2()
            Dim code =
<Code>
&lt;Assembly: Foo("")&gt;
</Code>

            Dim changedCode =
<Code>
&lt;Assembly: Foo(0)&gt;
</Code>

            Test(code, changedCode,
                 Unknown(""),
                 ArgChange("Foo"))
        End Sub

#End Region

#Region "Namespaces"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestNamespace_Add1()
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Namespace N
</Code>

            Test(code, changedCode,
                 Add("N"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestNamespace_Add2()
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Namespace N
End Namespace
</Code>

            Test(code, changedCode,
                 Add("N"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestNamespace_Add3()
            Dim code =
<Code>
Namespace N1
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N1
    Namespace N2
End Namespace
</Code>

            Test(code, changedCode,
                 Add("N2", "N1"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestNamespace_Remove1()
            Dim code =
<Code>
Namespace N
</Code>

            Dim changedCode =
<Code>
</Code>

            Test(code, changedCode,
                 Remove("N", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestNamespace_Remove2()
            Dim code =
<Code>
Namespace N
End Namespace
</Code>

            Dim changedCode =
<Code>
</Code>

            Test(code, changedCode,
                 Remove("N", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestNamespace_Remove3()
            Dim code =
<Code>
Namespace N1
    Namespace N2
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N1
End Namespace
</Code>

            Test(code, changedCode,
                 Remove("N2", "N1"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestNamespace_Rename1()
            Dim code =
<Code>
Namespace N1
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N2
End Namespace
</Code>

            Test(code, changedCode,
                 Rename("N2"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestNamespace_Rename2()
            Dim code =
<Code>
Namespace N1
    Namespace N2
    End Namespace
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N1
    Namespace N3
    End Namespace
End Namespace
</Code>

            Test(code, changedCode,
                 Rename("N3"))
        End Sub

#End Region

#Region "Classes"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Add1()
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Class C
End Class
</Code>

            Test(code, changedCode,
                 Add("C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Add2()
            Dim code =
<Code>
Namespace N
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Class C
    End Class
End Namespace
</Code>

            Test(code, changedCode,
                 Add("C", "N"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Add3()
            Dim code =
<Code>
Namespace N
    Class B
    End Class
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Class B
        Class C
        End Class
    End Class
End Namespace
</Code>

            Test(code, changedCode,
                 Add("C", "B"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Remove1()
            Dim code =
<Code>
Class C
End Class
</Code>

            Dim changedCode =
<Code>
</Code>

            Test(code, changedCode,
                 Remove("C", Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Remove2()
            Dim code =
<Code>
Namespace N
    Class C
    End Class
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
End Namespace
</Code>

            Test(code, changedCode,
                 Remove("C", "N"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Remove3()
            Dim code =
<Code>
Namespace N
    Class B
        Class C
        End Class
    End Class
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Class B
    End Class
End Namespace
</Code>

            Test(code, changedCode,
                 Remove("C", "B"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_ReplaceWithTwoClasses1()
            Dim code =
<Code>
Class D
End Class
</Code>

            Dim changedCode =
<Code>
Class B
End Class

Class C
End Class
</Code>

            Test(code, changedCode,
                 Unknown(Nothing))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_ReplaceWithTwoClasses2()
            Dim code =
<Code>
Namespace N
    Class D
    End Class
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Class B
    End Class

    Class C
    End Class
End Namespace
</Code>

            Test(code, changedCode,
                 Unknown("N"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_ChangeBaseList()
            Dim code =
<Code>
Namespace N
    Class B
    End Class

    Class C
    End Class
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Class B
    End Class

    Class C
        Inherits B
    End Class
End Namespace
</Code>

            Test(code, changedCode,
                 Add("Inherits", "C"),
                 BaseChange("C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Rename1()
            Dim code =
<Code>
Class C
End Class
</Code>

            Dim changedCode =
<Code>
Class D
End Class
</Code>

            Test(code, changedCode,
                 Rename("D"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_Rename2()
            Dim code =
<Code>
Namespace N
    Class C
    End Class
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Class D
    End Class
End Namespace
</Code>

            Test(code, changedCode,
                 Rename("D"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestClass_AddBaseClass()
            Dim code =
<Code>
Class attr
End Class
</Code>

            Dim changedCode =
<Code>
Class attr : Inherits Attribute
End Class
</Code>

            Test(code, changedCode,
                 Add("Inherits", "attr"),
                 BaseChange("attr"))
        End Sub

#End Region

#Region "Enums"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestEnum_Add1()
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Enum Foo
End Enum
</Code>

            Test(code, changedCode,
                 Add("Foo"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestEnum_Rename1()
            Dim code =
<Code>
Enum Foo
End Enum
</Code>

            Dim changedCode =
<Code>
Enum Bar
End Enum
</Code>

            Test(code, changedCode,
                 Rename("Bar"))
        End Sub

#End Region

#Region "Fields"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Add1()
            Dim code =
<Code>
Class C
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Test(code, changedCode,
                 Add("i", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Add2()
            Dim code =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim i As Integer
    Dim j As Integer
End Class
</Code>

            Test(code, changedCode,
                 Add("j", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Add3()
            Dim code =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim i, j As Integer
End Class
</Code>

            Test(code, changedCode,
                 Add("j", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Add4()
            Dim code =
<Code>
Class C
    Dim i, k As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim i, j, k As Integer
End Class
</Code>

            Test(code, changedCode,
                 Add("j", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Add5()
            Dim code =
<Code>
Class C
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim i, j As Integer
End Class
</Code>

            Test(code, changedCode,
                 Add("i", "C"),
                 Add("j", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Remove1()
            Dim code =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
End Class
</Code>

            Test(code, changedCode,
                 Remove("i", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Remove2()
            Dim code =
<Code>
Class C
    Dim i As Integer
    Dim j As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Test(code, changedCode,
                 Remove("j", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Remove3()
            Dim code =
<Code>
Class C
    Dim i, j As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Test(code, changedCode,
                 Remove("j", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Remove4()
            Dim code =
<Code>
Class C
    Dim i, j, k As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim i, k As Integer
End Class
</Code>

            Test(code, changedCode,
                 Remove("j", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_Remove5()
            Dim code =
<Code>
Class C
    Dim i, j As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
End Class
</Code>

            Test(code, changedCode,
                 Remove("i", "C"),
                 Remove("j", "C"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_AddAttributeToField()
            Dim code =
<Code>
Class C
    Dim foo As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim foo As Integer
End Class
</Code>

            Test(code, changedCode,
                 Add("System.CLSCompliant", "foo"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_AddAttributeToTwoFields()
            Dim code =
<Code>
Class C
    Dim foo, bar As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim foo, bar As Integer
End Class
</Code>

            Test(code, changedCode,
                 Add("System.CLSCompliant", "foo"),
                 Add("System.CLSCompliant", "bar"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_RemoveAttributeFromField()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim foo As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim foo As Integer
End Class
</Code>

            Test(code, changedCode,
                 Remove("System.CLSCompliant", "foo"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_RemoveAttributeFromTwoFields()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim foo, bar As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Dim foo, bar As Integer
End Class
</Code>

            Test(code, changedCode,
                 Remove("System.CLSCompliant", "foo"),
                 Remove("System.CLSCompliant", "bar"))
        End Sub

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_ChangeAttributeOnField()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim foo As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    &lt;System.CLSCompliant(False)&gt;
    Dim foo As Integer
End Class
</Code>

            ' Unknown event fires for attribute argument and attribute ArgChange event fires for each field

            Test(code, changedCode,
                 Unknown(""),
                 ArgChange("System.CLSCompliant"))
        End Sub

        <WorkItem(1147865)>
        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_ChangeAttributeOnTwoFields()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(False)&gt;
    Dim foo, bar As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim foo, bar As Integer
End Class
</Code>

            ' Unknown event fires for attribute argument and attribute ArgChange event fires for each field

            Test(code, changedCode,
                 Unknown(""),
                 ArgChange("System.CLSCompliant", "foo"),
                 ArgChange("System.CLSCompliant", "bar"))
        End Sub

        <WorkItem(1147865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_AddOneMoreAttribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(False)&gt;
    Dim foo, bar As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    &lt;System.CLSCompliant(False), System.NonSerialized()&gt;
    Dim foo, bar As Integer
End Class
</Code>
            Test(code, changedCode,
                 Add("System.NonSerialized", "foo"),
                 Add("System.NonSerialized", "bar"))
        End Sub

        <WorkItem(1147865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestField_RemoveOneAttribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(False), System.NonSerialized()&gt;
    Dim foo, bar As Integer
End Class
</Code>
            Dim changedCode =
<Code>
Class C
    &lt;System.CLSCompliant(False)&gt;
    Dim foo, bar As Integer
End Class
</Code>
            Test(code, changedCode,
                 Remove("System.NonSerialized", "foo"),
                 Remove("System.NonSerialized", "bar"))
        End Sub

#End Region

#Region "Methods"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_Add1()
            Dim code =
<Code>
Class C
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Sub M()
    End Sub
End Class
</Code>

            Test(code, changedCode,
                 Add("M", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_Remove1()
            Dim code =
<Code>
Class C
    Sub M()
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
End Class
</Code>

            Test(code, changedCode,
                 Remove("M", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_RemoveOperator1()
            Dim code =
<Code>
Class C
    Shared Operator *(i As Integer, c As C) As C
    End Operator
End Class
</Code>

            Dim changedCode =
<Code>
Class C
End Class
</Code>

            Test(code, changedCode,
                 Remove("*", "C"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_ChangeType1()
            Dim code =
<Code>
Class C
    Sub M()
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Function M() As Integer
    End Function
End Class
</Code>

            Test(code, changedCode,
                 TypeRefChange("M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestMethod_ChangeType2()
            Dim code =
<Code>
Class C
    Sub M()
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Function M() As Integer
    End Function
End Class
</Code>

            Test(code, changedCode,
                 TypeRefChange("M"))
        End Sub

#End Region

#Region "Parameters"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestParameter_Add1()
            Dim code =
<Code>
Class C
    Sub M()
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Sub M(i As Integer)
    End Sub
End Class
</Code>

            Test(code, changedCode,
                 Add("i", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestParameter_Add2()
            Dim code =
<Code>
Class C
    Sub M(i As Integer)
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Sub M(i As Integer, j As Integer)
    End Sub
End Class
</Code>

            Test(code, changedCode,
                 Add("j", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestParameter_Remove1()
            Dim code =
<Code>
Class C
    Sub M(i As Integer)
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Sub M()
    End Sub
End Class
</Code>

            Test(code, changedCode,
                 Remove("i", "M"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestParameter_ChangeModifier1()
            Dim code =
<Code>
Class C
    Sub M(i As Integer)
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Sub M(ByRef i As Integer)
    End Sub
End Class
</Code>

            Test(code, changedCode,
                 Unknown("i"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestParameter_ChangeModifier2()
            Dim code =
<Code>
Class C
    Sub M(ByVal i As Integer)
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Sub M(ByRef i As Integer)
    End Sub
End Class
</Code>

            Test(code, changedCode,
                 Unknown("i"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestParameter_ChangeTypeToTypeCharacter()
            Dim code =
<Code>
Class C
    Sub M(b As Boolean)
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Sub M(b%)
    End Sub
End Class
</Code>

            Test(code, changedCode,
                 TypeRefChange("b"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub TestParameter_ChangeTypeFromTypeCharacter()
            Dim code =
<Code>
Class C
    Sub M(b%)
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class C
    Sub M(b As Boolean)
    End Sub
End Class
</Code>

            Test(code, changedCode,
                 TypeRefChange("b"))
        End Sub

#End Region

#Region "Attribute Arguments"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub Attribute_AddArgument1()
            Dim code =
<Code>
Imports System

&lt;$$AttributeUsage()&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Dim changedCode =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Test(code, changedCode,
                 Add("", "AttributeUsage"),
                 ArgChange("AttributeUsage"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub Attribute_AddArgument2()
            Dim code =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Dim changedCode =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All, AllowMultiple:=False)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Test(code, changedCode,
                 Add("AllowMultiple", "AttributeUsage"),
                 ArgChange("AttributeUsage"))
        End Sub

#End Region

#Region "Other"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub RenameInterfaceMethod()
            Dim code =
<Code>
Interface IFoo
    Sub Foo()
    Function Bar() As Integer
End Interface

Class C
    Implements IFoo

    Public Sub Foo() Implements IFoo.Foo
        Throw New NotImplementedException()
    End Sub

    Public Function Bar() As Integer Implements IFoo.Bar
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Dim changedCode =
<Code>
Interface IFoo
    Sub Foo()
    Function Baz() As Integer
End Interface

Class C
    Implements IFoo

    Public Sub Foo() Implements IFoo.Foo
        Throw New NotImplementedException()
    End Sub

    Public Function Bar() As Integer Implements IFoo.Baz
        Throw New NotImplementedException()
    End Function
End Class
</Code>

            Test(code, changedCode,
                 Rename("Baz"))
        End Sub

        <WorkItem(575666)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub DontFireEventsForGarbage1()
            Dim code =
<Code>
Class C

End Class
</Code>

            Dim changedCode =
<Code>
Class C

AddHandler button1.Click, Async Sub(sender, e)
                              textBox1.Clear()
                              ' SumPageSizesAsync is a method that returns a Task.
                              Await SumPageSizesAsync()
                              textBox1.Text = vbCrLf &amp; "Control returned to button1_Click."
                          End Sub
End Class
</Code>

            Test(code, changedCode)
        End Sub

        <WorkItem(578249)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub DontFireEventsForGarbage2()
            Dim code =
<Code>
Partial Class SomeClass
    Partial Private Sub Foo()

    End Sub

    Private Sub Foo()

    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Partial Class SomeClass
    Partial Private Sub Foo()

    End Sub

    Private Sub Foo()

    End Sub
End Class

Partial C
</Code>

            Test(code, changedCode)
        End Sub

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub ComparePropertyStatementBeforeMethodBase()
            Dim code =
<Code>
Public MustInherit Class C1
    Public Property PropertyA() As Integer
        Get
            Return 1
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property
    Public Property PropertyB() As Integer
    Public MustOverride Property PropertyC() As Integer
End Class
</Code>

            Dim changedCode =
<Code>
Public MustInherit Class C1

    Public MustOverride Property PropertyC() As Integer

    Public Property PropertyB() As Integer

    Public Property PropertyA() As Integer
        Get
            Return 1
        End Get
        Set(ByVal value As Integer)
        End Set
    End Property

End Class
</Code>
            Test(code, changedCode, Unknown("C1"))
        End Sub

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub CompareEventStatementBeforeMethodBase()
            Dim code =
<Code>
Class Program
    Public Custom Event ServerChange As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event

    Public Event cust As EventHandler
End Class
</Code>

            Dim changedCode =
<Code>
Class Program
    Public Event cust As EventHandler

    Public Custom Event ServerChange As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>
            Test(code, changedCode, Unknown("Program"))
        End Sub

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub CompareEventStatementBeforeMethodBase_WithMethods_1()
            Dim code =
<Code>
Class Program
    Public Custom Event ServerChange As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event

    Public Sub Met()
    End Sub

    Public Event cust As EventHandler
End Class
</Code>

            Dim changedCode =
<Code>
Class Program
    Public Sub Met()
    End Sub
    Public Event cust As EventHandler

    Public Custom Event ServerChange As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>
            Test(code, changedCode, Unknown("Program"))
        End Sub

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub CompareEventStatementBeforeMethodBase_WithMethods_2()
            Dim code =
<Code>
Class Program
    Public Custom Event ServerChange As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event

    Public Event cust As EventHandler
    Public Sub Met()
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class Program
    Public Event cust As EventHandler

    Public Sub Met()
    End Sub

    Public Custom Event ServerChange As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>
            Test(code, changedCode, Unknown("Program"))
        End Sub

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub CompareEventStatementBeforeMethodBase_WithMethods_3()
            Dim code =
<Code>
Class Program
    Public Custom Event ServerChange As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event

    Public Event cust As EventHandler
    Public Sub Met()
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class Program
    Public Sub Met()
    End Sub

    Public Event cust As EventHandler

    Public Custom Event ServerChange As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As System.EventArgs)
        End RaiseEvent
    End Event
End Class
</Code>
            Test(code, changedCode, Unknown("Program"))
        End Sub

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Sub CompareMethodsOnly()
            Dim code =
<Code>
Class Program
    Public Sub Met()
    End Sub

    Public Sub Met1()
    End Sub
End Class
</Code>

            Dim changedCode =
<Code>
Class Program
    Public Sub Met1()
    End Sub
    Public Sub Met()
    End Sub
End Class
</Code>
            Test(code, changedCode, Unknown("Program"))
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
