' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module AnonymousTypeExtensions
        <Extension()>
        Friend Function IsSubDescription(fields As ImmutableArray(Of AnonymousTypeField)) As Boolean
            Return fields.Last().Name Is AnonymousTypeDescriptor.SubReturnParameterName
        End Function

    End Module

End Namespace
