' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp

    Public Class CodeNamespaceTests
        Inherits AbstractCodeNamespaceTests

#Region "Remove tests"

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Remove1()
            Dim code =
<Code>
namespace $$Foo
{
    class C
    {
    }
}
</Code>

            Dim expected =
<Code>
namespace Foo
{
}
</Code>

            TestRemoveChild(code, expected, "C")
        End Sub

#End Region

        <WorkItem(858153)>
        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Children1()
            Dim code =
<Code>
namespace N$$
{
    class C1 { }
    class C2 { }
    class C3 { }
}
</Code>

            TestChildren(code,
                         IsElement("C1", EnvDTE.vsCMElement.vsCMElementClass),
                         IsElement("C2", EnvDTE.vsCMElement.vsCMElementClass),
                         IsElement("C3", EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

    End Class
End Namespace
