' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
