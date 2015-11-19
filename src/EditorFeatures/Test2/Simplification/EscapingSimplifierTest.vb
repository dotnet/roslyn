' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class EscapingSimplifierTest
        Inherits AbstractSimplificationTests

#Region "Visual Basic Escaping Simplification tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyUnescapedIdentifier()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared z As Integer
    Sub M()
        {|Simplify:z|} = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared z As Integer
    Sub M()
        z = 23
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifier()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared z As Integer
    Sub M()
        {|Simplify:[z]|} = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared z As Integer
    Sub M()
        [z] = 23
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameWithUnescapedIdentifier()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared z As Integer
    Sub M()
        {|SimplifyParent:C.z|} = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared z As Integer
    Sub M()
        z = 23
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameWithEscapedIdentifier()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared z As Integer
    Sub M()
        {|SimplifyParent:C.[z]|} = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared z As Integer
    Sub M()
        [z] = 23
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierRem()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public [Rem] as Integer
    Sub M()
        {|SimplifyParent:C.[Rem]|} = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public [Rem] as Integer
    Sub M()
        [Rem] = 23
    End Sub
End Class
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierKeyword()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public [End] as Integer
    Sub M()
        {|Simplify:[End]|} = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public [End] as Integer
    Sub M()
        [End] = 23
    End Sub
End Class
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameWithUnescapedIdentifierKeyword()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public [End] as Integer
    Sub M()
        {|SimplifyParent:C.End|} = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public [End] as Integer
    Sub M()
        [End] = 23
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameWithEscapedIdentifierKeyword()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public [End] as Integer
    Sub M()
        {|SimplifyParent:C.[End]|} = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public [End] as Integer
    Sub M()
        [End] = 23
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyUnescapedIdentifierMid_1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        if {|Simplify:Mid|}(23) = 42 then
        end if
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        if Mid(23) = 42 then
        end if
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyUnescapedIdentifierMid_2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        dim s1 = "foo"
        {|Simplify:Mid|}(s1, 1, 1) = "bar"
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        dim s1 = "foo"
        Mid(s1, 1, 1) = "bar"
    End Sub
End Class

</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierMid_1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        {|Simplify:[Mid]|}(23) = 42
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        [Mid](23) = 42
    End Sub
End Class
</code>

            Test(input, expected)

        End Sub

        <WorkItem(547117)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameWithUnescapedIdentifierMid()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        C.{|SimplifyParent:Mid|}(23) = 42
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        [Mid](23) = 42
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WorkItem(547117)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameWithEscapedIdentifierMid()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        C.{|SimplifyParent:[Mid]|}(23) = 42
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Mid(23) as Integer
    Sub M()
        [Mid](23) = 42
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameUnescapedIdentifierMid()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim y = {|SimplifyParent:C.Mid|}(23)
    End Sub

    Shared Function Mid(p As Integer) as integer
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Sub M()
        Dim y = Mid(23)
    End Sub

    Shared Function Mid(p As Integer) as integer
    End Function
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierPreserve_1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Preserve as Integer()
    Sub M()
        {|Simplify:[Preserve]|}(23) = 32
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Preserve as Integer()
    Sub M()
        [Preserve](23) = 32
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierPreserve_2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Preserve as Integer()
    Sub M()
        ReDim {|Simplify:[Preserve]|}(23)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Preserve as Integer()
    Sub M()
        ReDim [Preserve](23)
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyUnescapedIdentifierPreserve()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Preserve as Integer()
    Sub M()
        {|Simplify:Preserve|}(23) = 23
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Preserve as Integer()
    Sub M()
        Preserve(23) = 23
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameUnescapedIdentifierPreserve_1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Preserve as Integer
    Sub M()
        {|SimplifyParent:C.Preserve|} = 42
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Preserve as Integer
    Sub M()
        Preserve = 42
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameUnescapedIdentifierPreserve_2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Preserve as Integer()
    Sub M()
        ReDim {|SimplifyParent:C.Preserve|}(23)
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Preserve as Integer()
    Sub M()
        ReDim [Preserve](23)
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyNameEscapedIdentifierPreserve()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Shared Public Preserve as Integer
    Sub M()
        {|SimplifyParent:C.[Preserve]|} = 42
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Class C
    Shared Public Preserve as Integer
    Sub M()
        [Preserve] = 42
    End Sub
