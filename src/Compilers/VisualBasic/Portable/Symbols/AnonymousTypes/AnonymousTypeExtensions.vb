' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
