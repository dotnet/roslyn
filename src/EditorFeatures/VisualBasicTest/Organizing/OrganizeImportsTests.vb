' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.OrganizeImports

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Organizing
    Public Class OrganizeImportsTests
        Private Async Function CheckAsync(initial As XElement, final As XElement, specialCaseSystem As Boolean) As Threading.Tasks.Task
            Using workspace = Await TestWorkspace.CreateVisualBasicAsync(initial.NormalizedValue)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)
                Dim newRoot = Await (Await OrganizeImportsService.OrganizeImportsAsync(document, specialCaseSystem)).GetSyntaxRootAsync()

                Assert.Equal(final.NormalizedValue, newRoot.ToFullString())
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Async Function TestEmptyFile() As Task
            Await CheckAsync(<content></content>, <content></content>, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Async Function TestSingleImportsStatement() As Task
            Dim initial = <content>Imports A</content>
            Dim final = initial
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Async Function TestMultipleClauses() As Task
            Dim initial = <content>Imports C, B, A</content>
            Dim final = <content>Imports A, B, C</content>
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <WorkItem(2480, "https://github.com/dotnet/roslyn/issues/2480")>
        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <WorkItem(2480, "https://github.com/dotnet/roslyn/issues/2480")>
        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <WorkItem(2480, "https://github.com/dotnet/roslyn/issues/2480")>
        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Async Function TestDuplicateUsings() As Task
            Dim initial =
<content>Imports A
Imports A</content>

            Dim final = initial

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        Public Async Function TestNestedRegionBlock() As Task
            Dim initial =
<content>Imports C
#region Z
Imports A
#endregion
Imports B</content>

            Dim final = initial

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Await CheckAsync(initial, final, False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
        <WorkItem(538367, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538367")>
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

            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

// If Kana is sensitive あ != ア, if Kana is insensitive あ == ア.
// If Width is sensitiveア != ｱ, if Width is insensitive ア == ｱ.</content>

            Dim final =
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

// If Kana is sensitive あ != ア, if Kana is insensitive あ == ア.
// If Width is sensitiveア != ｱ, if Width is insensitive ア == ｱ.</content>
            Await CheckAsync(initial, final, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Organizing)>
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

            Dim final =
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

            Await CheckAsync(initial, final, True)
        End Function
    End Class
End Namespace
