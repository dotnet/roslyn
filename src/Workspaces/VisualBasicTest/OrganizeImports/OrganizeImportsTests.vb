' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.OrganizeImports
Imports Microsoft.CodeAnalysis.[Shared].Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Workspaces.UnitTests.OrganizeImports
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Organizing)>
    Public Class OrganizeImportsTests
        Private Shared Async Function CheckAsync(initial As XElement, final As XElement,
                                          Optional placeSystemNamespaceFirst As Boolean = False,
                                          Optional separateImportGroups As Boolean = False,
                                                 Optional endOfLine As String = Nothing) As Task
            Using workspace = New AdhocWorkspace()
                Dim project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.VisualBasic)
                Dim document = project.AddDocument("Document", SourceText.From(initial.Value.ReplaceLineEndings(If(endOfLine, Environment.NewLine))))

                Dim service = document.GetRequiredLanguageService(Of IOrganizeImportsService)
                Dim options = New OrganizeImportsOptions() With
                {
                    .PlaceSystemNamespaceFirst = placeSystemNamespaceFirst,
                    .SeparateImportDirectiveGroups = separateImportGroups,
                    .NewLine = If(endOfLine, OrganizeImportsOptions.Default.NewLine)
                }

                Dim newDocument = Await service.OrganizeImportsAsync(document, options, CancellationToken.None)
                Dim newRoot = Await newDocument.GetSyntaxRootAsync()
                Assert.Equal(final.Value.ReplaceLineEndings(If(endOfLine, Environment.NewLine)), newRoot.ToFullString())
            End Using
        End Function

        Private Shared Async Function CheckWithFormatAsync(initial As XElement, final As XElement,
                                          Optional placeSystemNamespaceFirst As Boolean = False,
                                          Optional separateImportGroups As Boolean = False) As Task
            Using workspace = New AdhocWorkspace()
                Dim project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.VisualBasic)
                Dim document = project.AddDocument("Document", SourceText.From(initial.Value.NormalizeLineEndings()))

                Dim formattingOptions = New VisualBasicSyntaxFormattingOptions() With
                {
                    .SeparateImportDirectiveGroups = separateImportGroups
                }

                Dim organizeOptions = New OrganizeImportsOptions() With
                {
                    .PlaceSystemNamespaceFirst = placeSystemNamespaceFirst,
                    .SeparateImportDirectiveGroups = separateImportGroups
                }

                Dim service = document.GetRequiredLanguageService(Of IOrganizeImportsService)
                Dim organizedDocument = Await service.OrganizeImportsAsync(document, organizeOptions, CancellationToken.None)
                Dim formattedDocument = Await Formatter.FormatAsync(organizedDocument, formattingOptions, CancellationToken.None)

                Dim newRoot = Await formattedDocument.GetSyntaxRootAsync()
                Assert.Equal(final.Value.NormalizeLineEndings(), newRoot.ToFullString())
            End Using
        End Function

        <Fact>
        Public Async Function TestEmptyFile() As Task
            Await CheckAsync(<content></content>, <content></content>)
        End Function

        <Fact>
        Public Async Function TestSingleImportsStatement() As Task
            Dim initial = <content>Imports A</content>
            Dim final = initial
            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestMultipleClauses() As Task
            Dim initial = <content>Imports C, B, A</content>
            Dim final = <content>Imports A, B, C</content>
            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestAliasesAtBottom() As Task
            Dim initial =
<content>Imports A = B
Imports C
Imports D = E
Imports F</content>

            Dim final =
<content>Imports C
Imports F
Imports A = B
Imports D = E
</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestMultipleStatementsMultipleClauses() As Task
            Dim initial =
                <content>Imports F
Imports E
Imports D
Imports C, B, A</content>
            Dim final = <content>Imports A, B, C
Imports D
Imports E
Imports F
</content>
            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestSpecialCaseSystem() As Task
            Dim initial =
<content>Imports M2
Imports M1
Imports System.Linq
Imports System</content>

            Dim final =
