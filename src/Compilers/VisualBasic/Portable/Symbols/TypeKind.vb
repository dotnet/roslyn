﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module EnumConversions
        <Extension()>
        Friend Function ToCommon(kind As TypeKind) As TypeKind
            Select Case kind
                Case TypeKind.Class
                    Return TypeKind.Class
                Case TypeKind.Delegate
                    Return TypeKind.Delegate
                Case TypeKind.Enum
                    Return TypeKind.Enum
                Case TypeKind.Error
                    Return TypeKind.Error
                Case TypeKind.Interface
                    Return TypeKind.Interface
                Case TypeKind.Structure
                    Return TypeKind.Struct
                Case TypeKind.Module
                    Return TypeKind.Module
                Case TypeKind.Array
                    Return TypeKind.Array
                Case TypeKind.TypeParameter
                    Return TypeKind.TypeParameter
                Case TypeKind.Submission
                    Return TypeKind.Submission
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function
    End Module
End Namespace
