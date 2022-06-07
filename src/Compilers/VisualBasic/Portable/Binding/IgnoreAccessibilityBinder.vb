' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Public Overrides Function CheckAccessibility(sym As Symbol, <[In]> <Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol), Optional accessThroughType As TypeSymbol = Nothing, Optional basesBeingResolved As BasesBeingResolved = Nothing) As AccessCheckResult
            Return AccessCheckResult.Accessible
        End Function
    End Class
End Namespace