<content>Imports System
Imports System.Linq
Imports M1
Imports M2
</content>
            Await CheckAsync(initial, final, placeSystemNamespaceFirst:=True)
        End Function

        <Fact>
        Public Async Function TestDoNotSpecialCaseSystem() As Task
            Dim initial =
<content>Imports M2
Imports M1
Imports System.Linq
Imports System</content>

            Dim final =
<content>Imports M1
Imports M2
Imports System
Imports System.Linq
</content>

            Await CheckAsync(initial, final, placeSystemNamespaceFirst:=False)
        End Function

        <Fact>
        Public Async Function TestMissingNames() As Task
            Dim initial =
    <content>Imports B
Imports
Imports A</content>

            Dim final =
    <content>Imports
Imports A
Imports B
</content>
            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestDoNotTouchCommentsAtBeginningOfFile1() As Task
            Dim initial =
<content>' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports B
' I like namespace A
Imports A

namespace A { }
namespace B { }</content>

            Dim final =
<content>' Copyright (c) Microsoft Corporation.  All rights reserved.

' I like namespace A
Imports A
Imports B

namespace A { }
namespace B { }</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestDoNotTouchCommentsAtBeginningOfFile2() As Task
            Dim initial =
<content>'' Copyright (c) Microsoft Corporation.  All rights reserved. */

Imports B
 '' I like namespace A */
Imports A

namespace A { }
namespace B { }</content>

            Dim final =
<content>'' Copyright (c) Microsoft Corporation.  All rights reserved. */

'' I like namespace A */
Imports A
Imports B

namespace A { }
namespace B { }</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestDoNotTouchCommentsAtBeginningOfFile3() As Task
            Dim initial =
<content>' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports B
 ''' I like namespace A
Imports A

namespace A
end namespace
namespace B
end namespace</content>

            Dim final =
<content>' Copyright (c) Microsoft Corporation.  All rights reserved.

''' I like namespace A
Imports A
Imports B

namespace A
end namespace
namespace B
end namespace</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33251")>
        Public Async Function TestDoNotTouchCommentsAtBeginningOfFile4() As Task
            Dim initial =
<content>''' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports B
 ''' I like namespace A
Imports A

namespace A
end namespace
namespace B
end namespace</content>

            Dim final =
<content>''' Copyright (c) Microsoft Corporation.  All rights reserved.

''' I like namespace A
Imports A
Imports B

namespace A
end namespace
namespace B
end namespace</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")>
        Public Async Function TestDoTouchCommentsAtBeginningOfFile1() As Task
            Dim initial =
<content>' Copyright (c) Microsoft Corporation.  All rights reserved.
Imports B
' I like namespace A
Imports A

namespace A { }
namespace B { }</content>

            Dim final =
<content>' Copyright (c) Microsoft Corporation.  All rights reserved.
' I like namespace A
Imports A
Imports B

namespace A { }
namespace B { }</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")>
        Public Async Function TestDoTouchCommentsAtBeginningOfFile2() As Task
            Dim initial =
<content>'' Copyright (c) Microsoft Corporation.  All rights reserved. */
Imports B
'' I like namespace A */
Imports A

namespace A { }
namespace B { }</content>

            Dim final =
<content>'' Copyright (c) Microsoft Corporation.  All rights reserved. */
'' I like namespace A */
Imports A
Imports B

namespace A { }
namespace B { }</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2480")>
        Public Async Function TestDoTouchCommentsAtBeginningOfFile3() As Task
            Dim initial =
<content>''' Copyright (c) Microsoft Corporation.  All rights reserved.
Imports B
''' I like namespace A
Imports A

namespace A { }
namespace B { }</content>

            Dim final =
<content>''' I like namespace A
Imports A
''' Copyright (c) Microsoft Corporation.  All rights reserved.
Imports B

namespace A { }
namespace B { }</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestDoNotSortIfEndIfBlocks() As Task
            Dim initial =
