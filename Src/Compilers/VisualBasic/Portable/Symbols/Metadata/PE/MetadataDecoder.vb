' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' Helper class to resolve metadata tokens and signatures.
    ''' </summary>
    Friend Class MetadataDecoder
        Inherits MetadataDecoder(Of TypeSymbol, MethodSymbol, FieldSymbol, AssemblySymbol, Symbol)

        ''' <summary>
        ''' ModuleSymbol for the module - source of metadata.
        ''' </summary>
        Private m_ModuleSymbol As PEModuleSymbol

        ''' <summary>
        ''' Type context for resolving generic type arguments.
        ''' </summary>
        Private m_typeContextOpt As PENamedTypeSymbol

        ''' <summary>
        ''' Method context for resolving generic method type arguments.
        ''' </summary>
        Private m_methodContextOpt As PEMethodSymbol

        Public Sub New(
            moduleSymbol As PEModuleSymbol,
            context As PENamedTypeSymbol
        )
            Me.New(moduleSymbol, context, Nothing)
        End Sub

        Public Sub New(
            moduleSymbol As PEModuleSymbol,
            context As PEMethodSymbol
        )
            Me.New(moduleSymbol, DirectCast(context.ContainingType, PENamedTypeSymbol), context)
        End Sub

        Public Sub New(
            moduleSymbol As PEModuleSymbol
        )
            Me.New(moduleSymbol, Nothing, Nothing)
        End Sub

        Private Sub New(
            moduleSymbol As PEModuleSymbol,
            typeContextOpt As PENamedTypeSymbol,
            methodContextOpt As PEMethodSymbol
        )
            ' TODO (tomat): if the containing assembly is a source assembly and we are about to decode assembly level attributes, we run into a cycle,
            ' so for now ignore the assembly identity.
            MyBase.New(moduleSymbol.Module, If(TypeOf moduleSymbol.ContainingAssembly Is PEAssemblySymbol, moduleSymbol.ContainingAssembly.Identity, Nothing))

            Debug.Assert(moduleSymbol IsNot Nothing)

            m_ModuleSymbol = moduleSymbol
            m_typeContextOpt = typeContextOpt
            m_methodContextOpt = methodContextOpt
        End Sub

        Protected Overrides Function GetArrayTypeSymbol(dims As Integer, elementType As TypeSymbol) As TypeSymbol
            If TypeOf elementType Is UnsupportedMetadataTypeSymbol Then
                Return elementType
            End If

            If dims = 1 Then
                ' We do not support multi-dimensional arrays of rank 1, cannot distinguish
                ' them from SZARRAY.
                Return New UnsupportedMetadataTypeSymbol()
            End If

            Return New ArrayTypeSymbol(
                            elementType,
                            Nothing,
                            dims, m_ModuleSymbol.ContainingAssembly)
        End Function

        Protected Overrides Function GetSpecialType(specialType As SpecialType) As TypeSymbol
            Return m_ModuleSymbol.ContainingAssembly.GetSpecialType(specialType)
        End Function

        Protected Overrides ReadOnly Property SystemTypeSymbol As TypeSymbol
            Get
                Return Me.m_ModuleSymbol.SystemTypeSymbol
            End Get
        End Property

        Protected Overrides Function GetEnumUnderlyingType(type As TypeSymbol) As TypeSymbol
            Return type.GetEnumUnderlyingType()
        End Function

        Protected Overrides Function GetPrimitiveTypeCode(type As TypeSymbol) As Microsoft.Cci.PrimitiveTypeCode
            Return type.PrimitiveTypeCode
        End Function

        Protected Overrides Function IsVolatileModifierType(type As TypeSymbol) As Boolean
            ' VB doesn't deal with Volatile fields.
            Return False
        End Function

        Protected Overrides Function GetGenericMethodTypeParamSymbol(position As Integer) As TypeSymbol

            If m_methodContextOpt Is Nothing Then
                Return New UnsupportedMetadataTypeSymbol()
            End If

            Dim typeParameters = m_methodContextOpt.TypeParameters

            If typeParameters.Length <= position Then
                Return New UnsupportedMetadataTypeSymbol()
            End If

            Return typeParameters(position)
        End Function

        Protected Overrides Function GetGenericTypeParamSymbol(position As Integer) As TypeSymbol

            Dim type As PENamedTypeSymbol = m_typeContextOpt

            While type IsNot Nothing AndAlso (type.MetadataArity - type.Arity) > position
                type = TryCast(type.ContainingSymbol, PENamedTypeSymbol)
            End While

            If type Is Nothing OrElse type.MetadataArity <= position Then
                Return New UnsupportedMetadataTypeSymbol()
            End If

            position -= (type.MetadataArity - type.Arity)
            Debug.Assert(position >= 0 AndAlso position < type.Arity)

            Return type.TypeParameters(position)
        End Function

        Protected Overrides Function GetSZArrayTypeSymbol(elementType As TypeSymbol, customModifiers As ImmutableArray(Of ModifierInfo)) As TypeSymbol
            If TypeOf elementType Is UnsupportedMetadataTypeSymbol Then
                Return elementType
            End If

            Return New ArrayTypeSymbol(
                            elementType,
                            VisualBasicCustomModifier.Convert(customModifiers),
                            1,
                            m_ModuleSymbol.ContainingAssembly)
        End Function

        Protected Overrides Function GetTypeHandleToTypeMap() As ConcurrentDictionary(Of TypeHandle, TypeSymbol)
            Return m_ModuleSymbol.TypeHandleToTypeMap
        End Function

        Protected Overrides Function GetTypeRefHandleToTypeMap() As ConcurrentDictionary(Of TypeReferenceHandle, TypeSymbol)
            Return m_ModuleSymbol.TypeRefHandleToTypeMap
        End Function

        Protected Overrides Function GetUnsupportedMetadataTypeSymbol(Optional mrEx As BadImageFormatException = Nothing) As TypeSymbol
            Return New UnsupportedMetadataTypeSymbol(mrEx)
        End Function

        Protected Overrides Function GetByRefReturnTypeSymbol(referencedType As TypeSymbol) As TypeSymbol
            Return GetUnsupportedMetadataTypeSymbol() ' No special support for this scenario in VB.
        End Function

        Protected Overrides Function LookupNestedTypeDefSymbol(
            container As TypeSymbol,
            ByRef emittedName As MetadataTypeName
        ) As TypeSymbol
            Dim result = container.LookupMetadataType(emittedName)
            Debug.Assert(result IsNot Nothing)

            Return result
        End Function

        ''' <summary>
        ''' Lookup a type defined in referenced assembly.
        ''' </summary>
        Protected Overloads Overrides Function LookupTopLevelTypeDefSymbol(
            referencedAssemblyIndex As Integer,
            ByRef emittedName As MetadataTypeName
        ) As TypeSymbol
            Dim assembly As AssemblySymbol = m_ModuleSymbol.GetReferencedAssemblySymbols()(referencedAssemblyIndex)

            Return assembly.LookupTopLevelMetadataType(emittedName, digThroughForwardedTypes:=True)
        End Function

        ''' <summary>
        ''' Lookup a type defined in a module of a multi-module assembly.
        ''' </summary>
        Protected Overrides Function LookupTopLevelTypeDefSymbol(moduleName As String, ByRef emittedName As MetadataTypeName, <Out> ByRef isNoPiaLocalType As Boolean) As TypeSymbol
            For Each m As ModuleSymbol In m_ModuleSymbol.ContainingAssembly.Modules
                If String.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase) Then
                    If m Is m_ModuleSymbol Then
                        Return m_ModuleSymbol.LookupTopLevelMetadataType(emittedName, isNoPiaLocalType)
                    Else
                        isNoPiaLocalType = False
                        Return m.LookupTopLevelMetadataType(emittedName)
                    End If
                End If
            Next

            isNoPiaLocalType = False
            Return New MissingMetadataTypeSymbol.TopLevel(New MissingModuleSymbolWithName(m_ModuleSymbol.ContainingAssembly, moduleName), emittedName, SpecialType.None)
        End Function

        ''' <summary>
        ''' Lookup a type defined in this module.
        ''' This method will be called only if the type we are
        ''' looking for hasn't been loaded yet. Otherwise, MetadataDecoder
        ''' would have found the type in TypeDefRowIdToTypeMap based on its 
        ''' TypeDef row id. 
        ''' </summary>
        Protected Overloads Overrides Function LookupTopLevelTypeDefSymbol(ByRef emittedName As MetadataTypeName, <Out> ByRef isNoPiaLocalType As Boolean) As TypeSymbol
            Return m_ModuleSymbol.LookupTopLevelMetadataType(emittedName, isNoPiaLocalType)
        End Function

        Protected Overrides Function MakePointerTypeSymbol(type As TypeSymbol, customModifiers As ImmutableArray(Of ModifierInfo)) As TypeSymbol
            Return GetUnsupportedMetadataTypeSymbol()
        End Function

        Protected Overrides Function SubstituteWithUnboundIfGeneric(type As TypeSymbol) As TypeSymbol
            Dim namedType = TryCast(type, NamedTypeSymbol)
            Return If(namedType IsNot Nothing AndAlso namedType.IsGenericType, UnboundGenericType.Create(namedType), type)
        End Function

        ''' <summary>
        ''' Produce constructed type symbol.
        ''' </summary>
        ''' <param name="genericTypeDef">
        ''' Symbol for generic type.
        ''' </param>
        ''' <param name="arguments">
        ''' Generic type arguments, including those for nesting types.
        ''' </param>
        ''' <param name="refersToNoPiaLocalType">
        ''' Flags for arguments. Each item indicates whether corresponding argument refers to NoPia local types.
        ''' </param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function SubstituteTypeParameters(
            genericTypeDef As TypeSymbol,
            arguments() As TypeSymbol,
            refersToNoPiaLocalType() As Boolean
        ) As TypeSymbol

            If TypeOf genericTypeDef Is UnsupportedMetadataTypeSymbol Then
                Return genericTypeDef
            Else
                ' Let's return unsupported metadata type if any argument is unsupported metadata type 
                For Each arg In arguments
                    If arg.Kind = SymbolKind.ErrorType AndAlso
                        TypeOf arg Is UnsupportedMetadataTypeSymbol Then
                        Return New UnsupportedMetadataTypeSymbol()
                    End If
                Next

                Dim genericType As NamedTypeSymbol = DirectCast(genericTypeDef, NamedTypeSymbol)

                ' See if it is or its enclosing type is a non-interface closed over NoPia local types. 
                Dim linkedAssemblies As ImmutableArray(Of AssemblySymbol) = m_ModuleSymbol.ContainingAssembly.GetLinkedReferencedAssemblies()

                Dim noPiaIllegalGenericInstantiation As Boolean = False

                If Not linkedAssemblies.IsDefaultOrEmpty OrElse Me.Module.ContainsNoPiaLocalTypes() Then
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
                           IsOrClosedOverATypeFromAssemblies(arguments(i), linkedAssemblies)) Then
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

                Dim substitution As TypeSubstitution = TypeSubstitution.Create(genericTypeDef, genericParameters, ImmutableArray.Create(arguments))

                If substitution Is Nothing Then
                    Return genericTypeDef
                End If

                Dim constructedType = genericType.Construct(substitution)

                If noPiaIllegalGenericInstantiation Then
                    constructedType = New NoPiaIllegalGenericInstantiationSymbol(constructedType)
                End If

                Return DirectCast(constructedType, TypeSymbol)
            End If
        End Function


        ''' <summary>
        ''' Perform a check whether the type or at least one of its generic arguments 
        ''' is defined in the specified assemblies. The check is performed recursively. 
        ''' </summary>
        Public Shared Function IsOrClosedOverATypeFromAssemblies(this As TypeSymbol, assemblies As ImmutableArray(Of AssemblySymbol)) As Boolean
            Select Case this.Kind

                Case SymbolKind.TypeParameter
                    Return False

                Case SymbolKind.ArrayType

                    Return IsOrClosedOverATypeFromAssemblies(DirectCast(this, ArrayTypeSymbol).ElementType, assemblies)

                Case SymbolKind.NamedType, SymbolKind.ErrorType

                    Dim symbol = DirectCast(this, NamedTypeSymbol)
                    Dim containingAssembly As AssemblySymbol = symbol.OriginalDefinition.ContainingAssembly

                    If containingAssembly IsNot Nothing Then
                        For i = 0 To assemblies.Length - 1 Step 1
                            If containingAssembly Is assemblies(i) Then
                                Return True
                            End If
                        Next
                    End If

                    Do
                        For Each typeArgument In symbol.TypeArgumentsNoUseSiteDiagnostics
                            If IsOrClosedOverATypeFromAssemblies(typeArgument, assemblies) Then
                                Return True
                            End If
                        Next

                        symbol = symbol.ContainingType
                    Loop While symbol IsNot Nothing

                    Return False

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(this.Kind)
            End Select
        End Function

        Protected Overrides Function SubstituteNoPiaLocalType(
            typeDef As TypeHandle,
            ByRef name As MetadataTypeName,
            interfaceGuid As String,
            scope As String,
            identifier As String
        ) As TypeSymbol
            Dim lookupIn As ImmutableArray(Of AssemblySymbol)

            lookupIn = m_ModuleSymbol.ContainingAssembly.GetNoPiaResolutionAssemblies()

            Dim result As TypeSymbol = Nothing

            If Not lookupIn.IsDefault Then
                Try
                    Dim isInterface As Boolean = Me.Module.IsInterfaceOrThrow(typeDef)
                    Dim baseType As TypeSymbol = Nothing

                    If Not isInterface Then
                        Dim baseToken As Handle = Me.Module.GetBaseTypeOfTypeOrThrow(typeDef)

                        If Not baseToken.IsNil() Then
                            baseType = GetTypeOfToken(baseToken)
                        End If
                    End If

                    result = SubstituteNoPiaLocalType(
                        name,
                        isInterface,
                        baseType,
                        interfaceGuid,
                        scope,
                        identifier,
                        m_ModuleSymbol.ContainingAssembly,
                        lookupIn)

                Catch mrEx As BadImageFormatException
                    result = GetUnsupportedMetadataTypeSymbol(mrEx)
                End Try

                Debug.Assert(result IsNot Nothing)
            End If

            If result IsNot Nothing Then
                Dim cache As ConcurrentDictionary(Of TypeHandle, TypeSymbol) = GetTypeHandleToTypeMap()

                If cache IsNot Nothing Then
                    Dim newresult As TypeSymbol = cache.GetOrAdd(typeDef, result)
                    Debug.Assert(newresult Is result OrElse (newresult.Kind = SymbolKind.ErrorType))
                    result = newresult
                End If
            End If

            Return result
        End Function

        ''' <summary>
        ''' Find canonical type for NoPia embedded type.
        ''' </summary>
        ''' <param name="fullEmittedName"></param>
        ''' <param name="isInterface"></param>
        ''' <param name="baseType"></param>
        ''' <param name="interfaceGuid"></param>
        ''' <param name="scope"></param>
        ''' <param name="identifier"></param>
        ''' <param name="referringAssembly"></param>
        ''' <param name="lookupIn"></param>
        ''' <returns>
        ''' Symbol for the canonical type or an ErrorTypeSymbol. Never returns null.
        ''' </returns>
        Friend Overloads Shared Function SubstituteNoPiaLocalType(
            ByRef fullEmittedName As MetadataTypeName,
            isInterface As Boolean,
            baseType As TypeSymbol,
            interfaceGuid As String,
            scope As String,
            identifier As String,
            referringAssembly As AssemblySymbol,
            lookupIn As ImmutableArray(Of AssemblySymbol)
        ) As NamedTypeSymbol

            Dim result As NamedTypeSymbol = Nothing

            Dim interfaceGuidValue As Guid = New Guid()
            Dim haveInterfaceGuidValue As Boolean = False
            Dim scopeGuidValue As Guid = New Guid()
            Dim haveScopeGuidValue As Boolean = False

            If isInterface AndAlso interfaceGuid IsNot Nothing Then
                haveInterfaceGuidValue = Guid.TryParse(interfaceGuid, interfaceGuidValue)

                If haveInterfaceGuidValue Then
                    ' To have consistent errors.
                    scope = Nothing
                    identifier = Nothing
                End If
            End If

            If scope IsNot Nothing Then
                haveScopeGuidValue = Guid.TryParse(scope, scopeGuidValue)
            End If

            For Each assembly As AssemblySymbol In lookupIn
                If assembly Is Nothing OrElse assembly Is referringAssembly Then
                    Continue For
                End If

                Dim candidate As NamedTypeSymbol = assembly.LookupTopLevelMetadataType(fullEmittedName, digThroughForwardedTypes:=False)
                Debug.Assert(Not candidate.IsGenericType)

                ' Ignore type forwarders, error symbols and non-public types
                If candidate.Kind = SymbolKind.ErrorType OrElse
                   candidate.ContainingAssembly IsNot assembly OrElse
                   candidate.DeclaredAccessibility <> Accessibility.Public Then
                    Continue For
                End If

                ' Ignore NoPia local types.
                ' If candidate is coming from metadata, we don't need to do any special check,
                ' because we do not create symbols for local types. However, local types defined in source 
                ' is another story. However, if compilation explicitly defines a local type, it should be
                ' represented by a retargeting assembly, which is supposed to hide the local type.
                Debug.Assert((Not TypeOf assembly Is SourceAssemblySymbol) OrElse
                             Not DirectCast(assembly, SourceAssemblySymbol).SourceModule.MightContainNoPiaLocalTypes())

                Dim candidateGuid As String = Nothing
                Dim haveCandidateGuidValue As Boolean = False
                Dim candidateGuidValue As Guid = New Guid()

                ' The type must be of the same kind (interface, struct, delegate or enum).
                Select Case candidate.TypeKind
                    Case TypeKind.Interface
                        If Not isInterface Then
                            Continue For
                        End If

                        ' Get candidate's Guid
                        If candidate.GetGuidString(candidateGuid) AndAlso candidateGuid IsNot Nothing Then
                            haveCandidateGuidValue = Guid.TryParse(candidateGuid, candidateGuidValue)
                        End If

                    Case TypeKind.Delegate,
                         TypeKind.Enum,
                         TypeKind.Structure

                        If isInterface Then
                            Continue For
                        End If

                        ' Let's use a trick. To make sure the kind is the same, make sure
                        ' base type is the same.
                        If baseType IsNot candidate.BaseTypeNoUseSiteDiagnostics Then
                            Continue For
                        End If

                    Case Else
                        Continue For
                End Select

                If haveInterfaceGuidValue OrElse haveCandidateGuidValue Then
                    If Not haveInterfaceGuidValue OrElse Not haveCandidateGuidValue OrElse
                        candidateGuidValue <> interfaceGuidValue Then
                        Continue For
                    End If
                Else
                    If Not haveScopeGuidValue OrElse identifier Is Nothing OrElse Not String.Equals(identifier, fullEmittedName.FullName, StringComparison.Ordinal) Then
                        Continue For
                    End If

                    ' Scope guid must match candidate's assembly guid.
                    haveCandidateGuidValue = False
                    If assembly.GetGuidString(candidateGuid) AndAlso candidateGuid IsNot Nothing Then
                        haveCandidateGuidValue = Guid.TryParse(candidateGuid, candidateGuidValue)
                    End If

                    If Not haveCandidateGuidValue OrElse scopeGuidValue <> candidateGuidValue Then
                        Continue For
                    End If
                End If

                ' OK. It looks like we found canonical type definition.
                If result IsNot Nothing Then
                    ' Ambiguity 
                    result = New NoPiaAmbiguousCanonicalTypeSymbol(referringAssembly, result, candidate)
                    Exit For
                End If

                result = candidate
            Next

            If result Is Nothing Then
                result = New NoPiaMissingCanonicalTypeSymbol(
                                referringAssembly,
                                fullEmittedName.FullName,
                                interfaceGuid,
                                scope,
                                identifier)
            End If

            Return result
        End Function

        Protected Overrides Function FindMethodSymbolInType(typeSymbol As TypeSymbol, targetMethodDef As MethodHandle) As MethodSymbol
            Debug.Assert(TypeOf typeSymbol Is PENamedTypeSymbol OrElse TypeOf typeSymbol Is ErrorTypeSymbol)

            For Each member In typeSymbol.GetMembersUnordered()
                Dim method As PEMethodSymbol = TryCast(member, PEMethodSymbol)
                If method IsNot Nothing AndAlso method.Handle = targetMethodDef Then
                    Return method
                End If
            Next

            Return Nothing
        End Function

        Protected Overrides Function FindFieldSymbolInType(typeSymbol As TypeSymbol, fieldDef As FieldHandle) As FieldSymbol
            Debug.Assert(TypeOf typeSymbol Is PENamedTypeSymbol OrElse TypeOf typeSymbol Is ErrorTypeSymbol)

            For Each member In typeSymbol.GetMembersUnordered()
                Dim field As PEFieldSymbol = TryCast(member, PEFieldSymbol)
                If field IsNot Nothing AndAlso field.Handle = fieldDef Then
                    Return field
                End If
            Next

            Return Nothing
        End Function

        Friend Overrides Function GetSymbolForMemberRef(memberRef As MemberReferenceHandle, Optional scope As TypeSymbol = Nothing, Optional methodsOnly As Boolean = False) As Symbol
            Dim targetTypeSymbol As TypeSymbol = GetMemberRefTypeSymbol(memberRef)

            If scope IsNot Nothing AndAlso targetTypeSymbol <> scope AndAlso Not targetTypeSymbol.IsBaseTypeOrInterfaceOf(scope, Nothing) Then
                Return Nothing
            End If

            ' We're going to use a special decoder that can generate useable symbols for type parameters without full context.
            ' (We're not just using a different type - we're also changing the type context.)
            Dim memberRefDecoder = New MemberRefMetadataDecoder(m_ModuleSymbol, targetTypeSymbol)
            Return memberRefDecoder.FindMember(targetTypeSymbol, memberRef, methodsOnly)
        End Function

        Protected Overrides Sub EnqueueTypeSymbolInterfacesAndBaseTypes(typeDefsToSearch As Queue(Of TypeHandle), typeSymbolsToSearch As Queue(Of TypeSymbol), typeSymbol As TypeSymbol)
            For Each iface In typeSymbol.InterfacesNoUseSiteDiagnostics
                EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, iface)
            Next

            EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, typeSymbol.BaseTypeNoUseSiteDiagnostics)
        End Sub

        Protected Overrides Sub EnqueueTypeSymbol(typeDefsToSearch As Queue(Of TypeHandle), typeSymbolsToSearch As Queue(Of TypeSymbol), typeSymbol As TypeSymbol)
            If typeSymbol IsNot Nothing Then
                Dim peTypeSymbol As PENamedTypeSymbol = TryCast(typeSymbol, PENamedTypeSymbol)
                If peTypeSymbol IsNot Nothing AndAlso peTypeSymbol.ContainingPEModule Is m_ModuleSymbol Then
                    typeDefsToSearch.Enqueue(peTypeSymbol.Handle)
                Else
                    typeSymbolsToSearch.Enqueue(typeSymbol)
                End If

            End If
        End Sub

        Protected Overrides Function GetMethodHandle(method As MethodSymbol) As MethodHandle
            Dim peMethod As PEMethodSymbol = TryCast(method, PEMethodSymbol)
            If peMethod IsNot Nothing AndAlso peMethod.ContainingModule Is m_ModuleSymbol Then
                Return peMethod.Handle
            End If

            Return Nothing
        End Function
    End Class
End Namespace

