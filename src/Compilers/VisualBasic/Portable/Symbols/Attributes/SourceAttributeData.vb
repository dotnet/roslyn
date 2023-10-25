' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Reflection.Metadata

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class SourceAttributeData
        Inherits VisualBasicAttributeData

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _attributeClass As NamedTypeSymbol ' TODO - Remove attribute class. It is available from the constructor.
        Private ReadOnly _attributeConstructor As MethodSymbol
        Private ReadOnly _constructorArguments As ImmutableArray(Of TypedConstant)
        Private ReadOnly _namedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
        Private ReadOnly _isConditionallyOmitted As Boolean
        Private ReadOnly _hasErrors As Boolean
        Private ReadOnly _applicationNode As SyntaxReference

        Friend Sub New(ByVal compilation As VisualBasicCompilation,
                       ByVal applicationNode As SyntaxReference,
                       ByVal attrClass As NamedTypeSymbol,
                       ByVal attrMethod As MethodSymbol,
                       ByVal constructorArgs As ImmutableArray(Of TypedConstant),
                       ByVal namedArgs As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)),
                       ByVal isConditionallyOmitted As Boolean,
                       ByVal hasErrors As Boolean)
            Debug.Assert(compilation IsNot Nothing)
            Debug.Assert(applicationNode IsNot Nothing)
            Debug.Assert(attrMethod IsNot Nothing OrElse hasErrors)

            Me._compilation = compilation
            Me._applicationNode = applicationNode
            Me._attributeClass = attrClass
            Me._attributeConstructor = attrMethod
            Me._constructorArguments = constructorArgs.NullToEmpty()
            Me._namedArguments = If(namedArgs.IsDefault, ImmutableArray.Create(Of KeyValuePair(Of String, TypedConstant))(), namedArgs)
            Me._isConditionallyOmitted = isConditionallyOmitted
            Me._hasErrors = hasErrors
        End Sub

        Public Overrides ReadOnly Property AttributeClass As NamedTypeSymbol
            Get
                Return _attributeClass
            End Get
        End Property

        Public Overrides ReadOnly Property AttributeConstructor As MethodSymbol
            Get
                Return _attributeConstructor
            End Get
        End Property

        Public Overrides ReadOnly Property ApplicationSyntaxReference As SyntaxReference
            Get
                Return _applicationNode
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonConstructorArguments As ImmutableArray(Of TypedConstant)
            Get
                Return _constructorArguments
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonNamedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
            Get
                Return _namedArguments
            End Get
        End Property

        Friend Overrides ReadOnly Property IsConditionallyOmitted As Boolean
            Get
                Return _isConditionallyOmitted
            End Get
        End Property

        Friend Function WithOmittedCondition(isConditionallyOmitted As Boolean) As SourceAttributeData
            If Me.IsConditionallyOmitted = isConditionallyOmitted Then
                Return Me
            End If

            Return New SourceAttributeData(Me._compilation,
                                           Me.ApplicationSyntaxReference,
                                           Me.AttributeClass,
                                           Me.AttributeConstructor,
                                           Me.CommonConstructorArguments,
                                           Me.CommonNamedArguments,
                                           isConditionallyOmitted,
                                           Me.HasErrors)
        End Function

        Friend Overrides ReadOnly Property HasErrors As Boolean
            Get
                Return _hasErrors
            End Get
        End Property

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return Nothing ' Binder reports errors
            End Get
        End Property

        ''' <summary>
        ''' This method finds an attribute by metadata name and signature. The algorithm for signature matching is similar to the one
        ''' in Module.GetTargetAttributeSignatureIndex. Note, the signature matching is limited to primitive types
        ''' and System.Type.  It will not match an arbitrary signature but it is sufficient to match the signatures of the current set of
        ''' well known attributes.
        ''' </summary>
        ''' <param name="description">Attribute to match.</param>
        Friend Overrides Function GetTargetAttributeSignatureIndex(description As AttributeDescription) As Integer
            Return GetTargetAttributeSignatureIndex(_compilation, AttributeClass, AttributeConstructor, description)
        End Function

        Friend Overloads Shared Function GetTargetAttributeSignatureIndex(compilation As VisualBasicCompilation, attributeClass As NamedTypeSymbol, attributeConstructor As MethodSymbol, description As AttributeDescription) As Integer
            If Not IsTargetAttribute(attributeClass, description.Namespace, description.Name, description.MatchIgnoringCase) Then
                Return -1
            End If

            Dim lazySystemType As TypeSymbol = Nothing

            Dim ctor = attributeConstructor
            ' Ensure that the attribute data really has a constructor before comparing the signature.
            If ctor Is Nothing Then
                Return -1
            End If

            Dim parameters = ctor.Parameters
            Dim foundMatch = False

            For i = 0 To description.Signatures.Length - 1
                Dim targetSignature = description.Signatures(i)
                If targetSignature(0) <> SignatureAttributes.Instance Then
                    Continue For
                End If

                Dim parameterCount = targetSignature(1)
                If parameterCount <> parameters.Length Then
                    Continue For
                End If

                If CType(targetSignature(2), SignatureTypeCode) <> SignatureTypeCode.Void Then
                    Continue For
                End If

                foundMatch = (targetSignature.Length = 3)
                Dim k = 0
                For j = 3 To targetSignature.Length - 1
                    If k >= parameters.Length Then
                        Exit For
                    End If

                    Dim parameterType As TypeSymbol = parameters(k).Type
                    Dim specType = parameterType.GetEnumUnderlyingTypeOrSelf.SpecialType
                    Dim targetType As Byte = targetSignature(j)

                    If targetType = SignatureTypeCode.TypeHandle Then
                        j += 1

                        If parameterType.Kind <> SymbolKind.NamedType AndAlso parameterType.Kind <> SymbolKind.ErrorType Then
                            foundMatch = False
                            Exit For
                        End If

                        Dim namedType = DirectCast(parameterType, NamedTypeSymbol)
                        Dim targetInfo As AttributeDescription.TypeHandleTargetInfo = AttributeDescription.TypeHandleTargets(targetSignature(j))

                        ' Compare name and containing symbol name. Uses HasNameQualifier
                        ' extension method to avoid string allocations.
                        If Not String.Equals(namedType.MetadataName, targetInfo.Name, StringComparison.Ordinal) OrElse
                            Not namedType.HasNameQualifier(targetInfo.Namespace, StringComparison.Ordinal) Then
                            foundMatch = False
                            Exit For
                        End If

                        targetType = CByte(targetInfo.Underlying)

                    ElseIf parameterType.IsArrayType Then
                        specType = DirectCast(parameterType, ArrayTypeSymbol).ElementType.SpecialType

                    End If

                    Select Case targetType
                        Case CByte(SignatureTypeCode.Boolean)
                            foundMatch = specType = SpecialType.System_Boolean
                            k += 1

                        Case CByte(SignatureTypeCode.Char)
                            foundMatch = specType = SpecialType.System_Char
                            k += 1

                        Case CByte(SignatureTypeCode.SByte)
                            foundMatch = specType = SpecialType.System_SByte
                            k += 1

                        Case CByte(SignatureTypeCode.Byte)
                            foundMatch = specType = SpecialType.System_Byte
                            k += 1

                        Case CByte(SignatureTypeCode.Int16)
                            foundMatch = specType = SpecialType.System_Int16
                            k += 1

                        Case CByte(SignatureTypeCode.UInt16)
                            foundMatch = specType = SpecialType.System_UInt16
                            k += 1

                        Case CByte(SignatureTypeCode.Int32)
                            foundMatch = specType = SpecialType.System_Int32
                            k += 1

                        Case CByte(SignatureTypeCode.UInt32)
                            foundMatch = specType = SpecialType.System_UInt32
                            k += 1

                        Case CByte(SignatureTypeCode.Int64)
                            foundMatch = specType = SpecialType.System_Int64
                            k += 1

                        Case CByte(SignatureTypeCode.UInt64)
                            foundMatch = specType = SpecialType.System_UInt64
                            k += 1

                        Case CByte(SignatureTypeCode.Single)
                            foundMatch = specType = SpecialType.System_Single
                            k += 1

                        Case CByte(SignatureTypeCode.Double)
                            foundMatch = specType = SpecialType.System_Double
                            k += 1

                        Case CByte(SignatureTypeCode.String)
                            foundMatch = specType = SpecialType.System_String
                            k += 1

                        Case CByte(SignatureTypeCode.Object)
                            foundMatch = specType = SpecialType.System_Object
                            k += 1

                        Case CByte(SerializationTypeCode.Type)
                            If lazySystemType Is Nothing Then
                                lazySystemType = compilation.GetWellKnownType(WellKnownType.System_Type)
                            End If

                            foundMatch = TypeSymbol.Equals(parameterType, lazySystemType, TypeCompareKind.ConsiderEverything)
                            k += 1

                        Case CByte(SignatureTypeCode.SZArray)
                            ' skip over and check the next byte
                            foundMatch = parameterType.IsArrayType

                        Case Else
                            Return -1
                    End Select

                    If Not foundMatch Then
                        Exit For
                    End If
                Next

                If foundMatch Then
                    Return i
                End If
            Next

            Debug.Assert(Not foundMatch)
            Return -1
        End Function

        ''' <summary>
        ''' Compares the namespace and type name with the attribute's namespace and type name.  Returns true if they are the same.
        ''' </summary>
        Friend Overrides Function IsTargetAttribute(
            namespaceName As String,
            typeName As String,
            Optional ignoreCase As Boolean = False
        ) As Boolean
            Return IsTargetAttribute(AttributeClass, namespaceName, typeName, ignoreCase)
        End Function

        Friend Overloads Shared Function IsTargetAttribute(
            attributeClass As NamedTypeSymbol,
            namespaceName As String,
            typeName As String,
            Optional ignoreCase As Boolean = False
        ) As Boolean
            If attributeClass.IsErrorType() AndAlso Not TypeOf attributeClass Is MissingMetadataTypeSymbol Then
                ' Can't guarantee complete name information.
                Return False
            End If

            Dim options As StringComparison = If(ignoreCase, StringComparison.OrdinalIgnoreCase, StringComparison.Ordinal)
            Return attributeClass.HasNameQualifier(namespaceName, options) AndAlso
                attributeClass.Name.Equals(typeName, options)
        End Function
    End Class

End Namespace