<content>Imports D
#If MYCONFIG Then
Imports C
#Else
Imports B
#End If
Imports A

namespace A { }
namespace B { }
namespace C { }
namespace D { }</content>

            Dim final = initial
            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestDuplicateUsings() As Task
            Dim initial =
<content>Imports A
Imports A</content>

            Dim final = initial

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestTrailingComments() As Task
            Dim initial =
<content>Imports D '/*03*/
Imports C '/*07*/
Imports A '/*11*/
Imports B '/*15*/
</content>

            Dim final =
<content>Imports A '/*11*/
Imports B '/*15*/
Imports C '/*07*/
Imports D '/*03*/
</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestInsideRegionBlock() As Task
            Dim initial =
    <content>#region Using directives
Imports C
Imports A
Imports B
#endregion
</content>
            Dim final =
<content>#region Using directives
Imports A
Imports B
Imports C
#endregion
</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestNestedRegionBlock() As Task
            Dim initial =
<content>Imports C
#region Z
Imports A
#endregion
Imports B</content>

            Dim final = initial

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestMultipleRegionBlocks() As Task
            Dim initial =
    <content>#region Using directives
Imports C
#region Z
Imports A
#endregion
Imports B
#endregion</content>

            Dim final = initial

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestInterleavedNewlines() As Task
            Dim initial =
<content>Imports B

Imports A

Imports C

class D
end class</content>

            Dim final =
<content>Imports A
Imports B
Imports C

class D
end class</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestInsideIfEndIfBlock() As Task
            Dim initial =
<content>#if not X
Imports B
Imports A
Imports C
#end if</content>

            Dim final =
<content>#if not X
Imports A
Imports B
Imports C
#end if</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestIfEndIfBlockAbove() As Task
            Dim initial =
<content>#if not X
Imports C
Imports B
Imports F
#end if
Imports D
Imports A
Imports E</content>

            Dim final = initial
            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestIfEndIfBlockMiddle() As Task
            Dim initial =
<content>Imports D
Imports A
Imports H
#if not X
Imports C
Imports B
Imports I
#End If
Imports F
Imports E
Imports G</content>

            Dim final = initial
            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestIfEndIfBlockBelow() As Task
            Dim initial =
<content>Imports D
Imports A
Imports E
#if not X
Imports C
Imports B
Imports F
#end if</content>

            Dim final = initial
            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestKorean() As Task
            Dim initial =
    <content>Imports 하
Imports 파
Imports 타
Imports 카
Imports 차
Imports 자
Imports 아
Imports 사
Imports 바
Imports 마
Imports 라
Imports 다
Imports 나
Imports 가</content>

            Dim final =
<content>Imports 가
Imports 나
Imports 다
Imports 라
Imports 마
Imports 바
Imports 사
Imports 아
Imports 자
Imports 차
Imports 카
Imports 타
Imports 파
Imports 하
</content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestDoNotSpecialCaseSystem1() As Task
            Dim initial =
<content>Imports B
Imports System.Collections.Generic
Imports C
Imports _System
Imports SystemZ
Imports D.System
Imports System
Imports System.Collections
Imports A</content>

            Dim final =
<content>Imports _System
Imports A
Imports B
Imports C
Imports D.System
Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports SystemZ
</content>

            Await CheckAsync(initial, final, placeSystemNamespaceFirst:=False)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538367")>
        Public Async Function TestXml() As Task
            Dim initial =
<content><![CDATA[Imports System
Imports <xmlns="http://DefaultNamespace">
Imports System.Collections.Generic
Imports <xmlns:ab="http://NewNamespace">
Imports System.Linq
Imports <xmlns:zz="http://NextNamespace">]]></content>

            Dim final =
<content><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports <xmlns="http://DefaultNamespace">
Imports <xmlns:ab="http://NewNamespace">
Imports <xmlns:zz="http://NextNamespace">
]]></content>

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestCaseSensitivity1() As Task
            Dim initial =
