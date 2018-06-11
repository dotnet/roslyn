' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddParameter
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.GenerateConstructor
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddParameter
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddParameter), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateConstructor)>
    Friend Class VisualBasicAddParameterCodeFixProvider
        Inherits AbstractAddParameterCodeFixProvider(Of
        ArgumentSyntax,
        ArgumentSyntax,
        ArgumentListSyntax,
        ArgumentListSyntax,
        InvocationExpressionSyntax,
        ObjectCreationExpressionSyntax)

        Const BC30057 As String = NameOf(BC30057) ' error BC30057: Too many arguments to 'Public Sub New()'.
        Const BC30272 As String = NameOf(BC30272) ' error BC30272: 'p' is not a parameter of 'Public Sub New()'.
        Const BC30274 As String = NameOf(BC30274) ' error BC30274: Parameter 'prop' of 'Public Sub New(prop As String)' already has a matching argument.
        Const BC30311 As String = NameOf(BC30311) ' error BC30311: Value of type 'String' cannot be converted to 'Exception'.
        Const BC30389 As String = NameOf(BC30389) ' error BC30389: 'x' is not accessible in this context
        Const BC30512 As String = NameOf(BC30512) ' error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        Const BC32006 As String = NameOf(BC32006) ' error BC32006: 'Char' values cannot be converted to 'Integer'. 
        Const BC30387 As String = NameOf(BC30387) ' error BC32006: Class 'Derived' must declare a 'Sub New' because its base class 'Base' does not have an accessible 'Sub New' that can be called with no arguments. 
        Const BC30516 As String = NameOf(BC30516) ' error BC30516: Overload resolution failed because no accessible 'Blah' accepts this number of arguments.
        Const BC36582 As String = NameOf(BC36582) ' error BC36582: Too many arguments to extension method 'Public Sub ExtensionM1()' defined in 'Extensions'.
        Const BC36625 As String = NameOf(BC36625) ' error BC36625: Lambda expression cannot be converted to 'Integer' because 'Integer' is not a delegate type.

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(Of String)(
            IDEDiagnosticIds.UnboundConstructorId,
            BC30057, BC30272, BC30274, BC30311, BC30389, BC30512, BC32006, BC30387, BC30516, BC36582, BC36625)

        Protected Overrides ReadOnly Property TooManyArgumentsDiagnosticIds As ImmutableArray(Of String) =
        GenerateConstructorDiagnosticIds.TooManyArgumentsDiagnosticIds

        Protected Overrides ReadOnly Property CannotConvertDiagnosticIds As ImmutableArray(Of String) =
            GenerateConstructorDiagnosticIds.CannotConvertDiagnosticIds

    End Class
End Namespace
