' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.ArgumentProviders
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class DefaultArgumentProviderTests
        Inherits AbstractVisualBasicArgumentProviderTests

        Friend Overrides Function GetArgumentProviderType() As Type
            Return GetType(DefaultArgumentProvider)
        End Function

        <Theory>
        <InlineData("String")>
        <InlineData("Integer?")>
        Public Async Function TestDefaultValueIsNothing(type As String) As Task
            Dim markup = $"
Class C
    Sub Method()
        Me.Target($$)
    End Sub

    Sub Target(arg As {type})
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, "Nothing")
            Await VerifyDefaultValueAsync(markup, expectedDefaultValue:="prior", previousDefaultValue:="prior")
        End Function

        <Theory>
        <InlineData("Boolean", "False")>
        <InlineData("System.Boolean", "False")>
        <InlineData("Single", "0.0F")>
        <InlineData("System.Single", "0.0F")>
        <InlineData("Double", "0.0")>
        <InlineData("System.Double", "0.0")>
        <InlineData("Decimal", "0.0D")>
        <InlineData("System.Decimal", "0.0D")>
        <InlineData("Char", "Chr(0)")>
        <InlineData("System.Char", "Chr(0)")>
        <InlineData("Byte", "CByte(0)")>
        <InlineData("System.Byte", "CByte(0)")>
        <InlineData("SByte", "CSByte(0)")>
        <InlineData("System.SByte", "CSByte(0)")>
        <InlineData("Short", "0S")>
        <InlineData("System.Int16", "0S")>
        <InlineData("UShort", "0US")>
        <InlineData("System.UInt16", "0US")>
        <InlineData("Integer", "0")>
        <InlineData("System.Int32", "0")>
        <InlineData("UInteger", "0U")>
        <InlineData("System.UInt32", "0U")>
        <InlineData("Long", "0L")>
        <InlineData("System.Int64", "0L")>
        <InlineData("ULong", "0UL")>
        <InlineData("System.UInt64", "0UL")>
        Public Async Function TestDefaultValueIsZero(type As String, literalZero As String) As Task
            Dim markup = $"
Class C
    Sub Method()
        Me.Target($$)
    End Sub

    Sub Target(arg As {type})
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, literalZero)
            Await VerifyDefaultValueAsync(markup, expectedDefaultValue:="prior", previousDefaultValue:="prior")
        End Function
    End Class
End Namespace
