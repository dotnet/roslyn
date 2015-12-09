' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class EventCollectorTests
        Inherits AbstractEventCollectorTests

#Region "Imports statements"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Add1() As Task
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Imports System
</Code>

            Await TestAsync(code, changedCode,
                 Add("System"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Add2() As Task
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

            Await TestAsync(code, changedCode,
                 Add("System.Linq"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Add3() As Task
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

            Await TestAsync(code, changedCode,
                 Add("System.Linq"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Add4() As Task
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

            Await TestAsync(code, changedCode,
                 Add("System.Linq"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Add5() As Task
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

            Await TestAsync(code, changedCode,
                 Add("System.Linq"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Add6() As Task
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

            Await TestAsync(code, changedCode,
                 Add("System.Linq"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Add7() As Task
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

            Await TestAsync(code, changedCode,
                 Add("System.Linq"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Remove1() As Task
            Dim code =
<Code>
Imports System
</Code>

            Dim changedCode =
<Code>
</Code>

            Await TestAsync(code, changedCode,
                 Remove("System", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Remove2() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("System.Linq", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Remove3() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("System.Linq", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Remove4() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("System.Linq", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Remove5() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("System.Linq", Nothing),
                 Remove("C", Nothing))
        End Function
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Remove6() As Task
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
            Await TestAsync(code, changedCode)
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestImportsStatement_Rename1() As Task
            Dim code =
<Code>
Imports System
</Code>

            Dim changedCode =
<Code>
Imports System.Linq
</Code>

            Await TestAsync(code, changedCode,
                 Rename("System.Linq"))
        End Function

#End Region

#Region "Option statements"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Add1() As Task
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Option Strict On
</Code>

            Await TestAsync(code, changedCode,
                 Add("Option Strict On"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Add2() As Task
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

            Await TestAsync(code, changedCode,
                 Add("Option Explicit On"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Add3() As Task
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

            Await TestAsync(code, changedCode,
                 Add("Option Explicit On"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Add4() As Task
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

            Await TestAsync(code, changedCode,
                 Add("Option Explicit On"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Remove1() As Task
            Dim code =
<Code>
Option Strict On
</Code>

            Dim changedCode =
<Code>
</Code>

            Await TestAsync(code, changedCode,
                 Remove("Option Strict On", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Remove2() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("Option Explicit On", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Remove3() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("Option Explicit On", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Remove4() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("Option Explicit On", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Rename1() As Task
            Dim code =
<Code>
Option Strict On
</Code>

            Dim changedCode =
<Code>
Option Strict Off
</Code>

            Await TestAsync(code, changedCode,
                 Rename("Option Strict Off"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Rename2() As Task
            Dim code =
<Code>
Option Strict On
</Code>

            Dim changedCode =
<Code>
Option Explicit On
</Code>

            Await TestAsync(code, changedCode,
                 Rename("Option Explicit On"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestOptionsStatement_Rename3() As Task
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

            Await TestAsync(code, changedCode,
                 Rename("Option Strict Foo"))
        End Function

#End Region

#Region "File-level attributes"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_Add1() As Task
            Dim code =
<Code>
Imports System
</Code>

            Dim changedCode =
<Code>
Imports System
&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            Await TestAsync(code, changedCode,
                 Add("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_Add2() As Task
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

            Await TestAsync(code, changedCode,
                 Add("AssemblyTitle"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_Add3() As Task
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

            Await TestAsync(code, changedCode,
                 Add("AssemblyTitle"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_Add4() As Task
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

            Await TestAsync(code, changedCode,
                 Add("AssemblyTitle"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_Add5() As Task
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

            Await TestAsync(code, changedCode,
                 Add("AssemblyTitle"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_ChangeSpecifier1() As Task
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

            Await TestAsync(code, changedCode,
                 Unknown("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_AddArgument1() As Task
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

            Await TestAsync(code, changedCode,
                 Add("", "CLSCompliant"),
                 ArgChange("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_RemoveArgument1() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("", "CLSCompliant"),
                 ArgChange("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_OmitArgument1() As Task
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

            Await TestAsync(code, changedCode,
                 Change(CodeModelEventType.Rename Or CodeModelEventType.Unknown, ""),
                 ArgChange("Foo"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_RenameArgument1() As Task
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

            Await TestAsync(code, changedCode,
                 Rename("IsCompliant"),
                 ArgChange("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_ChangeArgument1() As Task
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

            Await TestAsync(code, changedCode,
                 Unknown(""),
                 ArgChange("CLSCompliant"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestFileLevelAttribute_ChangeArgument2() As Task
            Dim code =
<Code>
&lt;Assembly: Foo("")&gt;
</Code>

            Dim changedCode =
<Code>
&lt;Assembly: Foo(0)&gt;
</Code>

            Await TestAsync(code, changedCode,
                 Unknown(""),
                 ArgChange("Foo"))
        End Function

#End Region

#Region "Namespaces"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestNamespace_Add1() As Task
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Namespace N
</Code>

            Await TestAsync(code, changedCode,
                 Add("N"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestNamespace_Add2() As Task
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Namespace N
End Namespace
</Code>

            Await TestAsync(code, changedCode,
                 Add("N"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestNamespace_Add3() As Task
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

            Await TestAsync(code, changedCode,
                 Add("N2", "N1"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestNamespace_Remove1() As Task
            Dim code =
<Code>
Namespace N
</Code>

            Dim changedCode =
<Code>
</Code>

            Await TestAsync(code, changedCode,
                 Remove("N", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestNamespace_Remove2() As Task
            Dim code =
<Code>
Namespace N
End Namespace
</Code>

            Dim changedCode =
<Code>
</Code>

            Await TestAsync(code, changedCode,
                 Remove("N", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestNamespace_Remove3() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("N2", "N1"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestNamespace_Rename1() As Task
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

            Await TestAsync(code, changedCode,
                 Rename("N2"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestNamespace_Rename2() As Task
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

            Await TestAsync(code, changedCode,
                 Rename("N3"))
        End Function

#End Region

#Region "Classes"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Add1() As Task
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Class C
End Class
</Code>

            Await TestAsync(code, changedCode,
                 Add("C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Add2() As Task
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

            Await TestAsync(code, changedCode,
                 Add("C", "N"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Add3() As Task
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

            Await TestAsync(code, changedCode,
                 Add("C", "B"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Remove1() As Task
            Dim code =
<Code>
Class C
End Class
</Code>

            Dim changedCode =
<Code>
</Code>

            Await TestAsync(code, changedCode,
                 Remove("C", Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Remove2() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("C", "N"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Remove3() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("C", "B"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_ReplaceWithTwoClasses1() As Task
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

            Await TestAsync(code, changedCode,
                 Unknown(Nothing))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_ReplaceWithTwoClasses2() As Task
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

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_ChangeBaseList() As Task
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

            Await TestAsync(code, changedCode,
                 Add("Inherits", "C"),
                 BaseChange("C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Rename1() As Task
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

            Await TestAsync(code, changedCode,
                 Rename("D"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_Rename2() As Task
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

            Await TestAsync(code, changedCode,
                 Rename("D"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestClass_AddBaseClass() As Task
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

            Await TestAsync(code, changedCode,
                 Add("Inherits", "attr"),
                 BaseChange("attr"))
        End Function

#End Region

#Region "Enums"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestEnum_Add1() As Task
            Dim code =
<Code>
</Code>

            Dim changedCode =
<Code>
Enum Foo
End Enum
</Code>

            Await TestAsync(code, changedCode,
                 Add("Foo"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestEnum_Rename1() As Task
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

            Await TestAsync(code, changedCode,
                 Rename("Bar"))
        End Function

#End Region

#Region "Fields"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Add1() As Task
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

            Await TestAsync(code, changedCode,
                 Add("i", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Add2() As Task
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

            Await TestAsync(code, changedCode,
                 Add("j", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Add3() As Task
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

            Await TestAsync(code, changedCode,
                 Add("j", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Add4() As Task
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

            Await TestAsync(code, changedCode,
                 Add("j", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Add5() As Task
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

            Await TestAsync(code, changedCode,
                 Add("i", "C"),
                 Add("j", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Remove1() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("i", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Remove2() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("j", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Remove3() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("j", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Remove4() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("j", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_Remove5() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("i", "C"),
                 Remove("j", "C"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddAttributeToField() As Task
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

            Await TestAsync(code, changedCode,
                 Add("System.CLSCompliant", "foo"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddAttributeToTwoFields() As Task
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

            Await TestAsync(code, changedCode,
                 Add("System.CLSCompliant", "foo"),
                 Add("System.CLSCompliant", "bar"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_RemoveAttributeFromField() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("System.CLSCompliant", "foo"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_RemoveAttributeFromTwoFields() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("System.CLSCompliant", "foo"),
                 Remove("System.CLSCompliant", "bar"))
        End Function

        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_ChangeAttributeOnField() As Task
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

            Await TestAsync(code, changedCode,
                 Unknown(""),
                 ArgChange("System.CLSCompliant"))
        End Function

        <WorkItem(1147865)>
        <WorkItem(844611)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_ChangeAttributeOnTwoFields() As Task
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

            Await TestAsync(code, changedCode,
                 Unknown(""),
                 ArgChange("System.CLSCompliant", "foo"),
                 ArgChange("System.CLSCompliant", "bar"))
        End Function

        <WorkItem(1147865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_AddOneMoreAttribute() As Task
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
            Await TestAsync(code, changedCode,
                 Add("System.NonSerialized", "foo"),
                 Add("System.NonSerialized", "bar"))
        End Function

        <WorkItem(1147865)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestField_RemoveOneAttribute() As Task
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
            Await TestAsync(code, changedCode,
                 Remove("System.NonSerialized", "foo"),
                 Remove("System.NonSerialized", "bar"))
        End Function

#End Region

#Region "Methods"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_Add1() As Task
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

            Await TestAsync(code, changedCode,
                 Add("M", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_Remove1() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("M", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_RemoveOperator1() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("*", "C"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_ChangeType1() As Task
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

            Await TestAsync(code, changedCode,
                 TypeRefChange("M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestMethod_ChangeType2() As Task
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

            Await TestAsync(code, changedCode,
                 TypeRefChange("M"))
        End Function

#End Region

#Region "Parameters"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestParameter_Add1() As Task
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

            Await TestAsync(code, changedCode,
                 Add("i", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestParameter_Add2() As Task
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

            Await TestAsync(code, changedCode,
                 Add("j", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestParameter_Remove1() As Task
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

            Await TestAsync(code, changedCode,
                 Remove("i", "M"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestParameter_ChangeModifier1() As Task
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

            Await TestAsync(code, changedCode,
                 Unknown("i"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestParameter_ChangeModifier2() As Task
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

            Await TestAsync(code, changedCode,
                 Unknown("i"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestParameter_ChangeTypeToTypeCharacter() As Task
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

            Await TestAsync(code, changedCode,
                 TypeRefChange("b"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestParameter_ChangeTypeFromTypeCharacter() As Task
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

            Await TestAsync(code, changedCode,
                 TypeRefChange("b"))
        End Function

#End Region

#Region "Attribute Arguments"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestAttribute_AddArgument1() As Task
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

            Await TestAsync(code, changedCode,
                 Add("", "AttributeUsage"),
                 ArgChange("AttributeUsage"))
        End Function

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestAttribute_AddArgument2() As Task
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

            Await TestAsync(code, changedCode,
                 Add("AllowMultiple", "AttributeUsage"),
                 ArgChange("AttributeUsage"))
        End Function

#End Region

#Region "Other"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestRenameInterfaceMethod() As Task
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

            Await TestAsync(code, changedCode,
                 Rename("Baz"))
        End Function

        <WorkItem(575666)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestDontFireEventsForGarbage1() As Task
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

            Await TestAsync(code, changedCode)
        End Function

        <WorkItem(578249)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestDontFireEventsForGarbage2() As Task
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

            Await TestAsync(code, changedCode)
        End Function

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestComparePropertyStatementBeforeMethodBase() As Task
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
            Await TestAsync(code, changedCode, Unknown("C1"))
        End Function

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestCompareEventStatementBeforeMethodBase() As Task
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
            Await TestAsync(code, changedCode, Unknown("Program"))
        End Function

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestCompareEventStatementBeforeMethodBase_WithMethods_1() As Task
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
            Await TestAsync(code, changedCode, Unknown("Program"))
        End Function

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestCompareEventStatementBeforeMethodBase_WithMethods_2() As Task
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
            Await TestAsync(code, changedCode, Unknown("Program"))
        End Function

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestCompareEventStatementBeforeMethodBase_WithMethods_3() As Task
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
            Await TestAsync(code, changedCode, Unknown("Program"))
        End Function

        <WorkItem(1101185)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function TestCompareMethodsOnly() As Task
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
            Await TestAsync(code, changedCode, Unknown("Program"))
        End Function

#End Region

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DontCrashOnDuplicatedMethodsInNamespace() As Task
            Dim code =
<Code>
Namespace N
    Sub M()
    End Sub
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Sub M()
    End Sub

    Sub M()
    End Sub
End Namespace
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DontCrashOnDuplicatedPropertiesInNamespace() As Task
            Dim code =
<Code>
Namespace N
    ReadOnly Property P As Integer = 42
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    ReadOnly Property P As Integer = 42
    ReadOnly Property P As Integer = 42
End Namespace
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DontCrashOnDuplicatedEventsInNamespace1() As Task
            Dim code =
<Code>
Namespace N
    Event E()
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Event E()
    Event E()
End Namespace
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        <WorkItem(150349)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModelEvents)>
        Public Async Function DontCrashOnDuplicatedEventsInNamespace2() As Task
            Dim code =
<Code>
Namespace N
    Custom Event E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler
        RemoveHandler(value As System.EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event
End Namespace
</Code>

            Dim changedCode =
<Code>
Namespace N
    Custom Event E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler
        RemoveHandler(value As System.EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event

    Custom Event E As System.EventHandler
        AddHandler(value As System.EventHandler)
        End AddHandler
        RemoveHandler(value As System.EventHandler)
        End RemoveHandler
        RaiseEvent(sender As Object, e As System.EventArgs)
        End RaiseEvent
    End Event
End Namespace
</Code>

            Await TestAsync(code, changedCode,
                 Unknown("N"))
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
