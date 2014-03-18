' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class Symbol
        Implements Microsoft.Cci.IReference

        Friend Overridable Function IReferenceAsDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IDefinition _
            Implements Microsoft.Cci.IReference.AsDefinition

            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overridable Sub IReferenceDispatch(visitor As Microsoft.Cci.MetadataVisitor) _
            Implements Microsoft.Cci.IReference.Dispatch

            Throw ExceptionUtilities.Unreachable
        End Sub

        ''' <summary>
        ''' Return whether the symbol is either the original definition
        ''' or distinct from the original. Intended for use in Debug.Assert
        ''' only since it may include a deep comparison.
        ''' </summary>
        Friend Function IsDefinitionOrDistinct() As Boolean
            Return Me.IsDefinition OrElse Not Me.Equals(Me.OriginalDefinition)
        End Function

        Private Function IReferenceGetAttributes(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of Microsoft.Cci.ICustomAttribute) Implements Microsoft.Cci.IReference.GetAttributes
            Return GetCustomAttributesToEmit()
        End Function

        Friend Overridable Function GetCustomAttributesToEmit() As IEnumerable(Of VisualBasicAttributeData)
            Return GetCustomAttributesToEmit(emittingAssemblyAttributesInNetModule:=False)
        End Function

        Friend Function GetCustomAttributesToEmit(emittingAssemblyAttributesInNetModule As Boolean) As IEnumerable(Of VisualBasicAttributeData)
            Dim synthesized As ArrayBuilder(Of SynthesizedAttributeData) = Nothing
            AddSynthesizedAttributes(synthesized)
            Return GetCustomAttributesToEmit(Me.GetAttributes(), synthesized, isReturnType:=False, emittingAssemblyAttributesInNetModule:=emittingAssemblyAttributesInNetModule)
        End Function

        ''' <summary> 
        ''' Returns a list of attributes to emit to CustomAttribute table.
        '''  </summary>
        Friend Iterator Function GetCustomAttributesToEmit(userDefined As ImmutableArray(Of VisualBasicAttributeData),
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

End Namespace
