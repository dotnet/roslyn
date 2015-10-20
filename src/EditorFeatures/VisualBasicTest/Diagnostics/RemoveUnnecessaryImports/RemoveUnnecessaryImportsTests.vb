' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.RemoveUnnecessaryImports
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.RemoveUnnecessaryImports

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.RemoveUnnecessaryImports
    Partial Public Class RemoveUnnecessaryImportsTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicRemoveUnnecessaryImportsDiagnosticAnalyzer(), New RemoveUnnecessaryImportsCodeFixProvider())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestProjectLevelMemberImport1()
            Test(
NewLines("[|Imports System \n Module Program \n Sub Main(args As DateTime()) \n End Sub \n End Module|]"),
NewLines("Module Program \n Sub Main(args As DateTime()) \n End Sub \n End Module"),
parseOptions:=TestOptions.Regular,
compilationOptions:=TestOptions.ReleaseExe.WithGlobalImports({GlobalImport.Parse("System")}))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestProjectLevelMemberImport2()
            TestMissing(
NewLines("[|Imports System \n Module Program \n Sub Main(args As DateTime()) \n End Sub \n End Module|]"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestNoImports()
            Test(
NewLines("[|Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module|]"),
NewLines("Module Program \n Sub Main(args As String()) \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestSimpleTypeName()
            Test(
NewLines("[|Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim s As DateTime \n End Sub \n End Module|]"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim s As DateTime \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestGenericTypeName()
            Test(
NewLines("[|Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim s As List(Of Integer) \n End Sub \n End Module|]"),
NewLines("Imports System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Dim s As List(Of Integer) \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestNamespaceName()
            Test(
NewLines("[|Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim s As Collections.Generic.List(Of Integer) \n End Sub \n End Module|]"),
NewLines("Imports System \n Module Program \n Sub Main(args As String()) \n Dim s As Collections.Generic.List(Of Integer) \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestAliasName()
            Test(
NewLines("[|Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports G = System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Dim s As G.List(Of Integer) \n End Sub \n End Module|]"),
NewLines("Imports G = System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Dim s As G.List(Of Integer) \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestExtensionMethod()
            Test(
NewLines("[|Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n args.Where(Function(a) a.Length > 21) \n End Sub \n End Module|]"),
NewLines("Imports System.Linq \n Module Program \n Sub Main(args As String()) \n args.Where(Function(a) a.Length > 21) \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestModuleMember()
            Test(
NewLines("[|Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports Foo \n Namespace Foo \n Public Module M \n Public Sub Bar(i As Integer) \n End Sub \n End Module \n End Namespace \n Module Program \n Sub Main(args As String()) \n Bar(0) \n End Sub \n End Module|]"),
NewLines("Imports Foo \n Namespace Foo \n Public Module M \n Public Sub Bar(i As Integer) \n End Sub \n End Module \n End Namespace \n Module Program \n Sub Main(args As String()) \n Bar(0) \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestInvalidCodeRemovesImports()
            Test(
NewLines("[|Imports System \n Imports System.Collections.Generic \n Module Program \n Sub Main() \n gibberish Dim lst As List(Of String) \n Console.WriteLine(""TEST"") \n End Sub \n End Module|]"),
NewLines("Imports System \n Module Program \n Sub Main() \n gibberish Dim lst As List(Of String) \n Console.WriteLine(""TEST"") \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestExcludedCodeIsIgnored()
            Test(
NewLines("[|Imports System \n Module Program \n Sub Main() \n #If False Then \n Console.WriteLine(""TEST"") \n #End If \n End Sub \n End Module|]"),
NewLines("Module Program \n Sub Main() \n #If False Then \n Console.WriteLine(""TEST"") \n #End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(541744)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestCommentsAroundImportsStatement()
            Test(
<Text>'c1
[|Imports System.Configuration 'c2
Imports System, System.Collections.Generic 'c3
'c4

Module Module1
    Sub Main()
        Dim x As List(Of Integer)
    End Sub
End Module|]</Text>.NormalizedValue,
<Text>'c1
Imports System.Collections.Generic 'c3
'c4

Module Module1
    Sub Main()
        Dim x As List(Of Integer)
    End Sub
End Module</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(541747)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestAttribute()
            TestMissing(
NewLines("[|Imports SomeNamespace \n <SomeAttr> \n Class Foo \n End Class \n Namespace SomeNamespace \n Public Class SomeAttrAttribute \n Inherits Attribute \n End Class \n End Namespace|]"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestAttributeArgument()
            TestMissing(
NewLines("[|Imports System \n Imports SomeNamespace \n <SomeAttribute(Foo.C)> \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module \n Namespace SomeNamespace \n Public Enum Foo \n A \n B \n C \n End Enum \n End Namespace \n Public Class SomeAttribute \n Inherits Attribute \n Public Sub New(x As SomeNamespace.Foo) \n End Sub \n End Class|]"))
        End Sub

        <WorkItem(541757)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestImportsSurroundedByDirectives()
            Test(
NewLines("#If True Then \n [|Imports System.Collections.Generic \n #End If \n Module Program \n End Module|]"),
NewLines("#If True Then \n #End If \n Module Program \n End Module"))
        End Sub

        <WorkItem(541758)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemovingUnbindableImports()
            Test(
NewLines("[|Imports gibberish \n Module Program \n End Module|]"),
NewLines("Module Program \n End Module"))
        End Sub

        <WorkItem(541744)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestPreservePrecedingComments()
            Test(
<Text>' c1
[|Imports System 'c2
' C3

Module Module1
End Module|]</Text>.NormalizedValue,
<Text>' c1
' C3

Module Module1
End Module</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(541757)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestDirective1()
            Test(
<Text>#If True Then
[|Imports System.Collections.Generic
#End If

Module Program
End Module|]</Text>.NormalizedValue,
<Text>#If True Then
#End If

Module Program
End Module</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(541757)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestDirective2()
            Test(
<Text>#If True Then
[|Imports System
Imports System.Collections.Generic
#End If

Module Program
    Dim a As List(Of Integer)
End Module|]</Text>.NormalizedValue,
<Text>#If True Then
Imports System.Collections.Generic
#End If

Module Program
    Dim a As List(Of Integer)
End Module</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(541932)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestImportsClauseRemoval1()
            Test(
NewLines("[|Imports System, foo, System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Console.WriteLine(""TEST"") \n Dim q As List(Of Integer) \n End Sub \n End Module \n Namespace foo \n Class bar \n End Class \n End Namespace|]"),
NewLines("Imports System, System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Console.WriteLine(""TEST"") \n Dim q As List(Of Integer) \n End Sub \n End Module \n Namespace foo \n Class bar \n End Class \n End Namespace"))
        End Sub

        <WorkItem(541932)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestImportsClauseRemoval2()
            Test(
NewLines("[|Imports System, System.Collections.Generic, foo \n Module Program \n Sub Main(args As String()) \n Console.WriteLine(""TEST"") \n Dim q As List(Of Integer) \n End Sub \n End Module \n Namespace foo \n Class bar \n End Class \n End Namespace|]"),
NewLines("Imports System, System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Console.WriteLine(""TEST"") \n Dim q As List(Of Integer) \n End Sub \n End Module \n Namespace foo \n Class bar \n End Class \n End Namespace"))
        End Sub

        <WorkItem(541932)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestImportsClauseRemoval3()
            Test(
NewLines("[|Imports foo, System, System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Console.WriteLine(""TEST"") \n Dim q As List(Of Integer) \n End Sub \n End Module \n Namespace foo \n Class bar \n End Class \n End Namespace|]"),
NewLines("Imports System, System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Console.WriteLine(""TEST"") \n Dim q As List(Of Integer) \n End Sub \n End Module \n Namespace foo \n Class bar \n End Class \n End Namespace"))
        End Sub

        <WorkItem(541758)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestUnbindableNamespace()
            Test(
NewLines("[|Imports gibberish \n Module Program \n End Module|]"),
NewLines("Module Program \n End Module"))
        End Sub

        <WorkItem(541780)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemoveClause()
            Test(
NewLines("[|Imports System, foo, System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Console.WriteLine(""TEST"") \n Dim q As List(Of Integer) \n End Sub \n End Module \n Namespace foo \n Class bar \n End Class \n End Namespace|]"),
NewLines("Imports System, System.Collections.Generic \n Module Program \n Sub Main(args As String()) \n Console.WriteLine(""TEST"") \n Dim q As List(Of Integer) \n End Sub \n End Module \n Namespace foo \n Class bar \n End Class \n End Namespace"))
        End Sub

        <WorkItem(528603)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemoveClauseWithExplicitLC1()
            Test(
NewLines("[|Imports A _ \n , B \n Module Program \n Sub Main(args As String()) \n Dim q As CA \n End Sub \n End Module \n Namespace A \n Public Class CA \n End Class \n End Namespace \n Namespace B \n Public Class CB \n End Class \n End Namespace|]"),
NewLines("Imports A \n Module Program \n Sub Main(args As String()) \n Dim q As CA \n End Sub \n End Module \n Namespace A \n Public Class CA \n End Class \n End Namespace \n Namespace B \n Public Class CB \n End Class \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemoveClauseWithExplicitLC2()
            Test(
NewLines("[|Imports B _ \n , A \n Module Program \n Sub Main(args As String()) \n Dim q As CA \n End Sub \n End Module \n Namespace A \n Public Class CA \n End Class \n End Namespace \n Namespace B \n Public Class CB \n End Class \n End Namespace|]"),
NewLines("Imports A \n Module Program \n Sub Main(args As String()) \n Dim q As CA \n End Sub \n End Module \n Namespace A \n Public Class CA \n End Class \n End Namespace \n Namespace B \n Public Class CB \n End Class \n End Namespace"))
        End Sub

        <WorkItem(528603)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemoveClauseWithExplicitLC3()
            Test(
<Text>[|Imports A _
    , B _
    , C

Module Program
    Sub Main()
        Dim q As CB

    End Sub
End Module

Namespace A
    Public Class CA
    End Class
End Namespace

Namespace B
    Public Class CB
    End Class
End Namespace

Namespace C
    Public Class CC
    End Class
End Namespace|]</Text>.NormalizedValue,
<Text>Imports B _

Module Program
    Sub Main()
        Dim q As CB

    End Sub
End Module

Namespace A
    Public Class CA
    End Class
End Namespace

Namespace B
    Public Class CB
    End Class
End Namespace

Namespace C
    Public Class CC
    End Class
End Namespace</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestTypeImports()
            Test(
<Text>[|Imports Foo

Module Program
    Sub Main()
    End Sub
End Module

Public Class Foo
    Shared Sub Bar()
    End Sub
End Class|]</Text>.NormalizedValue,
<Text>Module Program
    Sub Main()
    End Sub
End Module

Public Class Foo
    Shared Sub Bar()
    End Sub
End Class</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(528603)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestTypeImports_DoesNotRemove()
            TestMissing(
<Text>[|Imports Foo

Module Program
    Sub Main()
        Bar()
    End Sub
End Module

Public Class Foo
    Shared Sub Bar()

    End Sub
End Class|]</Text>.NormalizedValue, parseOptions:=Nothing)
            ' TODO: Enable testing in script when it comes online
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestAlias()
            Test(
<Text>[|Imports F = SomeNS

Module Program
    Sub Main()
    End Sub
End Module

Namespace SomeNS
    Public Class Foo
    End Class
End Namespace|]</Text>.NormalizedValue,
<Text>Module Program
    Sub Main()
    End Sub
End Module

Namespace SomeNS
    Public Class Foo
    End Class
End Namespace</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestAlias_DoesNotRemove()
            TestMissing(
<Text>[|Imports F = SomeNS

Module Program
    Sub Main()
        Dim q As F.Foo
    End Sub
End Module

Namespace SomeNS
    Public Class Foo
    End Class
End Namespace|]</Text>.NormalizedValue)
        End Sub

        <WorkItem(541809)>
        <WorkItem(16488, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestImportsOnSameLine1()
            Test(
<Text>[|Imports A : Imports B

Module Program
    Sub Main()
        Dim q As ClassA
    End Sub
End Module

Namespace A
    Public Class ClassA
    End Class
End Namespace

Namespace B
    Public Class ClassB
    End Class
End Namespace|]</Text>.NormalizedValue,
<Text>Imports A

Module Program
    Sub Main()
        Dim q As ClassA
    End Sub
End Module

Namespace A
    Public Class ClassA
    End Class
End Namespace

Namespace B
    Public Class ClassB
    End Class
End Namespace</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestImportsOnSameLine2()
            Test(
<Text>[|Imports A : Imports B
Imports C

Module Program
    Sub Main()
        Dim q1 As ClassA
        Dim q2 As ClassC
    End Sub
End Module

Namespace A
    Public Class ClassA
    End Class
End Namespace

Namespace B
    Public Class ClassB
    End Class
End Namespace

Namespace C
    Public Class ClassC
    End Class
End Namespace|]</Text>.NormalizedValue,
<Text>Imports A
Imports C

Module Program
    Sub Main()
        Dim q1 As ClassA
        Dim q2 As ClassC
    End Sub
End Module

Namespace A
    Public Class ClassA
    End Class
End Namespace

Namespace B
    Public Class ClassB
    End Class
End Namespace

Namespace C
    Public Class ClassC
    End Class
End Namespace</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(541808)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestTypeImport1()
            TestMissing(
NewLines("[|Imports Foo \n Module Program \n Sub Main() \n Bar() \n End Sub \n End Module \n Public Class Foo \n Shared Sub Bar() \n End Sub \n End Class|]"),
parseOptions:=Nothing) 'TODO (tomat): modules not yet supported in script
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestTypeImport2()
            TestMissing(
NewLines("[|Imports Foo \n Module Program \n Sub Main() \n Dim q As Integer = Bar \n End Sub \n End Module \n Public Class Foo \n Public Shared Bar As Integer \n End Sub \n End Class|]"),
parseOptions:=Nothing) 'TODO (tomat): modules not yet supported in script
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestUnusedTypeImportIsRemoved()
            Test(
<Text>[|Imports SomeNS.Foo

Module Program
    Sub Main(args As String())
    End Sub
End Module

Namespace SomeNS
    Module Foo
    End Module
End Namespace|]</Text>.NormalizedValue,
<Text>Module Program
    Sub Main(args As String())
    End Sub
End Module

Namespace SomeNS
    Module Foo
    End Module
End Namespace</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(528643)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestExtensionMethodLinq()
            ' TODO: Enable script context testing.

            TestMissing(<Text>[|Imports System.Collections
Imports System
Imports SomeNS

Public Module Program
    Sub Main()
        Dim qq As Foo = New Foo()
        Dim x As IEnumerable = From q In qq Select q
    End Sub
End Module

Public Class Foo
    Public Sub Foo()
    End Sub
End Class

Namespace SomeNS
    Public Module SomeClass
        &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt;
        Public Function [Select](ByRef o As Foo, f As Func(Of Object, Object)) As IEnumerable
            Return Nothing
        End Function
    End Module
End Namespace|]</Text>.NormalizedValue, parseOptions:=TestOptions.Regular)
        End Sub

        <WorkItem(543217)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestExtensionMethodLinq2()
            TestMissing(
<Text>[|Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim product = New With {Key .Name = "", Key .Price = 0}
        Dim products = ToList(product)
        Dim namePriceQuery = From prod In products
                             Select prod.Name, prod.Price
    End Sub
 
    Function ToList(Of T)(a As T) As IEnumerable(Of T)
        Return Nothing
    End Function
End Module
|]</Text>.NormalizedValue)
        End Sub

        <WorkItem(542135)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestImportedTypeUsedAsGenericTypeArgument()
            TestMissing(
<Text>[|Imports GenericThingie

Public Class GenericType(Of T)
End Class

Namespace GenericThingie

    Public Class Something
    End Class

End Namespace

Public Class Program
    Sub foo()
        Dim type As GenericType(Of Something)
    End Sub
End Class|]</Text>.NormalizedValue)
        End Sub

        <WorkItem(542132)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemoveSuperfluousNewLines1()
            Test(
<Text>[|Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module|]</Text>.NormalizedValue,
<Text>Module Program
    Sub Main(args As String())

    End Sub
End Module</Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(542132)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemoveSuperfluousNewLines2()
            Test(
<Text><![CDATA[[|Imports System
Imports System.Collections.Generic
Imports System.Linq


<Assembly: System.Obsolete()>


Module Program
    Sub Main(args As String())

    End Sub
End Module|]]]></Text>.NormalizedValue,
<Text><![CDATA[<Assembly: System.Obsolete()>


Module Program
    Sub Main(args As String())

    End Sub
End Module]]></Text>.NormalizedValue,
compareTokens:=False)
        End Sub

        <WorkItem(542895)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub RegressionFor10326()
            Test(
NewLines("[|Imports System.ComponentModel \n <Foo(GetType(Category))> \n Class Category \n End Class|]"),
NewLines("<Foo(GetType(Category))> \n Class Category \n End Class"))
        End Sub

        <WorkItem(712656)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemovalSpan1()
            TestSpans(
<text>    [|Imports System

Namespace N
End Namespace|]</text>.NormalizedValue,
<text>    [|Imports System|]

Namespace N
End Namespace</text>.NormalizedValue)
        End Sub

        <WorkItem(545434)>
        <WorkItem(712656)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemovalSpan2()
            TestSpans(
<text>
#Const A = 1
[|Imports System
#Const B = 1
Imports System.Runtime.InteropServices|]</text>.NormalizedValue,
<text>
#Const A = 1
[|Imports System|]
#Const B = 1
[|Imports System.Runtime.InteropServices|]</text>.NormalizedValue, diagnosticId:=IDEDiagnosticIds.RemoveUnnecessaryImportsDiagnosticId)
        End Sub

        <WorkItem(712656)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestRemovalSpan3()
            Test(
<text>
#Const A = 1
Imports System
[|#Const B = 1|]
Imports System.Runtime.InteropServices
Class C : Dim x As Action : End Class</text>.NormalizedValue,
<text>
#Const A = 1
Imports System
#Const B = 1
Class C : Dim x As Action : End Class</text>.NormalizedValue)
        End Sub

        <WorkItem(545831)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestImplicitElementAtOrDefault()
            Test(
<Text><![CDATA[[|Option Strict On

Imports System
Imports System.Text
Imports System.Runtime.CompilerServices
Imports N

Module M
    Sub Main()
        Foo(Function(x) x(0), Nothing)
    End Sub

    Sub Foo(x As Func(Of C, Object), y As String)
        Console.WriteLine(1)
    End Sub

    Sub Foo(x As Func(Of Integer(), Object), y As Object)
        Console.WriteLine(2)
    End Sub
End Module

Class C
    Public Function ElementAtOrDefault(index As Integer) As Integer
    End Function
End Class

Namespace N
    Module E
        <Extension>
        Function [Select](x As C, y As Func(Of Integer, Integer)) As C
        End Function
    End Module
End Namespace|]]]></Text>.NormalizedValue,
<Text><![CDATA[Option Strict On

Imports System
Imports System.Runtime.CompilerServices
Imports N

Module M
    Sub Main()
        Foo(Function(x) x(0), Nothing)
    End Sub

    Sub Foo(x As Func(Of C, Object), y As String)
        Console.WriteLine(1)
    End Sub

    Sub Foo(x As Func(Of Integer(), Object), y As Object)
        Console.WriteLine(2)
    End Sub
End Module

Class C
    Public Function ElementAtOrDefault(index As Integer) As Integer
    End Function
End Class

Namespace N
    Module E
        <Extension>
        Function [Select](x As C, y As Func(Of Integer, Integer)) As C
        End Function
    End Module
End Namespace]]></Text>.NormalizedValue)
        End Sub

        <WorkItem(545964)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)>
        Public Sub TestMissingOnSynthesizedEventType()
            TestMissing(
NewLines("[|Class C \n Event E() \n End Class|]"))
        End Sub
    End Class
End Namespace
