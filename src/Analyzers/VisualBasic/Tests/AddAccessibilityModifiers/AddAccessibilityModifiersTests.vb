﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeStyle
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers.VisualBasicAddAccessibilityModifiersDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.AddAccessibilityModifiers.VisualBasicAddAccessibilityModifiersCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddAccessibilityModifiers
    Public Class AddAccessibilityModifiersTests

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)>
        Public Sub TestStandardProperties()
            VerifyVB.VerifyStandardProperties()
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)>
        Public Async Function TestAllConstructs() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"
namespace N
    namespace Outer.Inner
        class [|C|]
            class [|NestedClass|]
            end class

            structure [|NestedStruct|]
            end structure

            dim [|f1|] as integer
            dim [|f2|], f3 as integer
            dim [|f4|], f5 as integer, f6, f7 as boolean
            public {|BC30260:f4|} as integer

            event [|e1|] as {|BC31044:{|BC30002:Action|}|}
            public event e2 as {|BC31044:{|BC30002:Action|}|}

            custom event {|BC31130:{|BC31131:{|BC31132:[|e4|]|}|}|} as {|BC31044:{|BC30002:Action|}|}
            end event

            shared sub new()
            end sub

            sub [|new|]()
            end sub

            public sub new(i as integer)
            end sub

            sub [|M1|]()
            end sub

            public sub M2()
            end sub

            function [|M3|]() as integer
            end function

            {|BC30027:function [|M4|]() as integer|}

            {|BC30289:public function M5() as integer|}
            end function

            {|BC31432:partial|} sub [|M6|]()
            end sub

            property [|P1|] as integer
            
            property {|BC30124:[|P2|]|} as integer
                get
                end get
            end property

            public property P3 as integer

            shared operator [|&|](c1 as C, c2 as C) as integer
            end operator
        end class

        interface [|I|]
            event e6 as {|BC31044:{|BC30002:Action|}|}
            sub M3()
            function M4() as integer
            property P3 as integer
        end interface

        delegate sub [|D1|]()
        delegate function [|D2|]() as integer

        enum [|E|]
            EMember
        end enum

        structure [|S|]
            dim [|f|] as integer

            sub [|M|]()
            end sub

            shared operator [|&|](c1 as S, c2 as S) as integer
            end operator
        end structure

        module [|M|]
            dim [|f|] as integer

            sub [|M|]()
            end sub
        end module
    end namespace
end namespace",
"
namespace N
    namespace Outer.Inner
        Friend class C
            Public class NestedClass
            end class

            Public structure NestedStruct
            end structure

            Private f1 as integer
            Private f2, f3 as integer
            Private f4, f5 as integer, f6, f7 as boolean
            public {|BC30260:f4|} as integer

            Public event e1 as {|BC31044:{|BC30002:Action|}|}
            public event e2 as {|BC31044:{|BC30002:Action|}|}

            Public custom event {|BC31130:{|BC31131:{|BC31132:e4|}|}|} as {|BC31044:{|BC30002:Action|}|}
            end event

            shared sub new()
            end sub

            Public sub new()
            end sub

            public sub new(i as integer)
            end sub

            Public sub M1()
            end sub

            public sub M2()
            end sub

            Public function M3() as integer
            end function

            {|BC30027:Public function M4() as integer|}

            {|BC30289:public function M5() as integer|}
            end function

            partial {|BC31431:Public|} sub M6()
            end sub

            Public property P1 as integer

            Public property {|BC30124:P2|} as integer
                get
                end get
            end property

            public property P3 as integer

            Public shared operator &(c1 as C, c2 as C) as integer
            end operator
        end class

        Friend interface I
            event e6 as {|BC31044:{|BC30002:Action|}|}
            sub M3()
            function M4() as integer
            property P3 as integer
        end interface

        Friend delegate sub D1()
        Friend delegate function D2() as integer

        Friend enum E
            EMember
        end enum

        Friend structure S
            Public f as integer

            Public sub M()
            end sub

            Public shared operator &(c1 as S, c2 as S) as integer
            end operator
        end structure

        Friend module M
            Private f as integer

            Public sub M()
            end sub
        end module
    end namespace
end namespace")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)>
        Public Async Function TestAllConstructsWithOmit() As Task
            Dim source = "
