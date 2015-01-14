' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports System.Threading

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Interface IAttributeTargetSymbol
        ''' <summary>
        ''' Attribute location corresponding to this symbol.
        ''' </summary>
        ''' <remarks>
        ''' Location of an attribute if an explicit location is not specified via attribute target specification syntax.
        ''' </remarks>
        ReadOnly Property DefaultAttributeLocation As AttributeLocation
    End Interface
End Namespace

