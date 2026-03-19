' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Out = System.Runtime.InteropServices.OutAttribute

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Traverses the symbol table checking for CLS compliance.
    ''' </summary>
    Partial Friend Class ClsComplianceChecker
        Inherits VisualBasicSymbolVisitor

        Private ReadOnly _compilation As VisualBasicCompilation

        ' if not null, limit analysis to types residing in this tree.
        Private ReadOnly _filterTree As SyntaxTree

        ' if filterTree and filterSpanWithinTree is not null, limit analysis to types residing within this span in the filterTree.
        Private ReadOnly _filterSpanWithinTree As TextSpan?

        Private ReadOnly _diagnostics As BindingDiagnosticBag

        Private ReadOnly _cancellationToken As CancellationToken

        Private ReadOnly _declaredOrInheritedCompliance As ConcurrentDictionary(Of Symbol, Compliance)

        ''' <seealso cref="MethodCompiler._compilerTasks"/>
        Private ReadOnly _compilerTasks As ConcurrentStack(Of Task)

        Private Sub New(compilation As VisualBasicCompilation, filterTree As SyntaxTree, filterSpanWithinTree As TextSpan?, diagnostics As BindingDiagnosticBag, cancellationToken As CancellationToken)
            Debug.Assert(diagnostics.DependenciesBag Is Nothing OrElse TypeOf diagnostics.DependenciesBag Is ConcurrentSet(Of AssemblySymbol))

            Me._compilation = compilation
            Me._filterTree = filterTree
            Me._filterSpanWithinTree = filterSpanWithinTree
            Me._diagnostics = diagnostics
            Me._cancellationToken = cancellationToken
            Me._declaredOrInheritedCompliance = New ConcurrentDictionary(Of Symbol, Compliance)()

            If ConcurrentAnalysis Then
                Me._compilerTasks = New ConcurrentStack(Of Task)()
            End If
        End Sub

        ''' <summary>
        ''' Gets a value indicating whether <see cref="ClsComplianceChecker"/> Is allowed to analyze in parallel.
        ''' </summary>
        Private ReadOnly Property ConcurrentAnalysis As Boolean
            Get
                Return _filterTree Is Nothing AndAlso _compilation.Options.ConcurrentBuild
            End Get
        End Property

        ''' <summary>
        ''' Traverses the symbol table checking for CLS compliance.
        ''' </summary>
        ''' <param name="compilation">Compilation that owns the symbol table.</param>
        ''' <param name="diagnostics">Will be supplemented with documentation comment diagnostics.</param>
        ''' <param name="cancellationToken">To stop traversing the symbol table early.</param>
        ''' <param name="filterTree">Only report diagnostics from this syntax tree, if non-null.</param>
        ''' <param name="filterSpanWithinTree">If <paramref name="filterTree"/> and <paramref name="filterSpanWithinTree"/> is non-null, report diagnostics within this span in the <paramref name="filterTree"/>.</param>
        Public Shared Sub CheckCompliance(compilation As VisualBasicCompilation, diagnostics As BindingDiagnosticBag, cancellationToken As CancellationToken, Optional filterTree As SyntaxTree = Nothing, Optional filterSpanWithinTree As TextSpan? = Nothing)
            Dim queue = If(diagnostics.AccumulatesDependencies, BindingDiagnosticBag.GetConcurrentInstance(), BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False))
            Dim checker = New ClsComplianceChecker(compilation, filterTree, filterSpanWithinTree, queue, cancellationToken)
            checker.Visit(compilation.Assembly)
            checker.WaitForWorkers()
            diagnostics.AddRangeAndFree(queue)
        End Sub

        Private Sub WaitForWorkers()
            Dim tasks As ConcurrentStack(Of Task) = Me._compilerTasks
            If tasks Is Nothing Then
                Return
            End If

            Dim curTask As Task = Nothing
            While tasks.TryPop(curTask)
                curTask.GetAwaiter().GetResult()
            End While
        End Sub

        Public Overrides Sub VisitAssembly(symbol As AssemblySymbol)
            Me._cancellationToken.ThrowIfCancellationRequested()
            Debug.Assert(TypeOf symbol Is SourceAssemblySymbol)

            ' NOTE: unlike in C#, false at the assembly level does not short-circuit any checks.

            ' The regular attribute code handles conflicting attributes from included netmodules.

            If symbol.Modules.Length > 1 AndAlso ConcurrentAnalysis Then
                VisitAssemblyMembersAsTasks(symbol)
            Else
                VisitAssemblyMembers(symbol)
            End If
        End Sub

        Private Sub VisitAssemblyMembersAsTasks(symbol As AssemblySymbol)
            For Each m In symbol.Modules
                _compilerTasks.Push(
                    Task.Run(
                        UICultureUtilities.WithCurrentUICulture(
                            Sub()
                                Try
                                    VisitModule(m)
                                Catch e As Exception When FatalError.ReportAndPropagateUnlessCanceled(e)
                                    Throw ExceptionUtilities.Unreachable
                                End Try
                            End Sub),
                        Me._cancellationToken))
            Next
        End Sub

        Private Sub VisitAssemblyMembers(symbol As AssemblySymbol)
            For Each m In symbol.Modules
                VisitModule(m)
            Next
        End Sub

        Public Overrides Sub VisitModule(symbol As ModuleSymbol)
            Visit(symbol.GlobalNamespace)
        End Sub

        Public Overrides Sub VisitNamespace(symbol As NamespaceSymbol)
            Me._cancellationToken.ThrowIfCancellationRequested()
            If DoNotVisit(symbol) Then
                Return
            End If

            If IsTrue(GetDeclaredOrInheritedCompliance(symbol)) Then
                CheckName(symbol)
                CheckMemberDistinctness(symbol)
            End If

            If ConcurrentAnalysis Then
                VisitNamespaceMembersAsTasks(symbol)
            Else
                VisitNamespaceMembers(symbol)
            End If
        End Sub

        Private Sub VisitNamespaceMembersAsTasks(symbol As NamespaceSymbol)
            For Each m In symbol.GetMembersUnordered()
                _compilerTasks.Push(
                    Task.Run(
                        UICultureUtilities.WithCurrentUICulture(
                            Sub()
                                Try
                                    Visit(m)
                                Catch e As Exception When FatalError.ReportAndPropagateUnlessCanceled(e)
                                    Throw ExceptionUtilities.Unreachable
                                End Try
                            End Sub),
                        Me._cancellationToken))
            Next
        End Sub

        Private Sub VisitNamespaceMembers(symbol As NamespaceSymbol)
            For Each m In symbol.GetMembersUnordered()
                Visit(m)
            Next
        End Sub

        <PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", IsParallelEntry:=False)>
        Public Overrides Sub VisitNamedType(symbol As NamedTypeSymbol)
            Me._cancellationToken.ThrowIfCancellationRequested()
            If DoNotVisit(symbol) Then
                Return
            End If

            Debug.Assert(Not symbol.IsImplicitClass)
            Dim compliance As Compliance = GetDeclaredOrInheritedCompliance(symbol)
            If VisitTypeOrMember(symbol, compliance) AndAlso IsTrue(compliance) Then
                CheckBaseTypeCompliance(symbol)
                CheckTypeParameterCompliance(symbol.TypeParameters, symbol)
                CheckMemberDistinctness(symbol)
                If symbol.TypeKind = TypeKind.Delegate Then
                    CheckParameterCompliance(symbol.DelegateInvokeMethod.Parameters, symbol)
                End If
            End If

            For Each m In symbol.GetMembersUnordered()
                Visit(m)
            Next
        End Sub

        Public Overrides Sub VisitMethod(symbol As MethodSymbol)
            Me._cancellationToken.ThrowIfCancellationRequested()
            If DoNotVisit(symbol) Then
                Return
            End If

            Dim compliance As Compliance = GetDeclaredOrInheritedCompliance(symbol)

            Dim checkForAdditionalWarnings As Boolean = VisitTypeOrMember(symbol, compliance)
            Dim isAccessor As Boolean = symbol.IsAccessor()

            If Not checkForAdditionalWarnings AndAlso Not isAccessor Then
                Return
            End If

            If Not isAccessor Then
                If IsTrue(compliance) Then
                    CheckParameterCompliance(symbol.Parameters, symbol.ContainingType)
                    CheckTypeParameterCompliance(symbol.TypeParameters, symbol.ContainingType)
                End If
            Else
                Dim methodKind As MethodKind = symbol.MethodKind
                Select Case methodKind
                    Case MethodKind.PropertyGet, MethodKind.PropertySet
                        ' As in dev11, this warning is not produced for event accessors.
                        For Each attribute In symbol.GetAttributes()
                            If attribute.IsTargetAttribute(AttributeDescription.CLSCompliantAttribute) Then
                                Dim attributeLocation As Location = Nothing
                                If TryGetAttributeWarningLocation(attribute, attributeLocation) Then
                                    Dim attributeUsage As AttributeUsageInfo = attribute.AttributeClass.GetAttributeUsageInfo()
                                    Me.AddDiagnostic(symbol, ERRID.WRN_CLSAttrInvalidOnGetSet, attributeLocation, attribute.AttributeClass.Name, attributeUsage.GetValidTargetsErrorArgument())
                                    Exit For
                                End If
                            End If
                        Next

                    Case MethodKind.EventAdd, MethodKind.EventRemove
                        If checkForAdditionalWarnings Then
                            Dim containingType = symbol.ContainingType
                            ' As in dev11, this warning is not produced for EventRaise methods, because they are not accessible outside the assembly.
                            If Not IsTrue(GetDeclaredOrInheritedCompliance(containingType)) Then
                                ' Note that we can't reuse the value of GetDeclaredOrInheritedCompliance, because that is actually based on the event.
                                Dim attributeLocation As Location = Nothing
                                If GetDeclaredCompliance(symbol, attributeLocation) = True Then
                                    ' This warning is a little strange since attributes on event accessors are silently ignored.
                                    Me.AddDiagnostic(symbol, ERRID.WRN_CLSEventMethodInNonCLSType3, attributeLocation, methodKind.TryGetAccessorDisplayName(), symbol.AssociatedSymbol.Name, containingType)
                                End If
                            End If
                        End If
                End Select
            End If
        End Sub

        Public Overrides Sub VisitProperty(symbol As PropertySymbol)
            Me._cancellationToken.ThrowIfCancellationRequested()
            If DoNotVisit(symbol) Then
                Return
            End If

            Dim compliance As Compliance = GetDeclaredOrInheritedCompliance(symbol)
            If Not VisitTypeOrMember(symbol, compliance) Then
                Return
            End If

            If IsTrue(compliance) Then
                CheckParameterCompliance(symbol.Parameters, symbol.ContainingType)
            End If
        End Sub

        Public Overrides Sub VisitEvent(symbol As EventSymbol)
            Me._cancellationToken.ThrowIfCancellationRequested()
            If DoNotVisit(symbol) Then
                Return
            End If

            Dim compliance As Compliance = GetDeclaredOrInheritedCompliance(symbol)
            If Not VisitTypeOrMember(symbol, compliance) Then
                Return
            End If
        End Sub

        Public Overrides Sub VisitField(symbol As FieldSymbol)
            Me._cancellationToken.ThrowIfCancellationRequested()
            If DoNotVisit(symbol) Then
                Return
            End If

            Dim compliance As Compliance = GetDeclaredOrInheritedCompliance(symbol)
            If Not VisitTypeOrMember(symbol, compliance) Then
                Return
            End If
        End Sub

        Private Function VisitTypeOrMember(symbol As Symbol, compliance As Compliance) As Boolean
            Debug.Assert(symbol.Kind = SymbolKind.NamedType OrElse symbol.Kind = SymbolKind.Field OrElse symbol.Kind = SymbolKind.Property OrElse symbol.Kind = SymbolKind.Event OrElse symbol.Kind = SymbolKind.Method)

            If Not IsAccessibleOutsideAssembly(symbol) Then
                Return False
            End If

            Dim isAccessor As Boolean = symbol.IsAccessor()

            If IsTrue(compliance) Then
                CheckName(symbol)
                If Not isAccessor Then
                    ' There's a similar warning for event accessors, but it's handled separately.
                    CheckForCompliantWithinNonCompliant(symbol)
                End If

                If symbol.Kind = SymbolKind.NamedType Then
                    Dim invokeMethod = DirectCast(symbol, NamedTypeSymbol).DelegateInvokeMethod
                    If invokeMethod IsNot Nothing Then
                        CheckReturnTypeCompliance(invokeMethod)
                    End If
                ElseIf symbol.Kind = SymbolKind.Event Then
                    CheckEventTypeCompliance(DirectCast(symbol, EventSymbol))
                ElseIf Not isAccessor Then
                    CheckReturnTypeCompliance(symbol)
                End If
            ElseIf Not isAccessor AndAlso IsTrue(GetInheritedCompliance(symbol)) Then
                CheckForNonCompliantAbstractMember(symbol)
            End If

            Return True
        End Function

        Private Sub CheckForNonCompliantAbstractMember(symbol As Symbol)
            Debug.Assert(Not IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on non-compliant symbols")
            Dim containingType As NamedTypeSymbol = symbol.ContainingType
            If containingType IsNot Nothing AndAlso containingType.IsInterface Then
                Me.AddDiagnostic(symbol, ERRID.WRN_NonCLSMemberInCLSInterface1, symbol) ' NOTE: Dev11 actually reports the kind
            ElseIf symbol.IsMustOverride AndAlso symbol.Kind <> SymbolKind.NamedType Then
                Me.AddDiagnostic(symbol, ERRID.WRN_NonCLSMustOverrideInCLSType1, containingType) ' NOTE: Dev11 actually reports the type kind
            End If
        End Sub

        Private Sub CheckBaseTypeCompliance(symbol As NamedTypeSymbol)
            Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on compliant symbols")
            If symbol.IsInterface Then
                For Each interfaceType In symbol.InterfacesNoUseSiteDiagnostics
                    If ShouldReportNonCompliantType(interfaceType, symbol) Then
                        Me.AddDiagnostic(symbol, ERRID.WRN_InheritedInterfaceNotCLSCompliant2, symbol, interfaceType)
                    End If
                Next
            ElseIf symbol.TypeKind = TypeKind.Enum Then
                Dim underlyingType As NamedTypeSymbol = symbol.EnumUnderlyingType
                If ShouldReportNonCompliantType(underlyingType, symbol) Then
                    Me.AddDiagnostic(symbol, ERRID.WRN_EnumUnderlyingTypeNotCLS1, underlyingType)
                End If
            Else
                Dim baseType As NamedTypeSymbol = symbol.BaseTypeNoUseSiteDiagnostics
                Debug.Assert(baseType IsNot Nothing OrElse symbol.SpecialType = SpecialType.System_Object, "Only object has no base.")
                If baseType IsNot Nothing AndAlso ShouldReportNonCompliantType(baseType, symbol) Then
                    Me.AddDiagnostic(symbol, ERRID.WRN_BaseClassNotCLSCompliant2, symbol, baseType)
                End If
            End If
        End Sub

        Private Sub CheckForCompliantWithinNonCompliant(symbol As Symbol)
            Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on compliant symbols")
            Dim containingType As NamedTypeSymbol = symbol.ContainingType
            Debug.Assert(containingType Is Nothing OrElse Not containingType.IsImplicitClass)
            If containingType IsNot Nothing AndAlso Not IsTrue(GetDeclaredOrInheritedCompliance(containingType)) Then
                Me.AddDiagnostic(symbol, ERRID.WRN_CLSMemberInNonCLSType3, symbol.GetKindText(), symbol, containingType)
            End If
        End Sub

        Private Sub CheckTypeParameterCompliance(typeParameters As ImmutableArray(Of TypeParameterSymbol), context As NamedTypeSymbol)
            Debug.Assert(typeParameters.IsEmpty OrElse IsTrue(GetDeclaredOrInheritedCompliance(context)), "Only call on compliant symbols")
            For Each typeParameter In typeParameters
                For Each constraintType In typeParameter.ConstraintTypesNoUseSiteDiagnostics
                    If ShouldReportNonCompliantType(constraintType, context, typeParameter) Then
                        Me.AddDiagnostic(typeParameter, ERRID.WRN_GenericConstraintNotCLSCompliant1, constraintType)
                    End If
                Next
            Next
        End Sub

        Private Sub CheckParameterCompliance(parameters As ImmutableArray(Of ParameterSymbol), context As NamedTypeSymbol)
            ' Containing symbol check is for the implicit delegate for an event.
            Debug.Assert(parameters.IsEmpty OrElse
                         IsTrue(GetDeclaredOrInheritedCompliance(context)) OrElse
                         IsTrue(GetDeclaredOrInheritedCompliance(parameters(0).ContainingSymbol)),
                         "Only call on compliant symbols")
            For Each parameter In parameters
                If ShouldReportNonCompliantType(parameter.Type, context, parameter) Then
                    Me.AddDiagnostic(parameter, ERRID.WRN_ParamNotCLSCompliant1, parameter.Name)
                ElseIf parameter.HasExplicitDefaultValue Then
                    ' CLSComplianceChecker::VerifyProcForCLSCompliance checks for exactly these types
                    Select Case parameter.ExplicitDefaultConstantValue.Discriminator
                        Case ConstantValueTypeDiscriminator.SByte,
                            ConstantValueTypeDiscriminator.UInt16,
                            ConstantValueTypeDiscriminator.UInt32,
                            ConstantValueTypeDiscriminator.UInt64
                            Me.AddDiagnostic(parameter, ERRID.WRN_OptionalValueNotCLSCompliant1, parameter.Name)
                    End Select
                End If
            Next
        End Sub

        Private Function TryGetAttributeWarningLocation(attribute As VisualBasicAttributeData, ByRef location As Location) As Boolean
            Dim syntaxRef As SyntaxReference = attribute.ApplicationSyntaxReference
            If syntaxRef Is Nothing AndAlso _filterTree Is Nothing Then
                location = NoLocation.Singleton
                Return True
            ElseIf _filterTree Is Nothing OrElse (syntaxRef IsNot Nothing AndAlso syntaxRef.SyntaxTree Is _filterTree) Then
                Debug.Assert(syntaxRef.SyntaxTree.HasCompilationUnitRoot)
                location = New SourceLocation(syntaxRef)
                Return True
            End If

            location = Nothing
            Return False
        End Function

        Private Sub CheckReturnTypeCompliance(symbol As Symbol)
            Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on compliant symbols")
            Dim code As ERRID
            Dim type As TypeSymbol
            Select Case symbol.Kind
                Case SymbolKind.Field
                    code = ERRID.WRN_FieldNotCLSCompliant1
                    type = (DirectCast(symbol, FieldSymbol)).Type
                Case SymbolKind.Property
                    code = ERRID.WRN_ProcTypeNotCLSCompliant1
                    type = (DirectCast(symbol, PropertySymbol)).Type
                Case SymbolKind.Method
                    code = ERRID.WRN_ProcTypeNotCLSCompliant1
                    Dim method As MethodSymbol = DirectCast(symbol, MethodSymbol)
                    type = method.ReturnType
                    ' As in dev11, we report on the delegate Invoke method, rather than on the delegate itself.
                    Debug.Assert(Not method.IsAccessor())
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
            End Select

            If ShouldReportNonCompliantType(type, symbol.ContainingType, symbol) Then
                Me.AddDiagnostic(symbol, code, symbol.Name)
            End If
        End Sub

        Private Sub CheckEventTypeCompliance(symbol As EventSymbol)
            Dim type = symbol.Type
            If type.TypeKind = TypeKind.Delegate AndAlso type.IsImplicitlyDeclared AndAlso TryCast(type, NamedTypeSymbol)?.AssociatedSymbol Is symbol Then
                Debug.Assert(symbol.DelegateReturnType.SpecialType = SpecialType.System_Void)
                CheckParameterCompliance(symbol.DelegateParameters, symbol.ContainingType)
            ElseIf ShouldReportNonCompliantType(type, symbol.ContainingType, symbol) Then
                Me.AddDiagnostic(symbol, ERRID.WRN_EventDelegateTypeNotCLSCompliant2, type, symbol.Name)
            End If
        End Sub

        Private Sub CheckMemberDistinctness(symbol As NamespaceOrTypeSymbol)
            Debug.Assert(IsAccessibleOutsideAssembly(symbol))
            Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)))
            Dim seenByName As MultiDictionary(Of String, Symbol) = New MultiDictionary(Of String, Symbol)(CaseInsensitiveComparison.Comparer)

            ' BREAK: Dev11 does not consider collisions with inherited members

            ' // UNDONE:harishk
            ' // Don't known if we have to do the Overloads checking even
            ' // for the overloads across classes.
            ' //
            ' // We (the VB compiler) verify this for the overloaded members in the
            ' // same class, but do we have to verify this even for a derived class
            ' // method overloading a base class method.
            ' //
            ' // My reasoning is that if any other language cannot distinguish the
            ' // difference in signature between the overloaded methods in the base
            ' // and derived, then they would assume hide by sig, and their users
            ' // can always access the base method directly by casting their derived
            ' // instance. So this is CLS Compliant. Is this correct ?
            ' // sent email to pdrayton and jsmiller

            If symbol.Kind <> SymbolKind.Namespace Then
                Dim type As NamedTypeSymbol = DirectCast(symbol, NamedTypeSymbol)
                For Each [interface] In type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys
                    If Not IsAccessibleOutsideAssembly([interface]) Then
                        Continue For
                    End If

                    For Each member In [interface].GetMembersUnordered()
                        If IsAccessibleIfContainerIsAccessible(member) AndAlso (Not member.IsOverrides OrElse Not (member.Kind = SymbolKind.Method OrElse member.Kind = SymbolKind.Property)) Then
                            seenByName.Add(member.Name, member)
                        End If
                    Next
                Next

                Dim baseType As NamedTypeSymbol = type.BaseTypeNoUseSiteDiagnostics
                While baseType IsNot Nothing
                    For Each member In baseType.GetMembersUnordered()
                        If IsAccessibleOutsideAssembly(member) AndAlso IsTrue(GetDeclaredOrInheritedCompliance(member)) AndAlso (Not member.IsOverrides OrElse Not (member.Kind = SymbolKind.Method OrElse member.Kind = SymbolKind.Property)) Then
                            seenByName.Add(member.Name, member)
                        End If
                    Next

                    baseType = baseType.BaseTypeNoUseSiteDiagnostics
                End While
            End If

            For Each member In symbol.GetMembers()
                If DoNotVisit(member) OrElse Not IsAccessibleIfContainerIsAccessible(member) OrElse Not IsTrue(GetDeclaredOrInheritedCompliance(member)) OrElse member.IsOverrides Then
                    Continue For
                End If

                Dim name As String = member.Name
                Dim sameNameSymbols = seenByName(name)
                If sameNameSymbols.Count > 0 Then
                    CheckSymbolDistinctness(member, sameNameSymbols)
                End If

                seenByName.Add(name, member)
            Next
        End Sub

        ''' <remarks>
        ''' NOTE: Dev11 does some pretty weird things here.  First, it ignores arity,
        ''' which seems like a good way to disambiguate symbols (in particular,
        ''' CLS Rule 43 says that the name includes backtick-arity).  Second, it
        ''' does not consider two members with identical names (i.e. not differing
        ''' in case) to collide.
        ''' </remarks>
        Private Sub CheckSymbolDistinctness(symbol As Symbol, sameNameSymbols As MultiDictionary(Of String, Symbol).ValueSet)
            Debug.Assert(sameNameSymbols.Count > 0)

            Dim isMethodOrProperty As Boolean = symbol.Kind = SymbolKind.Method OrElse symbol.Kind = SymbolKind.Property
            If Not isMethodOrProperty Then
                Return
            End If

            For Each other As Symbol In sameNameSymbols
                ' Note: not checking accessor signatures, but checking accessor names.
                If symbol.Kind = other.Kind AndAlso Not symbol.IsAccessor() AndAlso Not other.IsAccessor() AndAlso SignaturesCollide(symbol, other) Then
                    Me.AddDiagnostic(symbol, ERRID.WRN_ArrayOverloadsNonCLS2, symbol, other)
                    ' NOTE: Unlike in C#, we can't stop after the first conflict because our diagnostic actually
                    ' references the other symbol and we need to produce the same diagnostics every time.
                End If
            Next
        End Sub

        Private Sub CheckName(symbol As Symbol)
            Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)))
            Debug.Assert(IsAccessibleOutsideAssembly(symbol))
            If Not symbol.CanBeReferencedByName Then ' NOTE: Unlike C#, VB checks override names.
                Return
            End If

            Dim name As String = symbol.Name
            Debug.Assert(name.Length = 0 OrElse name(0) <> ChrW(&HFF3F))
            If name.Length > 0 AndAlso name(0) = "_"c Then

                If symbol.Kind = SymbolKind.Namespace Then
                    Dim rootNamespace = Me._compilation.RootNamespace

                    Debug.Assert(symbol.ContainingNamespace IsNot Nothing, "Only true for the global namespace and that has an empty name.")
                    If symbol = rootNamespace AndAlso symbol.ContainingNamespace.IsGlobalNamespace Then
                        Me.AddDiagnostic(symbol, ERRID.WRN_RootNamespaceNotCLSCompliant1, rootNamespace)
                        Return
                    End If

                    Dim curr = rootNamespace
                    While curr IsNot Nothing
                        If symbol = curr Then
                            Me.AddDiagnostic(symbol, ERRID.WRN_RootNamespaceNotCLSCompliant2, symbol.Name, rootNamespace)
                            Return
                        End If

                        curr = curr.ContainingNamespace
                    End While
                End If

                Me.AddDiagnostic(symbol, ERRID.WRN_NameNotCLSCompliant1, name)
            End If
        End Sub

        Private Function DoNotVisit(symbol As Symbol) As Boolean
            If symbol.Kind = SymbolKind.Namespace Then
                Return False
            End If

            Return symbol.DeclaringCompilation IsNot Me._compilation OrElse symbol.IsImplicitlyDeclared OrElse IsSyntacticallyFilteredOut(symbol)
        End Function

        Private Function IsSyntacticallyFilteredOut(symbol As Symbol) As Boolean
            Return Me._filterTree IsNot Nothing AndAlso Not symbol.IsDefinedInSourceTree(Me._filterTree, Me._filterSpanWithinTree, Me._cancellationToken)
        End Function

        Private Function ShouldReportNonCompliantType(type As TypeSymbol, context As NamedTypeSymbol, Optional diagnosticSymbol As Symbol = Nothing) As Boolean
            ' NOTE: non-compliance of type arguments is checked separately and does not affect whether or
            ' not the "top-level" non-compliance diagnostic is reported.
            ReportNonCompliantTypeArguments(type, context, If(diagnosticSymbol, context))
            Return Not IsCompliantType(type, context)
        End Function

        Private Sub ReportNonCompliantTypeArguments(type As TypeSymbol, context As NamedTypeSymbol, diagnosticSymbol As Symbol)
            Select Case type.TypeKind
                Case TypeKind.Array
                    ReportNonCompliantTypeArguments((DirectCast(type, ArrayTypeSymbol)).ElementType, context, diagnosticSymbol)
                Case TypeKind.Error, TypeKind.TypeParameter
                    Return
                Case TypeKind.Class, TypeKind.Structure, TypeKind.Interface, TypeKind.Delegate, TypeKind.Enum, TypeKind.Submission, TypeKind.Module
                    ReportNonCompliantTypeArguments(DirectCast(type, NamedTypeSymbol), context, diagnosticSymbol)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(type.TypeKind)
            End Select
        End Sub

        Private Sub ReportNonCompliantTypeArguments(type As NamedTypeSymbol, context As NamedTypeSymbol, diagnosticSymbol As Symbol)
            If type.IsTupleType Then
                type = type.TupleUnderlyingType
            End If

            For Each typeArg In type.TypeArgumentsNoUseSiteDiagnostics
                If Not IsCompliantType(typeArg, context) Then
                    Me.AddDiagnostic(diagnosticSymbol, ERRID.WRN_TypeNotCLSCompliant1, typeArg)
                End If
                ReportNonCompliantTypeArguments(typeArg, context, diagnosticSymbol)
            Next
        End Sub

        Private Function IsCompliantType(type As TypeSymbol, context As NamedTypeSymbol) As Boolean
            Select Case type.TypeKind
                Case TypeKind.Array
                    Return IsCompliantType((DirectCast(type, ArrayTypeSymbol)).ElementType, context)
                Case TypeKind.Error, TypeKind.TypeParameter
                    Return True
                Case TypeKind.Class, TypeKind.Structure, TypeKind.Interface, TypeKind.Delegate, TypeKind.Enum, TypeKind.Submission, TypeKind.Module
                    Return IsCompliantType(DirectCast(type, NamedTypeSymbol))
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(type.TypeKind)
            End Select
        End Function

        Private Function IsCompliantType(type As NamedTypeSymbol) As Boolean
            Select Case type.SpecialType
                Case SpecialType.System_TypedReference, SpecialType.System_UIntPtr
                    Return False
                Case SpecialType.System_SByte, SpecialType.System_UInt16, SpecialType.System_UInt32, SpecialType.System_UInt64
                    Return False
            End Select

            If type.TypeKind = TypeKind.Error Then
                Return True
            End If

            If Not IsTrue(GetDeclaredOrInheritedCompliance(type.OriginalDefinition)) Then
                Return False
            End If

            If type.IsTupleType Then
                Return IsCompliantType(type.TupleUnderlyingType)
            End If

            ' NOTE: Type arguments are checked separately (see HasNonCompliantTypeArguments)

            ' NOTE: C# also does some checks about protected members of protected types, but VB does not.
            ' Presumably, they were unnecessary in dev11, because the native compiler had a bunch of
            ' errors in this area (removed in roslyn).
            Return True
        End Function

        Private Function GetDeclaredOrInheritedCompliance(symbol As Symbol) As Compliance
            Debug.Assert(symbol.Kind = SymbolKind.NamedType OrElse Not (TypeOf symbol Is TypeSymbol), "Type kinds without declarations are handled elsewhere.")

            Debug.Assert(symbol.Kind <> If(Me._compilation.Options.OutputKind = OutputKind.NetModule, SymbolKind.Assembly, SymbolKind.NetModule) OrElse
                         (symbol.Kind = SymbolKind.Assembly AndAlso Me._compilation.Assembly IsNot symbol),
                         "Don't care about assembly when building netmodule and vice versa")

            If symbol.Kind = SymbolKind.Namespace Then
                ' Don't bother storing entries for namespaces - just go straight to the assembly.
                Return GetDeclaredOrInheritedCompliance(GetContainingModuleOrAssembly(symbol))
            ElseIf symbol.Kind = SymbolKind.Method Then
                Dim method As MethodSymbol = DirectCast(symbol, MethodSymbol)
                Dim associated As Symbol = method.AssociatedSymbol
                If associated IsNot Nothing Then
                    ' Don't bother storing entries for accessors - just go straight to the property/event.
                    Return GetDeclaredOrInheritedCompliance(associated)
                End If
            End If

            Debug.Assert(symbol.Kind <> SymbolKind.Alias)
            Debug.Assert(symbol.Kind <> SymbolKind.Label)
            Debug.Assert(symbol.Kind <> SymbolKind.Namespace)
            Debug.Assert(symbol.Kind <> SymbolKind.Parameter)
            Debug.Assert(symbol.Kind <> SymbolKind.RangeVariable)
            Dim compliance As Compliance
            If Me._declaredOrInheritedCompliance.TryGetValue(symbol, compliance) Then
                Return compliance
            End If

            Dim ignoredLocation As Location = Nothing
            Dim declaredCompliance As Boolean? = GetDeclaredCompliance(symbol, ignoredLocation)
            If declaredCompliance.HasValue Then
                compliance = If(declaredCompliance.GetValueOrDefault(), Compliance.DeclaredTrue, Compliance.DeclaredFalse)
            ElseIf symbol.Kind = SymbolKind.Assembly OrElse symbol.Kind = SymbolKind.NetModule Then
                compliance = Compliance.ImpliedFalse
            Else
                compliance = If(IsTrue(GetInheritedCompliance(symbol)), Compliance.InheritedTrue, Compliance.InheritedFalse)
            End If

            Select Case (symbol.Kind)
                Case SymbolKind.Assembly, SymbolKind.NetModule, SymbolKind.NamedType
                    Return Me._declaredOrInheritedCompliance.GetOrAdd(symbol, compliance)
                Case Else
                    Return compliance
            End Select
        End Function

        ''' <summary>
        ''' What is the argument to the (first) CLSCompliantAttribute on this symbol, if there is one?
        ''' Consider attributes inherited from base types.
        ''' </summary>
        Private Function GetInheritedCompliance(symbol As Symbol) As Compliance
            Debug.Assert(symbol.Kind <> SymbolKind.Assembly)
            Debug.Assert(symbol.Kind <> SymbolKind.NetModule)
            Dim containing As Symbol = If(DirectCast(symbol.ContainingType, Symbol), GetContainingModuleOrAssembly(symbol))
            Debug.Assert(containing IsNot Nothing)
            Return GetDeclaredOrInheritedCompliance(containing)
        End Function

        Private Function GetDeclaredCompliance(symbol As Symbol, <Out> ByRef attributeLocation As Location) As Boolean?
            ' Unlike C#, VB considers the fact that CLSCompliantAttribute (usually) has AttributeUsage settings indicating that it should
            ' be inherited by derived types.  However, it only uses this information for imported types and only when the value is False
            ' (i.e. the type is marked non-compliant).

            '    If the CLS Compliance attribute is inherited, then
            '     - we only infer non-CLS compliantness from an inherited CLS Compliant
            '         attribute. The reason is we don't treat a container as CLS compliant
            '         unless it inherits from CLS compliant entities as well as contained
            '         within CLS compliant entities.
            '    
            '     - we only infer from an inherited CLS Compliant attribute for metadata
            '         containers. Source containers infer this from their containing type
            '         and an appropriate warning/error is given if their inherit from a
            '         non-CLS Compliant type

            If symbol.IsFromCompilation(Me._compilation) OrElse symbol.Kind <> SymbolKind.NamedType Then
                Return GetDeclaredComplianceHelper(symbol, attributeLocation, isAttributeInherited:=Nothing)
            End If

            Dim namedType = DirectCast(symbol, NamedTypeSymbol)

            ' Walk up the base type chain until we find a type with a CLSCompliantAttribute.
            While namedType IsNot Nothing
                Dim isAttributeInherited = False
                Dim temp = GetDeclaredComplianceHelper(namedType, attributeLocation, isAttributeInherited)
                If temp.HasValue Then
                    ' Inherit False but not True.  Stop regardless.
                    Return If(namedType Is symbol OrElse (isAttributeInherited AndAlso Not temp), temp, Nothing)
                End If

                ' For interfaces, the BaseType will be Nothing.  This is what we 
                ' want since they don't inherit attributes from base interfaces.
                namedType = namedType.BaseTypeNoUseSiteDiagnostics
            End While

            Return Nothing
        End Function

        ''' <summary>
        ''' What is the argument to the (first) CLSCompliantAttribute on this symbol, if there is one?
        ''' Do not consider attributes inherited from base types.
        ''' </summary>
        Private Function GetDeclaredComplianceHelper(symbol As Symbol, <Out> ByRef attributeLocation As Location, <Out> ByRef isAttributeInherited As Boolean) As Boolean?
            attributeLocation = Nothing
            isAttributeInherited = False
            For Each attributeData In symbol.GetAttributes()
                ' Check signature before HasErrors to avoid realizing symbols for other attributes.
                If attributeData.IsTargetAttribute(AttributeDescription.CLSCompliantAttribute) Then
                    Dim attributeClass = attributeData.AttributeClass
                    If attributeClass IsNot Nothing Then
                        _diagnostics.ReportUseSite(attributeClass, If(symbol.Locations.IsEmpty, NoLocation.Singleton, symbol.GetFirstLocation()))
                    End If

                    If Not attributeData.HasErrors Then
                        If Not TryGetAttributeWarningLocation(attributeData, attributeLocation) Then
                            attributeLocation = Nothing
                        End If

                        Debug.Assert(Not attributeData.AttributeClass.IsErrorType(), "Already checked HasErrors.")
                        isAttributeInherited = attributeData.AttributeClass.GetAttributeUsageInfo().Inherited

                        Dim args As ImmutableArray(Of TypedConstant) = attributeData.CommonConstructorArguments
                        Debug.Assert(args.Length = 1, "We already checked the signature and HasErrors.")

                        ' Duplicates are reported elsewhere - we only care about the first (error-free) occurrence.
                        Return DirectCast(args(0).ValueInternal, Boolean)
                    End If
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Return the containing module if the output kind is module and the containing assembly otherwise.
        ''' </summary>
        Private Function GetContainingModuleOrAssembly(symbol As Symbol) As Symbol
            Dim containingAssembly = symbol.ContainingAssembly

            If containingAssembly IsNot Me._compilation.Assembly Then
                Return containingAssembly
            End If

            Dim producingNetModule = Me._compilation.Options.OutputKind = OutputKind.NetModule
            Return If(producingNetModule, DirectCast(symbol.ContainingModule, Symbol), containingAssembly)
        End Function

        Private Shared Function IsAccessibleOutsideAssembly(symbol As Symbol) As Boolean
            While symbol IsNot Nothing AndAlso Not IsImplicitClass(symbol)
                If Not IsAccessibleIfContainerIsAccessible(symbol) Then
                    Return False
                End If

                symbol = symbol.ContainingType
            End While

            Return True
        End Function

        Private Shared Function IsAccessibleIfContainerIsAccessible(symbol As Symbol) As Boolean
            Select Case symbol.DeclaredAccessibility
                Case Accessibility.Public
                    Return True
                Case Accessibility.Protected, Accessibility.ProtectedOrFriend
                    ' NOTE: Unlike C#, VB considers protected members of sealed types inaccessible.
                    Dim containingType = symbol.ContainingType
                    Return containingType Is Nothing OrElse Not containingType.IsNotInheritable
                Case Accessibility.Private, Accessibility.ProtectedAndFriend, Accessibility.Friend
                    Return False
                Case Accessibility.NotApplicable
                    Debug.Assert(symbol.Kind = SymbolKind.ErrorType)
                    Return False
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.DeclaredAccessibility)
            End Select
        End Function

        Private Sub AddDiagnostic(symbol As Symbol, code As ERRID, ParamArray args As Object())
            Dim location = If(symbol.Locations.IsEmpty, NoLocation.Singleton, symbol.GetFirstLocation())
            Me.AddDiagnostic(symbol, code, location, args)
        End Sub

        Private Sub AddDiagnostic(symbol As Symbol, code As ERRID, location As Location, ParamArray args As Object())
            Dim info = New BadSymbolDiagnostic(symbol, code, args)
            Dim diag = New VBDiagnostic(info, location)
            Me._diagnostics.Add(diag)
        End Sub

        Private Shared Function IsImplicitClass(symbol As Symbol) As Boolean
            Return symbol.Kind = SymbolKind.NamedType AndAlso (DirectCast(symbol, NamedTypeSymbol)).IsImplicitClass
        End Function

        Private Shared Function IsTrue(compliance As Compliance) As Boolean
            Select Case compliance
                Case Compliance.DeclaredTrue, Compliance.InheritedTrue
                    Return True
                Case Compliance.DeclaredFalse, Compliance.InheritedFalse, Compliance.ImpliedFalse
                    Return False
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(compliance)
            End Select
        End Function

        Private Enum Compliance
            DeclaredTrue
            DeclaredFalse
            InheritedTrue
            InheritedFalse
            ImpliedFalse
        End Enum

        ''' <remarks>
        ''' Based on CompilationPass::CLSReduceSignature.
        ''' </remarks>
        Private Shared Function SignaturesCollide(x As Symbol, y As Symbol) As Boolean
            Debug.Assert(x IsNot Nothing)
            Debug.Assert(y IsNot Nothing)
            Debug.Assert(x IsNot y)
            Debug.Assert(x.Kind = y.Kind)
            Dim xParameterTypes As ImmutableArray(Of TypeSymbol) = GetParameterTypes(x)
            Dim yParameterTypes As ImmutableArray(Of TypeSymbol) = GetParameterTypes(y)
            Dim xRefKinds As ImmutableArray(Of RefKind) = GetParameterRefKinds(x)
            Dim yRefKinds As ImmutableArray(Of RefKind) = GetParameterRefKinds(y)

            Dim numParams As Integer = xParameterTypes.Length
            If yParameterTypes.Length <> numParams Then
                Return False
            End If

            ' Compare parameters without regard for RefKind (or other modifier),
            ' array rank, or unnamed array element types (e.g. int[][] == char[][]).
            Dim sawArrayRankDifference As Boolean = False
            Dim sawArrayOfArraysDifference As Boolean = False
            For i = 0 To numParams - 1
                Dim xType As TypeSymbol = xParameterTypes(i)
                Dim yType As TypeSymbol = yParameterTypes(i)
                Dim typeKind As TypeKind = xType.TypeKind
                If yType.TypeKind <> typeKind Then
                    Return False
                End If

                If typeKind = TypeKind.Array Then
                    Dim xArrayType As ArrayTypeSymbol = DirectCast(xType, ArrayTypeSymbol)
                    Dim yArrayType As ArrayTypeSymbol = DirectCast(yType, ArrayTypeSymbol)
                    sawArrayRankDifference = sawArrayRankDifference OrElse xArrayType.Rank <> yArrayType.Rank
                    Dim elementTypesDiffer As Boolean = Not TypeSymbol.Equals(xArrayType.ElementType, yArrayType.ElementType, TypeCompareKind.ConsiderEverything)
                    If IsArrayOfArrays(xArrayType) AndAlso IsArrayOfArrays(yArrayType) Then ' NOTE: C# uses OrElse
                        sawArrayOfArraysDifference = sawArrayOfArraysDifference OrElse elementTypesDiffer
                    ElseIf elementTypesDiffer Then
                        Return False
                    End If
                ElseIf Not TypeSymbol.Equals(xType, yType, TypeCompareKind.ConsiderEverything) Then
                    Return False
                End If
            Next

            Return sawArrayOfArraysDifference OrElse sawArrayRankDifference
        End Function

        Private Shared Function IsArrayOfArrays(arrayType As ArrayTypeSymbol) As Boolean
            Return arrayType.ElementType.Kind = SymbolKind.ArrayType
        End Function

        Private Shared Function GetParameterTypes(symbol As Symbol) As ImmutableArray(Of TypeSymbol)
            Dim parameters As ImmutableArray(Of ParameterSymbol)
            Select Case (symbol.Kind)
                Case SymbolKind.Method
                    parameters = DirectCast(symbol, MethodSymbol).Parameters
                Case SymbolKind.Property
                    parameters = DirectCast(symbol, PropertySymbol).Parameters
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
            End Select

            If parameters.IsEmpty Then
                Return ImmutableArray(Of TypeSymbol).Empty
            End If

            Dim builder = ArrayBuilder(Of TypeSymbol).GetInstance(parameters.Length)
            For Each parameter In parameters
                builder.Add(parameter.Type)
            Next
            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Function GetParameterRefKinds(symbol As Symbol) As ImmutableArray(Of RefKind)
            Dim parameters As ImmutableArray(Of ParameterSymbol)
            Select Case (symbol.Kind)
                Case SymbolKind.Method
                    parameters = DirectCast(symbol, MethodSymbol).Parameters
                Case SymbolKind.Property
                    parameters = DirectCast(symbol, PropertySymbol).Parameters
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
            End Select

            If parameters.IsEmpty Then
                Return ImmutableArray(Of RefKind).Empty
            End If

            Dim builder = ArrayBuilder(Of RefKind).GetInstance(parameters.Length)
            For Each parameter In parameters
                builder.Add(If(parameter.IsByRef, RefKind.Ref, RefKind.None))
            Next
            Return builder.ToImmutableAndFree()
        End Function
    End Class
End Namespace