namespace N
    namespace Outer.Inner
        Friend class [|C|]
            Public class [|NestedClass|]
            end class

            Public structure [|NestedStruct|]
            end structure

            Private [|f1|] as integer
            Private [|f2|], f3 as integer
            Private [|f4|], f5 as integer, f6, f7 as boolean
            public {|BC30260:f4|} as integer

            Private Const [|foo|] As long = 3
            private const [|bar|] = 4, barbar = 5

            public Const pfoo As long = 3
            public Const pbar = 4, pbarbar As ULong = 5

            Private Shared [|sfoo|] = 4
            private shared [|sbar|] as Long = 5, sbarbar = 0

            public Shared spfoo = 4
            public Shared spbar = 4, spbarbar as Long = 4

            Public event [|e1|] as {|BC31044:{|BC30002:Action|}|}
            public event [|e2|] as {|BC31044:{|BC30002:Action|}|}

            Public custom event {|BC31130:{|BC31131:{|BC31132:[|e4|]|}|}|} as {|BC31044:{|BC30002:Action|}|}
            end event

            shared sub new()
            end sub

            Public sub [|new|]()
            end sub

            public sub [|new|](i as integer)
            end sub

            Public sub [|M1|]()
            end sub

            public sub [|M2|]()
            end sub

            Public function [|M3|]() as integer
            end function

            {|BC30027:Public function [|M4|]() as integer|}

            {|BC30289:public function [|M5|]() as integer|}
            end function

            Private partial sub M6()
            end sub

            Public property [|P1|] as integer

            Public property {|BC30124:[|P2|]|} as integer
                get
                end get
            end property

            public property [|P3|] as integer

            Public shared operator [|&|](c1 as C, c2 as C) as integer
            end operator
        end class

        Friend interface [|I|]
            event e6 as {|BC31044:{|BC30002:Action|}|}
            sub M3()
            function M4() as integer
            property P3 as integer
        end interface

        Friend delegate sub [|D1|]()
        Friend delegate function [|D2|]() as integer

        Friend enum [|E|]
            EMember
        end enum

        Friend structure [|S|]
            Public [|f|] as integer

            Public sub [|M|]()
            end sub

            Public shared operator [|&|](c1 as S, c2 as S) as integer
            end operator
        end structure

        Friend module [|M|]
            Private [|f|] as integer

            Public sub [|M|]()
            end sub
        end module
    end namespace
end namespace"
            Dim fixedSource = "
namespace N
    namespace Outer.Inner
        class C
            class NestedClass
            end class

            structure NestedStruct
            end structure

            Dim f1 as integer
            Dim f2, f3 as integer
            Dim f4, f5 as integer, f6, f7 as boolean
            public {|BC30260:f4|} as integer

            Const foo As long = 3
            const bar = 4, barbar = 5

            public Const pfoo As long = 3
            public Const pbar = 4, pbarbar As ULong = 5

            Shared sfoo = 4
            shared sbar as Long = 5, sbarbar = 0

            public Shared spfoo = 4
            public Shared spbar = 4, spbarbar as Long = 4

            event e1 as {|BC31044:{|BC30002:Action|}|}
            event e2 as {|BC31044:{|BC30002:Action|}|}

            custom event {|BC31130:{|BC31131:{|BC31132:e4|}|}|} as {|BC31044:{|BC30002:Action|}|}
            end event

            shared sub new()
            end sub

            sub new()
            end sub

            sub new(i as integer)
            end sub

            sub M1()
            end sub

            sub M2()
            end sub

            function M3() as integer
            end function

            {|BC30027:function M4() as integer|}

            {|BC30289:function M5() as integer|}
            end function

            Private partial sub M6()
            end sub

            property P1 as integer

            property {|BC30124:P2|} as integer
                get
                end get
            end property

            property P3 as integer

            shared operator &(c1 as C, c2 as C) as integer
            end operator
        end class

        interface I
            event e6 as {|BC31044:{|BC30002:Action|}|}
            sub M3()
            function M4() as integer
            property P3 as integer
        end interface

        delegate sub D1()
        delegate function D2() as integer

        enum E
            EMember
        end enum

        structure S
            Dim f as integer

            sub M()
            end sub

            shared operator &(c1 as S, c2 as S) as integer
            end operator
        end structure

        module M
            Dim f as integer

            sub M()
            end sub
        end module
    end namespace
end namespace"

            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.Options.Add(CodeStyleOptions2.RequireAccessibilityModifiers, AccessibilityModifiersRequired.OmitIfDefault)

            Await test.RunAsync()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAccessibilityModifiers)>
        <WorkItem(44076, "https://github.com/dotnet/roslyn/issues/44076")>
        Public Async Function TestModuleConstructor() As Task
            Dim source = "
Friend Module Example
    Sub New()
    End Sub
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(source, source)
        End Function
    End Class
End Namespace
