' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a compiler generated field for a "MyGroupCollection" property.
    ''' </summary>
    Friend Class SynthesizedMyGroupCollectionPropertyBackingFieldSymbol
        Inherits SynthesizedFieldSymbol

        Public Sub New(
            containingType As NamedTypeSymbol,
            implicitlyDefinedBy As Symbol,
            type As TypeSymbol,
            name As String
        )
            ' This backing field must be public because Is/IsNot operator replaces references to properties with
            ' references to fields in order to avoid allocation of instances. 
            MyBase.New(containingType, implicitlyDefinedBy, type, name, Accessibility.Public, isReadOnly:=False, isShared:=False)
        End Sub

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return LexicalSortKey.NotInSource
        End Function

        Friend Overrides Sub AddSynthesizedAttributes(compilationState as ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            AddSynthesizedAttribute(attributes, Me.DeclaringCompilation.SynthesizeEditorBrowsableNeverAttribute())
        End Sub

    End Class

End Namespace
