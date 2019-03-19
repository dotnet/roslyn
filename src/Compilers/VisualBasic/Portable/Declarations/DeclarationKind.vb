' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module EnumConversions
        <Extension()>
        Public Function ToTypeKind(kind As DeclarationKind) As TypeKind
            Select Case kind
                Case DeclarationKind.Class,
                     DeclarationKind.Script,
                     DeclarationKind.ImplicitClass
                    Return TypeKind.Class

                Case DeclarationKind.Submission
                    Return TypeKind.Submission

                Case DeclarationKind.Delegate
                Case DeclarationKind.EventSyntheticDelegate
                    Return TypeKind.Delegate

                Case DeclarationKind.Enum
                    Return TypeKind.Enum

                Case DeclarationKind.Interface
                    Return TypeKind.Interface

                Case DeclarationKind.Structure
                    Return TypeKind.Structure

                Case DeclarationKind.Module
                    Return TypeKind.Module
            End Select

            Throw ExceptionUtilities.UnexpectedValue(kind)
        End Function
    End Module

    Friend Enum DeclarationKind As Byte
        [Namespace]
        [Class]
        [Interface]
        [Structure]
        [Enum]
        [Delegate]
        [Module]
        Script
        Submission
        ImplicitClass
        EventSyntheticDelegate
    End Enum

End Namespace
