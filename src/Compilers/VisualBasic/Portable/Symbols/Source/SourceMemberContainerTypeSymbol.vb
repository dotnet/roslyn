' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a named type symbol whose members are declared in source.
    ''' </summary>
    Friend MustInherit Class SourceMemberContainerTypeSymbol
        Inherits InstanceTypeSymbol

        ''' <summary>
        ''' Holds information about a SourceType in a compact form.
        ''' </summary>
        <Flags>
        Friend Enum SourceTypeFlags As UShort
            [Private] = CUShort(Accessibility.Private)
            [Protected] = CUShort(Accessibility.Protected)
            [Friend] = CUShort(Accessibility.Friend)
            ProtectedFriend = CUShort(Accessibility.ProtectedOrFriend)
            [Public] = CUShort(Accessibility.Public)
            AccessibilityMask = &H7

            [Class] = CUShort(TypeKind.Class) << TypeKindShift
            [Structure] = CUShort(TypeKind.Structure) << TypeKindShift
            [Interface] = CUShort(TypeKind.Interface) << TypeKindShift
            [Enum] = CUShort(TypeKind.Enum) << TypeKindShift
            [Delegate] = CUShort(TypeKind.Delegate) << TypeKindShift
            [Module] = CUShort(TypeKind.Module) << TypeKindShift
            Submission = CUShort(TypeKind.Submission) << TypeKindShift
            TypeKindMask = &HF0
            TypeKindShift = 4

            [MustInherit] = 1 << 8
            [NotInheritable] = 1 << 9
            [Shadows] = 1 << 10
            [Partial] = 1 << 11
        End Enum

        ' Flags about the type
        Private ReadOnly _flags As SourceTypeFlags

        ' Misc flags defining the state of this symbol (StateFlags)
        Protected m_lazyState As Integer

        <Flags>
        Protected Enum StateFlags As Integer
            FlattenedMembersIsSortedMask = &H1   ' Set if "m_lazyMembersFlattened" is sorted.
            ReportedVarianceDiagnostics = &H2    ' Set if variance diagnostics have been reported.
            ReportedBaseClassConstraintsDiagnostics = &H4    ' Set if base class constraints diagnostics have been reported.
            ReportedInterfacesConstraintsDiagnostics = &H8    ' Set if constraints diagnostics for base/implemented interfaces have been reported.
        End Enum

        ' Containing symbol
        Private ReadOnly _containingSymbol As NamespaceOrTypeSymbol

        ' Containing source module
        Protected ReadOnly m_containingModule As SourceModuleSymbol

        ' The declaration for this type.
        Private ReadOnly _declaration As MergedTypeDeclaration

        ' The name of the type, might be different than m_decl.Name depending on lexical sort order.
        Private ReadOnly _name As String

        ' The name of the default property if any.
        ' GetMembersAndInitializers must be called before accessing field.
        Private _defaultPropertyName As String

        ' The different kinds of members of this type
        Private _lazyMembersAndInitializers As MembersAndInitializers

        ' Maps names to nested type symbols.
        Private Shared ReadOnly s_emptyTypeMembers As New Dictionary(Of String, ImmutableArray(Of NamedTypeSymbol))(IdentifierComparison.Comparer)
        Private _lazyTypeMembers As Dictionary(Of String, ImmutableArray(Of NamedTypeSymbol))

        ' An array of members in declaration order.
        Private _lazyMembersFlattened As ImmutableArray(Of Symbol)

        ' Type parameters (Nothing if not created yet)
        Private _lazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Private _lazyEmitExtensionAttribute As ThreeState = ThreeState.Unknown
        Private _lazyContainsExtensionMethods As ThreeState = ThreeState.Unknown
        Private _lazyAnyMemberHasAttributes As ThreeState = ThreeState.Unknown

        Private _lazyStructureCycle As Integer = ThreeState.Unknown  ' Interlocked

        Private _lazyLexicalSortKey As LexicalSortKey = LexicalSortKey.NotInitialized

#Region "Construction"

        ' Create the type symbol and associated type parameter symbols. Most information
        ' is deferred until later.
        Protected Sub New(declaration As MergedTypeDeclaration,
                          containingSymbol As NamespaceOrTypeSymbol,
                          containingModule As SourceModuleSymbol)

            m_containingModule = containingModule
            _containingSymbol = containingSymbol
            _declaration = declaration
            _name = GetBestName(declaration, containingModule.ContainingSourceAssembly.DeclaringCompilation)
            _flags = ComputeTypeFlags(declaration, containingSymbol.IsNamespace)
        End Sub

        ' Figure out the "right" name spelling, it should come from lexically first declaration.
        Private Shared Function GetBestName(declaration As MergedTypeDeclaration, compilation As VisualBasicCompilation) As String
            Dim declarations As ImmutableArray(Of SingleTypeDeclaration) = declaration.Declarations
            Dim best As SingleTypeDeclaration = declarations(0)

            For i As Integer = 1 To declarations.Length - 1
                Dim bestLocation As Location = best.Location
                If compilation.FirstSourceLocation(bestLocation, declarations(i).Location) IsNot bestLocation Then
                    best = declarations(i)
                End If
            Next

            Return best.Name
        End Function

        ''' <summary>
        ''' Compute the type flags from the declaration.
        ''' This function DOES NOT diagnose errors in the modifiers. Given the set of modifiers,
        ''' it produces the flags, even in the case of potentially conflicting modifiers. We have to
        ''' return some answer even in the case of errors.
        ''' </summary>
        Private Function ComputeTypeFlags(declaration As MergedTypeDeclaration, isTopLevel As Boolean) As SourceTypeFlags
            Dim mergedModifiers As DeclarationModifiers = DeclarationModifiers.None
            For i = 0 To declaration.Declarations.Length - 1
                mergedModifiers = mergedModifiers Or declaration.Declarations(i).Modifiers
            Next

            Dim modifiers = mergedModifiers
            Dim flags As SourceTypeFlags = 0

            ' compute type kind, inheritability
            Select Case declaration.Kind
                Case DeclarationKind.Class
                    flags = SourceTypeFlags.Class
                    If (modifiers And DeclarationModifiers.NotInheritable) <> 0 Then
                        flags = flags Or SourceTypeFlags.NotInheritable
                    ElseIf (modifiers And DeclarationModifiers.MustInherit) <> 0 Then
                        flags = flags Or SourceTypeFlags.MustInherit
                    End If

                Case DeclarationKind.Script, DeclarationKind.ImplicitClass
                    flags = SourceTypeFlags.Class Or SourceTypeFlags.NotInheritable

                Case DeclarationKind.Submission
                    flags = SourceTypeFlags.Submission Or SourceTypeFlags.NotInheritable

                Case DeclarationKind.Structure
                    flags = SourceTypeFlags.Structure Or SourceTypeFlags.NotInheritable

                Case DeclarationKind.Interface
                    flags = SourceTypeFlags.Interface Or SourceTypeFlags.MustInherit

                Case DeclarationKind.Enum
                    flags = SourceTypeFlags.Enum Or SourceTypeFlags.NotInheritable

                Case DeclarationKind.Delegate,
                    DeclarationKind.EventSyntheticDelegate
                    flags = SourceTypeFlags.Delegate Or SourceTypeFlags.NotInheritable

                Case DeclarationKind.Module
                    flags = SourceTypeFlags.Module Or SourceTypeFlags.NotInheritable

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(declaration.Kind)
            End Select

            ' compute accessibility
            If isTopLevel Then
                ' top-level types (types in namespaces) can only be Friend or Public, and the default is friend
                If (modifiers And DeclarationModifiers.Friend) <> 0 Then
                    flags = flags Or SourceTypeFlags.Friend
                ElseIf (modifiers And DeclarationModifiers.Public) <> 0 Then
                    flags = flags Or SourceTypeFlags.Public
                Else
                    flags = flags Or SourceTypeFlags.Friend
                End If
            Else
                ' Nested types (including types in modules) can be any accessibility, and the default is public
                If (modifiers And DeclarationModifiers.Private) <> 0 Then
                    flags = flags Or SourceTypeFlags.Private
                ElseIf (modifiers And (DeclarationModifiers.Protected Or DeclarationModifiers.Friend)) =
                                      (DeclarationModifiers.Protected Or DeclarationModifiers.Friend) Then
                    flags = flags Or SourceTypeFlags.ProtectedFriend
                ElseIf (modifiers And DeclarationModifiers.Protected) <> 0 Then
                    flags = flags Or SourceTypeFlags.Protected
                ElseIf (modifiers And DeclarationModifiers.Friend) <> 0 Then
                    flags = flags Or SourceTypeFlags.Friend
                Else
                    flags = flags Or SourceTypeFlags.Public
                End If
            End If

            ' Compute partial
            If (modifiers And DeclarationModifiers.Partial) <> 0 Then
                flags = flags Or SourceTypeFlags.Partial
            End If

            ' Compute Shadows
            If (modifiers And DeclarationModifiers.Shadows) <> 0 Then
                flags = flags Or SourceTypeFlags.Shadows
            End If
            Return flags
        End Function

        Public Shared Function Create(declaration As MergedTypeDeclaration,
                                      containingSymbol As NamespaceOrTypeSymbol,
                                      containingModule As SourceModuleSymbol) As SourceMemberContainerTypeSymbol

            Dim kind = declaration.SyntaxReferences.First.SyntaxTree.GetEmbeddedKind()

            If kind <> EmbeddedSymbolKind.None Then
                Return New EmbeddedSymbolManager.EmbeddedNamedTypeSymbol(declaration, containingSymbol, containingModule, kind)
            End If

            Select Case declaration.Kind
                Case DeclarationKind.ImplicitClass,
                    DeclarationKind.Script,
                    DeclarationKind.Submission
                    Return New ImplicitNamedTypeSymbol(declaration, containingSymbol, containingModule)

                Case Else
                    Dim type = New SourceNamedTypeSymbol(declaration, containingSymbol, containingModule)

                    ' In case Vb Core Runtime is being embedded, we should mark attribute 
                    ' 'Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute'
                    ' as being referenced if the named type just created is a module
                    If type.TypeKind = TypeKind.Module Then
                        type.DeclaringCompilation.EmbeddedSymbolManager.RegisterModuleDeclaration()
                    End If

                    Return type
            End Select
        End Function

        ' Create a nested type with the given declaration.
        Private Function CreateNestedType(declaration As MergedTypeDeclaration) As NamedTypeSymbol
#If DEBUG Then
            ' Ensure that the type declaration is either from user code or embedded
            ' code, but not merged across embedded code/user code boundary.
            Dim embedded = EmbeddedSymbolKind.Unset
            For Each ref In declaration.SyntaxReferences
                Dim refKind = ref.SyntaxTree.GetEmbeddedKind()
                If embedded <> EmbeddedSymbolKind.Unset Then
                    Debug.Assert(embedded = refKind)
                Else
                    embedded = refKind
                End If
            Next
            Debug.Assert(embedded <> EmbeddedSymbolKind.Unset)
#End If

            If declaration.Kind = DeclarationKind.Delegate Then
                Debug.Assert(Not declaration.SyntaxReferences.First.SyntaxTree.IsEmbeddedSyntaxTree)
                Return New SourceNamedTypeSymbol(declaration, Me, m_containingModule)
            ElseIf declaration.Kind = DeclarationKind.EventSyntheticDelegate Then
                Debug.Assert(Not declaration.SyntaxReferences.First.SyntaxTree.IsEmbeddedSyntaxTree)
                Return New SynthesizedEventDelegateSymbol(declaration.SyntaxReferences(0), Me)
            Else
                Return Create(declaration, Me, m_containingModule)
            End If
        End Function
#End Region

