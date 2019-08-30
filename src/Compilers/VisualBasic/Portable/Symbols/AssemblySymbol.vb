' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Collections
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a .NET assembly. An assembly consists of one or more modules.
    ''' </summary>
    Friend MustInherit Class AssemblySymbol
        Inherits Symbol
        Implements IAssemblySymbolInternal

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version of Symbol.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ''' <summary>
        ''' The system assembly, which provides primitive types like Object, String, etc., e.g. mscorlib.dll. 
        ''' The value is provided by ReferenceManager and must not be modified. For SourceAssemblySymbol, non-missing 
        ''' coreLibrary must match one of the referenced assemblies returned by GetReferencedAssemblySymbols() method of 
        ''' the main module. If there is no existing assembly that can be used as a source for the primitive types, 
        ''' the value is a Compilation.MissingCorLibrary. 
        ''' </summary>
        Private _corLibrary As AssemblySymbol

        ''' <summary>
        ''' The system assembly, which provides primitive types like Object, String, etc., e.g. mscorlib.dll. 
        ''' The value is a MissingAssemblySymbol if none of the referenced assemblies can be used as a source for the 
        ''' primitive types and the owning assembly cannot be used as the source too. Otherwise, it is one of 
        ''' the referenced assemblies returned by GetReferencedAssemblySymbols() method or the owning assembly.
        ''' </summary>
        Friend ReadOnly Property CorLibrary As AssemblySymbol
            Get
                Return _corLibrary
            End Get
        End Property

        ''' <summary>
        ''' A helper method for ReferenceManager to set the system assembly, which provides primitive 
        ''' types like Object, String, etc., e.g. mscorlib.dll. 
        ''' </summary>
        ''' <param name="corLibrary"></param>
        Friend Sub SetCorLibrary(corLibrary As AssemblySymbol)
            Debug.Assert(_corLibrary Is Nothing)
            _corLibrary = corLibrary
        End Sub

        ''' <summary>
        ''' Simple name of the assembly. 
        ''' </summary>
        ''' <remarks>
        ''' This is equivalent to <see cref="Identity"/>.<see cref="AssemblyIdentity.Name"/>, but may be 
        ''' much faster to retrieve for source code assemblies, since it does not require binding the assembly-level
        ''' attributes that contain the version number and other assembly information.
        ''' </remarks>
        Public Overrides ReadOnly Property Name As String
            Get
                Return Identity.Name
            End Get
        End Property

        ''' <summary>
        ''' True if the assembly contains interactive code.
        ''' </summary>
        Public Overridable ReadOnly Property IsInteractive As Boolean Implements IAssemblySymbol.IsInteractive
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' If this symbol represents a metadata assembly returns the underlying <see cref="AssemblyMetadata"/>.
        ''' 
        ''' Otherwise, this returns <see langword="Nothing"/>.
        ''' </summary>
        Public MustOverride Function GetMetadata() As AssemblyMetadata Implements IAssemblySymbol.GetMetadata

        ''' <summary>
        ''' Get the name of this assembly.
        ''' </summary>
        Public MustOverride ReadOnly Property Identity As AssemblyIdentity Implements IAssemblySymbol.Identity

        Public MustOverride ReadOnly Property AssemblyVersionPattern As Version Implements IAssemblySymbolInternal.AssemblyVersionPattern

        ''' <summary>
        ''' Target architecture of the machine.
        ''' </summary>
        Friend ReadOnly Property Machine As System.Reflection.PortableExecutable.Machine
            Get
                Return Modules(0).Machine
            End Get
        End Property

        ''' <summary>
        ''' Indicates that this PE file makes Win32 calls. See CorPEKind.pe32BitRequired for more information (http://msdn.microsoft.com/en-us/library/ms230275.aspx).
        ''' </summary>
        Friend ReadOnly Property Bit32Required As Boolean
            Get
                Return Modules(0).Bit32Required
            End Get
        End Property

        ''' <summary>
        ''' Gets a read-only list of all the modules in this assembly. (There must be at least one.) The first one is the main module
        ''' that holds the assembly manifest.
        ''' </summary>
        Public MustOverride ReadOnly Property Modules As ImmutableArray(Of ModuleSymbol)

        ''' <summary>
        ''' Gets the merged root namespace that contains all namespaces and types defined in the modules
        ''' of this assembly. If there is just one module in this assembly, this property just returns the 
        ''' GlobalNamespace of that module.
        ''' </summary>
        Public MustOverride ReadOnly Property GlobalNamespace As NamespaceSymbol

        ''' <summary>
        ''' Given a namespace symbol, returns the corresponding assembly specific namespace symbol
        ''' </summary>
        Friend Function GetAssemblyNamespace(namespaceSymbol As NamespaceSymbol) As NamespaceSymbol
            If namespaceSymbol.IsGlobalNamespace Then
                Return Me.GlobalNamespace
            End If

            Dim container As NamespaceSymbol = namespaceSymbol.ContainingNamespace

            If container Is Nothing Then
                Return Me.GlobalNamespace
            End If

            If namespaceSymbol.Extent.Kind = NamespaceKind.Assembly AndAlso namespaceSymbol.ContainingAssembly = Me Then
                Return namespaceSymbol
            End If

            Dim assemblyContainer As NamespaceSymbol = GetAssemblyNamespace(container)

            If assemblyContainer Is container Then
                ' Trivial case, container isn't merged.
                Return namespaceSymbol
            End If

            If assemblyContainer Is Nothing Then
                Return Nothing
            End If

            Return assemblyContainer.GetNestedNamespace(namespaceSymbol.Name)
        End Function

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.Assembly
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitAssembly(Me, arg)
        End Function

        Friend Sub New()
            ' Only the compiler can create AssemblySymbols.
        End Sub

        ''' <summary>
        ''' Does this symbol represent a missing assembly.
        ''' </summary>
        Friend MustOverride ReadOnly Property IsMissing As Boolean

        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        ''' This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        ''' </summary>
        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Lookup a top level type referenced from metadata, names should be
        ''' compared case-sensitively.
        ''' </summary>
        ''' <param name="emittedName">
        ''' Full type name with generic name mangling.
        ''' </param>
        ''' <param name="digThroughForwardedTypes">
        ''' Take forwarded types into account.
        ''' </param>
        ''' <remarks></remarks>
        Friend Function LookupTopLevelMetadataType(ByRef emittedName As MetadataTypeName, digThroughForwardedTypes As Boolean) As NamedTypeSymbol
            Return LookupTopLevelMetadataTypeWithCycleDetection(emittedName, visitedAssemblies:=Nothing, digThroughForwardedTypes:=digThroughForwardedTypes)
        End Function

        ''' <summary>
        ''' Lookup a top level type referenced from metadata, names should be
        ''' compared case-sensitively.  Detect cycles during lookup.
        ''' </summary>
        ''' <param name="emittedName">
        ''' Full type name, possibly with generic name mangling.
        ''' </param>
        ''' <param name="visitedAssemblies">
        ''' List of assemblies lookup has already visited (since type forwarding can introduce cycles).
        ''' </param>
        ''' <param name="digThroughForwardedTypes">
        ''' Take forwarded types into account.
        ''' </param>
        Friend MustOverride Function LookupTopLevelMetadataTypeWithCycleDetection(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol), digThroughForwardedTypes As Boolean) As NamedTypeSymbol

        ''' <summary>
        ''' Returns the type symbol for a forwarded type based its canonical CLR metadata name.
        ''' The name should refer to a non-nested type. If type with this name Is Not forwarded,
        ''' null Is returned.
        ''' </summary>
        Public Function ResolveForwardedType(fullyQualifiedMetadataName As String) As NamedTypeSymbol
            If fullyQualifiedMetadataName Is Nothing Then
                Throw New ArgumentNullException(NameOf(fullyQualifiedMetadataName))
            End If

            Dim emittedName = MetadataTypeName.FromFullName(fullyQualifiedMetadataName)
            Return TryLookupForwardedMetadataType(emittedName, ignoreCase:=False)
        End Function

        ''' <summary>
        ''' Look up the given metadata type, if it Is forwarded.
        ''' </summary>
        Friend Function TryLookupForwardedMetadataType(ByRef emittedName As MetadataTypeName, ignoreCase As Boolean) As NamedTypeSymbol
            Return TryLookupForwardedMetadataTypeWithCycleDetection(emittedName, visitedAssemblies:=Nothing, ignoreCase:=ignoreCase)
        End Function

        ''' <summary>
        ''' Look up the given metadata type, if it is forwarded.
        ''' </summary>
        Friend Overridable Function TryLookupForwardedMetadataTypeWithCycleDetection(ByRef emittedName As MetadataTypeName, visitedAssemblies As ConsList(Of AssemblySymbol), ignoreCase As Boolean) As NamedTypeSymbol
            Return Nothing
        End Function

        Friend Function CreateCycleInTypeForwarderErrorTypeSymbol(ByRef emittedName As MetadataTypeName) As ErrorTypeSymbol
            Dim diagInfo As DiagnosticInfo = New DiagnosticInfo(MessageProvider.Instance, ERRID.ERR_TypeFwdCycle2, emittedName.FullName, Me)
            Return New MissingMetadataTypeSymbol.TopLevelWithCustomErrorInfo(Me.Modules(0), emittedName, diagInfo)
        End Function

        Friend Function CreateMultipleForwardingErrorTypeSymbol(ByRef emittedName As MetadataTypeName, forwardingModule As ModuleSymbol, destination1 As AssemblySymbol, destination2 As AssemblySymbol) As ErrorTypeSymbol
            Dim diagnosticInfo = New DiagnosticInfo(MessageProvider.Instance, ERRID.ERR_TypeForwardedToMultipleAssemblies, forwardingModule, Me, emittedName.FullName, destination1, destination2)
            Return New MissingMetadataTypeSymbol.TopLevelWithCustomErrorInfo(forwardingModule, emittedName, diagnosticInfo)
        End Function

        ''' <summary>
        ''' Lookup declaration for predefined CorLib type in this Assembly. Only valid if this 
        ''' assembly is the Cor Library
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend MustOverride Function GetDeclaredSpecialType(type As SpecialType) As NamedTypeSymbol

        ''' <summary>
        ''' Register declaration of predefined CorLib type in this Assembly.
        ''' </summary>
        ''' <param name="corType"></param>
        Friend Overridable Sub RegisterDeclaredSpecialType(corType As NamedTypeSymbol)
            Throw ExceptionUtilities.Unreachable
        End Sub

        ''' <summary>
        ''' Continue looking for declaration of predefined CorLib type in this Assembly
        ''' while symbols for new type declarations are constructed.
        ''' </summary>
        Friend Overridable ReadOnly Property KeepLookingForDeclaredSpecialTypes As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        ''' <summary>
        ''' Return an array of assemblies involved in canonical type resolution of
        ''' NoPia local types defined within this assembly. In other words, all 
        ''' references used by previous compilation referencing this assembly.
        ''' </summary>
        ''' <returns></returns>
        Friend MustOverride Function GetNoPiaResolutionAssemblies() As ImmutableArray(Of AssemblySymbol)
        Friend MustOverride Sub SetNoPiaResolutionAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))

        ''' <summary>
        ''' Return an array of assemblies referenced by this assembly, which are linked (/l-ed) by 
        ''' each compilation that is using this AssemblySymbol as a reference. 
        ''' If this AssemblySymbol is linked too, it will be in this array too.
        ''' </summary>
        Friend MustOverride Function GetLinkedReferencedAssemblies() As ImmutableArray(Of AssemblySymbol)
        Friend MustOverride Sub SetLinkedReferencedAssemblies(assemblies As ImmutableArray(Of AssemblySymbol))

        ''' <summary>
        ''' Assembly is /l-ed by compilation that is using it as a reference.
        ''' </summary>
        Friend MustOverride ReadOnly Property IsLinked As Boolean

        ''' <summary>
        ''' Returns true and a string from the first GuidAttribute on the assembly, 
        ''' the string might be null or an invalid guid representation. False, 
        ''' if there is no GuidAttribute with string argument.
        ''' </summary>
        Friend Overridable Function GetGuidString(ByRef guidString As String) As Boolean
            Return GetGuidStringDefaultImplementation(guidString)
        End Function

        Public MustOverride ReadOnly Property TypeNames As ICollection(Of String) Implements IAssemblySymbol.TypeNames

        Public MustOverride ReadOnly Property NamespaceNames As ICollection(Of String) Implements IAssemblySymbol.NamespaceNames

        ''' <summary>
        ''' An empty list means there was no IVT attribute with matching <paramref name="simpleName"/>.
        ''' An IVT attribute without a public key setting is represented by an entry that is empty in the returned list
        ''' </summary>
        ''' <param name="simpleName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend MustOverride Function GetInternalsVisibleToPublicKeys(simpleName As String) As IEnumerable(Of ImmutableArray(Of Byte))

        Friend MustOverride Function AreInternalsVisibleToThisAssembly(other As AssemblySymbol) As Boolean

        ''' <summary>
        ''' Get symbol for predefined type from Cor Library used by this assembly.
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns>The symbol for the pre-defined type or Nothing if the type is not defined in the core library</returns>
        ''' <remarks></remarks>
        Friend Function GetSpecialType(type As SpecialType) As NamedTypeSymbol
            If type <= SpecialType.None OrElse type > SpecialType.Count Then
                Throw New ArgumentOutOfRangeException(NameOf(type), $"Unexpected SpecialType: '{CType(type, Integer)}'.")
            End If

            Return CorLibrary.GetDeclaredSpecialType(type)
        End Function

        ''' <summary>
        ''' The NamedTypeSymbol for the .NET System.Object type, which could have a TypeKind of
        ''' Error if there was no COR Library in a compilation using the assembly.
        '''</summary>
        Friend ReadOnly Property ObjectType As NamedTypeSymbol
            Get
                Return GetSpecialType(SpecialType.System_Object)
            End Get
        End Property

        ''' <summary>
        ''' Get symbol for predefined type from Cor Library used by this assembly.
        ''' </summary>
        ''' <param name="type"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Friend Function GetPrimitiveType(type As Microsoft.Cci.PrimitiveTypeCode) As NamedTypeSymbol
            Return GetSpecialType(SpecialTypes.GetTypeFromMetadataName(type))
        End Function


        ''' <summary>
        ''' Lookup a type within the assembly using its canonical CLR metadata name (names are compared case-sensitively).
        ''' </summary>
        ''' <param name="fullyQualifiedMetadataName">
        ''' </param>
        ''' <returns>
        ''' Symbol for the type or null if type cannot be found or is ambiguous. 
        ''' </returns>
        Public Function GetTypeByMetadataName(fullyQualifiedMetadataName As String) As NamedTypeSymbol
            Return GetTypeByMetadataName(fullyQualifiedMetadataName, includeReferences:=False, isWellKnownType:=False, conflicts:=Nothing)
        End Function

        Private Shared ReadOnly s_nestedTypeNameSeparators As Char() = {"+"c}

        ''' <summary>
        ''' Lookup a type within the assembly using its canonical CLR metadata name (names are compared case-sensitively).
        ''' </summary>
        ''' <param name="metadataName"></param>
        ''' <param name="includeReferences">
        ''' If search within assembly fails, lookup in assemblies referenced by the primary module.
        ''' For source assembly, this is equivalent to all assembly references given to compilation.
        ''' </param>
        ''' <param name="isWellKnownType">
        ''' Extra restrictions apply when searching for a well-known type.  In particular, the type must be public.
        ''' </param>
        ''' <param name="useCLSCompliantNameArityEncoding">
        ''' While resolving the name, consider only types following CLS-compliant generic type names and arity encoding (ECMA-335, section 10.7.2).
        ''' I.e. arity is inferred from the name and matching type must have the same emitted name and arity.
        ''' </param>
        ''' <param name="ignoreCorLibraryDuplicatedTypes">
        ''' When set, any duplicate coming from corlib is ignored.
        ''' </param>
        ''' <param name="conflicts">
        ''' In cases a type could not be found because of ambiguity, we return two of the candidates that caused the ambiguity.
        ''' </param>
        ''' <returns></returns>
        Friend Function GetTypeByMetadataName(metadataName As String, includeReferences As Boolean, isWellKnownType As Boolean, <Out> ByRef conflicts As (AssemblySymbol, AssemblySymbol),
                                              Optional useCLSCompliantNameArityEncoding As Boolean = False, Optional ignoreCorLibraryDuplicatedTypes As Boolean = False) As NamedTypeSymbol

            If metadataName Is Nothing Then
                Throw New ArgumentNullException(NameOf(metadataName))
            End If

            Dim type As NamedTypeSymbol
            Dim mdName As MetadataTypeName

            If metadataName.Contains("+"c) Then

                Dim parts() As String = metadataName.Split(s_nestedTypeNameSeparators)
                Debug.Assert(parts.Length > 0)
                mdName = MetadataTypeName.FromFullName(parts(0), useCLSCompliantNameArityEncoding)
                type = GetTopLevelTypeByMetadataName(mdName, includeReferences, isWellKnownType, conflicts)

                Dim i As Integer = 1

                While type IsNot Nothing AndAlso Not type.IsErrorType() AndAlso i < parts.Length
                    mdName = MetadataTypeName.FromTypeName(parts(i))
                    Dim temp = type.LookupMetadataType(mdName)
                    type = If(Not isWellKnownType OrElse IsValidWellKnownType(temp), temp, Nothing)
                    i += 1
                End While
            Else
                mdName = MetadataTypeName.FromFullName(metadataName, useCLSCompliantNameArityEncoding)
                type = GetTopLevelTypeByMetadataName(mdName, includeReferences, isWellKnownType, conflicts,
                                                     ignoreCorLibraryDuplicatedTypes:=ignoreCorLibraryDuplicatedTypes)
            End If

            Return If(type Is Nothing OrElse type.IsErrorType(), Nothing, type)
        End Function


        ''' <summary>
        ''' Lookup a top level type within the assembly or one of the assemblies referenced by the primary module, 
        ''' names are compared case-sensitively. In case of ambiguity, type from this assembly wins,
        ''' otherwise Nothing is returned.
        ''' </summary>
        ''' <returns>
        ''' Symbol for the type or Nothing if type cannot be found or ambiguous. 
        ''' </returns>
        Friend Function GetTopLevelTypeByMetadataName(ByRef metadataName As MetadataTypeName, includeReferences As Boolean, isWellKnownType As Boolean, <Out> ByRef conflicts As (AssemblySymbol, AssemblySymbol),
                                                      Optional ignoreCorLibraryDuplicatedTypes As Boolean = False) As NamedTypeSymbol
            conflicts = Nothing
            Dim result As NamedTypeSymbol

            ' First try this assembly
            result = Me.LookupTopLevelMetadataType(metadataName, digThroughForwardedTypes:=False)

            If isWellKnownType AndAlso Not IsValidWellKnownType(result) Then
                result = Nothing
            End If

            If (IsAcceptableMatchForGetTypeByNameAndArity(result)) Then
                Return result
            End If

            result = Nothing

            If includeReferences Then
                ' Lookup in references
                Dim references As ImmutableArray(Of AssemblySymbol) = Me.Modules(0).GetReferencedAssemblySymbols()

                For i As Integer = 0 To references.Length - 1 Step 1
                    Debug.Assert(Not (TypeOf Me Is SourceAssemblySymbol AndAlso references(i).IsMissing)) ' Non-source assemblies can have missing references
                    Dim candidate As NamedTypeSymbol = references(i).LookupTopLevelMetadataType(metadataName, digThroughForwardedTypes:=False)

                    If isWellKnownType AndAlso Not IsValidWellKnownType(candidate) Then
                        candidate = Nothing
                    End If

                    If IsAcceptableMatchForGetTypeByNameAndArity(candidate) AndAlso
                        Not candidate.IsHiddenByVisualBasicEmbeddedAttribute() AndAlso
                        Not candidate.IsHiddenByCodeAnalysisEmbeddedAttribute() AndAlso
                        Not TypeSymbol.Equals(candidate, result, TypeCompareKind.ConsiderEverything) Then

                        If (result IsNot Nothing) Then
                            ' Ambiguity
                            If ignoreCorLibraryDuplicatedTypes Then
                                If IsInCorLib(candidate) Then
                                    ' ignore candidate
                                    Continue For
                                End If
                                If IsInCorLib(result) Then
                                    ' drop previous result
                                    result = candidate
                                    Continue For
                                End If
                            End If

                            conflicts = (result.ContainingAssembly, candidate.ContainingAssembly)
                            Return Nothing
                        End If

                        result = candidate
                    End If
                Next
            End If

            Return result
        End Function

        Private Function IsInCorLib(type As NamedTypeSymbol) As Boolean
            Return type.ContainingAssembly Is CorLibrary
        End Function

        Friend Shared Function IsAcceptableMatchForGetTypeByNameAndArity(candidate As NamedTypeSymbol) As Boolean
            Return candidate IsNot Nothing AndAlso (candidate.Kind <> SymbolKind.ErrorType OrElse Not (TypeOf candidate Is MissingMetadataTypeSymbol))
        End Function

        ''' <summary>
        ''' If this property returns false, it is certain that there are no extension
        ''' methods (from language perspective) inside this assembly. If this property returns true, 
        ''' it is highly likely (but not certain) that this type contains extension methods. 
        ''' This property allows the search for extension methods to be narrowed much more quickly.
        ''' 
        ''' !!! Note that this property can mutate during lifetime of the symbol !!!
        ''' !!! from True to False, as we learn more about the assembly.         !!! 
        ''' </summary>
        Public MustOverride ReadOnly Property MightContainExtensionMethods As Boolean Implements IAssemblySymbol.MightContainExtensionMethods

        Friend MustOverride ReadOnly Property PublicKey As ImmutableArray(Of Byte)

        Friend Function IsValidWellKnownType(result As NamedTypeSymbol) As Boolean
            If result Is Nothing OrElse result.TypeKind = TypeKind.Error Then
                Return False
            End If

            Debug.Assert(result.ContainingType Is Nothing OrElse IsValidWellKnownType(result.ContainingType),
                         "Checking the containing type is the caller's responsibility.")

            Return result.DeclaredAccessibility = Accessibility.Public OrElse IsSymbolAccessible(result, Me)
        End Function

#Region "IAssemblySymbol"

        Private ReadOnly Property IAssemblySymbol_GlobalNamespace As INamespaceSymbol Implements IAssemblySymbol.GlobalNamespace
            Get
                Return Me.GlobalNamespace
            End Get
        End Property

        Private Function IAssemblySymbol_GivesAccessTo(assemblyWantingAccess As IAssemblySymbol) As Boolean Implements IAssemblySymbol.GivesAccessTo
            If Equals(Me, assemblyWantingAccess) Then
                Return True
            End If

            Dim myKeys = Me.GetInternalsVisibleToPublicKeys(assemblyWantingAccess.Name)

            ' We have an easy out here. Suppose the assembly wanting access is
            ' being compiled as a module. You can only strong-name an assembly. So we are going to optimistically
            ' assume that it Is going to be compiled into an assembly with a matching strong name, if necessary
            If myKeys.Any() AndAlso assemblyWantingAccess.IsNetModule() Then
                Return True
            End If

            For Each key In myKeys
                Dim conclusion As IVTConclusion = Me.Identity.PerformIVTCheck(assemblyWantingAccess.Identity.PublicKey, key)
                Debug.Assert(conclusion <> IVTConclusion.NoRelationshipClaimed)
                If conclusion = IVTConclusion.Match Then
                    ' Note that C# includes  OrElse conclusion = IVTConclusion.OneSignedOneNot
                    Return True
                End If
            Next

            Return False
        End Function

        Private ReadOnly Property IAssemblySymbol_Modules As IEnumerable(Of IModuleSymbol) Implements IAssemblySymbol.Modules
            Get
                Return Me.Modules
            End Get
        End Property

        Private Function IAssemblySymbol_ResolveForwardedType(metadataName As String) As INamedTypeSymbol Implements IAssemblySymbol.ResolveForwardedType
            Return Me.ResolveForwardedType(metadataName)
        End Function

        Private Function IAssemblySymbol_GetTypeByMetadataName(metadataName As String) As INamedTypeSymbol Implements IAssemblySymbol.GetTypeByMetadataName
            Return Me.GetTypeByMetadataName(metadataName)
        End Function

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitAssembly(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitAssembly(Me)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitAssembly(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitAssembly(Me)
        End Function

#End Region

    End Class
End Namespace
