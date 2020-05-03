' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A specific location for binding.
    ''' </summary>
    Friend Enum BindingLocation
        None ' No specific location
        BaseTypes
        MethodSignature ' method parameters and return type
        GenericConstraintsClause ' "T As {...}"
        ProjectImportsDeclaration
        SourceFileImportsDeclaration
        Attribute
        EventSignature
        FieldType
        HandlesClause
        PropertySignature
        PropertyAccessorSignature
        EventAccessorSignature
    End Enum

End Namespace
