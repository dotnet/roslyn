' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' Helper class to resolve metadata tokens and signatures.
    ''' </summary>
    Friend Class MetadataDecoder
        Inherits MetadataDecoder(Of PEModuleSymbol, TypeSymbol, MethodSymbol, FieldSymbol, Symbol)

        ''' <summary>
        ''' Type context for resolving generic type arguments.
        ''' </summary>
        Private ReadOnly _typeContextOpt As PENamedTypeSymbol

        ''' <summary>
        ''' Method context for resolving generic method type arguments.
        ''' </summary>
        Private ReadOnly _methodContextOpt As PEMethodSymbol

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
            MyBase.New(moduleSymbol.Module, If(TypeOf moduleSymbol.ContainingAssembly Is PEAssemblySymbol, moduleSymbol.ContainingAssembly.Identity, Nothing), SymbolFactory.Instance, moduleSymbol)

            Debug.Assert(moduleSymbol IsNot Nothing)

            _typeContextOpt = typeContextOpt
            _methodContextOpt = methodContextOpt
        End Sub

        Friend Shadows ReadOnly Property ModuleSymbol As PEModuleSymbol
            Get
                Return MyBase.moduleSymbol
            End Get
        End Property

        Protected Overrides Function GetGenericMethodTypeParamSymbol(position As Integer) As TypeSymbol

            If _methodContextOpt Is Nothing Then
                Return New UnsupportedMetadataTypeSymbol()
            End If

            Dim typeParameters = _methodContextOpt.TypeParameters

            If typeParameters.Length <= position Then
                Return New UnsupportedMetadataTypeSymbol()
            End If

            Return typeParameters(position)
        End Function

        Protected Overrides Function GetGenericTypeParamSymbol(position As Integer) As TypeSymbol

            Dim type As PENamedTypeSymbol = _typeContextOpt

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

        Protected Overrides Function GetTypeHandleToTypeMap() As ConcurrentDictionary(Of TypeDefinitionHandle, TypeSymbol)
            Return ModuleSymbol.TypeHandleToTypeMap
        End Function

        Protected Overrides Function GetTypeRefHandleToTypeMap() As ConcurrentDictionary(Of TypeReferenceHandle, TypeSymbol)
            Return ModuleSymbol.TypeRefHandleToTypeMap
        End Function

        Protected Overrides Function LookupNestedTypeDefSymbol(
            container As TypeSymbol,
            ByRef emittedName As MetadataTypeName
        ) As TypeSymbol
            Dim result = container.LookupMetadataType(emittedName)
            Debug.Assert(If(Not result?.IsErrorType(), True))

            Return If(result, New MissingMetadataTypeSymbol.Nested(DirectCast(container, NamedTypeSymbol), emittedName))
        End Function

        ''' <summary>
        ''' Lookup a type defined in referenced assembly.
        ''' </summary>
        Protected Overloads Overrides Function LookupTopLevelTypeDefSymbol(
            referencedAssemblyIndex As Integer,
            ByRef emittedName As MetadataTypeName
        ) As TypeSymbol
            Dim assembly As AssemblySymbol = ModuleSymbol.GetReferencedAssemblySymbol(referencedAssemblyIndex)
            If assembly Is Nothing Then
                Return New UnsupportedMetadataTypeSymbol()
            End If

            Try
                Return assembly.LookupDeclaredOrForwardedTopLevelMetadataType(emittedName, visitedAssemblies:=Nothing)
            Catch e As Exception When FatalError.ReportAndPropagate(e) ' Trying to get more useful Watson dumps.
                Throw ExceptionUtilities.Unreachable
            End Try
        End Function

        ''' <summary>
        ''' Lookup a type defined in a module of a multi-module assembly.
        ''' </summary>
        Protected Overrides Function LookupTopLevelTypeDefSymbol(moduleName As String, ByRef emittedName As MetadataTypeName, <Out> ByRef isNoPiaLocalType As Boolean) As TypeSymbol
            For Each m As ModuleSymbol In ModuleSymbol.ContainingAssembly.Modules
                If String.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase) Then
                    If m Is ModuleSymbol Then
                        Return ModuleSymbol.LookupTopLevelMetadataType(emittedName, isNoPiaLocalType)
                    Else
                        isNoPiaLocalType = False
                        Dim result As NamedTypeSymbol = m.LookupTopLevelMetadataType(emittedName)
                        Debug.Assert(If(Not result?.IsErrorType(), True))

                        Return If(result, New MissingMetadataTypeSymbol.TopLevel(m, emittedName))
                    End If
                End If
            Next

            isNoPiaLocalType = False
            Return New MissingMetadataTypeSymbol.TopLevel(New MissingModuleSymbolWithName(ModuleSymbol.ContainingAssembly, moduleName), emittedName, SpecialType.None)
        End Function

        ''' <summary>
        ''' Lookup a type defined in this module.
        ''' This method will be called only if the type we are
        ''' looking for hasn't been loaded yet. Otherwise, MetadataDecoder
        ''' would have found the type in TypeDefRowIdToTypeMap based on its 
        ''' TypeDef row id. 
        ''' </summary>
        Protected Overloads Overrides Function LookupTopLevelTypeDefSymbol(ByRef emittedName As MetadataTypeName, <Out> ByRef isNoPiaLocalType As Boolean) As TypeSymbol
            Return ModuleSymbol.LookupTopLevelMetadataType(emittedName, isNoPiaLocalType)
        End Function

        Protected Overrides Function GetIndexOfReferencedAssembly(identity As AssemblyIdentity) As Integer
            ' Go through all assemblies referenced by the current module And
            ' find the one which *exactly* matches the given identity.
            ' No unification will be performed
            Dim assemblies = ModuleSymbol.GetReferencedAssemblies()
            For i = 0 To assemblies.Length - 1
                If identity.Equals(assemblies(i)) Then
                    Return i
                End If
            Next
            Return -1
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
                        If symbol.IsTupleType Then
                            Return IsOrClosedOverATypeFromAssemblies(symbol.TupleUnderlyingType, assemblies)
                        End If

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
            typeDef As TypeDefinitionHandle,
            ByRef name As MetadataTypeName,
            interfaceGuid As String,
            scope As String,
            identifier As String
        ) As TypeSymbol

            Dim result As TypeSymbol

            Try
                Dim isInterface As Boolean = Me.Module.IsInterfaceOrThrow(typeDef)
                Dim baseType As TypeSymbol = Nothing

                If Not isInterface Then
                    Dim baseToken As EntityHandle = Me.Module.GetBaseTypeOfTypeOrThrow(typeDef)

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
                        ModuleSymbol.ContainingAssembly)

            Catch mrEx As BadImageFormatException
                result = GetUnsupportedMetadataTypeSymbol(mrEx)
            End Try

            Debug.Assert(result IsNot Nothing)

            Dim cache As ConcurrentDictionary(Of TypeDefinitionHandle, TypeSymbol) = GetTypeHandleToTypeMap()
            Debug.Assert(cache IsNot Nothing)

            Dim newresult As TypeSymbol = cache.GetOrAdd(typeDef, result)
            Debug.Assert(newresult Is result OrElse (newresult.Kind = SymbolKind.ErrorType))
            Return newresult
        End Function

        ''' <summary>
        ''' Find canonical type for NoPia embedded type.
        ''' </summary>
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
            referringAssembly As AssemblySymbol
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

            For Each assembly As AssemblySymbol In referringAssembly.GetNoPiaResolutionAssemblies()
                Debug.Assert(assembly IsNot Nothing)
                If assembly Is referringAssembly Then
                    Continue For
                End If

                Dim candidate As NamedTypeSymbol = assembly.LookupDeclaredTopLevelMetadataType(fullEmittedName)
                Debug.Assert(If(Not candidate?.IsGenericType, True))
                Debug.Assert(If(Not candidate?.IsErrorType(), True))

                ' Ignore type forwarders and non-public types
                If candidate Is Nothing OrElse
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
                        Dim baseSpecialType As SpecialType = If(candidate.BaseTypeNoUseSiteDiagnostics?.SpecialType, SpecialType.None)
                        If baseSpecialType = SpecialType.None OrElse baseSpecialType <> If(baseType?.SpecialType, SpecialType.None) Then
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

        Protected Overrides Function FindMethodSymbolInType(typeSymbol As TypeSymbol, targetMethodDef As MethodDefinitionHandle) As MethodSymbol
            Debug.Assert(typeSymbol.IsDefinition)

            Dim peTypeSymbol As PENamedTypeSymbol = TryCast(typeSymbol, PENamedTypeSymbol)
            If peTypeSymbol IsNot Nothing AndAlso peTypeSymbol.ContainingPEModule Is ModuleSymbol Then

                For Each member In typeSymbol.GetMembersUnordered()
                    Dim method As PEMethodSymbol = TryCast(member, PEMethodSymbol)
                    If method IsNot Nothing AndAlso method.Handle = targetMethodDef Then
                        Return method
                    End If
                Next

            ElseIf Not TypeOf typeSymbol Is ErrorTypeSymbol Then

                ' We're going to use a special decoder that can generate usable symbols for type parameters without full context.
                ' (We're not just using a different type - we're also changing the type context.)
                Dim memberRefDecoder = New MemberRefMetadataDecoder(ModuleSymbol, typeSymbol)

                Return DirectCast(memberRefDecoder.FindMember(targetMethodDef, methodsOnly:=True), MethodSymbol)
            End If

            Return Nothing
        End Function

        Protected Overrides Function FindFieldSymbolInType(typeSymbol As TypeSymbol, fieldDef As FieldDefinitionHandle) As FieldSymbol
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

            If targetTypeSymbol Is Nothing Then
                Return Nothing
            End If

            Debug.Assert(Not targetTypeSymbol.IsTupleType)

            If scope IsNot Nothing AndAlso Not TypeSymbol.Equals(targetTypeSymbol, scope, TypeCompareKind.ConsiderEverything) AndAlso Not targetTypeSymbol.IsBaseTypeOrInterfaceOf(scope, CompoundUseSiteInfo(Of AssemblySymbol).Discarded) Then
                Return Nothing
            End If

            If Not targetTypeSymbol.IsTupleCompatible() Then
                targetTypeSymbol = TupleTypeDecoder.DecodeTupleTypesIfApplicable(targetTypeSymbol, elementNames:=Nothing)
            End If

            ' We're going to use a special decoder that can generate usable symbols for type parameters without full context.
            ' (We're not just using a different type - we're also changing the type context.)
            Dim memberRefDecoder = New MemberRefMetadataDecoder(ModuleSymbol, targetTypeSymbol.OriginalDefinition)

            Dim definition = memberRefDecoder.FindMember(memberRef, methodsOnly)

            If definition IsNot Nothing AndAlso Not targetTypeSymbol.IsDefinition Then
                Return definition.AsMember(DirectCast(targetTypeSymbol, NamedTypeSymbol))
            End If

            Return definition
        End Function

        Protected Overrides Sub EnqueueTypeSymbolInterfacesAndBaseTypes(typeDefsToSearch As Queue(Of TypeDefinitionHandle), typeSymbolsToSearch As Queue(Of TypeSymbol), typeSymbol As TypeSymbol)
            For Each iface In typeSymbol.InterfacesNoUseSiteDiagnostics
                EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, iface)
            Next

            EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, typeSymbol.BaseTypeNoUseSiteDiagnostics)
        End Sub

        Protected Overrides Sub EnqueueTypeSymbol(typeDefsToSearch As Queue(Of TypeDefinitionHandle), typeSymbolsToSearch As Queue(Of TypeSymbol), typeSymbol As TypeSymbol)
            If typeSymbol IsNot Nothing Then
                Dim peTypeSymbol As PENamedTypeSymbol = TryCast(typeSymbol, PENamedTypeSymbol)
                If peTypeSymbol IsNot Nothing AndAlso peTypeSymbol.ContainingPEModule Is ModuleSymbol Then
                    typeDefsToSearch.Enqueue(peTypeSymbol.Handle)
                Else
                    typeSymbolsToSearch.Enqueue(typeSymbol)
                End If

            End If
        End Sub

        Protected Overrides Function GetMethodHandle(method As MethodSymbol) As MethodDefinitionHandle
            Dim peMethod As PEMethodSymbol = TryCast(method, PEMethodSymbol)
            If peMethod IsNot Nothing AndAlso peMethod.ContainingModule Is ModuleSymbol Then
                Return peMethod.Handle
            End If

            Return Nothing
        End Function
    End Class
End Namespace

