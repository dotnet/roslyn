' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class IgnoreAccessibilityBinder
        Inherits Binder

        Public Sub New(containingBinder As Binder)
            MyBase.New(containingBinder)
        End Sub

        Friend Overrides Function BinderSpecificLookupOptions(options As LookupOptions) As LookupOptions
            Return ContainingBinder.BinderSpecificLookupOptions(options) Or LookupOptions.IgnoreAccessibility
        End Function

        Public Overrides Function CheckAccessibility(sym As Symbol, <[In]> <Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo), Optional accessThroughType As TypeSymbol = Nothing, Optional basesBeingResolved As BasesBeingResolved = Nothing) As AccessCheckResult
            Return AccessCheckResult.Accessible
        End Function
    End Class
End Namespace
