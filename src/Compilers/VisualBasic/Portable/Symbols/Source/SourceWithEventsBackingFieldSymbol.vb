' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a backing field of WithEvents property. 
    ''' Attributes applied on the property syntax are applied on the backing field.
    ''' </summary>
    Friend NotInheritable Class SourceWithEventsBackingFieldSymbol
        Inherits SourceMemberFieldSymbol

        Private ReadOnly _property As SourcePropertySymbol

        Public Sub New([property] As SourcePropertySymbol,
                       syntaxRef As SyntaxReference,
                       name As String)

            MyBase.New([property].ContainingSourceType,
                       syntaxRef,
                       name,
                       SourceMemberFlags.AccessibilityPrivate Or If([property].IsShared, SourceMemberFlags.Shared, SourceMemberFlags.None))

            _property = [property]
        End Sub

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return _property
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplicitlyDefinedBy(Optional membersInProgress As Dictionary(Of String, ArrayBuilder(Of Symbol)) = Nothing) As Symbol
            Get
                Return _property
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            Dim compilation = _property.DeclaringCompilation

            Debug.Assert(Not Me.ContainingType.IsImplicitlyDeclared)

            AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

            AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerBrowsableNeverAttribute())

            AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
               WellKnownMember.System_Runtime_CompilerServices_AccessedThroughPropertyAttribute__ctor,
               ImmutableArray.Create(New TypedConstant(compilation.GetSpecialType(SpecialType.System_String),
                                                               TypedConstantKind.Primitive,
                                                               AssociatedSymbol.Name))))
        End Sub
    End Class
End Namespace
