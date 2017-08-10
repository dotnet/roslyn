' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestNotInLeadingWhitespace() As Task
            Dim markup = "
class C
    [||]
    sub Goo(i as integer, j as integer)
    end sub
end class
"

            Await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction:=False)
        End Function

        <WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestNotInLeadingTrivia1() As Task
            Dim markup = "
class C
    ' [||]
    sub Goo(i as integer, j as integer)
    end sub
end class
"

            Await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction:=False)
        End Function

        <WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestNotInLeadingTrivia2() As Task
            Dim markup = "
class C
    [||] '
    sub Goo(i as integer, j as integer)
    end sub
end class
"

            Await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction:=False)
        End Function

        <WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestNotInLeadingAttributes1() As Task
            Dim markup = "
class C
    [||]<X>
    sub Goo(i as integer, j as integer)
    end sub
end class
"

            Await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction:=False)
        End Function

        <WorkItem(17309, "https://github.com/dotnet/roslyn/issues/17309")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestNotInLeadingAttributes2() As Task
            Dim markup = "
class C
    <X>[||]
    sub Goo(i as integer, j as integer)
    end sub
end class
"

            Await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction:=False)
        End Function
    End Class
End Namespace