End Class
</code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierNew_1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On

Class CFoo
    Public Shared [New] As Integer = 23
End Class

Structure Foo3
    Public Sub Doo()
        Dim w = CFoo.{|Simplify:[New]|}
    End Sub
End Structure
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On

Class CFoo
    Public Shared [New] As Integer = 23
End Class

Structure Foo3
    Public Sub Doo()
        Dim w = CFoo.[New]
    End Sub
End Structure
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierNew_2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On

Interface IFoo
    Property [New] As Integer
End Interface

Structure Foo3
    Public Sub Doo()
        Dim a as IFoo
        Dim w = a.{|Simplify:[New]|} ' not really needed, but we can't provide a better result at the moment
    End Sub
End Structure
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On

Interface IFoo
    Property [New] As Integer
End Interface

Structure Foo3
    Public Sub Doo()
        Dim a as IFoo
        Dim w = a.[New] ' not really needed, but we can't provide a better result at the moment
    End Sub
End Structure
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierNew_3()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On

Enum EFoo
    [New] = 1
End Enum

Structure Foo3
    Public Sub Doo()
        Dim z = EFoo.{|Simplify:[New]|} ' not really needed, but we can't provide a better result at the moment
    End Sub
End Structure
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On

Enum EFoo
    [New] = 1
End Enum

Structure Foo3
    Public Sub Doo()
        Dim z = EFoo.[New] ' not really needed, but we can't provide a better result at the moment
    End Sub
End Structure
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierNew_4()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On

Class Foo1
    Public Sub New()
    End Sub

    Public Sub New(p as integer)
        MyClass.{|Simplify:New|}()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On

Class Foo1
    Public Sub New()
    End Sub

    Public Sub New(p as integer)
        MyClass.New()
    End Sub
End Class
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierQueryOperatorOutsideOfQuery_1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On

Class Foo1
    Public Sub Main()
        Dim x = From a in ""
        {|SimplifyParent:Foo1.Take|}()
    End Sub

    Shared Public Sub Take()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On

Class Foo1
    Public Sub Main()
        Dim x = From a in ""
        [Take]()
    End Sub

    Shared Public Sub Take()
    End Sub
End Class
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierQueryOperatorOutsideOfQuery_2()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On

Class Foo1
    Public Sub Main()
        Dim x = From a in ""

        {|SimplifyParent:Foo1.Take|}()
    End Sub

    Shared Public Sub Take()
    End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On

Class Foo1
    Public Sub Main()
        Dim x = From a in ""

        Take()
    End Sub

    Shared Public Sub Take()
    End Sub
End Class
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierQueryOperatorOutsideOfQuery_3()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On

Module Program
    Public Sub Main()
        Dim myBooks() As String = {"abc", "def", "hij"}

        Dim y = From books In New List(Of IEnumerable(Of String))() From {(From book In myBooks Where book.Length > 1 Select book),
                                                                          (From book In myBooks Where book.Length > 1 Select book)} Select books
        {|SimplifyParent:Program.Group|}()        

    End Sub

    Public Sub Group()
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On

Module Program
    Public Sub Main()
        Dim myBooks() As String = {"abc", "def", "hij"}

        Dim y = From books In New List(Of IEnumerable(Of String))() From {(From book In myBooks Where book.Length > 1 Select book),
                                                                          (From book In myBooks Where book.Length > 1 Select book)} Select books
        [Group]()        

    End Sub

    Public Sub Group()
    End Sub
End Module

</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_SimplifyEscapedIdentifierQueryOperatorInsideOfQuery_1()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Option Strict On

Class Foo1
    Public Sub Main()
        Dim x = From a in {|SimplifyParent:Foo1.Take|}()
    End Sub

    Shared Public Function Take() as String
        return ""
    End Function
End Class
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Option Strict On

Class Foo1
    Public Sub Main()
        Dim x = From a in Take()
    End Sub

    Shared Public Function Take() as String
        return ""
    End Function
End Class
</code>

            Test(input, expected)

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub VisualBasic_DoNotEscapeIdentifierWithEmptyValueText()
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Module Program
    Sub Main()
        Call From z In, {|Simplify:Dim|} x = Take ()
    End Sub
End Module
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
Module Program
    Sub Main()
        Call From z In, Dim x = Take ()
    End Sub
End Module
</code>

            Test(input, expected)
        End Sub

#End Region