<content>Imports Bb
Imports B
Imports bB
Imports b
Imports Aa
Imports a
Imports A
Imports aa
Imports aA
Imports AA
Imports bb
Imports BB
Imports bBb
Imports bbB
Imports あ
Imports ア
Imports ｱ
Imports ああ
Imports あア
Imports あｱ
Imports アあ
Imports cC
Imports Cc
Imports アア
Imports アｱ
Imports ｱあ
Imports ｱア
Imports ｱｱ
Imports BBb
Imports BbB
Imports bBB
Imports BBB
Imports c
Imports C
Imports bbb
Imports Bbb
Imports cc
Imports cC
Imports CC
</content>

            Dim final As XElement
            If GlobalizationUtilities.ICUMode() Then
                final =
<content>Imports a
Imports A
Imports aa
Imports aA
Imports Aa
Imports AA
Imports b
Imports B
Imports bb
Imports bB
Imports Bb
Imports BB
Imports bbb
Imports bbB
Imports bBb
Imports bBB
Imports Bbb
Imports BbB
Imports BBb
Imports BBB
Imports c
Imports C
Imports cc
Imports cC
Imports cC
Imports Cc
Imports CC
Imports あ
Imports ｱ
Imports ああ
Imports あｱ
Imports ｱあ
Imports ｱｱ
Imports あア
Imports ｱア
Imports ア
Imports アあ
Imports アｱ
Imports アア
</content>
            Else
                final =
<content>Imports a
Imports A
Imports aa
Imports aA
Imports Aa
Imports AA
Imports b
Imports B
Imports bb
Imports bB
Imports Bb
Imports BB
Imports bbb
Imports bbB
Imports bBb
Imports bBB
Imports Bbb
Imports BbB
Imports BBb
Imports BBB
Imports c
Imports C
Imports cc
Imports cC
Imports cC
Imports Cc
Imports CC
Imports ア
Imports ｱ
Imports あ
Imports アア
Imports アｱ
Imports ｱア
Imports ｱｱ
Imports アあ
Imports ｱあ
Imports あア
Imports あｱ
Imports ああ
</content>
            End If

            Await CheckAsync(initial, final)
        End Function

        <Fact>
        Public Async Function TestCaseSensitivity2() As Task
            Dim initial =
<content>Imports あ
Imports ア
Imports ｱ
Imports ああ
Imports あア
Imports あｱ
Imports アあ
Imports アア
Imports アｱ
Imports ｱあ
Imports ｱア
Imports ｱｱ</content>

            Dim final As XElement
            If GlobalizationUtilities.ICUMode() Then
                final =
<content>Imports あ
Imports ｱ
Imports ああ
Imports あｱ
Imports ｱあ
Imports ｱｱ
Imports あア
Imports ｱア
Imports ア
Imports アあ
Imports アｱ
Imports アア
</content>
            Else
                final =
<content>Imports ア
Imports ｱ
Imports あ
Imports アア
Imports アｱ
Imports ｱア
Imports ｱｱ
Imports アあ
Imports ｱあ
Imports あア
Imports あｱ
Imports ああ
</content>
            End If

            Await CheckAsync(initial, final)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20988")>
        Public Async Function TestGrouping() As Task
            Dim initial =
<content><![CDATA[' Banner

Imports Microsoft.CodeAnalysis.CSharp.Extensions
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports <xmlns:ab="http://NewNamespace">
Imports <xmlns="http://DefaultNamespace">
Imports Roslyn.Utilities
Imports IntList = System.Collections.Generic.List(Of Integer)
Imports <xmlns:zz="http://NextNamespace">
]]></content>

            Dim final =
