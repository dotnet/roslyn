﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.VisualStudio.Debugger
Imports Microsoft.VisualStudio.Debugger.Clr
Imports System.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend NotInheritable Class VisualBasicInstructionDecoder : Inherits InstructionDecoder(Of VisualBasicCompilation, MethodSymbol, PEModuleSymbol, TypeSymbol, TypeParameterSymbol)

        ' These strings were not localized in the old EE.  We'll keep them that way
        ' so as not to break consumers who may have been parsing frame names...
        Private Const s_closureDisplayName As String = "<closure>"
        Private Const s_lambdaDisplayName As String = "<lambda{0}>"

        ''' <summary>
        ''' Singleton instance of <see cref="VisualBasicInstructionDecoder"/> (created using default constructor).
        ''' </summary>
        Friend Shared ReadOnly Instance As VisualBasicInstructionDecoder = New VisualBasicInstructionDecoder()

        Private Sub New()
        End Sub

        Friend Overrides Sub AppendFullName(builder As StringBuilder, method As MethodSymbol)
            Dim parts = method.ToDisplayParts(DisplayFormat)
            Dim numParts = parts.Length
            For i = 0 To numParts - 1
                Dim part = parts(i)
                Dim displayString = part.ToString()
                Select Case part.Kind
                    Case SymbolDisplayPartKind.ClassName
                        If Not displayString.StartsWith(StringConstants.DisplayClassPrefix, StringComparison.Ordinal) Then
                            builder.Append(displayString)
                        Else
                            ' Drop any remaining display class name parts and the subsequent dot...
                            Do
                                i += 1
                            Loop While ((i < numParts) AndAlso parts(i).Kind <> SymbolDisplayPartKind.MethodName)
                            i -= 1
                        End If
                    Case SymbolDisplayPartKind.MethodName
                        If displayString.StartsWith(StringConstants.LambdaMethodNamePrefix, StringComparison.Ordinal) Then
                            builder.Append(s_closureDisplayName)
                            builder.Append("."c)
                            ' NOTE: The old implementation only appended the first ordinal number.  Since this is not useful
                            ' in uniquely identifying the lambda, we'll append the entire ordinal suffix (which may contain
                            ' multiple numbers, as well as '-' or '_').
                            builder.AppendFormat(s_lambdaDisplayName, displayString.Substring(StringConstants.LambdaMethodNamePrefix.Length))
                        Else
                            builder.Append(displayString)
                        End If
                    Case SymbolDisplayPartKind.PropertyName
                        builder.Append(method.Name)
                    Case Else
                        builder.Append(displayString)
                End Select
            Next
        End Sub

        Friend Overrides Function ConstructMethod(method As MethodSymbol, typeParameters As ImmutableArray(Of TypeParameterSymbol), typeArguments As ImmutableArray(Of TypeSymbol)) As MethodSymbol
            Dim methodArity = method.Arity
            Dim methodArgumentStartIndex = typeParameters.Length - methodArity
            Dim typeMap = TypeSubstitution.Create(
                method,
                ImmutableArray.Create(typeParameters, 0, methodArgumentStartIndex),
                ImmutableArray.Create(typeArguments, 0, methodArgumentStartIndex))
            Dim substitutedType = typeMap.SubstituteNamedType(method.ContainingType)
            method = method.AsMember(substitutedType)
            If methodArity > 0 Then
                method = method.Construct(ImmutableArray.Create(typeArguments, methodArgumentStartIndex, methodArity))
            End If
            Return method
        End Function

        Friend Overrides Function GetAllTypeParameters(method As MethodSymbol) As ImmutableArray(Of TypeParameterSymbol)
            Return method.GetAllTypeParameters()
        End Function

        Friend Overrides Function GetCompilation(moduleInstance As DkmClrModuleInstance) As VisualBasicCompilation
            Dim appDomain = moduleInstance.AppDomain
            Dim previous = appDomain.GetMetadataContext(Of VisualBasicMetadataContext)()
            Dim metadataBlocks = moduleInstance.RuntimeInstance.GetMetadataBlocks(appDomain, previous.MetadataBlocks)

            Dim compilation As VisualBasicCompilation
            If previous.Matches(metadataBlocks) Then
                compilation = previous.Compilation
            Else
                compilation = metadataBlocks.ToCompilation()
                appDomain.SetMetadataContext(New VisualBasicMetadataContext(metadataBlocks, compilation))
            End If

            Return compilation
        End Function

        Friend Overrides Function GetMethod(compilation As VisualBasicCompilation, instructionAddress As DkmClrInstructionAddress) As MethodSymbol
            Return compilation.GetSourceMethod(instructionAddress.ModuleInstance.Mvid, instructionAddress.MethodId.Token)
        End Function

        Friend Overrides Function GetTypeNameDecoder(compilation As VisualBasicCompilation, method As MethodSymbol) As TypeNameDecoder(Of PEModuleSymbol, TypeSymbol)
            Debug.Assert(TypeOf method Is PEMethodSymbol)
            Return New EETypeNameDecoder(compilation, DirectCast(method.ContainingModule, PEModuleSymbol))
        End Function

    End Class

End Namespace