#Region "Completion"

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend NotOverridable Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            GenerateAllDeclarationErrorsImpl(cancellationToken)
        End Sub

        Protected Overridable Sub GenerateAllDeclarationErrorsImpl(cancellationToken As CancellationToken)
            cancellationToken.ThrowIfCancellationRequested()

            Dim membersAndInitializers = GetMembersAndInitializers()

            cancellationToken.ThrowIfCancellationRequested()

            For Each member In Me.GetMembers()
                ' we already visited types
                If member.Kind <> SymbolKind.NamedType Then
                    member.GenerateDeclarationErrors(cancellationToken)
                End If
            Next

            cancellationToken.ThrowIfCancellationRequested()
            Dim unused1 = BaseTypeNoUseSiteDiagnostics

            cancellationToken.ThrowIfCancellationRequested()
            Dim unused2 = InterfacesNoUseSiteDiagnostics

            cancellationToken.ThrowIfCancellationRequested()
            Dim unused3 = ExplicitInterfaceImplementationMap

            cancellationToken.ThrowIfCancellationRequested()
            Dim typeParams = TypeParameters
            If Not typeParams.IsEmpty Then
                TypeParameterSymbol.EnsureAllConstraintsAreResolved(typeParams)
            End If

            cancellationToken.ThrowIfCancellationRequested()
            Dim unused4 = GetAttributes()

            cancellationToken.ThrowIfCancellationRequested()
            BindAllMemberAttributes(cancellationToken)

            cancellationToken.ThrowIfCancellationRequested()
            GenerateVarianceDiagnostics()
        End Sub

        Private Sub GenerateVarianceDiagnostics()
            If (m_lazyState And StateFlags.ReportedVarianceDiagnostics) <> 0 Then
                Return
            End If

            Dim diagnostics As DiagnosticBag = Nothing
            Dim infosBuffer As ArrayBuilder(Of DiagnosticInfo) = Nothing

            Select Case Me.TypeKind
                Case TypeKind.Interface
                    GenerateVarianceDiagnosticsForInterface(diagnostics, infosBuffer)

                Case TypeKind.Delegate
                    GenerateVarianceDiagnosticsForDelegate(diagnostics, infosBuffer)

                Case TypeKind.Class, TypeKind.Enum, TypeKind.Structure
                    ReportNestingIntoVariantInterface(diagnostics)

                Case TypeKind.Module, TypeKind.Submission
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(Me.TypeKind)
            End Select

            m_containingModule.AtomicSetFlagAndStoreDiagnostics(m_lazyState,
                                                                StateFlags.ReportedVarianceDiagnostics,
                                                                0,
                                                                diagnostics,
                                                                CompilationStage.Declare)

            If diagnostics IsNot Nothing Then
                diagnostics.Free()
            End If

            If infosBuffer IsNot Nothing Then
                ' all diagnostics were reported to diagnostic bag:
                Debug.Assert(infosBuffer.Count = 0)
                infosBuffer.Free()
            End If
        End Sub

        Private Sub ReportNestingIntoVariantInterface(<[In], Out> ByRef diagnostics As DiagnosticBag)
            If Not _containingSymbol.IsType Then
                Return
            End If

            ' Check for illegal nesting into variant interface.
            Dim container = DirectCast(_containingSymbol, NamedTypeSymbol)

            Do
                If Not container.IsInterfaceType() Then
                    Debug.Assert(Not container.IsDelegateType())
                    ' The same validation will be performed for the container and 
                    ' there is no reason to duplicate the same errors, if any, on this type.
                    container = Nothing
                    Exit Do
                End If

                If container.TypeParameters.HaveVariance() Then
                    ' We are inside of a variant interface
                    Exit Do
                End If

                ' This interface isn't variant, but its containing interface might be.
                container = container.ContainingType
            Loop While container IsNot Nothing

            If container IsNot Nothing Then
                Debug.Assert(container.IsInterfaceType() AndAlso container.HasVariance())
                If diagnostics Is Nothing Then
                    diagnostics = DiagnosticBag.GetInstance()
                End If

                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_VarianceInterfaceNesting), Locations(0)))
            End If
        End Sub

        Private Sub GenerateVarianceDiagnosticsForInterface(
            <[In], Out> ByRef diagnostics As DiagnosticBag,
            <[In], Out> ByRef infosBuffer As ArrayBuilder(Of DiagnosticInfo)
        )
            ' Dev10 didn't do this shortcut, but I and Lucian believe that the checks below
            ' can be safely skipped for an invariant interface.
            If Not Me.HasVariance() Then
                Return
            End If

            ' Variance spec $2:
            ' An interface I is valid if and only if
            '  * every method signature in I is valid and has no variant generic parameters (no variant generic
            '    parameters part is handled by SourceMethodSymbol.BindTypeParameterConstraints), and
            '  * every property in I is valid, and
            '  * every event in I is valid, and
            '  * every immediate base interface type of I is valid covariantly, and
            '  * the interface is either invariant or it lacks nested classes and structs, and
            '  * every nested type is valid.
            '
            ' A property "Property Foo as T" is valid if and only if either
            '  * The property is read-only and T is valid covariantly, or
            '  * The property is write-only and T is valid invariantly, or
            '  * The property is readable and writable and T is invariant.
            '
            ' An event "Event e as D" is valid if and only if
            '  * the delegate type D is valid contravariantly
            '
            ' An event "Event e(Signature)" is valid if and only if
            '  * it is not contained (not even nested) in a variant interface, 
            '    this is handled by SynthesizedEventDelegateSymbol.
            '
            ' The test that nested types are valid isn't needed, since GenerateVarianceDiagnostics
            ' will anyways be called on them all. (and for any invalid nested type, we report the
            ' error on it, rather than on this container.)
            '
            ' The check that an interface lacks nested classes and structs is done inside
            ' ReportNestingIntoVariantInterface. Why? Because we have to look for indirectly
            ' nested classes/structs, not just immediate ones. And it seemed nicer for classes/structs
            ' to look UP for variant containers, rather than for interfaces to look DOWN for class/struct contents.

            For Each batch As ImmutableArray(Of Symbol) In GetMembersAndInitializers().Members.Values
                For Each member As Symbol In batch
                    If Not member.IsImplicitlyDeclared Then
                        Select Case member.Kind
                            Case SymbolKind.Method
                                GenerateVarianceDiagnosticsForMethod(DirectCast(member, MethodSymbol), diagnostics, infosBuffer)
                                Debug.Assert(Not HaveDiagnostics(infosBuffer))

                            Case SymbolKind.Property
                                GenerateVarianceDiagnosticsForProperty(DirectCast(member, PropertySymbol), diagnostics, infosBuffer)
                                Debug.Assert(Not HaveDiagnostics(infosBuffer))

                            Case SymbolKind.Event
                                GenerateVarianceDiagnosticsForEvent(DirectCast(member, EventSymbol), diagnostics, infosBuffer)
                                Debug.Assert(Not HaveDiagnostics(infosBuffer))

                        End Select
                    End If
                Next
            Next

            ' 3. every immediate base interface is valid covariantly.
            ' Actually, the only type that's invalid covariantly and allowable as a base interface,
            ' is a generic instantiation X(T1,...) where we've instantiated it wrongly (e.g. given it "Out Ti"
            ' for a generic parameter that was declared as an "In"). Look what happens:
            ' Interface IZoo(Of In T)     | Dim x as IZoo(Of Animal)
            '   Inherits IReadOnly(Of T)  | Dim y as IZoo(Of Mammal) = x   ' through contravariance of IZoo
            ' End Interface               | Dim z as IReadOnly(Of Mammal) = y  ' through inheritance from IZoo
            ' Now we might give "z" to someone who's expecting to read only Mammals, even though we know the zoo
            ' contains all kinds of animals.
            For Each implemented As NamedTypeSymbol In Me.InterfacesNoUseSiteDiagnostics
                If Not implemented.IsErrorType() Then
                    Debug.Assert(Not HaveDiagnostics(infosBuffer))
                    GenerateVarianceDiagnosticsForType(implemented, VarianceKind.Out, VarianceContext.Complex, infosBuffer)
                    If HaveDiagnostics(infosBuffer) Then
                        ReportDiagnostics(diagnostics, GetInheritsOrImplementsLocation(implemented, getInherits:=True), infosBuffer)
                    End If
                End If
            Next
        End Sub

        ' Gets the implements location for a particular interface, which must be implemented but might be indirectly implemented.
        ' Also gets the direct interface it was inherited through
        Private Function GetImplementsLocation(implementedInterface As NamedTypeSymbol, ByRef directInterface As NamedTypeSymbol) As Location
            Debug.Assert(Me.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Contains(implementedInterface))

            ' Find the directly implemented interface that "implementedIface" was inherited through.
            directInterface = Nothing

            For Each iface In Me.InterfacesNoUseSiteDiagnostics
                If iface = implementedInterface Then
                    directInterface = iface
                    Exit For
                ElseIf directInterface Is Nothing AndAlso iface.ImplementsInterface(implementedInterface, useSiteDiagnostics:=Nothing) Then
                    directInterface = iface
                End If
            Next

            Debug.Assert(directInterface IsNot Nothing)

            Return GetInheritsOrImplementsLocation(directInterface, Me.IsInterfaceType())
        End Function

        Private Function GetImplementsLocation(implementedInterface As NamedTypeSymbol) As Location
            Dim dummy As NamedTypeSymbol = Nothing
            Return GetImplementsLocation(implementedInterface, dummy)
        End Function

        Protected MustOverride Function GetInheritsOrImplementsLocation(base As NamedTypeSymbol, getInherits As Boolean) As Location

        Private Sub GenerateVarianceDiagnosticsForDelegate(
            <[In], Out> ByRef diagnostics As DiagnosticBag,
            <[In], Out> ByRef infosBuffer As ArrayBuilder(Of DiagnosticInfo)
        )
            ' Dev10 didn't do this shortcut, but I and Lucian believe that the checks below
            ' can be safely skipped for an invariant interface.
            If Not Me.HasVariance() Then
                Return
            End If

            ' Variance spec $2
            ' A delegate "Delegate Function/Sub Foo(Of T1, ... Tn)Signature" is valid if and only if
            '  * the signature is valid.
            '
            ' That delegate becomes "Class Foo(Of T1, ... Tn) : Function Invoke(...) As ... : End Class
            ' So we just need to pick up the "Invoke" method and check that it's valid.
            ' NB. that delegates can have variance in their generic params, and hence so can e.g. "Class Foo(Of Out T1)"
            ' This is the only place in the CLI where a class can have variant generic params.
            '
            ' Note: delegates that are synthesized from events are already dealt with in
            ' SynthesizedEventDelegateSymbol.GenerateAllDeclarationErrors, so we won't run into them here.

            Dim invoke As MethodSymbol = Me.DelegateInvokeMethod

            If invoke IsNot Nothing Then
                GenerateVarianceDiagnosticsForMethod(invoke, diagnostics, infosBuffer)
            End If
        End Sub

        Private Shared Sub ReportDiagnostics(
            <[In], Out> ByRef diagnostics As DiagnosticBag,
            location As Location,
            infos As ArrayBuilder(Of DiagnosticInfo)
        )
            If diagnostics Is Nothing Then
                diagnostics = DiagnosticBag.GetInstance()
            End If

            For Each info In infos
                diagnostics.Add(info, location)
            Next

            infos.Clear()
        End Sub

        Private Shared Function HaveDiagnostics(diagnostics As ArrayBuilder(Of DiagnosticInfo)) As Boolean
            Return diagnostics IsNot Nothing AndAlso diagnostics.Count > 0
        End Function

        ''' <summary>
        ''' Following enum is used just to help give more specific error messages.
        ''' </summary>
        Private Enum VarianceContext
            ' We'll give specific error messages in these simple contexts:
            [ByVal]
            [ByRef]
            [Return]
            [Constraint]
            Nullable
            ReadOnlyProperty
            WriteOnlyProperty
            [Property]

            ' Otherwise (e.g. nested inside a generic) we use the following as a catch-all:
            Complex
        End Enum

        Private Sub GenerateVarianceDiagnosticsForType(
            type As TypeSymbol,
            requiredVariance As VarianceKind,
            context As VarianceContext,
            <[In], Out> ByRef diagnostics As ArrayBuilder(Of DiagnosticInfo)
        )
            GenerateVarianceDiagnosticsForTypeRecursively(type, requiredVariance, context, Nothing, 0, diagnostics)
        End Sub

        Private Shared Sub AppendVarianceDiagnosticInfo(
            <[In], Out> ByRef diagnostics As ArrayBuilder(Of DiagnosticInfo),
            info As DiagnosticInfo
        )
            If diagnostics Is Nothing Then
                diagnostics = ArrayBuilder(Of DiagnosticInfo).GetInstance()
            End If

            diagnostics.Add(info)
        End Sub

        Private Structure VarianceDiagnosticsTargetTypeParameter
            Public ReadOnly ConstructedType As NamedTypeSymbol
            Private ReadOnly _typeParameterIndex As Integer

            Public ReadOnly Property TypeParameter As TypeParameterSymbol
                Get
                    Return ConstructedType.TypeParameters(_typeParameterIndex)
                End Get
            End Property

            Public Sub New(constructedType As NamedTypeSymbol, typeParameterIndex As Integer)
                Debug.Assert(typeParameterIndex >= 0 AndAlso typeParameterIndex < constructedType.Arity)
                Me.ConstructedType = constructedType
                _typeParameterIndex = typeParameterIndex
            End Sub
        End Structure

        Private Sub GenerateVarianceDiagnosticsForTypeRecursively(
            type As TypeSymbol,
            requiredVariance As VarianceKind,
            context As VarianceContext,
            typeParameterInfo As VarianceDiagnosticsTargetTypeParameter,
            constructionDepth As Integer,
            <[In], Out> ByRef diagnostics As ArrayBuilder(Of DiagnosticInfo)
        )
            ' Variance spec $2:
            '
            ' A type T is valid invariantly if and only if:
            '  * it is valid covariantly, and
            '  * it is valid contravariantly.
            '
            ' A type T is valid covariantly if and only if one of the following hold: either
            '  * T is a generic parameter which was not declared contravariant, or
            '  * T is an array type U() where U is valid covariantly, or
            '  * T is a construction X1(Of T11...)...Xn(Of Tn1...) of some generic struct/class/interface/delegate Xn
            '    declared as X1(Of X11...)...Xn(Of Xn1...) such that for each i and j,
            '      - if Xij was declared covariant then Tij is valid covariantly
            '      - if Xij was declared contravariant then Tij is valid contravariantly
            '      - if Xij was declared invariant then Tij is valid invariantly
            '  * or T is a non-generic struct/class/interface/delegate/enum.
            '
            ' A type T is valid contravariantly if and only if one of the following hold: either
            '  * T is a generic parameter which was not declared covariant, or
            '  * T is an array type U() where U is valid contravariantly, or
            '  * T is a construction X1(Of T11...)...Xn(Of Tn1...) of some generic struct/class/interface/delegate Xn
            '    declared as X1(Of X11...)...Xn(Of Xn1...) such that for each i and j,
            '      - if Xij was declared covariant then Tij is valid contravariantly
            '      - if Xij was declared contravariant then Tij is valid covariantly
            '      - if Xij was declared invariant then Tij is valid invariantly
            '  * or T is a non-generic struct/class/interface/delegate/enum.
            '
            '
            ' In all cases, if a type fails a variance validity check, then it ultimately failed
            ' because somewhere there were one or more generic parameters "T" which were declared with
            ' the wrong kind of variance. In particular, they were either declared In when they'd have
            ' to be Out or InOut, or they were declared Out when they'd have to be In or InOut.
            ' We mark all these as errors.
            '
            ' BUT... CLS restrictions say that in any generic type, all nested types first copy their
            ' containers's generic parameters. This restriction is embodied in the BCSYM structure.
            '    SOURCE:                        BCSYM:                               IL:
            '    Interface I(Of Out T1)         Interface"I"/genericparams=T1        .interface I(Of Out T1)
            '      Interface J : End Interface    Interface"J"/no genericparams         .interface J(Of Out T1)
            '      Sub f(ByVal x as J)            ... GenericTypeBinding(J,args=[],       .proc f(x As J(Of T1))
            '    End Interface                                     parentargs=I[T1])
            ' Observe that, by construction, any time we use a nested type like J in a contravariant position
            ' then it's bound to be invalid. If we simply applied the previous paragraph then we'd emit a
            ' confusing error to the user like "J is invalid because T1 is an Out parameter". So we want
            ' to do a better job of reporting errors. In particular,
            '   * If we are checking a GenericTypeBinding (e.g. x as J(Of T1)) for contravariant validity, look up
            '     to find the outermost ancestor binding (e.g. parentargs=I[T1]) which is of a variant interface.
            '     If this is also the outermost variant container of the current context, then it's an error.

            Select Case type.Kind
                Case SymbolKind.TypeParameter
                    ' 1. if T is a generic parameter which was declared wrongly
                    Dim typeParam = DirectCast(type, TypeParameterSymbol)

                    If (typeParam.Variance = VarianceKind.Out AndAlso requiredVariance <> VarianceKind.Out) OrElse
                        (typeParam.Variance = VarianceKind.In AndAlso requiredVariance <> VarianceKind.In) Then

                        ' The error is either because we have an "Out" param and Out is inappropriate here,
                        ' or we used an "In" param and In is inappropriate here. This flag says which:
                        Dim inappropriateOut As Boolean = (typeParam.Variance = VarianceKind.Out)

                        ' OKAY, so now we need to report an error. Simple enough, but we've tried to give helpful
                        ' context-specific error messages to the user, and so the code has to work through a lot
                        ' of special cases.

                        Select Case context
                            Case VarianceContext.ByVal
                                ' "Type '|1' cannot be used as a ByVal parameter type because '|1' is an 'Out' type parameter."
                                Debug.Assert(inappropriateOut, "unexpected: an variance error in ByVal must be due to an inappropriate out")
                                AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceOutByValDisallowed1, type.Name))

                            Case VarianceContext.ByRef
                                ' "Type '|1' cannot be used in this context because 'In' and 'Out' type parameters cannot be used for ByRef parameter types, and '|1' is an 'Out/In' type parameter."
                                AppendVarianceDiagnosticInfo(diagnostics,
                                                             ErrorFactory.ErrorInfo(If(inappropriateOut,
                                                                                       ERRID.ERR_VarianceOutByRefDisallowed1,
                                                                                       ERRID.ERR_VarianceInByRefDisallowed1),
                                                                                    type.Name))

                            Case VarianceContext.Return
                                ' "Type '|1' cannot be used as a return type because '|1' is an 'In' type parameter."
                                Debug.Assert(Not inappropriateOut, "unexpected: a variance error in Return Type must be due to an inappropriate in")
                                AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceInReturnDisallowed1, type.Name))

                            Case VarianceContext.Constraint
                                ' "Type '|1' cannot be used as a generic type constraint because '|1' is an 'Out' type parameter."
                                Debug.Assert(inappropriateOut, "unexpected: a variance error in Constraint must be due to an inappropriate out")
                                AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceOutConstraintDisallowed1, type.Name))

                            Case VarianceContext.Nullable
                                ' "Type '|1' cannot be used in '|2' because 'In' and 'Out' type parameters cannot be made nullable, and '|1' is an 'In/Out' type parameter."
                                AppendVarianceDiagnosticInfo(diagnostics,
                                                             ErrorFactory.ErrorInfo(If(inappropriateOut,
                                                                                       ERRID.ERR_VarianceOutNullableDisallowed2,
                                                                                       ERRID.ERR_VarianceInNullableDisallowed2),
                                                                                    type.Name,
                                                                                    CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType)))

                            Case VarianceContext.ReadOnlyProperty
                                ' "Type '|1' cannot be used as a ReadOnly property type because '|1' is an 'In' type parameter."
                                Debug.Assert(Not inappropriateOut, "unexpected: a variance error in ReadOnlyProperty must be due to an inappropriate in")
                                AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceInReadOnlyPropertyDisallowed1, type.Name))

                            Case VarianceContext.WriteOnlyProperty
                                ' "Type '|1' cannot be used as a WriteOnly property type because '|1' is an 'Out' type parameter."
                                Debug.Assert(inappropriateOut, "unexpected: a variance error in WriteOnlyProperty must be due to an inappropriate out")
                                AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceOutWriteOnlyPropertyDisallowed1, type.Name))

                            Case VarianceContext.Property
                                ' "Type '|1' cannot be used as a property type in this context because '|1' is an 'Out/In' type parameter and the property is not marked ReadOnly/WriteOnly.")
                                AppendVarianceDiagnosticInfo(diagnostics,
                                                             ErrorFactory.ErrorInfo(If(inappropriateOut,
                                                                                       ERRID.ERR_VarianceOutPropertyDisallowed1,
                                                                                       ERRID.ERR_VarianceInPropertyDisallowed1),
                                                                                    type.Name))

                            Case VarianceContext.Complex

                                ' Otherwise, we're in "VarianceContextComplex" property. And so the error message needs
                                ' to spell out precisely where in the context we are:
                                ' "Type '|1' cannot be used in this context because '|1' is an 'Out|In' type parameter."
                                ' "Type '|1' cannot be used for the '|2' in '|3' in this context because '|1' is an 'Out|In' type parameter."
                                ' "Type '|1' cannot be used in '|2' in this context because '|1' is an 'Out|In' type parameter."
                                ' "Type '|1' cannot be used in '|2' for the '|3' in '|4' in this context because '|1' is an 'Out' type parameter."
                                ' We need the "in '|2' here" clause when ErrorBindingIsNested, to show which instantiation we're talking about.
                                ' We need the "for the '|3' in '|4'" when ErrorBinding->GetGenericParamCount()>1

                                If typeParameterInfo.ConstructedType Is Nothing Then
                                    ' "Type '|1' cannot be used in this context because '|1' is an 'Out|In' type parameter."
                                    ' Used for simple errors where the erroneous generic-param is NOT inside a generic binding:
                                    ' e.g. "Sub f(ByVal a as O)" for some parameter declared as "Out O"
                                    ' gives the error "An 'Out' parameter like 'O' cannot be user here".
                                    AppendVarianceDiagnosticInfo(diagnostics,
                                                                 ErrorFactory.ErrorInfo(If(inappropriateOut,
                                                                                           ERRID.ERR_VarianceOutParamDisallowed1,
                                                                                           ERRID.ERR_VarianceInParamDisallowed1),
                                                                                        type.Name))
                                ElseIf constructionDepth <= 1 Then
                                    If typeParameterInfo.ConstructedType.Arity <= 1 Then
                                        ' "Type '|1' cannot be used in this context because '|1' is an 'Out|In' type parameter."
                                        ' e.g. "Sub f(ByVal a As IEnumerable(Of O))" yields
                                        ' "An 'Out' parameter like 'O' cannot be used here."
                                        AppendVarianceDiagnosticInfo(diagnostics,
                                                                     ErrorFactory.ErrorInfo(If(inappropriateOut,
                                                                                               ERRID.ERR_VarianceOutParamDisallowed1,
                                                                                               ERRID.ERR_VarianceInParamDisallowed1),
                                                                                            type.Name))
                                    Else
                                        Debug.Assert(typeParameterInfo.ConstructedType.Arity > 1)
                                        ' "Type '|1' cannot be used for the '|2' in '|3' in this context because '|1' is an 'Out|In' type parameter."
                                        ' e.g. "Sub f(ByVal a As IDoubleEnumerable(Of O,I)) yields
                                        ' "An 'Out' parameter like 'O' cannot be used for type parameter 'T1' of 'IDoubleEnumerable(Of T1,T2)'."
                                        AppendVarianceDiagnosticInfo(diagnostics,
                                                                     ErrorFactory.ErrorInfo(If(inappropriateOut,
                                                                                               ERRID.ERR_VarianceOutParamDisallowedForGeneric3,
                                                                                               ERRID.ERR_VarianceInParamDisallowedForGeneric3),
                                                                                            type.Name,
                                                                                            typeParameterInfo.TypeParameter.Name,
                                                                                            CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType.OriginalDefinition)))
                                    End If
                                Else
                                    Debug.Assert(constructionDepth > 1)
                                    If typeParameterInfo.ConstructedType.Arity <= 1 Then
                                        ' "Type '|1' cannot be used in '|2' in this context because '|1' is an 'Out|In' type parameter."
                                        ' e.g. "Sub f(ByVal a as Func(Of IEnumerable(Of O), IEnumerable(Of O))" yields
                                        ' "In 'IEnumerable(Of O)' here, an 'Out' parameter like 'O' cannot be used."
                                        AppendVarianceDiagnosticInfo(diagnostics,
                                                                     ErrorFactory.ErrorInfo(If(inappropriateOut,
                                                                                               ERRID.ERR_VarianceOutParamDisallowedHere2,
                                                                                               ERRID.ERR_VarianceInParamDisallowedHere2),
                                                                                            type.Name,
                                                                                            CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType)))

                                    Else
                                        Debug.Assert(typeParameterInfo.ConstructedType.Arity > 1)
                                        ' "Type '|1' cannot be used in '|2' for the '|3' in '|4' in this context because '|1' is an 'Out' type parameter."
                                        ' e.g. "Sub f(ByVal a as IEnumerable(Of Func(Of O,O))" yields
                                        ' "In 'Func(Of O,O)' here, an 'Out' parameter like 'O' cannot be used for type parameter 'Tresult' of 'Func(Of Tresult,T)'."
                                        AppendVarianceDiagnosticInfo(diagnostics,
                                                                     ErrorFactory.ErrorInfo(If(inappropriateOut,
                                                                                               ERRID.ERR_VarianceOutParamDisallowedHereForGeneric4,
                                                                                               ERRID.ERR_VarianceInParamDisallowedHereForGeneric4),
                                                                                            type.Name,
                                                                                            CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType),
                                                                                            typeParameterInfo.TypeParameter.Name,
                                                                                            CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType.OriginalDefinition)))

                                    End If
                                End If

                            Case Else
                                Throw ExceptionUtilities.UnexpectedValue(context)
                        End Select
                    End If

                Case SymbolKind.ArrayType
                    ' 2. if T is an array U():
                    GenerateVarianceDiagnosticsForTypeRecursively(DirectCast(type, ArrayTypeSymbol).ElementType,
                                                                  requiredVariance,
                                                                  context,
                                                                  typeParameterInfo,
                                                                  constructionDepth,
                                                                  diagnostics)

                Case SymbolKind.NamedType
                    Dim namedType = DirectCast(type, NamedTypeSymbol)

                    If Not namedType.IsGenericType Then
                        Return
                    End If

                    ' 3. T is a construction X1(Of T11...)...Xn(Of Tn1...) of some generic struct/class/interface/delegate X1(Of X11...)...Xn(Of Xn1...)
                    ' Special check, discussed above, for better error-reporting when we find a generic binding in an
                    ' illegal contravariant position
                    If requiredVariance <> VarianceKind.Out Then
                        Dim outermostVarianceContainerOfType As NamedTypeSymbol = Nothing
                        Dim container As NamedTypeSymbol = type.ContainingType

                        While container IsNot Nothing
                            If container.TypeParameters.HaveVariance() Then
                                outermostVarianceContainerOfType = container.OriginalDefinition
                            End If

                            container = container.ContainingType
                        End While

                        Dim outermostVarianceContainerOfContext As NamedTypeSymbol = Nothing
                        container = Me

                        Do
                            If container.TypeParameters.HaveVariance() Then
                                outermostVarianceContainerOfContext = container
                            End If

                            container = container.ContainingType
                        Loop While container IsNot Nothing

                        If outermostVarianceContainerOfType IsNot Nothing AndAlso outermostVarianceContainerOfType Is outermostVarianceContainerOfContext Then
                            ' ERRID_VarianceTypeDisallowed2.               "Type '|1' cannot be used in this context because both the context and the definition of '|1' are nested within type '|2', and '|2' has 'In' or 'Out' type parameters. Consider moving '|1' outside of '|2'."
                            ' ERRID_VarianceTypeDisallowedForGeneric4.     "Type '|1' cannot be used for the '|3' in '|4' in this context because both the context and the definition of '|1' are nested within type '|2', and '|2' has 'In' or 'Out' type parameters. Consider moving '|1' outside of '|2'."
                            ' ERRID_VarianceTypeDisallowedHere3.           "Type '|1' cannot be used in '|3' in this context because both the context and the definition of '|1' are nested within type '|2', and '|2' has 'In' or 'Out' type parameters. Consider moving '|1' outside of '|2'."
                            ' ERRID_VarianceTypeDisallowedHereForGeneric5. "Type '|1' cannot be used for the '|4' of '|5' in '|3' in this context because both the context and the definition of '|1' are nested within type '|2', and '|2' has 'In' or 'Out' type parameters. Consider moving '|1' outside of '|2'."
                            If typeParameterInfo.ConstructedType Is Nothing Then
                                AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceTypeDisallowed2,
                                                                                                 CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(type.OriginalDefinition),
                                                                                                 CustomSymbolDisplayFormatter.QualifiedName(outermostVarianceContainerOfType)))
                            ElseIf constructionDepth <= 1 Then
                                If typeParameterInfo.ConstructedType.Arity <= 1 Then
                                    AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceTypeDisallowed2,
                                                                                                     CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(type.OriginalDefinition),
                                                                                                     CustomSymbolDisplayFormatter.QualifiedName(outermostVarianceContainerOfType)))
                                Else
                                    Debug.Assert(typeParameterInfo.ConstructedType.Arity > 1)
                                    AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceTypeDisallowedForGeneric4,
                                                                                                     CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(type.OriginalDefinition),
                                                                                                     CustomSymbolDisplayFormatter.QualifiedName(outermostVarianceContainerOfType),
                                                                                                     typeParameterInfo.TypeParameter.Name,
                                                                                                     CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType.OriginalDefinition)))
                                End If
                            Else
                                Debug.Assert(constructionDepth > 1)
                                If typeParameterInfo.ConstructedType.Arity <= 1 Then
                                    AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceTypeDisallowedHere3,
                                                                                                     CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(type.OriginalDefinition),
                                                                                                     CustomSymbolDisplayFormatter.QualifiedName(outermostVarianceContainerOfType),
                                                                                                     CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType)))
                                Else
                                    Debug.Assert(typeParameterInfo.ConstructedType.Arity > 1)
                                    AppendVarianceDiagnosticInfo(diagnostics, ErrorFactory.ErrorInfo(ERRID.ERR_VarianceTypeDisallowedHereForGeneric5,
                                                                                                     CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(type.OriginalDefinition),
                                                                                                     CustomSymbolDisplayFormatter.QualifiedName(outermostVarianceContainerOfType),
                                                                                                     CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType),
                                                                                                     typeParameterInfo.TypeParameter.Name,
                                                                                                     CustomSymbolDisplayFormatter.QualifiedName(typeParameterInfo.ConstructedType.OriginalDefinition)))
                                End If
                            End If

                            Return
                        End If
                    End If

                    ' The general code below will catch the case of nullables "T?" or "Nullable(Of T)", which require T to
                    ' be invariant. But we want more specific error reporting for this case, so we check for it first.
                    If namedType.IsNullableType() Then
                        Debug.Assert(namedType.TypeParameters(0).Variance = VarianceKind.None, "unexpected: a nullable type should have one generic parameter with no variance")
                        If namedType.TypeArgumentsNoUseSiteDiagnostics(0).IsValueType Then
                            GenerateVarianceDiagnosticsForTypeRecursively(namedType.TypeArgumentsNoUseSiteDiagnostics(0),
                                                                          VarianceKind.None,
                                                                          VarianceContext.Nullable,
                                                                          New VarianceDiagnosticsTargetTypeParameter(namedType, 0),
                                                                          constructionDepth,
                                                                          diagnostics)
                        End If

                        Return
                    End If

                    ' "Type" will refer to the last generic binding, Xn(Of Tn1...). So we have to check all the way up to X1.
                    Do
                        For argumentIndex As Integer = 0 To namedType.Arity - 1
                            ' nb. the InvertVariance() here is the only difference between covariantly-valid and contravariantly-valid
                            ' for generic constructions.
                            Dim argumentRequiredVariance As VarianceKind

                            Select Case requiredVariance
                                Case VarianceKind.In
                                    Select Case namedType.TypeParameters(argumentIndex).Variance
                                        Case VarianceKind.In
                                            argumentRequiredVariance = VarianceKind.Out
                                        Case VarianceKind.Out
                                            argumentRequiredVariance = VarianceKind.In
                                        Case Else
                                            argumentRequiredVariance = VarianceKind.None
                                    End Select
                                Case VarianceKind.Out
                                    argumentRequiredVariance = namedType.TypeParameters(argumentIndex).Variance
                                Case Else
                                    argumentRequiredVariance = VarianceKind.None
                            End Select

                            GenerateVarianceDiagnosticsForTypeRecursively(namedType.TypeArgumentsNoUseSiteDiagnostics(argumentIndex),
                                                                          argumentRequiredVariance,
                                                                          VarianceContext.Complex,
                                                                          New VarianceDiagnosticsTargetTypeParameter(namedType, argumentIndex),
                                                                          constructionDepth + 1,
                                                                          diagnostics)
                        Next

                        namedType = namedType.ContainingType
                    Loop While namedType IsNot Nothing

                Case SymbolKind.ErrorType
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(type.Kind)
            End Select
        End Sub

        Private Sub GenerateVarianceDiagnosticsForMethod(
            method As MethodSymbol,
            <[In], Out> ByRef diagnostics As DiagnosticBag,
            <[In], Out> ByRef infosBuffer As ArrayBuilder(Of DiagnosticInfo)
        )
            Debug.Assert(Not HaveDiagnostics(infosBuffer))

            Select Case method.MethodKind
                Case MethodKind.EventAdd, MethodKind.EventRemove, MethodKind.PropertyGet, MethodKind.PropertySet
                    Return
            End Select

            GenerateVarianceDiagnosticsForParameters(method.Parameters, diagnostics, infosBuffer)

            ' Return type is valid covariantly
            Debug.Assert(Not HaveDiagnostics(infosBuffer))
            GenerateVarianceDiagnosticsForType(method.ReturnType, VarianceKind.Out, VarianceContext.Return, infosBuffer)
            If HaveDiagnostics(infosBuffer) Then
                Dim location As Location
                Dim syntax As MethodBaseSyntax = method.GetDeclaringSyntaxNode(Of MethodBaseSyntax)()

                If syntax Is Nothing AndAlso method.MethodKind = MethodKind.DelegateInvoke Then
                    syntax = method.ContainingType.GetDeclaringSyntaxNode(Of MethodBaseSyntax)()
                End If

                Dim asClause As AsClauseSyntax = If(syntax IsNot Nothing, syntax.AsClauseInternal, Nothing)

                If asClause IsNot Nothing Then
                    location = asClause.Type.GetLocation()
                Else
                    location = method.Locations(0)
                End If

                ReportDiagnostics(diagnostics, location, infosBuffer)
            End If

            GenerateVarianceDiagnosticsForConstraints(method.TypeParameters, diagnostics, infosBuffer)
        End Sub

        Private Sub GenerateVarianceDiagnosticsForParameters(
            parameters As ImmutableArray(Of ParameterSymbol),
            <[In], Out> ByRef diagnostics As DiagnosticBag,
            <[In], Out> ByRef infosBuffer As ArrayBuilder(Of DiagnosticInfo)
        )
            Debug.Assert(Not HaveDiagnostics(infosBuffer))

            ' Each ByVal Pi is valid contravariantly, and each ByRef Pi is valid invariantly
            For Each param As ParameterSymbol In parameters
                Dim requiredVariance As VarianceKind
                Dim context As VarianceContext

                If param.IsByRef Then
                    requiredVariance = VarianceKind.None
                    context = VarianceContext.ByRef
                Else
                    requiredVariance = VarianceKind.In
                    context = VarianceContext.ByVal
                End If

                GenerateVarianceDiagnosticsForType(param.Type, requiredVariance, context, infosBuffer)
                If HaveDiagnostics(infosBuffer) Then
                    Dim location As Location
                    Dim syntax As ParameterSyntax = param.GetDeclaringSyntaxNode(Of ParameterSyntax)()

                    If syntax IsNot Nothing AndAlso syntax.AsClause IsNot Nothing Then
                        location = syntax.AsClause.Type.GetLocation()
                    Else
                        location = param.Locations(0)
                    End If

                    ReportDiagnostics(diagnostics, location, infosBuffer)
                End If
            Next
        End Sub

        Private Sub GenerateVarianceDiagnosticsForConstraints(
            parameters As ImmutableArray(Of TypeParameterSymbol),
            <[In], Out> ByRef diagnostics As DiagnosticBag,
            <[In], Out> ByRef infosBuffer As ArrayBuilder(Of DiagnosticInfo)
        )
            Debug.Assert(Not HaveDiagnostics(infosBuffer))

            ' Each constraint on U1...Un is valid contravariantly
            ' "It is character-building to consider why this is required" [Eric Lippert, 2008]
            ' Interface IReadOnly(Of Out T) | Class Zoo(Of T)            |  Dim m As IReadOnly(Of Mammal) = new Zoo(Of Mammal)  ' OK through inheritance
            ' Sub Fun(Of U As T)()          | Implements IReadOnly(Of T) |  Dim a as IReadOnly(Of Animal) = m   ' OK through covariance
            ' End Interface                 | End Class                  |  a.Fun(Of Fish)() ' BAD: Fish is an Animal (satisfies "U as T"), but fun is expecting a mammal!

            For Each param As TypeParameterSymbol In parameters
                For Each constraint As TypeSymbol In param.ConstraintTypesNoUseSiteDiagnostics
                    GenerateVarianceDiagnosticsForType(constraint, VarianceKind.In, VarianceContext.Constraint, infosBuffer)
                    If HaveDiagnostics(infosBuffer) Then
                        Dim location As Location = param.Locations(0)

                        For Each constraintInfo As TypeParameterConstraint In param.GetConstraints()
                            If constraintInfo.TypeConstraint IsNot Nothing AndAlso
                               constraintInfo.TypeConstraint.IsSameTypeIgnoringCustomModifiers(constraint) Then
                                location = constraintInfo.LocationOpt
                                Exit For
                            End If
                        Next

                        ReportDiagnostics(diagnostics, location, infosBuffer)
                    End If
                Next
            Next
        End Sub

        Private Sub GenerateVarianceDiagnosticsForProperty(
            [property] As PropertySymbol,
            <[In], Out> ByRef diagnostics As DiagnosticBag,
            <[In], Out> ByRef infosBuffer As ArrayBuilder(Of DiagnosticInfo)
        )
            Debug.Assert(Not HaveDiagnostics(infosBuffer))

            ' Gettable: requires covariance. Settable: requires contravariance. Gettable and settable: requires invariance.
            Dim requiredVariance As VarianceKind
            Dim context As VarianceContext

            If [property].IsReadOnly Then
                requiredVariance = VarianceKind.Out
                context = VarianceContext.ReadOnlyProperty
            ElseIf [property].IsWriteOnly Then
                requiredVariance = VarianceKind.In
                context = VarianceContext.WriteOnlyProperty
            Else
                requiredVariance = VarianceKind.None
                context = VarianceContext.Property
            End If

            GenerateVarianceDiagnosticsForType([property].Type, requiredVariance, context, infosBuffer)
            If HaveDiagnostics(infosBuffer) Then
                Dim location As Location
                Dim syntax As PropertyStatementSyntax = [property].GetDeclaringSyntaxNode(Of PropertyStatementSyntax)()

                If syntax IsNot Nothing AndAlso syntax.AsClause IsNot Nothing Then
                    location = syntax.AsClause.Type.GetLocation()
                Else
                    location = [property].Locations(0)
                End If

                ReportDiagnostics(diagnostics, location, infosBuffer)
            End If

            ' A property might be declared with extra parameters, so we have to check that these are variance-valid.
            GenerateVarianceDiagnosticsForParameters([property].Parameters, diagnostics, infosBuffer)
        End Sub

        Private Sub GenerateVarianceDiagnosticsForEvent(
            [event] As EventSymbol,
            <[In], Out> ByRef diagnostics As DiagnosticBag,
            <[In], Out> ByRef infosBuffer As ArrayBuilder(Of DiagnosticInfo)
        )
            Debug.Assert(Not HaveDiagnostics(infosBuffer))

            Dim type As TypeSymbol = [event].Type

            If Not type.IsDelegateType() Then
                Return
            End If

            If type.ImplicitlyDefinedBy() Is [event] Then
                Return
            End If

            GenerateVarianceDiagnosticsForType(type, VarianceKind.In, VarianceContext.Complex, infosBuffer)

            If HaveDiagnostics(infosBuffer) Then
                Dim location As Location
                Dim syntax As EventStatementSyntax = [event].GetDeclaringSyntaxNode(Of EventStatementSyntax)()

                If syntax IsNot Nothing AndAlso syntax.AsClause IsNot Nothing Then
                    location = syntax.AsClause.Type.GetLocation()
                Else
                    location = [event].Locations(0)
                End If

                ReportDiagnostics(diagnostics, location, infosBuffer)
            End If
        End Sub

        ''' <summary>
        ''' Ensure all attributes on all members in the named type are bound.
        ''' </summary>
        Private Sub BindAllMemberAttributes(cancellationToken As CancellationToken)
            ' Ensure all members are declared
            Dim lookup = Me.MemberAndInitializerLookup

            Dim haveExtensionMethods As Boolean = False

            ' Now bind all attributes on all members.  This must be done after the members are declared to avoid
            ' infinite recursion.
            For Each syms In lookup.Members.Values
                For Each sym In syms
                    sym.GetAttributes()

                    ' Make a note of extension methods
                    If Not haveExtensionMethods Then
                        haveExtensionMethods = (sym.Kind = SymbolKind.Method AndAlso DirectCast(sym, MethodSymbol).IsExtensionMethod)
                    End If

                    cancellationToken.ThrowIfCancellationRequested()
                Next
            Next

            If haveExtensionMethods Then
                Debug.Assert(Me.MightContainExtensionMethods)

                m_containingModule.RecordPresenceOfExtensionMethods()

                Debug.Assert(_lazyContainsExtensionMethods <> ThreeState.False)
                _lazyContainsExtensionMethods = ThreeState.True

                ' At this point we already processed all the attributes on the type.
                ' and should know whether there is an explicit Extension attribute on it.
                ' If there is an explicit attribute, or we passed through this code before,
                ' m_lazyEmitExtensionAttribute should have known value.
                If _lazyEmitExtensionAttribute = ThreeState.Unknown Then

                    ' We need to emit an Extension attribute on the type. 
                    ' Can we locate it?
                    Dim useSiteError As DiagnosticInfo = Nothing
                    m_containingModule.ContainingSourceAssembly.DeclaringCompilation.GetExtensionAttributeConstructor(useSiteError:=useSiteError)

                    If useSiteError IsNot Nothing Then
                        ' Note, we are storing false because, even though we should emit the attribute,
                        ' we can't do that due to the use site error.
                        _lazyEmitExtensionAttribute = ThreeState.False

                        ' also notify the containing assembly to not use the extension attribute
                        m_containingModule.ContainingSourceAssembly.AnErrorHasBeenReportedAboutExtensionAttribute()
                    Else
                        ' We have extension methods, we don't have explicit Extension attribute
                        ' on the type, which we were able to locate. Should emit it.
                        Debug.Assert(_lazyEmitExtensionAttribute <> ThreeState.False)
                        _lazyEmitExtensionAttribute = ThreeState.True
                    End If
                End If
            Else
                Debug.Assert(_lazyContainsExtensionMethods <> ThreeState.True)
                _lazyContainsExtensionMethods = ThreeState.False

                Debug.Assert(_lazyEmitExtensionAttribute <> ThreeState.True)
                _lazyEmitExtensionAttribute = ThreeState.False
            End If

            Debug.Assert(_lazyEmitExtensionAttribute <> ThreeState.Unknown)
            Debug.Assert(_lazyContainsExtensionMethods <> ThreeState.Unknown)
            Debug.Assert(_lazyEmitExtensionAttribute = ThreeState.False OrElse _lazyContainsExtensionMethods = ThreeState.True)
        End Sub
#End Region

#Region "Containers"

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return TryCast(_containingSymbol, NamedTypeSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return m_containingModule
            End Get
        End Property

        Public ReadOnly Property ContainingSourceModule As SourceModuleSymbol
            Get
                Return m_containingModule
            End Get
        End Property

#End Region

#Region "Flags Encoded Properties"

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return CType((_flags And SourceTypeFlags.AccessibilityMask), Accessibility)
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustInherit As Boolean
            Get
                Return (_flags And SourceTypeFlags.MustInherit) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotInheritable As Boolean
            Get
                Return (_flags And SourceTypeFlags.NotInheritable) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return (_flags And SourceTypeFlags.Shadows) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return CType((_flags And SourceTypeFlags.TypeKindMask) >> CUInt(SourceTypeFlags.TypeKindShift), TypeKind)
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return Me.TypeKind = TypeKind.Interface
            End Get
        End Property

        Friend ReadOnly Property IsPartial As Boolean
            Get
                Return (_flags And SourceTypeFlags.Partial) <> 0
            End Get
        End Property

#End Region

#Region "Syntax"

        Friend ReadOnly Property TypeDeclaration As MergedTypeDeclaration
            Get
                Return _declaration
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsScriptClass As Boolean
            Get
                Dim kind = _declaration.Declarations(0).Kind
                Return kind = DeclarationKind.Script OrElse kind = DeclarationKind.Submission
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsImplicitClass As Boolean
            Get
                Return _declaration.Declarations(0).Kind = DeclarationKind.ImplicitClass
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Arity As Integer
            Get
                Return _declaration.Arity
            End Get
        End Property

        ' Get the declaration kind. Used to match up symbols with declarations in the BinderCache.
        ' Name, Arity, and DeclarationKind must match to exactly one source symbol, even in the presence 
        ' of errors.
        Friend ReadOnly Property DeclarationKind As DeclarationKind
            Get
                Return _declaration.Kind
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property MangleName As Boolean
            Get
                Return Arity > 0
            End Get
        End Property

        ''' <summary>
        ''' Should return full emitted namespace name for a top level type if the name 
        ''' might be different in case from containing namespace symbol full name, Nothing otherwise.
        ''' 
        ''' Although namespaces unify based on case-insensitive name, VB uses the casing the namespace
        ''' declaration surround the class definition for the name emitted to metadata. 
        ''' 
        ''' Namespace FOO
        '''    Class X
        '''    End Class
        ''' ENd Namespace
        ''' Namespace foo
        '''    Class Y
        '''    End Class
        ''' ENd Namespace
        ''' 
        ''' In metadata, these are classes "FOO.X" and "foo.Y" (and thus appear in different namespaces
        ''' when imported into C#.) This function determines the casing of the namespace part of a class, if needed
        ''' to override the namespace name.
        ''' </summary>
        Friend Overrides Function GetEmittedNamespaceName() As String
            Dim containingSourceNamespace = TryCast(_containingSymbol, SourceNamespaceSymbol)
            If containingSourceNamespace IsNot Nothing AndAlso containingSourceNamespace.HasMultipleSpellings Then
                ' Find the namespace spelling surrounding the first declaration.
                Debug.Assert(Locations.Length > 0)
                Dim firstLocation = Me.DeclaringCompilation.FirstSourceLocation(Locations)
                Debug.Assert(firstLocation.IsInSource)

                Return containingSourceNamespace.GetDeclarationSpelling(firstLocation.SourceTree, firstLocation.SourceSpan.Start)
            End If

            Return Nothing
        End Function

        Friend NotOverridable Overrides Function GetLexicalSortKey() As LexicalSortKey
            ' WARNING: this should not allocate memory!
            If Not _lazyLexicalSortKey.IsInitialized Then
                _lazyLexicalSortKey.SetFrom(_declaration.GetLexicalSortKey(DeclaringCompilation))
            End If

            Return _lazyLexicalSortKey
        End Function

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Dim result = _declaration.NameLocations
                Return result
            End Get
        End Property

        ''' <summary>
        ''' Syntax references of all parts of the type declaration. 
        ''' Submission and script classes are represented by their containing <see cref="CompilationUnitSyntax"/>,
        ''' implicit class can be represented by <see cref="CompilationUnitSyntax"/> or <see cref="NamespaceBlockSyntax"/>.
        ''' </summary>
        Public ReadOnly Property SyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _declaration.SyntaxReferences
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(SyntaxReferences)
            End Get
        End Property
#End Region

#Region "Member from Syntax"
        ' Types defined in source code go through the following states, described here. 
        ' The data that is computed by each state is stored in a contained immutable object, for storage efficiency
        ' and ease of lock-free implementation.
        ' 1) Created. Only the name, accessibility, arity, typekind, type parameters (but not their names),
        '    and references to the source code declarations are known.
        '    No access to the syntax tree is needed for this state, only the declaration.
        ' 2) Nested types created. No errors are diagnosed yet. Still no access to the syntax tree is
        '    needed for this state.
        ' 3) Type parameters names, variance, and constraints are known. Errors relating to type parameters
        '    are diagnosed. This needs to be done before resolving inheritance, because the base type can
        '    involve the type parameters.
        ' 4) Inheritance resolved. The base type, interfaces, type parameter names, variance, and
        '    constraints are known. Errors relating to partial types are reported.
        ' 5) Members declared. All members of the type are created, and errors with them reported. 
        '    Errors relating to nested type are reported here also.
        '
        ' Which phases have been completed is tracked by whether the state data for that
        ' that phase has been initialized. This allows us to update phases in a lock-free
        ' manner, which avoids many deadlock possibilities. It also makes sure that diagnostics are
        ' reported once and only once.
        '
        ' The states are not visible outside the class, the class moves to the given
        ' state on demand. Care must be given in implementing the transitions to avoid
        ' deadlock or infinite recursion.

        ' Given the syntax declaration, and a container, get the symbol relating to that syntax.
        ' This is done by looking up the name from the declaration in the container, handling duplicates, arity, and
        ' so forth correctly.
        Friend Shared Function FindSymbolFromSyntax(declarationSyntax As TypeStatementSyntax,
                                                    container As NamespaceOrTypeSymbol,
                                                    sourceModule As ModuleSymbol) As SourceNamedTypeSymbol
            Dim childName As String = declarationSyntax.Identifier.ValueText
            Dim childArity As Integer = DeclarationTreeBuilder.GetArity(declarationSyntax.TypeParameterList)
            Dim childDeclKind As DeclarationKind = DeclarationTreeBuilder.GetKind(declarationSyntax.Kind)

            Return FindSymbolInContainer(childName, childArity, childDeclKind, container, sourceModule)
        End Function

        ' Given the syntax declaration, and a container, get the symbol relating to that syntax.
        ' This is done by lookup up the name from the declaration in the container, handling duplicates, arity, and
        ' so forth correctly.
        Friend Shared Function FindSymbolFromSyntax(declarationSyntax As EnumStatementSyntax,
                                                    container As NamespaceOrTypeSymbol,
                                                    sourceModule As ModuleSymbol) As SourceNamedTypeSymbol
            Dim childName As String = declarationSyntax.Identifier.ValueText
            Dim childArity As Integer = 0
            Dim childDeclKind As DeclarationKind = DeclarationTreeBuilder.GetKind(declarationSyntax.Kind)

            Return FindSymbolInContainer(childName, childArity, childDeclKind, container, sourceModule)
        End Function

        ' Given the syntax declaration, and a container, get the symbol relating to that syntax.
        ' This is done by lookup up the name from the declaration in the container, handling duplicates, arity, and
        ' so forth correctly.
        Friend Shared Function FindSymbolFromSyntax(declarationSyntax As DelegateStatementSyntax,
                                                    container As NamespaceOrTypeSymbol,
                                                    sourceModule As ModuleSymbol) As SourceNamedTypeSymbol
            Dim childName As String = declarationSyntax.Identifier.ValueText
            Dim childArity As Integer = DeclarationTreeBuilder.GetArity(declarationSyntax.TypeParameterList)
            Dim childDeclKind As DeclarationKind = VisualBasic.Symbols.DeclarationKind.Delegate

            Return FindSymbolInContainer(childName, childArity, childDeclKind, container, sourceModule)
        End Function

        ' Helper for FindSymbolFromSyntax. Finds a child source type based on name, arity, DeclarationKind.
        Private Shared Function FindSymbolInContainer(childName As String,
                                                      childArity As Integer,
                                                      childDeclKind As DeclarationKind,
                                                      container As NamespaceOrTypeSymbol,
                                                      sourceModule As ModuleSymbol) As SourceNamedTypeSymbol
            ' We need to find the correct symbol, even in error cases. There must be only one
            ' symbol that is a source symbol, defined in our module, with the given name, arity,
            ' and declaration kind. The declaration table merges together symbols with the same
            ' arity, name, and declaration kind (regardless of the Partial modifier).

            For Each child In container.GetTypeMembers(childName, childArity)
                Dim sourceType = TryCast(child, SourceNamedTypeSymbol)
                If sourceType IsNot Nothing Then
                    If (sourceType.ContainingModule Is sourceModule AndAlso
                        sourceType.DeclarationKind = childDeclKind) Then
                        Return sourceType
                    End If
                End If
            Next

            Return Nothing
        End Function

#End Region

#Region "Members (phase 5)"

        ''' <summary>
        '''  Structure to wrap the different arrays of members.
        ''' </summary>
        Friend Class MembersAndInitializers
            Friend ReadOnly Members As Dictionary(Of String, ImmutableArray(Of Symbol))
            Friend ReadOnly StaticInitializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer))
            Friend ReadOnly InstanceInitializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer))
            Friend ReadOnly StaticInitializersSyntaxLength As Integer
            Friend ReadOnly InstanceInitializersSyntaxLength As Integer

            ''' <summary>
            ''' Initializes a new instance of the <see cref="MembersAndInitializers" /> class.
            ''' </summary>
            ''' <param name="members">The members.</param>
            ''' <param name="staticInitializers">The static initializers.</param>
            ''' <param name="instanceInitializers">The instance initializers.</param>
            Friend Sub New(
                members As Dictionary(Of String, ImmutableArray(Of Symbol)),
                staticInitializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer)),
                instanceInitializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer)),
                staticInitializersSyntaxLength As Integer,
                instanceInitializersSyntaxLength As Integer)

                Me.Members = members
                Me.StaticInitializers = staticInitializers
                Me.InstanceInitializers = instanceInitializers

                Debug.Assert(staticInitializersSyntaxLength = If(staticInitializers.IsDefaultOrEmpty, 0, staticInitializers.Sum(Function(s) s.Sum(Function(i) If(Not i.IsMetadataConstant, i.Syntax.Span.Length, 0)))))
                Debug.Assert(instanceInitializersSyntaxLength = If(instanceInitializers.IsDefaultOrEmpty, 0, instanceInitializers.Sum(Function(s) s.Sum(Function(i) i.Syntax.Span.Length))))
                Me.StaticInitializersSyntaxLength = staticInitializersSyntaxLength
                Me.InstanceInitializersSyntaxLength = instanceInitializersSyntaxLength
            End Sub
        End Class

        ''' <summary>
        '''  Accumulates different members kinds used while building the members.
        ''' </summary>
        Friend NotInheritable Class MembersAndInitializersBuilder
            Friend ReadOnly Members As Dictionary(Of String, ArrayBuilder(Of Symbol)) = New Dictionary(Of String, ArrayBuilder(Of Symbol))(IdentifierComparison.Comparer)
            Friend Property StaticInitializers As ArrayBuilder(Of ImmutableArray(Of FieldOrPropertyInitializer))
            Friend Property InstanceInitializers As ArrayBuilder(Of ImmutableArray(Of FieldOrPropertyInitializer))

            Friend ReadOnly DeferredMemberDiagnostic As ArrayBuilder(Of ValueTuple(Of Symbol, Binder)) = ArrayBuilder(Of ValueTuple(Of Symbol, Binder)).GetInstance()

            Friend StaticSyntaxLength As Integer = 0
            Friend InstanceSyntaxLength As Integer = 0

            Friend Function ToReadOnlyAndFree() As MembersAndInitializers
                DeferredMemberDiagnostic.Free()

                Dim readonlyMembers = New Dictionary(Of String, ImmutableArray(Of Symbol))(IdentifierComparison.Comparer)
                For Each memberList In Members.Values
                    readonlyMembers.Add(memberList(0).Name, memberList.ToImmutableAndFree())
                Next

                Return New MembersAndInitializers(
                    readonlyMembers,
                    If(StaticInitializers IsNot Nothing, StaticInitializers.ToImmutableAndFree(), Nothing),
                    If(InstanceInitializers IsNot Nothing, InstanceInitializers.ToImmutableAndFree(), Nothing),
                    StaticSyntaxLength,
                    InstanceSyntaxLength)
            End Function
        End Class

        ''' <summary>
        ''' Adds a field initializer for the field to list of field initializers
        ''' </summary>
        ''' <param name="initializers">All initializers.</param>
        ''' <param name="computeInitializer">Compute the field initializer to add to the list of initializers.</param>
        Friend Shared Sub AddInitializer(ByRef initializers As ArrayBuilder(Of FieldOrPropertyInitializer), computeInitializer As Func(Of Integer, FieldOrPropertyInitializer), ByRef aggregateSyntaxLength As Integer)
            Dim initializer = computeInitializer(aggregateSyntaxLength)

            If initializers Is Nothing Then
                initializers = ArrayBuilder(Of FieldOrPropertyInitializer).GetInstance()
            Else
                ' initializers should be added in syntax order
                Debug.Assert(initializer.Syntax.SyntaxTree Is initializers.Last().Syntax.SyntaxTree)
                Debug.Assert(initializer.Syntax.Span.Start > initializers.Last().Syntax.Span.Start)
            End If

            initializers.Add(initializer)

            ' A constant field of type decimal needs a field initializer, so
            ' check if it is a metadata constant, not just a constant to exclude
            ' decimals. Other constants do not need field initializers.
            If Not initializer.IsMetadataConstant Then
                ' ignore leading and trailing trivia of the node
                aggregateSyntaxLength += initializer.Syntax.Span.Length
            End If
        End Sub

        ''' <summary>
        ''' Adds an array of initializers to the member collections structure
        ''' </summary>
        ''' <param name="allInitializers">All initializers.</param>
        ''' <param name="siblings">The siblings.</param>
        Friend Shared Sub AddInitializers(ByRef allInitializers As ArrayBuilder(Of ImmutableArray(Of FieldOrPropertyInitializer)), siblings As ArrayBuilder(Of FieldOrPropertyInitializer))
            If siblings IsNot Nothing Then
                If allInitializers Is Nothing Then
                    allInitializers = New ArrayBuilder(Of ImmutableArray(Of FieldOrPropertyInitializer))()
                End If

                allInitializers.Add(siblings.ToImmutableAndFree())
            End If
        End Sub

        Protected Function GetTypeMembersDictionary() As Dictionary(Of String, ImmutableArray(Of NamedTypeSymbol))
            If _lazyTypeMembers Is Nothing Then
                Interlocked.CompareExchange(_lazyTypeMembers, MakeTypeMembers(), Nothing)
                Debug.Assert(_lazyTypeMembers IsNot Nothing)
            End If
            Return _lazyTypeMembers
        End Function

        ' Create symbols for all the nested types, and put them in a lookup indexed by (case-insensitive) name.
        Private Function MakeTypeMembers() As Dictionary(Of String, ImmutableArray(Of NamedTypeSymbol))
            Dim children As ImmutableArray(Of MergedTypeDeclaration) = _declaration.Children

            Debug.Assert(s_emptyTypeMembers.Count = 0)

            If children.IsEmpty Then
                Return s_emptyTypeMembers
            End If

            Return children.Select(Function(decl) CreateNestedType(decl)).ToDictionary(
                Function(decl) decl.Name,
                IdentifierComparison.Comparer)
        End Function

        Friend Overrides Function GetTypeMembersUnordered() As ImmutableArray(Of NamedTypeSymbol)
            Return GetTypeMembersDictionary().Flatten()
        End Function

        Public Overloads Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return GetTypeMembersDictionary().Flatten(LexicalOrderSymbolComparer.Instance)
        End Function

        Public Overloads Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Dim members As ImmutableArray(Of NamedTypeSymbol) = Nothing
            If GetTypeMembersDictionary().TryGetValue(name, members) Then
                Return members
            End If
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return GetTypeMembers(name).WhereAsArray(Function(t) t.Arity = arity)
        End Function

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                If TypeKind <> TypeKind.Delegate Then
                    GetMembersAndInitializers() ' Ensure m_defaultPropertyName is set.
                Else
                    Debug.Assert(_defaultPropertyName Is Nothing)
                End If

                Return _defaultPropertyName
            End Get
        End Property

        Private ReadOnly Property MemberAndInitializerLookup As MembersAndInitializers
            Get
                Return GetMembersAndInitializers()
            End Get
        End Property

        Private Function GetMembersAndInitializers() As MembersAndInitializers
            If _lazyMembersAndInitializers Is Nothing Then
                Dim diagBag = DiagnosticBag.GetInstance()
                Dim membersAndInitializers = BuildMembersAndInitializers(diagBag)
                m_containingModule.AtomicStoreReferenceAndDiagnostics(_lazyMembersAndInitializers, membersAndInitializers, diagBag, CompilationStage.Declare)
                Debug.Assert(_lazyMembersAndInitializers IsNot Nothing)
                diagBag.Free()

                Dim unused = Me.KnownCircularStruct

#If DEBUG Then
                VerifyMembers()
#End If
            End If

            Return _lazyMembersAndInitializers
        End Function

#If DEBUG Then
        Protected Overridable Sub VerifyMembers()
        End Sub
#End If

        Friend ReadOnly Property MembersHaveBeenCreated As Boolean
            Get
                Return _lazyMembersAndInitializers IsNot Nothing
            End Get
        End Property

#If DEBUG Then
        ' A thread local hash table to catch cases when BuildMembersAndInitializers
        ' is called recursively for the same symbol. 
        <ThreadStatic>
        Private Shared s_SymbolsBuildingMembersAndInitializers As HashSet(Of SourceMemberContainerTypeSymbol)
#End If

        Private Function BuildMembersAndInitializers(diagBag As DiagnosticBag) As MembersAndInitializers

            Dim membersAndInitializers As MembersAndInitializers

#If DEBUG Then
            If s_SymbolsBuildingMembersAndInitializers Is Nothing Then
                s_SymbolsBuildingMembersAndInitializers = New HashSet(Of SourceMemberContainerTypeSymbol)(ReferenceEqualityComparer.Instance)
            End If

            Dim added As Boolean = s_SymbolsBuildingMembersAndInitializers.Add(Me)

            Debug.Assert(added)
            Try
#End If
                ' Get type members
                Dim typeMembers = GetTypeMembersDictionary()

                ' Get non-type members
                membersAndInitializers = BuildNonTypeMembers(diagBag)
                _defaultPropertyName = DetermineDefaultPropertyName(membersAndInitializers.Members, diagBag)

                ' Find/process partial methods
                ProcessPartialMethodsIfAny(membersAndInitializers.Members, diagBag)

                ' Merge types with non-types
                For Each typeSymbols In typeMembers.Values
                    Dim nontypeSymbols As ImmutableArray(Of Symbol) = Nothing
                    Dim name = typeSymbols(0).Name
                    If Not membersAndInitializers.Members.TryGetValue(name, nontypeSymbols) Then
                        membersAndInitializers.Members.Add(name, StaticCast(Of Symbol).From(typeSymbols))
                    Else
                        membersAndInitializers.Members(name) = nontypeSymbols.Concat(StaticCast(Of Symbol).From(typeSymbols))
                    End If
                Next

#If DEBUG Then
            Finally
                If added Then
                    s_SymbolsBuildingMembersAndInitializers.Remove(Me)
                End If
            End Try
#End If
            Return membersAndInitializers
        End Function

        ''' <summary> Examines the members collection and builds a set of partial methods if any, otherwise returns nothing </summary>
        Private Function FindPartialMethodDeclarations(diagnostics As DiagnosticBag, members As Dictionary(Of String, ImmutableArray(Of Symbol))) As HashSet(Of SourceMemberMethodSymbol)
            Dim partialMethods As HashSet(Of SourceMemberMethodSymbol) = Nothing
            For Each memberGroup In members
                For Each member In memberGroup.Value
                    Dim method = TryCast(member, SourceMemberMethodSymbol)
                    If method IsNot Nothing AndAlso method.IsPartial AndAlso method.MethodKind = MethodKind.Ordinary Then

                        If Not method.IsSub Then
                            Debug.Assert(method.Locations.Length = 1)
                            diagnostics.Add(ERRID.ERR_PartialMethodsMustBeSub1, method.NonMergedLocation, method.Name)
                        Else
                            If partialMethods Is Nothing Then
                                partialMethods = New HashSet(Of SourceMemberMethodSymbol)(ReferenceEqualityComparer.Instance)
                            End If
                            partialMethods.Add(method)
                        End If
                    End If
                Next
            Next
            Return partialMethods
        End Function

        Private Sub ProcessPartialMethodsIfAny(members As Dictionary(Of String, ImmutableArray(Of Symbol)), diagnostics As DiagnosticBag)
            '  Detect all partial method declarations
            Dim partialMethods As HashSet(Of SourceMemberMethodSymbol) = FindPartialMethodDeclarations(diagnostics, members)
            If partialMethods Is Nothing Then
                Return
            End If

            ' we have at least one partial method, note that the methods we 
            ' found may have the same names and/or signatures

            ' NOTE: we process partial methods one-by-one which is not optimal for the 
            '       case where we have a bunch of partial methods with the same name.
            ' TODO: revise

            While partialMethods.Count > 0

                Dim originalPartialMethod As SourceMemberMethodSymbol = partialMethods.First()
                partialMethods.Remove(originalPartialMethod)

                ' The best partial method
                Dim bestPartialMethod As SourceMemberMethodSymbol = originalPartialMethod
                Dim bestPartialMethodLocation As Location = bestPartialMethod.NonMergedLocation
                Debug.Assert(bestPartialMethodLocation IsNot Nothing)

                ' The best partial method implementation
                Dim bestImplMethod As SourceMemberMethodSymbol = Nothing
                Dim bestImplLocation As Location = Nothing

                '  Process the members group by name
                Dim memberGroup As ImmutableArray(Of Symbol) = members(originalPartialMethod.Name)
                For Each member In memberGroup
                    Dim candidate As SourceMemberMethodSymbol = TryCast(member, SourceMemberMethodSymbol)

                    If candidate IsNot Nothing AndAlso candidate IsNot originalPartialMethod AndAlso candidate.MethodKind = MethodKind.Ordinary Then
                        If ComparePartialMethodSignatures(originalPartialMethod, candidate) Then

                            '  candidate location
                            Dim candidateLocation As Location = candidate.NonMergedLocation
                            Debug.Assert(candidateLocation IsNot Nothing)

                            If partialMethods.Contains(candidate) Then
                                '  partial-partial conflict
                                partialMethods.Remove(candidate)

                                '  the 'best' partial method is the one with the 'smallest' 
                                ' location, we should report errors on the other
                                Dim candidateIsBigger As Boolean = Me.DeclaringCompilation.CompareSourceLocations(bestPartialMethodLocation, candidateLocation) < 0
                                Dim reportOnMethod As SourceMemberMethodSymbol = If(candidateIsBigger, candidate, bestPartialMethod)
                                Dim nameToReport As String = reportOnMethod.Name

                                diagnostics.Add(ERRID.ERR_OnlyOnePartialMethodAllowed2,
                                                If(candidateIsBigger, candidateLocation, bestPartialMethodLocation),
                                                nameToReport, nameToReport)
                                reportOnMethod.SuppressDuplicateProcDefDiagnostics = True

                                '  change the best partial method if needed
                                If Not candidateIsBigger Then
                                    bestPartialMethod = candidate
                                    bestPartialMethodLocation = candidateLocation
                                End If

                            ElseIf Not candidate.IsPartial Then

                                '  if there are more than one method
                                If bestImplMethod Is Nothing Then
                                    bestImplMethod = candidate
                                    bestImplLocation = candidateLocation

                                Else
                                    '  the 'best' implementation method is the one with the 'smallest' 
                                    ' location, we should report errors on the others
                                    Dim candidateIsBigger = Me.DeclaringCompilation.CompareSourceLocations(bestImplLocation, candidateLocation) < 0
                                    Dim reportOnMethod As SourceMemberMethodSymbol = If(candidateIsBigger, candidate, bestImplMethod)
                                    Dim reportedName As String = reportOnMethod.Name

                                    diagnostics.Add(ERRID.ERR_OnlyOneImplementingMethodAllowed3,
                                                    If(candidateIsBigger, candidateLocation, bestImplLocation),
                                                    reportedName, reportedName, reportedName)
                                    reportOnMethod.SuppressDuplicateProcDefDiagnostics = True

                                    '  change the best implementation if needed
                                    If Not candidateIsBigger Then
                                        bestImplMethod = candidate
                                        bestImplLocation = candidateLocation
                                    End If

                                End If

                            End If

                            ' NOTE: the rest of partial methods are already processed, those can be safely ignored

                        End If
                    End If
                Next

                '  Report ERR_PartialMethodMustBeEmpty
                If bestPartialMethod.BlockSyntax IsNot Nothing AndAlso bestPartialMethod.BlockSyntax.Statements.Count > 0 Then
                    diagnostics.Add(ERRID.ERR_PartialMethodMustBeEmpty, bestPartialMethodLocation)
                End If

                If bestImplMethod IsNot Nothing Then
                    ' We found the partial method implementation

                    ' Remove the best implementation from members
                    ' NOTE: conflicting partial method declarations and implementations are NOT removed 
                    Dim newMembers = ArrayBuilder(Of Symbol).GetInstance()
                    For i = 0 To memberGroup.Length - 1
                        Dim member As Symbol = memberGroup(i)
                        If bestImplMethod IsNot member Then
                            newMembers.Add(member)
                        End If
                    Next
                    members(originalPartialMethod.Name) = newMembers.ToImmutableAndFree()

                    '  Assign implementation to best partial method
                    SourceMemberMethodSymbol.InitializePartialMethodParts(bestPartialMethod, bestImplMethod)

                    ' Report errors on partial method implementation
                    ReportErrorsOnPartialMethodImplementation(bestPartialMethod, bestImplMethod, bestImplLocation, diagnostics)

                Else
                    '  There is no implementation
                    SourceMemberMethodSymbol.InitializePartialMethodParts(bestPartialMethod, Nothing)
                End If

            End While

        End Sub

        Private Sub ReportErrorsOnPartialMethodImplementation(partialMethod As SourceMethodSymbol,
                                                              implMethod As SourceMethodSymbol,
                                                              implMethodLocation As Location,
                                                              diagnostics As DiagnosticBag)

            ' Report 'Method '...' must be declared 'Private' in order to implement partial method '...'
            If implMethod.DeclaredAccessibility <> Accessibility.Private Then
                diagnostics.Add(ERRID.ERR_ImplementationMustBePrivate2,
                                implMethodLocation,
                                implMethod.Name, partialMethod.Name)
            End If

            ' Check method parameters' names
            If partialMethod.ParameterCount > 0 Then
                Debug.Assert(partialMethod.ParameterCount = implMethod.ParameterCount)

                Dim declMethodParams As ImmutableArray(Of ParameterSymbol) = partialMethod.Parameters
                Dim implMethodParams As ImmutableArray(Of ParameterSymbol) = implMethod.Parameters

                For index = 0 To declMethodParams.Length - 1
                    Dim declParameter As ParameterSymbol = declMethodParams(index)
                    Dim implParameter As ParameterSymbol = implMethodParams(index)

                    '  Check type parameter name
                    If Not CaseInsensitiveComparison.Equals(declParameter.Name, implParameter.Name) Then

                        Debug.Assert(implParameter.Locations.Length = 1)
                        diagnostics.Add(ERRID.ERR_PartialMethodParamNamesMustMatch3,
                                        implParameter.Locations(0),
                                        implParameter.Name, declParameter.Name, implMethod.Name)
                    End If
                Next
            End If

            ' Generic type names/constraints 
            If implMethod.Arity > 0 Then
                Dim declTypeParams As ImmutableArray(Of TypeParameterSymbol) = partialMethod.TypeParameters
                Dim implTypeParams As ImmutableArray(Of TypeParameterSymbol) = implMethod.TypeParameters
                Debug.Assert(declTypeParams.Length = implTypeParams.Length)

                For index = 0 To declTypeParams.Length - 1
                    Dim declParameter As TypeParameterSymbol = declTypeParams(index)
                    Dim implParameter As TypeParameterSymbol = implTypeParams(index)

                    '  Check parameter name
                    If Not CaseInsensitiveComparison.Equals(declParameter.Name, implParameter.Name) Then

                        Debug.Assert(implParameter.Locations.Length = 1)
                        diagnostics.Add(ERRID.ERR_PartialMethodTypeParamNameMismatch3,
                                        implParameter.Locations(0),
                                        implParameter.Name, declParameter.Name, implMethod.Name)
                    End If

                Next

                ' If type parameters constraints don't match at least on one of type 
                ' parameters, report an error on the implementation method
                Dim options = SymbolComparisonResults.ArityMismatch Or SymbolComparisonResults.ConstraintMismatch
                If MethodSignatureComparer.DetailedCompare(partialMethod, implMethod, options) <> Nothing Then
                    diagnostics.Add(ERRID.ERR_PartialMethodGenericConstraints2,
                                    implMethodLocation,
                                    implMethod.Name, partialMethod.Name)
                End If
            End If

        End Sub

        ''' <summary>
        ''' Compares two methods to check if the 'candidate' can be an implementation of the 'partialDeclaration'.
        ''' </summary>
        Private Function ComparePartialMethodSignatures(partialDeclaration As SourceMethodSymbol, candidate As SourceMethodSymbol) As Boolean
            ' Don't check values of optional parameters yet, this might cause an infinite cycle.
            ' Don't check ParamArray mismatch either, might cause us to bind attributes too early.
            Dim comparisons = SymbolComparisonResults.AllMismatches And
                                   Not (SymbolComparisonResults.CallingConventionMismatch Or
                                        SymbolComparisonResults.ConstraintMismatch Or
                                        SymbolComparisonResults.OptionalParameterValueMismatch Or
                                        SymbolComparisonResults.ParamArrayMismatch)

            Dim result As SymbolComparisonResults = MethodSignatureComparer.DetailedCompare(partialDeclaration, candidate, comparisons)

            If result <> Nothing Then
                Return False
            End If

            ' Dev10 also compares EQ_Flags { Shared|Overrides|MustOverride|Overloads }, but ignores 'Overloads'
            Return partialDeclaration.IsShared = candidate.IsShared AndAlso
                   partialDeclaration.IsOverrides = candidate.IsOverrides AndAlso
                   partialDeclaration.IsMustOverride = candidate.IsMustOverride
        End Function

        Friend Overrides ReadOnly Property KnownCircularStruct As Boolean
            Get
                If _lazyStructureCycle = ThreeState.Unknown Then
                    If Not Me.IsStructureType Then
                        _lazyStructureCycle = ThreeState.False
                    Else
                        Dim diagnostics = DiagnosticBag.GetInstance()
                        Dim hasCycle = Me.CheckStructureCircularity(diagnostics)

                        ' In either case we use AtomicStoreIntegerAndDiagnostics.
                        m_containingModule.AtomicStoreIntegerAndDiagnostics(_lazyStructureCycle,
                                                                            If(hasCycle, ThreeState.True, ThreeState.False),
                                                                            ThreeState.Unknown,
                                                                            diagnostics,
                                                                            CompilationStage.Declare)
                        diagnostics.Free()
                    End If
                End If

                Return _lazyStructureCycle = ThreeState.True
            End Get
        End Property

        ''' <summary> 
        ''' Poolable data set to be used in structure circularity detection.
        ''' </summary>
        Private Class StructureCircularityDetectionDataSet

            ''' <summary> 
            ''' Following C# implementation we keep up to 32 data sets so that we do not need to allocate 
            ''' them over and over. In this implementation though, circularity detection in one type can trigger
            ''' circularity detection in other types while it traverses the types tree. The traversal is being 
            ''' performed breadth-first, so the number of data sets used by one thread is not longer than the 
            ''' length of the longest structure-in-structure nesting chain.
            ''' </summary>
            Private Shared ReadOnly s_pool As New ObjectPool(Of StructureCircularityDetectionDataSet)(
                                                    Function() New StructureCircularityDetectionDataSet(), 32)

            ''' <summary> Set of processed structure types </summary>
            Public ReadOnly ProcessedTypes As HashSet(Of NamedTypeSymbol)

            ''' <summary> Queue element structure </summary>
            Public Structure QueueElement
                Public ReadOnly Type As NamedTypeSymbol
                Public ReadOnly Path As ConsList(Of FieldSymbol)

                Public Sub New(type As NamedTypeSymbol, path As ConsList(Of FieldSymbol))
                    Debug.Assert(type IsNot Nothing)
                    Debug.Assert(path IsNot Nothing)
                    Me.Type = type
                    Me.Path = path
                End Sub
            End Structure

            ''' <summary> Queue of the types to be processed </summary>
            Public ReadOnly Queue As Queue(Of QueueElement)

            Private Sub New()
                ProcessedTypes = New HashSet(Of NamedTypeSymbol)()
                Queue = New Queue(Of QueueElement)
            End Sub

            Public Shared Function GetInstance() As StructureCircularityDetectionDataSet
                Return s_pool.Allocate()
            End Function

            Public Sub Free()
                Me.Queue.Clear()
                Me.ProcessedTypes.Clear()
                s_pool.Free(Me)
            End Sub

        End Class

        ''' <summary>
        ''' Analyzes structure type for circularities. Reports only errors relevant for 'structBeingAnalyzed' type.
        ''' </summary>
        ''' <remarks>
        ''' When VB Dev10 detects circularity it reports the error only once for each cycle. Thus, if the cycle 
        ''' is {S1 --> S2 --> S3 --> S1}, only one error will be reported, which one of S1/S2/S3 will have error
        ''' is non-deterministic (depends on the order of symbols in a hash table).
        ''' 
        ''' Moreover, Dev10 analyzes the type graph and reports only one error in case S1 --> S2 --> S1 even if 
        ''' there are two fields referencing S2 from S1.
        ''' 
        ''' Example:
        '''    Structure S2
        '''      Dim s1 As S1
        '''    End Structure
        ''' 
        '''    Structure S3
        '''      Dim s1 As S1
        '''    End Structure
        ''' 
        '''    Structure S1
        '''      Dim s2 As S2  ' ERROR
        '''      Dim s2_ As S2 ' NO ERROR 
        '''      Dim s3 As S3  ' ERROR
        '''    End Structure
        ''' 
        ''' Dev10 also reports only one error for all the cycles starting with the same field, which one is reported 
        ''' depends on the declaration order. Current implementation reports all of the cycles for consistency. 
        ''' See testcases MultiplyCyclesInStructure03 and MultiplyCyclesInStructure04 (report different errors in Dev10).
        ''' </remarks>
        Private Function CheckStructureCircularity(diagnostics As DiagnosticBag) As Boolean
            '  Must be a structure
            Debug.Assert(Me.IsValueType AndAlso Not Me.IsTypeParameter)

            '  Allocate data set
            Dim data = StructureCircularityDetectionDataSet.GetInstance()
            data.Queue.Enqueue(New StructureCircularityDetectionDataSet.QueueElement(Me, ConsList(Of FieldSymbol).Empty))

            Dim hasCycle = False

            Try
                While data.Queue.Count > 0

                    Dim current = data.Queue.Dequeue()
                    If Not data.ProcessedTypes.Add(current.Type) Then
                        ' In some cases the queue may contain two same types which are not processed yet
                        Continue While
                    End If

                    Dim cycleReportedForCurrentType As Boolean = False

                    '  iterate over non-static fields of structure data type
                    For Each member In current.Type.GetMembers()

                        Dim field = TryCast(member, FieldSymbol)
                        If field IsNot Nothing AndAlso Not field.IsShared Then

                            Dim fieldType = TryCast(field.Type, NamedTypeSymbol)
                            If fieldType IsNot Nothing AndAlso fieldType.IsValueType Then

                                '  if the type is constructed from a generic structure, we should 
                                '  process ONLY fields which types are instantiated from type arguments
                                If Not field.IsDefinition AndAlso field.Type.Equals(field.OriginalDefinition.Type) Then
                                    Continue For
                                End If

                                If fieldType.OriginalDefinition.Equals(Me) Then
                                    '  a cycle detected

                                    If Not cycleReportedForCurrentType Then

                                        '  the cycle includes 'current.Path' and ends with 'field'; the order is reversed in the list
                                        Dim cycleFields = New ConsList(Of FieldSymbol)(field, current.Path)

                                        '  generate a message info
                                        Dim diagnosticInfos = ArrayBuilder(Of DiagnosticInfo).GetInstance()
                                        Dim firstField As FieldSymbol = Nothing ' after the cycle is processed this will hold the last element in the list
                                        While Not cycleFields.IsEmpty
                                            firstField = cycleFields.Head

                                            '  generate next field description
                                            diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_RecordEmbeds2,
                                                                                       firstField.ContainingType,
                                                                                       firstField.Type,
                                                                                       firstField.Name))
                                            cycleFields = cycleFields.Tail
                                        End While
                                        diagnosticInfos.ReverseContents()
                                        Debug.Assert(firstField IsNot Nothing)

                                        '  Report an error
                                        Dim symbolToReportErrorOn As Symbol = If(firstField.AssociatedSymbol, DirectCast(firstField, Symbol))
                                        Debug.Assert(symbolToReportErrorOn.Locations.Length > 0)
                                        diagnostics.Add(ERRID.ERR_RecordCycle2,
                                                        symbolToReportErrorOn.Locations(0),
                                                        firstField.ContainingType.Name,
                                                        New CompoundDiagnosticInfo(diagnosticInfos.ToArrayAndFree()))

                                        '  Don't report errors for other fields of this type referencing 'structBeingAnalyzed'
                                        cycleReportedForCurrentType = True
                                        hasCycle = True
                                    End If

                                ElseIf Not data.ProcessedTypes.Contains(fieldType) Then
                                    ' Add to the queue if we don't know yet if it was processed

                                    If Not fieldType.IsDefinition Then
                                        ' Types constructed from generic types are considered to be a separate types. We never report 
                                        ' errors on such types. We also process only fields actually changed compared to original generic type.
                                        data.Queue.Enqueue(New StructureCircularityDetectionDataSet.QueueElement(
                                                fieldType, New ConsList(Of FieldSymbol)(field, current.Path)))

                                        ' The original Generic type is added using regular rules (see next note).
                                        fieldType = fieldType.OriginalDefinition
                                    End If

                                    ' NOTE: we want to make sure we report the same error for the same types 
                                    '       consistently and don't depend on the call order; this solution uses
                                    '       the following approach: 
                                    '           (a) for each cycle we report the error only on the type which is
                                    '               'smaller' than the other types in this cycle; the criteria 
                                    '               used for detection of the 'smallest' type does not matter;
                                    '           (b) thus, this analysis only considers the cycles consisting of the 
                                    '               types which are 'bigger' than 'structBeingAnalyzed' because we will not 
                                    '               report the error regarding this cycle for this type anyway
                                    Dim stepIntoType As Boolean = DetectTypeCircularity_ShouldStepIntoType(fieldType)
                                    If stepIntoType Then
                                        '  enqueue to be processed
                                        data.Queue.Enqueue(New StructureCircularityDetectionDataSet.QueueElement(
                                                fieldType, New ConsList(Of FieldSymbol)(field, current.Path)))
                                    Else
                                        '  should not process 
                                        data.ProcessedTypes.Add(fieldType)
                                    End If
                                End If
                            End If
                        End If
                    Next
                End While
            Finally
                data.Free()
            End Try

            Return hasCycle
        End Function

        ''' <summary>
        ''' Simple check of whether or not we should step into the type 'typeToTest' during 
        ''' type graph traversal inside 'DetectStructureCircularity' or 'GetDependenceChain'.
        ''' 
        ''' The following rules are in place: 
        '''   (a) we order all symbols according their first source location 
        '''       comparison rules: first, source file names are compared, 
        '''       then SourceSpan.Start is used for symbols inside the same file;
        '''   (b) given this order we enter the loop if only 'typeToTest' is 'less' than 
        '''       'structBeingAnalyzed';
        '''   (c) we also always enter types from other modules
        ''' 
        ''' !!! To be ONLY used in 'CheckStructureCircularity'.
        ''' </summary>
        ''' <returns>True if detect type circularity code should step into 'typeToTest' type </returns>
        Friend Function DetectTypeCircularity_ShouldStepIntoType(typeToTest As NamedTypeSymbol) As Boolean

            If typeToTest.ContainingModule Is Nothing OrElse Not typeToTest.ContainingModule.Equals(Me.ContainingModule) Then
                ' Types from other modules should never be considered target types
                Return True
            End If

            ' We use simple comparison based on source location 
            Debug.Assert(typeToTest.Locations.Length > 0)
            Dim typeToTestLocation = typeToTest.Locations(0)

            Debug.Assert(Me.Locations.Length > 0)
            Dim structBeingAnalyzedLocation = Me.Locations(0)

            Dim compilation = Me.DeclaringCompilation
            Dim fileCompResult = compilation.CompareSourceLocations(typeToTestLocation, structBeingAnalyzedLocation)

            ' NOTE: we use '>=' for locations comparison; this is a safeguard against the case where two different
            '       types are declared in the files with same file name (if possible) and have the same location;
            '       if we used '>' we would not report the cycle, with '>=' we will report the cycle twice.
            Return (fileCompResult > 0) OrElse
                    ((fileCompResult = 0) AndAlso typeToTestLocation.SourceSpan.Start >= structBeingAnalyzedLocation.SourceSpan.Start)

        End Function

        Private Function DetermineDefaultPropertyName(membersByName As Dictionary(Of String, ImmutableArray(Of Symbol)), diagBag As DiagnosticBag) As String
            Dim defaultPropertyName As String = Nothing

            For Each pair In membersByName
                Dim name = pair.Key
                Dim members = pair.Value
                Dim defaultProperty As PropertySymbol = Nothing

                ' Check if any of the properties are marked default.
                For Each member In members
                    If member.Kind = SymbolKind.Property Then
                        Dim propertySymbol = DirectCast(member, PropertySymbol)

                        If propertySymbol.IsDefault Then
                            If defaultPropertyName Is Nothing Then
                                defaultProperty = propertySymbol
                                defaultPropertyName = name

                                If Not defaultProperty.ShadowsExplicitly Then
                                    CheckDefaultPropertyAgainstAllBases(Me, defaultPropertyName, propertySymbol.Locations(0), diagBag)
                                End If
                            Else
                                ' "'Default' can be applied to only one property name in a {0}."
                                diagBag.Add(ERRID.ERR_DuplicateDefaultProps1, propertySymbol.Locations(0), GetKindText())
                            End If
                            Exit For
                        End If
                    End If
                Next

                If defaultPropertyName IsNot Nothing AndAlso defaultPropertyName = name Then
                    Debug.Assert(defaultProperty IsNot Nothing)

                    ' Report an error for any property with this name not marked as default.
                    For Each member In members
                        If (member.Kind = SymbolKind.Property) Then
                            Dim propertySymbol = DirectCast(member, SourcePropertySymbol)

                            If Not propertySymbol.IsDefault Then
                                ' "'{0}' and '{1}' cannot overload each other because only one is declared 'Default'."
                                diagBag.Add(ERRID.ERR_DefaultMissingFromProperty2, propertySymbol.Locations(0), defaultProperty, propertySymbol)
                            End If
                        End If
                    Next
                End If
            Next

            Return defaultPropertyName
        End Function

        ' Check all bases of "namedType" and warn if they have a default property named "defaultPropertyName".
        Private Sub CheckDefaultPropertyAgainstAllBases(namedType As NamedTypeSymbol, defaultPropertyName As String, location As Location, diagBag As DiagnosticBag)
            If namedType.IsInterfaceType() Then
                For Each iface In namedType.InterfacesNoUseSiteDiagnostics
                    CheckDefaultPropertyAgainstBase(defaultPropertyName, iface, location, diagBag)
                Next
            Else
                CheckDefaultPropertyAgainstBase(defaultPropertyName, namedType.BaseTypeNoUseSiteDiagnostics, location, diagBag)
            End If
        End Sub

        ' Check and warn if "baseType" has a default property named "defaultProperty Name.
        ' If "baseType" doesn't have a default property, check its base types.
        Private Sub CheckDefaultPropertyAgainstBase(defaultPropertyName As String, baseType As NamedTypeSymbol, location As Location, diagBag As DiagnosticBag)
            If baseType IsNot Nothing Then
                Dim baseDefaultPropertyName = baseType.DefaultPropertyName
                If baseDefaultPropertyName IsNot Nothing Then
                    If Not CaseInsensitiveComparison.Equals(defaultPropertyName, baseDefaultPropertyName) Then
                        ' BC40007: Default property '{0}' conflicts with the default property '{1}' in the base {2} '{3}'. '{0}' will be the default property
                        diagBag.Add(ERRID.WRN_DefaultnessShadowed4, location,
                                    defaultPropertyName, baseDefaultPropertyName, baseType.GetKindText(), CustomSymbolDisplayFormatter.ShortErrorName(baseType))
                    End If
                Else
                    ' If this type didn't have a default property name, recursively check base(s) of this base.
                    ' If this type did have a default property name, don't go any further.
                    CheckDefaultPropertyAgainstAllBases(baseType, defaultPropertyName, location, diagBag)
                End If
            End If
        End Sub


        ''' <summary>
        ''' Returns true if at least one of the elements of this list needs to be injected into a 
        ''' constructor because it's not a const or it is a const and it's type is either decimal 
        ''' or date. Non const fields always require a constructor, so this function should be called to 
        ''' determine if a synthesized constructor is needed that is not listed in members list.
        ''' </summary>
        Friend Function AnyInitializerToBeInjectedIntoConstructor(
            initializerSet As IEnumerable(Of ImmutableArray(Of FieldOrPropertyInitializer)),
            includingNonMetadataConstants As Boolean
        ) As Boolean
            If initializerSet IsNot Nothing Then
                For Each initializers In initializerSet
                    For Each initializer In initializers
                        Dim fieldOrPropertyArray As ImmutableArray(Of Symbol) = initializer.FieldsOrProperties

                        If Not fieldOrPropertyArray.IsDefault Then
                            Debug.Assert(fieldOrPropertyArray.Length > 0)
                            Dim fieldOrProperty As Symbol = fieldOrPropertyArray.First

                            If fieldOrProperty.Kind = SymbolKind.Property Then
                                ' All properties require initializers to be injected
                                Return True

                            Else
                                Dim fieldSymbol = DirectCast(fieldOrProperty, FieldSymbol)
                                If Not fieldSymbol.IsConst OrElse includingNonMetadataConstants AndAlso fieldSymbol.IsConstButNotMetadataConstant Then
                                    Return True
                                End If
                            End If
                        End If
                    Next
                Next
            End If
            Return False
        End Function

        ''' <summary>
        ''' Performs a check for overloads/overrides/shadows conflicts, generates diagnostics.
        ''' </summary>
        ''' <param name="membersAndInitializers"></param>
        ''' <param name="diagBag"></param>
        ''' <remarks></remarks>
        Private Sub CheckForOverloadOverridesShadowsClashesInSameType(membersAndInitializers As MembersAndInitializers, diagBag As DiagnosticBag)
            For Each member In membersAndInitializers.Members
                '  list may contain both properties and methods
                Dim checkProperties As Boolean = True
                Dim checkMethods As Boolean = True
                '  result 
                Dim explicitlyShadows As Boolean = False
                Dim explicitlyOverloads As Boolean = False
                '  symbol flags
                Dim shadowsExplicitly As Boolean
                Dim overloadsExplicitly As Boolean
                Dim overridesExplicitly As Boolean

                For Each symbol In member.Value
                    '  only ordinary methods 
                    Select Case symbol.Kind
                        Case SymbolKind.Method
                            If Not checkMethods Then
                                Continue For
                            End If
                            '  skip properties from with this name if any
                            checkProperties = False
                        Case SymbolKind.Property
                            If Not checkProperties Then
                                Continue For
                            End If
                            '  skip methods from with this name if any
                            checkMethods = False
                        Case Else
                            '  other kind of member cancels the analysis
                            explicitlyShadows = False
                            explicitlyOverloads = False
                            Exit For
                    End Select

                    '  initialize symbol flags
                    If (GetExplicitSymbolFlags(symbol, shadowsExplicitly, overloadsExplicitly, overridesExplicitly)) Then
                        If shadowsExplicitly Then
                            '  if the method/property shadows explicitly the rest of the methods may be skipped
                            explicitlyShadows = True
                            Exit For
                        ElseIf overloadsExplicitly OrElse overridesExplicitly Then
                            explicitlyOverloads = True
                            '  continue search
                        End If
                    End If
                Next

                '  skip the whole name
                If explicitlyShadows OrElse explicitlyOverloads Then
                    '  all symbols are SourceMethodSymbol
                    For Each symbol In member.Value
                        If (symbol.Kind = SymbolKind.Method AndAlso checkMethods) OrElse (symbol.IsPropertyAndNotWithEvents AndAlso checkProperties) Then
                            '  initialize symbol flags
                            If (GetExplicitSymbolFlags(symbol, shadowsExplicitly, overloadsExplicitly, overridesExplicitly)) Then
                                If explicitlyShadows Then
                                    If Not shadowsExplicitly Then
                                        Debug.Assert(symbol.Locations.Length > 0)
                                        diagBag.Add(ERRID.ERR_MustShadow2, symbol.Locations(0), symbol.GetKindText(), symbol.Name)
                                    End If
                                ElseIf explicitlyOverloads Then
                                    If Not overridesExplicitly AndAlso Not overloadsExplicitly Then
                                        Debug.Assert(symbol.Locations.Length > 0)
                                        diagBag.Add(ERRID.ERR_MustBeOverloads2, symbol.Locations(0), symbol.GetKindText(), symbol.Name)
                                    End If
                                End If
                            End If
                        End If
                    Next
                End If
            Next
        End Sub

        Private Function GetExplicitSymbolFlags(symbol As Symbol, ByRef shadowsExplicitly As Boolean, ByRef overloadsExplicitly As Boolean, ByRef overridesExplicitly As Boolean) As Boolean
            Select Case symbol.Kind
                Case SymbolKind.Method
                    Dim sourceMethodSymbol As SourceMethodSymbol = TryCast(symbol, SourceMethodSymbol)
                    If (sourceMethodSymbol Is Nothing) Then
                        Return False
                    End If

                    shadowsExplicitly = sourceMethodSymbol.ShadowsExplicitly
                    overloadsExplicitly = sourceMethodSymbol.OverloadsExplicitly
                    overridesExplicitly = sourceMethodSymbol.OverridesExplicitly
                    Return sourceMethodSymbol.MethodKind = MethodKind.Ordinary OrElse sourceMethodSymbol.MethodKind = MethodKind.DeclareMethod

                Case SymbolKind.Property
                    Dim sourcePropertySymbol As SourcePropertySymbol = TryCast(symbol, SourcePropertySymbol)
                    If (sourcePropertySymbol Is Nothing) Then
                        Return False
                    End If
                    shadowsExplicitly = sourcePropertySymbol.ShadowsExplicitly
                    overloadsExplicitly = sourcePropertySymbol.OverloadsExplicitly
                    overridesExplicitly = sourcePropertySymbol.OverridesExplicitly
                    Return True

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
            End Select
        End Function

        ' Declare all the non-type members and put them in a list.
        Private Function BuildNonTypeMembers(diagnostics As DiagnosticBag) As MembersAndInitializers
            Dim membersBuilder As New MembersAndInitializersBuilder()

            AddDeclaredNonTypeMembers(membersBuilder, diagnostics)

            ' Add the default constructor, if needed.
            AddDefaultConstructorIfNeeded(membersBuilder, False, membersBuilder.InstanceInitializers, diagnostics)

            ' If there is a shared field, a shared constructor must be synthesized and added to the member list.
            ' Const fields of type Date or Decimal also require a synthesized shared constructor, but there was a decision
            ' to not add this to the member list in this case.
            AddDefaultConstructorIfNeeded(membersBuilder, True, membersBuilder.StaticInitializers, diagnostics)

            ' If there are any "Handles" methods, optimistically create methods/ctors that would be hosting
            ' hookup code.
            AddWithEventsHookupConstructorsIfNeeded(membersBuilder, diagnostics)

            ' Add "Group Class" members.
            AddGroupClassMembersIfNeeded(membersBuilder, diagnostics)

            ' Add synthetic Main method, if needed.
            AddEntryPointIfNeeded(membersBuilder)

            CheckMemberDiagnostics(membersBuilder, diagnostics)

            Dim membersAndInitializers = membersBuilder.ToReadOnlyAndFree()

            '  Check for overloads, overrides, shadows and implicit shadows clashes
            CheckForOverloadOverridesShadowsClashesInSameType(membersAndInitializers, diagnostics)

            Return membersAndInitializers
        End Function

        Protected Overridable Sub AddEntryPointIfNeeded(membersBuilder As MembersAndInitializersBuilder)
        End Sub

        Protected MustOverride Sub AddDeclaredNonTypeMembers(membersBuilder As MembersAndInitializersBuilder, diagnostics As DiagnosticBag)

        Protected Overridable Sub AddGroupClassMembersIfNeeded(membersBuilder As MembersAndInitializersBuilder, diagnostics As DiagnosticBag)
        End Sub

        ' Create symbol(s) for member syntax and add them to the member list
        Protected Sub AddMember(memberSyntax As StatementSyntax,
                                    binder As Binder,
                                    diagBag As DiagnosticBag,
                                    members As MembersAndInitializersBuilder,
                                    ByRef staticInitializers As ArrayBuilder(Of FieldOrPropertyInitializer),
                                    ByRef instanceInitializers As ArrayBuilder(Of FieldOrPropertyInitializer),
                                    reportAsInvalid As Boolean)

            ' Partial methods are implemented by a postpass that matches up the declaration with the implementation.
            ' Here we treat them as independent methods.

            Select Case memberSyntax.Kind
                Case SyntaxKind.FieldDeclaration
                    Dim fieldDecl = DirectCast(memberSyntax, FieldDeclarationSyntax)

                    If reportAsInvalid Then
                        diagBag.Add(ERRID.ERR_InvalidInNamespace, fieldDecl.GetLocation())
                    End If

                    ' Declare all variables that a declared by this syntax, and add them to the list.
                    SourceMemberFieldSymbol.Create(Me, fieldDecl, binder, members, staticInitializers, instanceInitializers, diagBag)

                Case _
                    SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.OperatorBlock

                    Dim methodDecl = DirectCast(memberSyntax, MethodBlockBaseSyntax).BlockStatement

                    If reportAsInvalid Then
                        diagBag.Add(ERRID.ERR_InvalidInNamespace, methodDecl.GetLocation())
                    End If

                    Dim methodSymbol = CreateMethodMember(methodDecl, binder, diagBag)
                    If methodSymbol IsNot Nothing Then
                        AddMember(methodSymbol, binder, members, omitDiagnostics:=False)
                    End If

                Case _
                    SyntaxKind.SubStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.DeclareSubStatement,
                    SyntaxKind.DeclareFunctionStatement,
                    SyntaxKind.OperatorStatement

                    Dim methodDecl = DirectCast(memberSyntax, MethodBaseSyntax)

                    If reportAsInvalid Then
                        diagBag.Add(ERRID.ERR_InvalidInNamespace, methodDecl.GetLocation())
                    End If

                    Dim methodSymbol = CreateMethodMember(DirectCast(memberSyntax, MethodBaseSyntax), binder, diagBag)
                    If methodSymbol IsNot Nothing Then
                        AddMember(methodSymbol, binder, members, omitDiagnostics:=False)
                    End If

                Case SyntaxKind.PropertyBlock
                    Dim propertyDecl = DirectCast(memberSyntax, PropertyBlockSyntax)

                    If reportAsInvalid Then
                        diagBag.Add(ERRID.ERR_InvalidInNamespace, propertyDecl.PropertyStatement.GetLocation())
                    End If

                    CreateProperty(propertyDecl.PropertyStatement, propertyDecl, binder, diagBag, members, staticInitializers, instanceInitializers)

                Case SyntaxKind.PropertyStatement
                    Dim propertyDecl = DirectCast(memberSyntax, PropertyStatementSyntax)

                    If reportAsInvalid Then
                        diagBag.Add(ERRID.ERR_InvalidInNamespace, propertyDecl.GetLocation())
                    End If

                    CreateProperty(propertyDecl, Nothing, binder, diagBag, members, staticInitializers, instanceInitializers)

                Case SyntaxKind.LabelStatement
                    ' TODO (tomat): should be added to the initializers
                    Exit Select

                Case SyntaxKind.EventStatement
                    Dim eventDecl = DirectCast(memberSyntax, EventStatementSyntax)
                    CreateEvent(eventDecl, Nothing, binder, diagBag, members)

                Case SyntaxKind.EventBlock
                    Dim eventDecl = DirectCast(memberSyntax, EventBlockSyntax)
                    CreateEvent(eventDecl.EventStatement, eventDecl, binder, diagBag, members)

                Case Else
                    If binder.BindingTopLevelScriptCode Then
                        If memberSyntax.Kind = SyntaxKind.EmptyStatement OrElse TypeOf memberSyntax Is ExecutableStatementSyntax Then

                            If reportAsInvalid Then
                                diagBag.Add(ERRID.ERR_InvalidInNamespace, memberSyntax.GetLocation())
                            End If

                            Dim initializer = Function(precedingInitializersLength As Integer)
                                                  Return New FieldOrPropertyInitializer(binder.GetSyntaxReference(memberSyntax), precedingInitializersLength)
                                              End Function
                            SourceNamedTypeSymbol.AddInitializer(instanceInitializers, initializer, members.InstanceSyntaxLength)
                        End If
                    End If
            End Select
        End Sub

        Private Sub CreateProperty(syntax As PropertyStatementSyntax,
                                   blockSyntaxOpt As PropertyBlockSyntax,
                                   binder As Binder,
                                   diagBag As DiagnosticBag,
                                   members As MembersAndInitializersBuilder,
                                   ByRef staticInitializers As ArrayBuilder(Of FieldOrPropertyInitializer),
                                   ByRef instanceInitializers As ArrayBuilder(Of FieldOrPropertyInitializer))

            Dim propertySymbol = SourcePropertySymbol.Create(Me, binder, syntax, blockSyntaxOpt, diagBag)

            AddPropertyAndAccessors(propertySymbol, binder, members)

            ' initialization can happen because of a "= value" (InitializerOpt) or a "As New Type(...)" (AsClauseOpt)
            Dim initializerOpt = syntax.Initializer
            Dim asClauseOpt = syntax.AsClause
            Dim equalsValueOrAsNewSyntax As VisualBasicSyntaxNode
            If asClauseOpt IsNot Nothing AndAlso asClauseOpt.Kind = SyntaxKind.AsNewClause Then
                equalsValueOrAsNewSyntax = asClauseOpt
            Else
                equalsValueOrAsNewSyntax = initializerOpt
            End If

            If equalsValueOrAsNewSyntax IsNot Nothing Then
                Dim initializerOptRef = binder.GetSyntaxReference(equalsValueOrAsNewSyntax)
                Dim initializer = Function(precedingInitializersLength As Integer)
                                      Return New FieldOrPropertyInitializer(propertySymbol, initializerOptRef, precedingInitializersLength)
                                  End Function

                If propertySymbol.IsShared Then
                    AddInitializer(staticInitializers, initializer, members.StaticSyntaxLength)
                Else
                    ' auto implemented properties inside of structures can only have an initialization value
                    ' if they are shared.
                    If propertySymbol.IsAutoProperty AndAlso
                        propertySymbol.ContainingType.TypeKind = TypeKind.Structure Then

                        Binder.ReportDiagnostic(diagBag, syntax.Identifier, ERRID.ERR_AutoPropertyInitializedInStructure)
                    End If

                    AddInitializer(instanceInitializers, initializer, members.InstanceSyntaxLength)
                End If
            End If
        End Sub

        Private Sub CreateEvent(syntax As EventStatementSyntax,
                           blockSyntaxOpt As EventBlockSyntax,
                           binder As Binder,
                           diagBag As DiagnosticBag,
                           members As MembersAndInitializersBuilder)

            Dim propertySymbol = New SourceEventSymbol(Me, binder, syntax, blockSyntaxOpt, diagBag)

            AddEventAndAccessors(propertySymbol, binder, members)
        End Sub

        Private Function CreateMethodMember(methodBaseSyntax As MethodBaseSyntax,
                                             binder As Binder,
                                             diagBag As DiagnosticBag) As SourceMethodSymbol
            Select Case methodBaseSyntax.Kind
                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    Return SourceMethodSymbol.CreateRegularMethod(Me, DirectCast(methodBaseSyntax, MethodStatementSyntax), binder, diagBag)
                Case SyntaxKind.SubNewStatement
                    Return SourceMethodSymbol.CreateConstructor(Me, DirectCast(methodBaseSyntax, SubNewStatementSyntax), binder, diagBag)
                Case SyntaxKind.OperatorStatement
                    Return SourceMethodSymbol.CreateOperator(Me, DirectCast(methodBaseSyntax, OperatorStatementSyntax), binder, diagBag)
                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    Return SourceMethodSymbol.CreateDeclareMethod(Me, DirectCast(methodBaseSyntax, DeclareStatementSyntax), binder, diagBag)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(methodBaseSyntax.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Check to see if we need a default instance|shared constructor, and if so, create it.
        ''' 
        ''' NOTE: we only need a shared constructor if there are any initializers to be 
        ''' injected into it, we don't create a constructor otherwise. In this case we also 
        ''' ignore const fields which will still require to be injected, because in this case
        ''' we don't see the constructor to be visible in symbol table.
        ''' </summary>
        Private Sub AddDefaultConstructorIfNeeded(members As MembersAndInitializersBuilder,
                                                  isShared As Boolean,
                                                  initializers As ArrayBuilder(Of ImmutableArray(Of FieldOrPropertyInitializer)),
                                                  diagnostics As DiagnosticBag)

            If TypeKind = TypeKind.Submission Then

                ' Only add an constructor if it is not shared OR if there are shared initializers
                If Not isShared OrElse Me.AnyInitializerToBeInjectedIntoConstructor(initializers, False) Then

                    ' a submission can only have a single declaration:
                    Dim syntaxRef = SyntaxReferences.Single()

                    Dim binder As Binder = BinderBuilder.CreateBinderForType(m_containingModule, syntaxRef.SyntaxTree, Me)
                    Dim constructor As New SynthesizedSubmissionConstructorSymbol(syntaxRef, Me, isShared, binder, diagnostics)
                    AddMember(constructor, binder, members, omitDiagnostics:=False)
                End If

            ElseIf TypeKind = TypeKind.Class OrElse
                   TypeKind = TypeKind.Structure OrElse
                   TypeKind = TypeKind.Enum OrElse
                   (TypeKind = TypeKind.Module AndAlso isShared) Then

                ' Do we need to create a constructor? We need a constructor if this is 
                ' an instance constructor, or if this is a shared constructor and 
                ' there is at least one non-constant initializers
                Dim anyInitializersToInject As Boolean =
                        Me.AnyInitializerToBeInjectedIntoConstructor(initializers, Not isShared)
                ' NOTE: for shared constructor we DO NOT check for non-metadata-const 
                '       initializers, those will be addressed later

                If isShared AndAlso Not anyInitializersToInject Then
                    Return
                End If

                Dim isDebuggable As Boolean = anyInitializersToInject

                EnsureCtor(members, isShared, isDebuggable, diagnostics)
            End If

            If Not isShared AndAlso IsScriptClass Then
                ' a submission can only have a single declaration:
                Dim syntaxRef = SyntaxReferences.Single()
                Dim scriptInitializer = New SynthesizedInteractiveInitializerMethod(syntaxRef, Me, diagnostics)
                AddSymbolToMembers(scriptInitializer, members.Members)
                Dim scriptEntryPoint = SynthesizedEntryPointSymbol.Create(scriptInitializer, diagnostics)
                AddSymbolToMembers(scriptEntryPoint, members.Members)
            End If
        End Sub

        Private Sub EnsureCtor(members As MembersAndInitializersBuilder, isShared As Boolean, isDebuggable As Boolean, diagBag As DiagnosticBag)

            Dim constructorName = If(isShared, WellKnownMemberNames.StaticConstructorName, WellKnownMemberNames.InstanceConstructorName)

            ' Check to see if we have already declared an instance|shared constructor.
            Dim symbols As ArrayBuilder(Of Symbol) = Nothing
            If members.Members.TryGetValue(constructorName, symbols) Then
                Debug.Assert(symbols.Where(Function(sym) sym.Kind = SymbolKind.Method AndAlso
                                (DirectCast(sym, MethodSymbol).MethodKind = MethodKind.Constructor OrElse
                                 DirectCast(sym, MethodSymbol).MethodKind = MethodKind.SharedConstructor)
                             ).Any)

                For Each method As MethodSymbol In symbols
                    If method.MethodKind = MethodKind.Constructor AndAlso method.ParameterCount = 0 Then
                        Return ' definitely don't need to synthesize a constructor
                    End If
                Next
                ' have to synthesize a constructor if this is a non-shared struct

                If TypeKind <> TypeKind.Structure OrElse isShared Then
                    Return ' already have an instance|shared constructor. Don't add another one.
                End If
            End If


            ' Add a new instance|shared constructor.
            Dim syntaxRef = SyntaxReferences.First() ' use arbitrary part
            ' TODO: does it need to be deterministic?

            Dim binder As Binder = BinderBuilder.CreateBinderForType(m_containingModule, syntaxRef.SyntaxTree, Me)
            Dim constructor As New SynthesizedConstructorSymbol(syntaxRef, Me, isShared, isDebuggable, binder, diagBag)
            AddMember(constructor, binder, members, omitDiagnostics:=False)
        End Sub

        Private Sub AddWithEventsHookupConstructorsIfNeeded(members As MembersAndInitializersBuilder, diagBag As DiagnosticBag)
            If TypeKind = TypeKind.Submission Then
                'TODO: anything to do here?

            ElseIf TypeKind = TypeKind.Class OrElse TypeKind = TypeKind.Module Then

                ' we need a separate list of methods since we may need to modify the members dictionary.
                Dim sourceMethodsWithHandles As ArrayBuilder(Of SourceMethodSymbol) = Nothing

                For Each membersOfSameName In members.Members.Values
                    For Each member In membersOfSameName
                        Dim sourceMethod = TryCast(member, SourceMethodSymbol)
                        If sourceMethod IsNot Nothing Then
                            If Not sourceMethod.HandlesEvents Then
                                Continue For
                            End If

                            If sourceMethodsWithHandles Is Nothing Then
                                sourceMethodsWithHandles = ArrayBuilder(Of SourceMethodSymbol).GetInstance
                            End If
                            sourceMethodsWithHandles.Add(sourceMethod)
                        End If
                    Next
                Next

                If sourceMethodsWithHandles Is Nothing Then
                    ' no source methods with Handles - we are done
                    Return
                End If

                ' binder used if we need to find something in the base. will be created when needed
                Dim baseBinder As Binder = Nothing

                For Each sourceMethod In sourceMethodsWithHandles
                    Dim methodStatement = DirectCast(sourceMethod.DeclarationSyntax, MethodStatementSyntax)

                    For Each handlesClause In methodStatement.HandlesClause.Events
                        If handlesClause.EventContainer.Kind = SyntaxKind.KeywordEventContainer Then

                            If Not sourceMethod.IsShared Then
                                ' if the method is not shared, we will be hooking up in the instance ctor
                                EnsureCtor(members, isShared:=False, isDebuggable:=False, diagBag:=diagBag)
                            Else
                                ' if both event and handler are shared, then hookup goes into shared ctor
                                ' otherwise into instance ctor

                                ' find our event
                                Dim eventName = handlesClause.EventMember.Identifier.ValueText
                                Dim eventSym As EventSymbol = Nothing

                                ' look in current members
                                If handlesClause.EventContainer.Kind <> SyntaxKind.MyBaseKeyword Then
                                    Dim candidates As ArrayBuilder(Of Symbol) = Nothing
                                    If members.Members.TryGetValue(eventName, candidates) Then
                                        If candidates.Count = 1 AndAlso candidates(0).Kind = SymbolKind.Event Then
                                            eventSym = DirectCast(candidates(0), EventSymbol)
                                        End If
                                    End If
                                End If

                                ' try find in base
                                If eventSym Is Nothing Then
                                    ' Set up a binder.
                                    baseBinder = If(baseBinder, BinderBuilder.CreateBinderForType(m_containingModule, methodStatement.SyntaxTree, Me))

                                    Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                                    eventSym = SourceMemberMethodSymbol.FindEvent(Me.BaseTypeNoUseSiteDiagnostics, baseBinder, eventName, isThroughMyBase:=True, useSiteDiagnostics:=useSiteDiagnostics)
                                    diagBag.Add(handlesClause.EventMember, useSiteDiagnostics)
                                End If

                                ' still nothing?
                                If eventSym Is Nothing Then
                                    Continue For
                                End If

                                EnsureCtor(members, eventSym.IsShared, isDebuggable:=False, diagBag:=diagBag)
                            End If
                        End If
                    Next
                Next

                sourceMethodsWithHandles.Free()
            End If
        End Sub

        Private Sub AddPropertyAndAccessors(propertySymbol As SourcePropertySymbol,
                                           binder As Binder,
                                           members As MembersAndInitializersBuilder)

            AddMember(propertySymbol, binder, members, omitDiagnostics:=False)
            If propertySymbol.GetMethod IsNot Nothing Then
                AddMember(propertySymbol.GetMethod, binder, members, omitDiagnostics:=False)
            End If
            If propertySymbol.SetMethod IsNot Nothing Then
                AddMember(propertySymbol.SetMethod, binder, members, omitDiagnostics:=False)
            End If
            If propertySymbol.AssociatedField IsNot Nothing Then
                AddMember(propertySymbol.AssociatedField, binder, members, omitDiagnostics:=False)
            End If
        End Sub

        Private Sub AddEventAndAccessors(eventSymbol As SourceEventSymbol,
                                   binder As Binder,
                                   members As MembersAndInitializersBuilder)

            AddMember(eventSymbol, binder, members, omitDiagnostics:=False)

            If eventSymbol.AddMethod IsNot Nothing Then
                AddMember(eventSymbol.AddMethod, binder, members, omitDiagnostics:=False)
            End If

            If eventSymbol.RemoveMethod IsNot Nothing Then
                AddMember(eventSymbol.RemoveMethod, binder, members, omitDiagnostics:=False)
            End If

            If eventSymbol.RaiseMethod IsNot Nothing Then
                AddMember(eventSymbol.RaiseMethod, binder, members, omitDiagnostics:=False)
            End If

            If eventSymbol.AssociatedField IsNot Nothing Then
                AddMember(eventSymbol.AssociatedField, binder, members, omitDiagnostics:=False)
            End If
        End Sub


        Private Sub CheckMemberDiagnostics(
                             members As MembersAndInitializersBuilder,
                             diagBag As DiagnosticBag)

            If Me.Locations.Length > 1 AndAlso Not Me.IsPartial Then
                ' Suppress conflict member diagnostics when the enclosing type is an accidental duplicate
                Return
            End If

            For Each pair In members.DeferredMemberDiagnostic
                Dim sym As Symbol = pair.Item1
                Dim binder As Binder = pair.Item2

                ' Check name for duplicate type declarations
                ' First check if the member name conflicts with a type declaration in the container then
                ' Check if the member name conflicts with another member in the container.
                If Not CheckIfMemberNameConflictsWithTypeMember(sym, members, diagBag) Then
                    CheckIfMemberNameIsDuplicate(sym, diagBag, members)
                End If

                If sym.CanBeReferencedByName AndAlso
                    TypeParameters.MatchesAnyName(sym.Name) Then
                    If sym.IsImplicitlyDeclared Then
                        Dim symImplicitlyDefinedBy = sym.ImplicitlyDefinedBy(members.Members)
                        Debug.Assert(symImplicitlyDefinedBy IsNot Nothing)

                        ' "{0} '{1}' implicitly defines a member '{2}' which has the same name as a type parameter."
                        Binder.ReportDiagnostic(diagBag,
                                                symImplicitlyDefinedBy.Locations(0),
                                                ERRID.ERR_SyntMemberShadowsGenericParam3,
                                                symImplicitlyDefinedBy.GetKindText(),
                                                symImplicitlyDefinedBy.Name,
                                                sym.Name)
                    Else
                        ' "'{0}' has the same name as a type parameter."
                        Binder.ReportDiagnostic(diagBag, sym.Locations(0), ERRID.ERR_ShadowingGenericParamWithMember1, sym.Name)
                    End If
                End If
            Next
        End Sub

        Friend Sub AddMember(sym As Symbol,
                             binder As Binder,
                             members As MembersAndInitializersBuilder,
                             omitDiagnostics As Boolean)

            If Not omitDiagnostics Then
                members.DeferredMemberDiagnostic.Add(ValueTuple.Create(sym, binder))
            End If

            AddSymbolToMembers(sym, members.Members)
        End Sub

        Friend Sub AddSymbolToMembers(memberSymbol As Symbol,
                                      members As Dictionary(Of String, ArrayBuilder(Of Symbol)))

            Dim symbols As ArrayBuilder(Of Symbol) = Nothing
            If members.TryGetValue(memberSymbol.Name, symbols) Then
                symbols.Add(memberSymbol)
            Else
                symbols = New ArrayBuilder(Of Symbol)

                symbols.Add(memberSymbol)
                members(memberSymbol.Name) = symbols
            End If
        End Sub

        Private Function CheckIfMemberNameConflictsWithTypeMember(sym As Symbol,
                                                                  members As MembersAndInitializersBuilder,
                                                                  diagBag As DiagnosticBag) As Boolean
            ' Check name for conflicts with type members
            Dim definedTypes = Me.GetTypeMembers(sym.Name)

            If definedTypes.Length > 0 Then
                Dim type = definedTypes(0)
                If sym <> type Then
                    Return CheckIfMemberNameIsDuplicate(sym, type, members, diagBag, includeKind:=True)
                End If
            End If

            Return False
        End Function

        Private Function CheckIfMemberNameIsDuplicate(sym As Symbol,
                                                      diagBag As DiagnosticBag,
                                                      members As MembersAndInitializersBuilder) As Boolean
            ' Check name for duplicate declarations
            Dim definedSymbols As ArrayBuilder(Of Symbol) = Nothing

            If members.Members.TryGetValue(sym.Name, definedSymbols) Then
                Debug.Assert(definedSymbols.Count > 0)
                Dim other = definedSymbols(0)
                If (sym <> other) Then
                    Return CheckIfMemberNameIsDuplicate(sym, other, members, diagBag, includeKind:=False)
                End If
            End If

            Return False
        End Function

        Private Function CheckIfMemberNameIsDuplicate(firstSymbol As Symbol,
                                          secondSymbol As Symbol,
                                          members As MembersAndInitializersBuilder,
                                          diagBag As DiagnosticBag,
                                          includeKind As Boolean) As Boolean

            Dim firstAssociatedSymbol = secondSymbol.ImplicitlyDefinedBy(members.Members)
            If firstAssociatedSymbol Is Nothing AndAlso secondSymbol.IsUserDefinedOperator() Then
                ' For the purpose of this check, operator methods are treated as implicitly defined by themselves.
                firstAssociatedSymbol = secondSymbol
            End If

            Dim secondAssociatedSymbol = firstSymbol.ImplicitlyDefinedBy(members.Members)
            If secondAssociatedSymbol Is Nothing AndAlso firstSymbol.IsUserDefinedOperator() Then
                ' For the purpose of this check, operator methods are treated as implicitly defined by themselves.
                secondAssociatedSymbol = firstSymbol
            End If

            If firstAssociatedSymbol IsNot Nothing Then
                If secondAssociatedSymbol Is Nothing Then
                    Dim asType = TryCast(firstAssociatedSymbol, TypeSymbol)
                    If asType IsNot Nothing AndAlso asType.IsEnumType Then
                        ' enum members may conflict only with __Value and that produces a special diagnostics.
                        Return True
                    End If

                    ' "{0} '{1}' implicitly defines '{2}', which conflicts with a member of the same name in {3} '{4}'."
                    Binder.ReportDiagnostic(
                            diagBag,
                            firstAssociatedSymbol.Locations(0),
                            ERRID.ERR_SynthMemberClashesWithMember5,
                            firstAssociatedSymbol.GetKindText(),
                            OverrideHidingHelper.AssociatedSymbolName(firstAssociatedSymbol),
                            secondSymbol.Name,
                            Me.GetKindText(),
                            Me.Name)

                    Return True
                Else
                    ' If both symbols are implicitly defined (say an overloaded property P where each
                    ' overload implicitly defines get_P), no error is reported. 
                    ' If there are any errors in cases if defining members have same names.
                    ' In such cases, the errors should be reported on the defining symbols.

                    If Not CaseInsensitiveComparison.Equals(firstAssociatedSymbol.Name,
                                                       secondAssociatedSymbol.Name) Then
                        '{0} '{1}' implicitly defines '{2}', which conflicts with a member implicitly declared for {3} '{4}' in {5} '{6}'.
                        Binder.ReportDiagnostic(
                                diagBag,
                                firstAssociatedSymbol.Locations(0),
                                ERRID.ERR_SynthMemberClashesWithSynth7,
                                firstAssociatedSymbol.GetKindText(),
                                OverrideHidingHelper.AssociatedSymbolName(firstAssociatedSymbol),
                                secondSymbol.Name,
                                secondAssociatedSymbol.GetKindText(),
                                OverrideHidingHelper.AssociatedSymbolName(secondAssociatedSymbol),
                                Me.GetKindText(),
                                Me.Name)
                    End If
                End If
            ElseIf secondAssociatedSymbol IsNot Nothing Then

                ' "{0} '{1}' conflicts with a member implicitly declared for {2} '{3}' in {4} '{5}'."
                Binder.ReportDiagnostic(
                        diagBag,
                        secondSymbol.Locations(0),
                        ERRID.ERR_MemberClashesWithSynth6,
                        secondSymbol.GetKindText(),
                        secondSymbol.Name,
                        secondAssociatedSymbol.GetKindText(),
                        OverrideHidingHelper.AssociatedSymbolName(secondAssociatedSymbol),
                        Me.GetKindText(),
                        Me.Name)

                Return True

            ElseIf ((firstSymbol.Kind <> SymbolKind.Method) AndAlso (Not firstSymbol.IsPropertyAndNotWithEvents)) OrElse
                    (firstSymbol.Kind <> secondSymbol.Kind) Then

                If Me.IsEnumType() Then

                    ' For Enum members, give more specific simpler errors.
                    ' "'{0}' is already declared in this {1}."
                    Binder.ReportDiagnostic(
                            diagBag,
                            firstSymbol.Locations(0),
                            ERRID.ERR_MultiplyDefinedEnumMember2,
                            firstSymbol.Name,
                            Me.GetKindText())

                Else
                    ' the formatting of this error message is quite special and needs special treatment
                    ' e.g. 'foo' is already declared as 'Class Foo' in this class.

                    ' "'{0}' is already declared as '{1}' in this {2}."
                    Binder.ReportDiagnostic(
                            diagBag,
                            firstSymbol.Locations(0),
                            ERRID.ERR_MultiplyDefinedType3,
                            firstSymbol.Name,
                            If(includeKind,
                               DirectCast(CustomSymbolDisplayFormatter.ErrorNameWithKind(secondSymbol), Object),
                               secondSymbol),
                            Me.GetKindText())
                End If

                Return True
            End If

            Return False
        End Function

        Public Overrides ReadOnly Property MemberNames As IEnumerable(Of String)
            Get
                Return _declaration.MemberNames
            End Get
        End Property

        Friend Overrides Function GetMembersUnordered() As ImmutableArray(Of Symbol)
            If _lazyMembersFlattened.IsDefault Then
                Dim lookup = Me.MemberAndInitializerLookup
                Dim result = lookup.Members.Flatten(Nothing)  ' Do Not sort right now.
                ImmutableInterlocked.InterlockedInitialize(Me._lazyMembersFlattened, result)
            End If

#If DEBUG Then
            ' In DEBUG, swap first And last elements so that use of Unordered in a place it isn't warranted is caught
            ' more obviously.
            Return _lazyMembersFlattened.DeOrder()
#Else
            Return _lazyMembersFlattened
#End If
        End Function

        Public Overloads Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            If (m_lazyState And StateFlags.FlattenedMembersIsSortedMask) <> 0 Then
                Return _lazyMembersFlattened

            Else
                Dim allMembers = Me.GetMembersUnordered()

                If allMembers.Length >= 2 Then
                    allMembers = allMembers.Sort(LexicalOrderSymbolComparer.Instance)
                    ImmutableInterlocked.InterlockedExchange(_lazyMembersFlattened, allMembers)
                End If

                ThreadSafeFlagOperations.Set(m_lazyState, StateFlags.FlattenedMembersIsSortedMask)

                Return allMembers
            End If
        End Function

        Public Overloads Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Dim lookup = Me.MemberAndInitializerLookup
            Dim members As ImmutableArray(Of Symbol) = Nothing

            If lookup.Members.TryGetValue(name, members) Then
                Return members
            End If

            Return ImmutableArray(Of Symbol).Empty
        End Function

        Friend Overrides Function GetSimpleNonTypeMembers(name As String) As ImmutableArray(Of Symbol)
            If _lazyMembersAndInitializers IsNot Nothing OrElse MemberNames.Contains(name) Then
                Return GetMembers(name)
            End If

            Return ImmutableArray(Of Symbol).Empty
        End Function

        ''' <summary>
        ''' In case the passed initializers require a shared constructor, this method returns a new MethodSymbol instance for the 
        ''' shared constructor if there is not already an explicit shared constructor
        ''' </summary>
        Friend Function CreateSharedConstructorsForConstFieldsIfRequired(binder As Binder, diagnostics As DiagnosticBag) As MethodSymbol
            Dim lookup = Me.MemberAndInitializerLookup
            Dim staticInitializers = lookup.StaticInitializers

            If Not staticInitializers.IsDefaultOrEmpty Then
                Dim symbols As ImmutableArray(Of Symbol) = Nothing
                If Not MemberAndInitializerLookup.Members.TryGetValue(WellKnownMemberNames.StaticConstructorName, symbols) Then

                    ' call AnyInitializerToBeInjectedIntoConstructor if only there is no static constructor
                    If Me.AnyInitializerToBeInjectedIntoConstructor(staticInitializers, True) Then
                        Dim syntaxRef = SyntaxReferences.First() ' use arbitrary part
                        Return New SynthesizedConstructorSymbol(syntaxRef, Me,
                                                                isShared:=True, isDebuggable:=True,
                                                                binder:=binder, diagnostics:=diagnostics)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Friend Overrides Iterator Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
            For Each member In GetMembersForCci()
                If member.Kind = SymbolKind.Field Then
                    Yield DirectCast(member, FieldSymbol)
                End If
            Next
        End Function

        ''' <summary>
        ''' Gets the static initializers.
        ''' </summary>
        Public ReadOnly Property StaticInitializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer))
            Get
                Return Me.MemberAndInitializerLookup.StaticInitializers
            End Get
        End Property

        ''' <summary>
        ''' Gets the instance initializers.
        ''' </summary>
        Public ReadOnly Property InstanceInitializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer))
            Get
                Return Me.MemberAndInitializerLookup.InstanceInitializers
            End Get
        End Property

        Friend Function CalculateSyntaxOffsetInSynthesizedConstructor(position As Integer, tree As SyntaxTree, isShared As Boolean) As Integer
            If IsScriptClass AndAlso Not isShared Then
                Dim aggregateLength As Integer = 0

                For Each declaration In Me._declaration.Declarations
                    Dim syntaxRef = declaration.SyntaxReference

                    If tree Is syntaxRef.SyntaxTree Then
                        Return aggregateLength + position
                    End If

                    aggregateLength += syntaxRef.Span.Length
                Next

                ' This point should not be reachable.
                Throw ExceptionUtilities.Unreachable
            End If

            Dim syntaxOffset As Integer
            If TryCalculateSyntaxOffsetOfPositionInInitializer(position, tree, isShared, syntaxOffset:=syntaxOffset) Then
                Return syntaxOffset
            End If

            ' This point should not be reachable. An implicit constructor has no body and no initializer,
            ' so the variable has to be declared in a member initializer.
            Throw ExceptionUtilities.Unreachable
        End Function

        ' Calculates a syntax offset of a syntax position that is contained in a property or field initializer (if it is in fact contained in one).
        Friend Function TryCalculateSyntaxOffsetOfPositionInInitializer(position As Integer, tree As SyntaxTree, isShared As Boolean, ByRef syntaxOffset As Integer) As Boolean
            Dim membersAndInitializers = GetMembersAndInitializers()
            Dim allInitializers = If(isShared, membersAndInitializers.StaticInitializers, membersAndInitializers.InstanceInitializers)

            Dim siblingInitializers = GetInitializersInSourceTree(tree, allInitializers)
            Dim index = IndexOfInitializerContainingPosition(siblingInitializers, position)
            If index < 0 Then
                syntaxOffset = 0
                Return False
            End If

            '                                 |<-----------distanceFromCtorBody---------->|
            ' [      initializer 0    ][ initializer 1 ][ initializer 2 ][ initializer 3 ][ctor body]
            ' |<--preceding init len-->|      ^
            '                              position 
            Dim initializersLength = If(isShared, membersAndInitializers.StaticInitializersSyntaxLength, membersAndInitializers.InstanceInitializersSyntaxLength)
            Dim distanceFromInitializerStart = position - siblingInitializers(index).Syntax.Span.Start
            Dim distanceFromCtorBody = initializersLength - (siblingInitializers(index).PrecedingInitializersLength + distanceFromInitializerStart)

            Debug.Assert(distanceFromCtorBody > 0)

            ' syntax offset 0 is at the start of the ctor body:
            syntaxOffset = -distanceFromCtorBody
            Return True
        End Function

        Private Shared Function GetInitializersInSourceTree(tree As SyntaxTree, initializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer))) As ImmutableArray(Of FieldOrPropertyInitializer)
            Dim builder = ArrayBuilder(Of FieldOrPropertyInitializer).GetInstance()
            For Each siblingInitializers As ImmutableArray(Of FieldOrPropertyInitializer) In initializers
                If (siblingInitializers.First().Syntax.SyntaxTree Is tree) Then
                    builder.AddRange(siblingInitializers)
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Function IndexOfInitializerContainingPosition(initializers As ImmutableArray(Of FieldOrPropertyInitializer), position As Integer) As Integer
            ' Search for the start of the span (the spans are non-overlapping and sorted)
            Dim index = initializers.BinarySearch(position, Function(initializer, pos) initializer.Syntax.Span.Start.CompareTo(pos))

            ' Binary search returns non-negative result if the position is exactly the start of some span.
            If index >= 0 Then
                Return index
            End If

            ' Otherwise, "Not index" is the closest span whose start is greater than the position.
            ' Make sure that this closest span contains the position.
            index = (Not index) - 1
            If index >= 0 AndAlso initializers(index).Syntax.Span.Contains(position) Then
                Return index
            End If

            Return -1
        End Function

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                ' Only Modules can declare extension methods.

                If _lazyContainsExtensionMethods = ThreeState.Unknown Then
                    If Not (_containingSymbol.Kind = SymbolKind.Namespace AndAlso Me.AllowsExtensionMethods() AndAlso Me.AnyMemberHasAttributes) Then
                        _lazyContainsExtensionMethods = ThreeState.False
                    End If
                End If

                Return _lazyContainsExtensionMethods <> ThreeState.False
            End Get
        End Property

        Friend Overrides Sub BuildExtensionMethodsMap(map As Dictionary(Of String, ArrayBuilder(Of MethodSymbol)),
                                                      appendThrough As NamespaceSymbol)
            If Me.MightContainExtensionMethods Then
                Dim lookup = Me.MemberAndInitializerLookup

                If Not appendThrough.BuildExtensionMethodsMap(map, lookup.Members) Then
                    ' Didn't find any extension methods, record the fact.
                    _lazyContainsExtensionMethods = ThreeState.False
                End If
            End If
        End Sub

        Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                 options As LookupOptions,
                                                                 originalBinder As Binder,
                                                                 appendThrough As NamedTypeSymbol)
            If Me.MightContainExtensionMethods Then
                Dim lookup = Me.MemberAndInitializerLookup

                If Not appendThrough.AddExtensionMethodLookupSymbolsInfo(nameSet, options, originalBinder, lookup.Members) Then
                    ' Didn't find any extension methods, record the fact.
                    _lazyContainsExtensionMethods = ThreeState.False
                End If
            End If
        End Sub

        ' Build the explicit interface map for this type. 
        '
        ' Also diagnoses the following errors and places diagnostics in the diagnostic bag:
        '   Same symbol implemented twice
        '   Interface symbol that should have been implemented wasn't.
        '   Generic interfaces might unify.
        Private Function MakeExplicitInterfaceImplementationMap(diagnostics As DiagnosticBag) As Dictionary(Of Symbol, Symbol)
            If Me.IsClassType() OrElse Me.IsStructureType() OrElse Me.IsInterfaceType() Then
                CheckInterfaceUnificationAndVariance(diagnostics)
            End If

            If Me.IsClassType() OrElse Me.IsStructureType() Then
                ' Go through all explicit interface implementations and record them.
                Dim map = New Dictionary(Of Symbol, Symbol)()
                For Each implementingMember In Me.GetMembers()
                    For Each interfaceMember In GetExplicitInterfaceImplementations(implementingMember)
                        If Not map.ContainsKey(interfaceMember) Then
                            map.Add(interfaceMember, implementingMember)
                        Else
                            'the same symbol was implemented twice.
                            If ShouldReportImplementationError(interfaceMember) Then
                                Dim diag = ErrorFactory.ErrorInfo(ERRID.ERR_MethodAlreadyImplemented2,
                                                                  CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(interfaceMember.ContainingType),
                                                                  CustomSymbolDisplayFormatter.ShortErrorName(interfaceMember))
                                diagnostics.Add(New VBDiagnostic(diag, GetImplementingLocation(implementingMember, interfaceMember)))
                            End If
                        End If
                    Next
                Next

                ' Check to make sure all members of interfaces were implemented. Note that if our base class implemented
                ' an interface, we do not have to implemented those members again (although we can).
                For Each iface In InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics
                    ' Only check interfaces that our base type does NOT implement.
                    If Not Me.BaseTypeNoUseSiteDiagnostics.ImplementsInterface(iface, useSiteDiagnostics:=Nothing) Then
                        For Each ifaceMember In iface.GetMembers()
                            If ifaceMember.RequiresImplementation() AndAlso ShouldReportImplementationError(ifaceMember) Then
                                Dim implementingMember As Symbol = Nothing
                                Dim useSiteErrorInfo = ifaceMember.GetUseSiteErrorInfo()
                                If Not map.TryGetValue(ifaceMember, implementingMember) Then
                                    'member was not implemented.
                                    Dim diag = If(useSiteErrorInfo, ErrorFactory.ErrorInfo(ERRID.ERR_UnimplementedMember3,
                                                                        If(Me.IsStructureType(), "Structure", "Class"),
                                                                        CustomSymbolDisplayFormatter.ShortErrorName(Me),
                                                                        ifaceMember,
                                                                        CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(iface)))
                                    diagnostics.Add(New VBDiagnostic(diag, GetImplementsLocation(iface)))
                                Else
                                    If useSiteErrorInfo IsNot Nothing Then
                                        diagnostics.Add(New VBDiagnostic(useSiteErrorInfo, implementingMember.Locations(0)))
                                    End If
                                End If
                            End If
                        Next
                    End If
                Next

                If map.Count > 0 Then
                    Return map
                Else
                    Return EmptyExplicitImplementationMap  ' Better to use singleton and garbage collection the empty dictionary we just created.
                End If
            Else
                Return EmptyExplicitImplementationMap
            End If
        End Function

        ' Should we report implementation errors for this member?
        ' We don't report errors on accessors, because we already report the errors on their containing property/event.
        Private Function ShouldReportImplementationError(interfaceMember As Symbol) As Boolean
            If interfaceMember.Kind = SymbolKind.Method AndAlso DirectCast(interfaceMember, MethodSymbol).MethodKind <> MethodKind.Ordinary Then
                Return False
            Else
                Return True
            End If
        End Function

        ' Get a dictionary with all the explicitly implemented interface symbols declared on this type. key = interface
        ' method/property/event, value = explicitly implementing method/property/event declared on this type
        '
        ' Getting this property also ensures that diagnostics relating to interface implementation, overriding, hiding and 
        ' overloading are created.
        Friend Overrides ReadOnly Property ExplicitInterfaceImplementationMap As Dictionary(Of Symbol, Symbol)
            Get
                If m_lazyExplicitInterfaceImplementationMap Is Nothing Then
                    Dim diagnostics As DiagnosticBag = DiagnosticBag.GetInstance()
                    Dim implementationMap = MakeExplicitInterfaceImplementationMap(diagnostics)
                    OverrideHidingHelper.CheckHidingAndOverridingForType(Me, diagnostics)

                    ' checking the overloads accesses the parameter types. The parameters are now lazily created to not happen at 
                    ' method/property symbol creation time. That's the reason why this check is delayed until here.
                    CheckForOverloadsErrors(diagnostics)

                    m_containingModule.AtomicStoreReferenceAndDiagnostics(m_lazyExplicitInterfaceImplementationMap, implementationMap, diagnostics, CompilationStage.Declare)
                    diagnostics.Free()
                End If

                Return m_lazyExplicitInterfaceImplementationMap
            End Get
        End Property

        ''' <summary>
        ''' Reports the overloads error for this type.
        ''' </summary>
        ''' <param name="diagnostics">The diagnostics.</param>
        Private Sub CheckForOverloadsErrors(diagnostics As DiagnosticBag)
            Debug.Assert(Me.IsDefinition) ' Don't do this on constructed types

            ' Enums and Delegates have nothing to do.
            Dim myTypeKind As TypeKind = Me.TypeKind
            Dim operatorsKnownToHavePair As HashSet(Of MethodSymbol) = Nothing

            If myTypeKind = TypeKind.Class OrElse myTypeKind = TypeKind.Interface OrElse myTypeKind = TypeKind.Structure OrElse myTypeKind = TypeKind.Module Then
                Dim lookup = MemberAndInitializerLookup

                Dim structEnumerator As Dictionary(Of String, ImmutableArray(Of Symbol)).Enumerator = lookup.Members.GetEnumerator
                Dim canDeclareOperators As Boolean = (myTypeKind <> TypeKind.Module AndAlso myTypeKind <> TypeKind.Interface)

                While structEnumerator.MoveNext()
                    Dim memberList As ImmutableArray(Of Symbol) = structEnumerator.Current.Value

                    ' only proceed if there are multiple declarations of methods with the same name
                    If memberList.Length < 2 Then
                        ' Validate operator overloading
                        If canDeclareOperators AndAlso CheckForOperatorOverloadingErrors(memberList, 0, structEnumerator, operatorsKnownToHavePair, diagnostics) Then
                            Continue While
                        End If

                        Continue While
                    End If

                    For Each memberKind In {SymbolKind.Method, SymbolKind.Property}

                        For memberIndex = 0 To memberList.Length - 2
                            Dim member As Symbol = memberList(memberIndex)
                            If member.Kind <> memberKind OrElse member.IsAccessor OrElse member.IsWithEventsProperty Then
                                Continue For
                            End If

                            ' Validate operator overloading
                            If canDeclareOperators AndAlso memberKind = SymbolKind.Method AndAlso
                               CheckForOperatorOverloadingErrors(memberList, memberIndex, structEnumerator, operatorsKnownToHavePair, diagnostics) Then
                                Continue For
                            End If

                            Dim sourceMethod = TryCast(member, SourceMemberMethodSymbol)
                            If sourceMethod IsNot Nothing Then
                                Debug.Assert(Not sourceMethod.IsUserDefinedOperator())
                                If sourceMethod.IsUserDefinedOperator() Then
                                    Continue For
                                End If

                                If sourceMethod.SuppressDuplicateProcDefDiagnostics Then
                                    Continue For
                                End If
                            End If

                            ' TODO: this is O(N^2), maybe this can be improved.
                            For nextMemberIndex = memberIndex + 1 To memberList.Length - 1
                                Dim nextMember = memberList(nextMemberIndex)
                                If nextMember.Kind <> memberKind OrElse nextMember.IsAccessor OrElse nextMember.IsWithEventsProperty Then
                                    Continue For
                                End If

                                sourceMethod = TryCast(nextMember, SourceMemberMethodSymbol)
                                If sourceMethod IsNot Nothing Then
                                    If sourceMethod.IsUserDefinedOperator() Then
                                        Continue For
                                    End If

                                    If sourceMethod.SuppressDuplicateProcDefDiagnostics Then
                                        Continue For
                                    End If
                                End If

                                ' only process non synthesized symbols
                                If Not member.IsImplicitlyDeclared AndAlso
                                   Not nextMember.IsImplicitlyDeclared Then

                                    ' Overload resolution (CollectOverloadedCandidates) does similar check for imported members
                                    ' from the same container. Both places should be in sync. CheckForOperatorOverloadingErrors too.
                                    Dim comparisonResults As SymbolComparisonResults = OverrideHidingHelper.DetailedSignatureCompare(
                                        member,
                                        nextMember,
                                        SymbolComparisonResults.AllMismatches And Not (SymbolComparisonResults.CallingConventionMismatch Or SymbolComparisonResults.ConstraintMismatch))

                                    ' only report diagnostics if the signature is considered equal following VB rules.
                                    If (comparisonResults And Not SymbolComparisonResults.MismatchesForConflictingMethods) = 0 Then
                                        ReportOverloadsErrors(comparisonResults, member, nextMember, member.Locations(0), diagnostics)
                                        Exit For
                                    End If
                                End If
                            Next
                        Next

                        ' Validate operator overloading for the last operator, it is not handled by the loop
                        If canDeclareOperators AndAlso memberKind = SymbolKind.Method AndAlso
                           CheckForOperatorOverloadingErrors(memberList, memberList.Length - 1, structEnumerator, operatorsKnownToHavePair, diagnostics) Then
                            Continue For
                        End If
                    Next
                End While
            End If
        End Sub

        ''' <summary>
        ''' Returns True if memberList(memberIndex) is an operator.
        ''' Also performs operator overloading validation and reports appropriate errors.
        ''' </summary>
        Private Function CheckForOperatorOverloadingErrors(
            memberList As ImmutableArray(Of Symbol),
            memberIndex As Integer,
            membersEnumerator As Dictionary(Of String, ImmutableArray(Of Symbol)).Enumerator,
            <[In](), Out()> ByRef operatorsKnownToHavePair As HashSet(Of MethodSymbol),
            diagnostics As DiagnosticBag
        ) As Boolean
            Dim member As Symbol = memberList(memberIndex)

            If member.Kind <> SymbolKind.Method Then
                Return False
            End If

            Dim method = DirectCast(member, MethodSymbol)
            Dim significantDiff As SymbolComparisonResults = Not SymbolComparisonResults.MismatchesForConflictingMethods
            Dim methodMethodKind As MethodKind = method.MethodKind

            Select Case methodMethodKind
                Case MethodKind.Conversion
                    significantDiff = significantDiff Or SymbolComparisonResults.ReturnTypeMismatch
                Case MethodKind.UserDefinedOperator
                Case Else
                    ' Not an operator.
                    Return False
            End Select

            Dim opInfo As OverloadResolution.OperatorInfo = OverloadResolution.GetOperatorInfo(method.Name)

            If Not OverloadResolution.ValidateOverloadedOperator(method, opInfo, diagnostics) Then
                ' Malformed operator, but still an operator.
                Return True
            End If

            ' Check conflicting overloading with other operators.
            If IsConflictingOperatorOverloading(method, significantDiff, memberList, memberIndex + 1, diagnostics) Then
                Return True
            End If

            ' CType overloads across Widening and Narrowing, which use different metadata names.
            ' Need to handle this specially.
            If methodMethodKind = MethodKind.Conversion Then
                Dim otherName As String = If(IdentifierComparison.Equals(WellKnownMemberNames.ImplicitConversionName, method.Name),
                                             WellKnownMemberNames.ExplicitConversionName, WellKnownMemberNames.ImplicitConversionName)

                Dim otherMembers As ImmutableArray(Of Symbol) = Nothing
                If MemberAndInitializerLookup.Members.TryGetValue(otherName, otherMembers) Then
                    While membersEnumerator.MoveNext()
                        If membersEnumerator.Current.Value = otherMembers Then
                            If IsConflictingOperatorOverloading(method, significantDiff, otherMembers, 0, diagnostics) Then
                                Return True
                            End If

                            Exit While
                        End If
                    End While
                End If
            End If

            ' Check for operators that must be declared in pairs.
            Dim nameOfThePair As String = Nothing

            If opInfo.IsUnary Then
                Select Case opInfo.UnaryOperatorKind
                    Case UnaryOperatorKind.IsTrue
                        nameOfThePair = WellKnownMemberNames.FalseOperatorName
                    Case UnaryOperatorKind.IsFalse
                        nameOfThePair = WellKnownMemberNames.TrueOperatorName
                End Select
            Else
                Select Case opInfo.BinaryOperatorKind
                    Case BinaryOperatorKind.Equals
                        nameOfThePair = WellKnownMemberNames.InequalityOperatorName
                    Case BinaryOperatorKind.NotEquals
                        nameOfThePair = WellKnownMemberNames.EqualityOperatorName
                    Case BinaryOperatorKind.LessThan
                        nameOfThePair = WellKnownMemberNames.GreaterThanOperatorName
                    Case BinaryOperatorKind.GreaterThan
                        nameOfThePair = WellKnownMemberNames.LessThanOperatorName
                    Case BinaryOperatorKind.LessThanOrEqual
                        nameOfThePair = WellKnownMemberNames.GreaterThanOrEqualOperatorName
                    Case BinaryOperatorKind.GreaterThanOrEqual
                        nameOfThePair = WellKnownMemberNames.LessThanOrEqualOperatorName
                End Select
            End If

            If nameOfThePair IsNot Nothing AndAlso
               (operatorsKnownToHavePair Is Nothing OrElse Not operatorsKnownToHavePair.Contains(method)) Then

                Dim otherMembers As ImmutableArray(Of Symbol) = Nothing
                If MemberAndInitializerLookup.Members.TryGetValue(nameOfThePair, otherMembers) Then
                    For Each other As Symbol In otherMembers
                        If other.IsUserDefinedOperator() Then
                            Dim otherMethod = DirectCast(other, MethodSymbol)
                            Dim comparisonResults As SymbolComparisonResults = MethodSignatureComparer.DetailedCompare(
                                method,
                                otherMethod,
                                SymbolComparisonResults.AllMismatches And
                                Not (SymbolComparisonResults.CallingConventionMismatch Or
                                     SymbolComparisonResults.ConstraintMismatch Or
                                     SymbolComparisonResults.CustomModifierMismatch Or
                                     SymbolComparisonResults.NameMismatch))

                            If (comparisonResults And (Not SymbolComparisonResults.MismatchesForConflictingMethods Or SymbolComparisonResults.ReturnTypeMismatch)) = 0 Then
                                ' Found the pair
                                If operatorsKnownToHavePair Is Nothing Then
                                    operatorsKnownToHavePair = New HashSet(Of MethodSymbol)(ReferenceEqualityComparer.Instance)
                                End If

                                operatorsKnownToHavePair.Add(otherMethod)

                                Return True
                            End If
                        End If
                    Next
                End If

                diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_MatchingOperatorExpected2,
                                                       SyntaxFacts.GetText(OverloadResolution.GetOperatorTokenKind(nameOfThePair)),
                                                       method), method.Locations(0))
            End If

            Return True
        End Function

        ''' <summary>
        ''' See if any member in [memberList] starting with [memberIndex] conflict with [method],
        ''' report appropriate error and return true. 
        ''' </summary>
        Private Function IsConflictingOperatorOverloading(
            method As MethodSymbol,
            significantDiff As SymbolComparisonResults,
            memberList As ImmutableArray(Of Symbol),
            memberIndex As Integer,
            diagnostics As DiagnosticBag
        ) As Boolean
            For nextMemberIndex = memberIndex To memberList.Length - 1
                Dim nextMember = memberList(nextMemberIndex)
                If nextMember.Kind <> SymbolKind.Method Then
                    Continue For
                End If

                Dim nextMethod = DirectCast(nextMember, MethodSymbol)

                If nextMethod.MethodKind <> method.MethodKind Then
                    Continue For
                End If

                Dim comparisonResults As SymbolComparisonResults = MethodSignatureComparer.DetailedCompare(
                    method,
                    nextMethod,
                    SymbolComparisonResults.AllMismatches And
                    Not (SymbolComparisonResults.CallingConventionMismatch Or
                         SymbolComparisonResults.ConstraintMismatch Or
                         SymbolComparisonResults.CustomModifierMismatch Or
                         SymbolComparisonResults.NameMismatch))

                ' only report diagnostics if the signature is considered equal following VB rules.
                If (comparisonResults And significantDiff) = 0 Then
                    ReportOverloadsErrors(comparisonResults, method, nextMethod, method.Locations(0), diagnostics)
                    Return True
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Check for two different diagnostics on the set of implemented interfaces:
        '''   1) It is invalid for a type to directly (vs through a base class) implement two interfaces that
        '''   unify (i.e. are the same for some substitution of type parameters).
        ''' 
        '''   2) It is a warning to implement variant interfaces twice with type arguments that could cause
        '''   ambiguity during method dispatch.
        ''' </summary>
        Private Sub CheckInterfaceUnificationAndVariance(diagnostics As DiagnosticBag)
            Dim interfaces = Me.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics

            If interfaces.Count < 2 Then
                Return ' can't have any conflicts
            End If

            ' We only need to check pairs of generic interfaces that have the same original definition. Put the interfaces
            ' into buckets by original definition.
            Dim originalDefinitionBuckets As New MultiDictionary(Of NamedTypeSymbol, NamedTypeSymbol)
            For Each iface In interfaces
                If iface.IsGenericType Then
                    originalDefinitionBuckets.Add(iface.OriginalDefinition, iface)
                End If
            Next

            ' Compare all pairs of interfaces in each bucket.
            For Each kvp In originalDefinitionBuckets
                If kvp.Value.Count >= 2 Then
                    Dim i1 As Integer = 0
                    For Each interface1 In kvp.Value
                        Dim i2 As Integer = 0
                        For Each interface2 In kvp.Value
                            If i2 > i1 Then
                                Debug.Assert(interface2.IsGenericType AndAlso interface1.OriginalDefinition = interface2.OriginalDefinition)

                                ' Check for interface unification, then variance ambiguity
                                If TypeUnification.CanUnify(Me, interface1, interface2) Then
                                    ReportInterfaceUnificationError(diagnostics, interface1, interface2)
                                ElseIf VarianceAmbiguity.HasVarianceAmbiguity(Me, interface1, interface2, Nothing) Then
                                    ReportVarianceAmbiguityWarning(diagnostics, interface1, interface2)
                                End If
                            End If

                            i2 += 1
                        Next

                        i1 += 1
                    Next
                End If
            Next

        End Sub

        Private Sub ReportOverloadsErrors(comparisonResults As SymbolComparisonResults, firstMember As Symbol, secondMember As Symbol, location As Location, diagnostics As DiagnosticBag)
            If (Me.Locations.Length > 1 AndAlso Not Me.IsPartial) Then
                ' if there was an error with the enclosing class, suppress these diagnostics
            ElseIf comparisonResults = 0 Then
                diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_DuplicateProcDef1, firstMember), location)
            Else
                ' TODO: maybe rewrite these diagnostics to if/elseifs to report just one diagnostic per
                ' symbol. This would reduce the error count, but may lead to a new diagnostics once the 
                ' previous one was fixed (byref + return type).

                If (comparisonResults And SymbolComparisonResults.ParameterByrefMismatch) <> 0 Then
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadWithByref2, firstMember, secondMember), location)
                End If

                If (comparisonResults And SymbolComparisonResults.ReturnTypeMismatch) <> 0 Then
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadWithReturnType2, firstMember, secondMember), location)
                End If

                If (comparisonResults And SymbolComparisonResults.ParamArrayMismatch) <> 0 Then
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadWithArrayVsParamArray2, firstMember, secondMember), location)
                End If

                If (comparisonResults And SymbolComparisonResults.OptionalParameterMismatch) <> 0 AndAlso (comparisonResults And SymbolComparisonResults.TotalParameterCountMismatch) = 0 Then
                    ' We have Optional/Required parameter disparity AND the same number of parameters
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadWithOptional2, firstMember, secondMember), location)
                End If

                '  With changes in overloading with optional parameters this should never happen
                Debug.Assert((comparisonResults And SymbolComparisonResults.OptionalParameterTypeMismatch) = 0)
                'If (comparisonResults And SymbolComparisonResults.OptionalParameterTypeMismatch) <> 0 Then
                '   diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadWithOptionalTypes2, firstMember, secondMember), location)
                ' ...

                ' Dev10 only checks the equality of the default values if the types match in 
                ' CompareParams, so we need to suppress the diagnostic here.
                If (comparisonResults And SymbolComparisonResults.OptionalParameterValueMismatch) <> 0 Then
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadWithDefault2, firstMember, secondMember), location)
                End If

                If (comparisonResults And SymbolComparisonResults.PropertyAccessorMismatch) <> 0 Then
                    diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverloadingPropertyKind2, firstMember, secondMember), location)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Interface1 and Interface2 conflict for some type arguments. Report the correct error in the correct location.
        ''' </summary>
        Private Sub ReportInterfaceUnificationError(diagnostics As DiagnosticBag, interface1 As NamedTypeSymbol, interface2 As NamedTypeSymbol)
            If GetImplementsLocation(interface1).SourceSpan.Start > GetImplementsLocation(interface2).SourceSpan.Start Then
                ' Report error on second implement, for consistency.
                Dim temp = interface1
                interface1 = interface2
                interface2 = temp
            End If

            ' The direct base interfaces that interface1/2 were inherited through.
            Dim directInterface1 As NamedTypeSymbol = Nothing
            Dim directInterface2 As NamedTypeSymbol = Nothing
            Dim location1, location2 As Location
            location1 = GetImplementsLocation(interface1, directInterface1)
            location2 = GetImplementsLocation(interface2, directInterface2)
            Dim isInterface As Boolean = Me.IsInterfaceType()
            Dim diag As DiagnosticInfo

            If (directInterface1 = interface1 AndAlso directInterface2 = interface2) Then
                diag = ErrorFactory.ErrorInfo(If(isInterface, ERRID.ERR_InterfaceUnifiesWithInterface2, ERRID.ERR_InterfacePossiblyImplTwice2),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(interface2),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(interface1))
            ElseIf (directInterface1 <> interface1 AndAlso directInterface2 = interface2) Then
                diag = ErrorFactory.ErrorInfo(If(isInterface, ERRID.ERR_InterfaceUnifiesWithBase3, ERRID.ERR_ClassInheritsInterfaceUnifiesWithBase3),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(interface2),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(interface1),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(directInterface1))
            ElseIf (directInterface1 = interface1 AndAlso directInterface2 <> interface2) Then
                diag = ErrorFactory.ErrorInfo(If(isInterface, ERRID.ERR_BaseUnifiesWithInterfaces3, ERRID.ERR_ClassInheritsBaseUnifiesWithInterfaces3),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(directInterface2),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(interface2),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(interface1))
            Else
                Debug.Assert(directInterface1 <> interface1 AndAlso directInterface2 <> interface2)
                diag = ErrorFactory.ErrorInfo(If(isInterface, ERRID.ERR_InterfaceBaseUnifiesWithBase4, ERRID.ERR_ClassInheritsInterfaceBaseUnifiesWithBase4),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(directInterface2),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(interface2),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(interface1),
                                              CustomSymbolDisplayFormatter.ShortNameWithTypeArgsAndContainingTypes(directInterface1))
            End If

            diagnostics.Add(New VBDiagnostic(diag, location2))
        End Sub

        ''' <summary>
        ''' Interface1 and Interface2 have variable ambiguity. Report the warning in the correct location.
        ''' </summary>
        Private Sub ReportVarianceAmbiguityWarning(diagnostics As DiagnosticBag, interface1 As NamedTypeSymbol, interface2 As NamedTypeSymbol)
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim hasVarianceAmbiguity As Boolean = VarianceAmbiguity.HasVarianceAmbiguity(Me, interface1, interface2, useSiteDiagnostics)

            If hasVarianceAmbiguity OrElse Not useSiteDiagnostics.IsNullOrEmpty Then
                If GetImplementsLocation(interface1).SourceSpan.Start > GetImplementsLocation(interface2).SourceSpan.Start Then
                    ' Report error on second implement, for consistency.
                    Dim temp = interface1
                    interface1 = interface2
                    interface2 = temp
                End If

                ' The direct base interfaces that interface1/2 were inherited through.
                Dim directInterface1 As NamedTypeSymbol = Nothing
                Dim directInterface2 As NamedTypeSymbol = Nothing
                Dim location1, location2 As Location
                location1 = GetImplementsLocation(interface1, directInterface1)
                location2 = GetImplementsLocation(interface2, directInterface2)

                If Not diagnostics.Add(location2, useSiteDiagnostics) AndAlso hasVarianceAmbiguity Then
                    Dim diag As DiagnosticInfo
                    diag = ErrorFactory.ErrorInfo(ERRID.WRN_VarianceDeclarationAmbiguous3,
                                                      CustomSymbolDisplayFormatter.QualifiedName(directInterface2),
                                                      CustomSymbolDisplayFormatter.QualifiedName(directInterface1),
                                                      CustomSymbolDisplayFormatter.ErrorNameWithKind(interface1.OriginalDefinition))
                    diagnostics.Add(New VBDiagnostic(diag, location2))
                End If
            End If
        End Sub

#End Region

#Region "Attributes"
        Protected Sub SuppressExtensionAttributeSynthesis()
            Debug.Assert(_lazyEmitExtensionAttribute <> ThreeState.True)
            _lazyEmitExtensionAttribute = ThreeState.False
        End Sub

        Private ReadOnly Property EmitExtensionAttribute As Boolean
            Get
                If _lazyEmitExtensionAttribute = ThreeState.Unknown Then
                    BindAllMemberAttributes(cancellationToken:=Nothing)
                End If

                Debug.Assert(_lazyEmitExtensionAttribute <> ThreeState.Unknown)
                Return _lazyEmitExtensionAttribute = ThreeState.True
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            If EmitExtensionAttribute Then
                AddSynthesizedAttribute(attributes, Me.DeclaringCompilation.SynthesizeExtensionAttribute())
            End If
        End Sub
#End Region

        Friend ReadOnly Property AnyMemberHasAttributes As Boolean
            Get
                If (Not Me._lazyAnyMemberHasAttributes.HasValue()) Then
                    Me._lazyAnyMemberHasAttributes = Me._declaration.AnyMemberHasAttributes.ToThreeState()
                End If

                Return Me._lazyAnyMemberHasAttributes.Value()
            End Get
        End Property
    End Class
End Namespace

