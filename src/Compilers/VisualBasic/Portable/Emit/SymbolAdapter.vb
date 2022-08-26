' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

#If DEBUG Then
    Partial Friend MustInherit Class SymbolAdapter
#Else
    Partial Friend Class Symbol
#End If
        Implements Cci.IReference

        Friend Overridable Function IReferenceAsDefinition(context As EmitContext) As Cci.IDefinition _
            Implements Cci.IReference.AsDefinition

            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function IReferenceGetInternalSymbol() As CodeAnalysis.Symbols.ISymbolInternal Implements Cci.IReference.GetInternalSymbol
            Return AdaptedSymbol
        End Function

        Friend Overridable Sub IReferenceDispatch(visitor As Cci.MetadataVisitor) _
            Implements Cci.IReference.Dispatch

            Throw ExceptionUtilities.Unreachable
        End Sub

        Private Function IReferenceGetAttributes(context As EmitContext) As IEnumerable(Of Cci.ICustomAttribute) Implements Cci.IReference.GetAttributes
            Return AdaptedSymbol.GetCustomAttributesToEmit(DirectCast(context.Module, PEModuleBuilder))
        End Function
    End Class

    Partial Friend Class Symbol
#If DEBUG Then
        Friend Function GetCciAdapter() As SymbolAdapter
            Return GetCciAdapterImpl()
        End Function

        Protected Overridable Function GetCciAdapterImpl() As SymbolAdapter
            Throw ExceptionUtilities.Unreachable
        End Function
#Else
        Friend ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return Me
            End Get
        End Property

        Friend Function GetCciAdapter() As Symbol
            Return Me
        End Function
#End If

        Private Function ISymbolInternalGetCciAdapter() As Cci.IReference Implements CodeAnalysis.Symbols.ISymbolInternal.GetCciAdapter
            Return GetCciAdapter()
        End Function

        ''' <summary>
        ''' Return whether the symbol is either the original definition
        ''' or distinct from the original. Intended for use in Debug.Assert
        ''' only since it may include a deep comparison.
        ''' </summary>
        Friend Function IsDefinitionOrDistinct() As Boolean
            Return Me.IsDefinition OrElse Not Me.Equals(Me.OriginalDefinition)
        End Function

        Friend Overridable Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return GetCustomAttributesToEmit(moduleBuilder, emittingAssemblyAttributesInNetModule:=False)
        End Function

        Friend Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder, emittingAssemblyAttributesInNetModule As Boolean) As IEnumerable(Of VisualBasicAttributeData)
            Debug.Assert(Me.Kind <> SymbolKind.Assembly)

            Dim synthesized As ArrayBuilder(Of SynthesizedAttributeData) = Nothing
            AddSynthesizedAttributes(moduleBuilder, synthesized)
            Return GetCustomAttributesToEmit(Me.GetAttributes(), synthesized, isReturnType:=False, emittingAssemblyAttributesInNetModule:=emittingAssemblyAttributesInNetModule)
        End Function

        ''' <summary> 
        ''' Returns a list of attributes to emit to CustomAttribute table.
        ''' </summary>
        Friend Function GetCustomAttributesToEmit(userDefined As ImmutableArray(Of VisualBasicAttributeData),
                                                  synthesized As ArrayBuilder(Of SynthesizedAttributeData),
                                                  isReturnType As Boolean,
                                                  emittingAssemblyAttributesInNetModule As Boolean) As IEnumerable(Of VisualBasicAttributeData)

            ' PERF: Avoid creating an iterator for the common case of no attributes.
            If userDefined.IsEmpty AndAlso synthesized Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of VisualBasicAttributeData)()
            End If

            Return GetCustomAttributesToEmitIterator(userDefined, synthesized, isReturnType, emittingAssemblyAttributesInNetModule)
        End Function

        Private Iterator Function GetCustomAttributesToEmitIterator(userDefined As ImmutableArray(Of VisualBasicAttributeData),
                                                  synthesized As ArrayBuilder(Of SynthesizedAttributeData),
                                                  isReturnType As Boolean,
                                                  emittingAssemblyAttributesInNetModule As Boolean) As IEnumerable(Of VisualBasicAttributeData)

            If synthesized IsNot Nothing Then
                For Each attribute In synthesized
                    Debug.Assert(attribute.ShouldEmitAttribute(Me, isReturnType, emittingAssemblyAttributesInNetModule:=False))
                    Yield attribute
                Next

                synthesized.Free()
            End If

            For i = 0 To userDefined.Length - 1
                Dim attribute As VisualBasicAttributeData = userDefined(i)

                If Me.Kind = SymbolKind.Assembly Then
                    ' We need to filter out duplicate assembly attributes,
                    ' i.e. attributes that bind to the same constructor and have identical arguments.
                    If DirectCast(Me, SourceAssemblySymbol).IsIndexOfDuplicateAssemblyAttribute(i) Then
                        Continue For
                    End If
                End If

                If attribute.ShouldEmitAttribute(Me, isReturnType, emittingAssemblyAttributesInNetModule) Then
                    Yield attribute
                End If
            Next
        End Function

        ''' <summary>
        ''' Checks if this symbol is a definition and its containing module is a SourceModuleSymbol.
        ''' </summary>
        <Conditional("DEBUG")>
        Protected Friend Sub CheckDefinitionInvariant()
            ' can't be generic instantiation
            Debug.Assert(Me.IsDefinition)

            ' must be declared in the module we are building
            Debug.Assert(TypeOf Me.ContainingModule Is SourceModuleSymbol)
        End Sub
    End Class

#If DEBUG Then
    Partial Friend Class SymbolAdapter
        Friend MustOverride ReadOnly Property AdaptedSymbol As Symbol

        Public NotOverridable Overrides Function ToString() As String
            Return AdaptedSymbol.ToString()
        End Function

        Public NotOverridable Overrides Function Equals(obj As Object) As Boolean
            ' It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
        End Function

        Public NotOverridable Overrides Function GetHashCode() As Integer
            ' It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
        End Function

        <Conditional("DEBUG")>
        Protected Friend Sub CheckDefinitionInvariant()
            AdaptedSymbol.CheckDefinitionInvariant()
        End Sub

        Friend Function IsDefinitionOrDistinct() As Boolean
            Return AdaptedSymbol.IsDefinitionOrDistinct()
        End Function
    End Class
#End If

End Namespace