<content><![CDATA[' Banner

Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.CodeAnalysis.CSharp.Extensions
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports Microsoft.CodeAnalysis.Shared.Extensions

Imports Roslyn.Utilities

Imports IntList = System.Collections.Generic.List(Of Integer)

Imports <xmlns:ab="http://NewNamespace">
Imports <xmlns="http://DefaultNamespace">
Imports <xmlns:zz="http://NextNamespace">
]]></content>

            Await CheckAsync(initial, final, placeSystemNamespaceFirst:=True, separateImportGroups:=True)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20988")>
        Public Async Function TestGrouping2() As Task
            ' Make sure we don't insert extra newlines if they're already there.
            Dim initial =
<content><![CDATA[' Banner

Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.CodeAnalysis.CSharp.Extensions
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports Microsoft.CodeAnalysis.Shared.Extensions

Imports Roslyn.Utilities

Imports IntList = System.Collections.Generic.List(Of Integer)

Imports <xmlns:ab="http://NewNamespace">
Imports <xmlns="http://DefaultNamespace">
Imports <xmlns:zz="http://NextNamespace">
]]></content>

            Dim final =
<content><![CDATA[' Banner

Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.CodeAnalysis.CSharp.Extensions
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports Microsoft.CodeAnalysis.Shared.Extensions

Imports Roslyn.Utilities

Imports IntList = System.Collections.Generic.List(Of Integer)

Imports <xmlns:ab="http://NewNamespace">
Imports <xmlns="http://DefaultNamespace">
Imports <xmlns:zz="http://NextNamespace">
]]></content>

            Await CheckAsync(initial, final, placeSystemNamespaceFirst:=True, separateImportGroups:=True)
        End Function

        <Theory, WorkItem("https://github.com/dotnet/roslyn/issues/19306")>
        <InlineData(vbLf)>
        <InlineData(vbCrLf)>
        Public Async Function TestGrouping3(endOfLine As String) As Task
            Dim initial =
<content><![CDATA[' Banner

Imports Microsoft.CodeAnalysis.CSharp.Extensions
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports <xmlns:ab="http://NewNamespace">
Imports <xmlns="http://DefaultNamespace">
Imports Roslyn.Utilities
Imports IntList = System.Collections.Generic.List(Of Integer)
Imports <xmlns:zz="http://NextNamespace">
]]></content>

            Dim final =
<content><![CDATA[' Banner

Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.CodeAnalysis.CSharp.Extensions
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports Microsoft.CodeAnalysis.Shared.Extensions

Imports Roslyn.Utilities

Imports IntList = System.Collections.Generic.List(Of Integer)

Imports <xmlns:ab="http://NewNamespace">
Imports <xmlns="http://DefaultNamespace">
Imports <xmlns:zz="http://NextNamespace">
]]></content>

            Await CheckAsync(initial, final, placeSystemNamespaceFirst:=True, separateImportGroups:=True, endOfLine:=endOfLine)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36984")>
        Public Async Function TestGroupingWithFormat() As Task
            Dim initial =
<content><![CDATA[Imports M
Imports System

Class Program
    Console.WriteLine("Hello World!")

    New Goo()
End Class

Namespace M
    Class Goo
    End Class
End Namespace
]]></content>

            Dim final =
<content><![CDATA[Imports M

Imports System

Class Program
    Console.WriteLine("Hello World!")

    New Goo()
End Class

Namespace M
    Class Goo
    End Class
End Namespace
]]></content>

            Await CheckWithFormatAsync(initial, final, placeSystemNamespaceFirst:=False, separateImportGroups:=True)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36984")>
        Public Async Function TestSortingAndGroupingWithFormat() As Task
            Dim initial =
<content><![CDATA[Imports M
Imports System

Class Program
    Console.WriteLine("Hello World!")

    New Goo()
End Class

Namespace M
    Class Goo
    End Class
End Namespace
]]></content>

            Dim final =
<content><![CDATA[Imports System

Imports M

Class Program
    Console.WriteLine("Hello World!")

    New Goo()
End Class

Namespace M
    Class Goo
    End Class
End Namespace
]]></content>

            Await CheckWithFormatAsync(initial, final, placeSystemNamespaceFirst:=True, separateImportGroups:=True)
        End Function
    End Class
End Namespace