#Region "CSharp Escaping Simplification tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyUnescapedIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            static int z;
            void M()
            {
                {|Simplify:z|} = 23;
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        class C
        {
            static int z;
            void M()
            {
                z = 23;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyEscapedIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            static int z;
            void M()
            {
                {|Simplify:@z|} = 23;
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        class C
        {
            static int z;
            void M()
            {
                @z = 23;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyNameUnescapedIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            static int z;
            void M()
            {
                {|SimplifyParent:C.z|} = 23;
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        class C
        {
            static int z;
            void M()
            {
                z = 23;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyNameEscapedIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            static int z;
            void M()
            {
                {|SimplifyParent:C.@z|} = 23;
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        class C
        {
            static int z;
            void M()
            {
                @z = 23;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyEscapedTypenameAsIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            static int @int;
            void M()
            {
                {|Simplify:@int|} = 23;
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        class C
        {
            static int @int;
            void M()
            {
                @int = 23;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyEscapedKeywordAsIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            static int @if;
            void M()
            {
                {|Simplify:@if|} = 23;
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        class C
        {
            static int @if;
            void M()
            {
                @if = 23;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyNameEscapedTypenameAsIdentifier()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            static int @int;
            void M()
            {
                {|SimplifyParent:C.@int|} = 23;
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        class C
        {
            static int @int;
            void M()
            {
                @int = 23;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyEscapedContextualKeywordAsIdentifierInQuery()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System;
        using System.Linq;

        class C
        {
            static int from;

            static void Main()
            {
                var q = from y in "" select {|SimplifyParent:C.@from|};
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        using System;
        using System.Linq;

        class C
        {
            static int from;

            static void Main()
            {
                var q = from y in "" select @from;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyEscapedContextualKeywordAsIdentifierInNestedQuery_1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System.Linq;
        using System.Collections.Generic;

        class Program
        {
            static int from = 2;

            static void Main()
            {
                string[] myBooks = { "abc", "def", "hij" };

                var y = from books in new List&lt;IEnumerable&lt;string>>() {from book in myBooks where book.Length > 1 select book,
                                         from book in myBooks where book.Length > {|SimplifyParent:Program.@from|} select book}
                select books;
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        using System.Linq;
        using System.Collections.Generic;

        class Program
        {
            static int from = 2;

            static void Main()
            {
                string[] myBooks = { "abc", "def", "hij" };

                var y = from books in new List&lt;IEnumerable&lt;string>>() {from book in myBooks where book.Length > 1 select book,
                                         from book in myBooks where book.Length > @from select book}
                select books;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyEscapedContextualKeywordAsIdentifierOutsideQuery()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        using System;
        using System.Linq;

        class C
        {
            static int from;

            static void Main()
            {
                var q = from y in "" select @from;
                var x = {|SimplifyParent:C.@from|};
            }
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        using System;
        using System.Linq;

        class C
        {
            static int from;

            static void Main()
            {
                var q = from y in "" select @from;
                var x = @from;
            }
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyUnescapedUnambiguousAttributeName()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [{|SimplifyToken:@C|}]
        class C : System.Attribute
        {
        }
                </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        [C]
        class C : System.Attribute
        {
        }

        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyUnEscapedUnambiguousAttributeName2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [{|SimplifyToken:@CAttribute|}]
        class C : System.Attribute
        {
        }
        class CAttribute : System.Attribute
        {
        }       
         </Document>
    </Project>
</Workspace>

            Dim expected =
        <code>
        [CAttribute]
        class C : System.Attribute
        {
        }
        class CAttribute : System.Attribute
        {
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyUnEscapedUnambiguousAttributeName3()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [{|Simplify:@CAttribute|}]
        class CAttribute : System.Attribute
        {
        }       
         </Document>
    </Project>
</Workspace>

            Dim expected =
        <code>
        [C]
        class CAttribute : System.Attribute
        {
        }
        </code>

            Test(input, expected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_SimplifyEscapedAmbiguousAttributeName()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        [{|SimplifyToken:@C|}]
        class C : System.Attribute
        {
        }

        class CAttribute : System.Attribute
        {
        }
        </Document>
    </Project>
</Workspace>

            Dim expected =
<code>
        [@C]
        class C : System.Attribute
        {
        }

        class CAttribute : System.Attribute
        {
        }
        </code>

            Test(input, expected)
        End Sub

#End Region

    End Class
End Namespace
