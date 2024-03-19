' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    Friend NotInheritable Class SymbolFactory
        Inherits SymbolFactory(Of PEModuleSymbol, TypeSymbol)

        Friend Shared ReadOnly Instance As New SymbolFactory()

        Friend Overrides Function GetMDArrayTypeSymbol(
            moduleSymbol As PEModuleSymbol,
            rank As Integer,
            elementType As TypeSymbol,
            customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol)),
            sizes As ImmutableArray(Of Integer),
            lowerBounds As ImmutableArray(Of Integer)
        ) As TypeSymbol
            If TypeOf elementType Is UnsupportedMetadataTypeSymbol Then
                Return elementType
            End If

            Return ArrayTypeSymbol.CreateMDArray(
                            elementType,
                            VisualBasicCustomModifier.Convert(customModifiers),
                            rank, sizes, lowerBounds, moduleSymbol.ContainingAssembly)
        End Function

        Friend Overrides Function GetSpecialType(moduleSymbol As PEModuleSymbol, specialType As SpecialType) As TypeSymbol
            Return moduleSymbol.ContainingAssembly.GetSpecialType(specialType)
        End Function

        Friend Overrides Function GetSystemTypeSymbol(moduleSymbol As PEModuleSymbol) As TypeSymbol
            Return moduleSymbol.SystemTypeSymbol
        End Function

        Friend Overrides Function GetEnumUnderlyingType(moduleSymbol As PEModuleSymbol, type As TypeSymbol) As TypeSymbol
            Return type.GetEnumUnderlyingType()
        End Function

        Friend Overrides Function GetPrimitiveTypeCode(moduleSymbol As PEModuleSymbol, type As TypeSymbol) As Microsoft.Cci.PrimitiveTypeCode
            Return type.PrimitiveTypeCode
        End Function

        Friend Overrides Function GetSZArrayTypeSymbol(moduleSymbol As PEModuleSymbol, elementType As TypeSymbol, customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol))) As TypeSymbol
            If TypeOf elementType Is UnsupportedMetadataTypeSymbol Then
                Return elementType
            End If

            Return ArrayTypeSymbol.CreateSZArray(
                            elementType,
                            VisualBasicCustomModifier.Convert(customModifiers),
                            moduleSymbol.ContainingAssembly)
        End Function

        Friend Overrides Function GetUnsupportedMetadataTypeSymbol(moduleSymbol As PEModuleSymbol, exception As BadImageFormatException) As TypeSymbol
            Return New UnsupportedMetadataTypeSymbol(exception)
        End Function

        Friend Overrides Function MakePointerTypeSymbol(moduleSymbol As PEModuleSymbol, type As TypeSymbol, customModifiers As ImmutableArray(Of ModifierInfo(Of TypeSymbol))) As TypeSymbol
            Return New PointerTypeSymbol(type, VisualBasicCustomModifier.Convert(customModifiers))
        End Function

        Friend Overrides Function SubstituteTypeParameters(
            moduleSymbol As PEModuleSymbol,
            genericTypeDef As TypeSymbol,
            arguments As ImmutableArray(Of KeyValuePair(Of TypeSymbol, ImmutableArray(Of ModifierInfo(Of TypeSymbol)))),
            refersToNoPiaLocalType As ImmutableArray(Of Boolean)
        ) As TypeSymbol

            If TypeOf genericTypeDef Is UnsupportedMetadataTypeSymbol Then
                Return genericTypeDef
            End If

            ' Let's return unsupported metadata type if any argument is unsupported metadata type 
            For Each arg In arguments
                If arg.Key.Kind = SymbolKind.ErrorType AndAlso
                        TypeOf arg.Key Is UnsupportedMetadataTypeSymbol Then
                    Return New UnsupportedMetadataTypeSymbol()
                End If
            Next

            Dim genericType As NamedTypeSymbol = DirectCast(genericTypeDef, NamedTypeSymbol)

            ' See if it is or its enclosing type is a non-interface closed over NoPia local types. 
            Dim linkedAssemblies As ImmutableArray(Of AssemblySymbol) = moduleSymbol.ContainingAssembly.GetLinkedReferencedAssemblies()

            Dim noPiaIllegalGenericInstantiation As Boolean = False

            If Not linkedAssemblies.IsDefaultOrEmpty OrElse moduleSymbol.Module.ContainsNoPiaLocalTypes() Then
                Dim typeToCheck As NamedTypeSymbol = genericType
                Dim argumentIndex As Integer = refersToNoPiaLocalType.Length - 1

                Do
                    If Not typeToCheck.IsInterface Then
                        Exit Do
                    Else
                        argumentIndex -= typeToCheck.Arity
                    End If

                    typeToCheck = typeToCheck.ContainingType
                Loop While typeToCheck IsNot Nothing

                For i As Integer = argumentIndex To 0 Step -1
                    If refersToNoPiaLocalType(i) OrElse
                           (Not linkedAssemblies.IsDefaultOrEmpty AndAlso
                           MetadataDecoder.IsOrClosedOverATypeFromAssemblies(arguments(i).Key, linkedAssemblies)) Then
                        noPiaIllegalGenericInstantiation = True
                        Exit For
                    End If
                Next
            End If

            ' Collect generic parameters for the type and its containers in the order
            ' that matches passed in arguments, i.e. sorted by the nesting.
            Dim genericParameters = genericType.GetAllTypeParameters()
            Debug.Assert(genericParameters.Length > 0)

            If genericParameters.Length <> arguments.Length Then
                Return New UnsupportedMetadataTypeSymbol()
            End If

            Dim substitution As TypeSubstitution = TypeSubstitution.Create(genericTypeDef, genericParameters,
                                                                           arguments.SelectAsArray(Function(pair) New TypeWithModifiers(pair.Key, VisualBasicCustomModifier.Convert(pair.Value))))

            If substitution Is Nothing Then
                Return genericTypeDef
            End If

            Dim constructedType = genericType.Construct(substitution)

            If noPiaIllegalGenericInstantiation Then
                constructedType = New NoPiaIllegalGenericInstantiationSymbol(constructedType)
            End If

            Return DirectCast(constructedType, TypeSymbol)
        End Function

        Friend Overrides Function MakeUnboundIfGeneric(moduleSymbol As PEModuleSymbol, type As TypeSymbol) As TypeSymbol
            Dim namedType = TryCast(type, NamedTypeSymbol)
            Return If(namedType IsNot Nothing AndAlso namedType.IsGenericType, UnboundGenericType.Create(namedType), type)
        End Function

        Friend Overrides Function MakeFunctionPointerTypeSymbol(moduleSymbol As PEModuleSymbol, callingConvention As Cci.CallingConvention, retAndParamTypes As ImmutableArray(Of ParamInfo(Of TypeSymbol))) As TypeSymbol
            Return New UnsupportedMetadataTypeSymbol()
        End Function
    End Class

End Namespace
