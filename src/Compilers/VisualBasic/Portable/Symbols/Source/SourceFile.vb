' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class SourceFile
        Implements Cci.IImportScope

        Private ReadOnly _sourceModule As SourceModuleSymbol
        Private ReadOnly _syntaxTree As SyntaxTree

        ' holds diagnostics related to source code in this particular source file, for 
        ' each stage.
        Private ReadOnly _diagnosticBagDeclare As New DiagnosticBag()

        ' Lazily filled in.
        Private _lazyBoundInformation As BoundFileInformation

        ' Set to nonzero when import validated errors have been reported.
        Private _importsValidated As Integer

        ' lazily populate with quick attribute checker that is initialized with the imports.
        Private _lazyQuickAttributeChecker As QuickAttributeChecker

        ''' <summary>
        ''' The bound information from a file.
        ''' </summary>
        Private NotInheritable Class BoundFileInformation

            ' Does not contain error types (imports with errors are ignored).
            Public ReadOnly MemberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition)

            ' Does not contain error imports with errors.
            Public ReadOnly MemberImportsSyntax As ImmutableArray(Of SyntaxReference)

            ' Can be Nothing if no alias imports. May contain alias whose target is an error type.
            Public ReadOnly AliasImportsOpt As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition)

            ' Can be Nothing if no xmlns imports.
            Public ReadOnly XmlNamespacesOpt As IReadOnlyDictionary(Of String, XmlNamespaceAndImportsClausePosition)

            ' HasValue is false if the given option wasn't present in the file.
            Public ReadOnly OptionStrict As Boolean?
            Public ReadOnly OptionInfer As Boolean?
            Public ReadOnly OptionExplicit As Boolean?
            Public ReadOnly OptionCompareText As Boolean?

            Public Sub New(memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                           memberImportsSyntax As ImmutableArray(Of SyntaxReference),
                           importAliasesOpt As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition),
                           xmlNamespacesOpt As IReadOnlyDictionary(Of String, XmlNamespaceAndImportsClausePosition),
                           optionStrict As Boolean?,
                           optionInfer As Boolean?,
                           optionExplicit As Boolean?,
                           optionCompareText As Boolean?)

                Debug.Assert(Not memberImports.IsDefault)
                Debug.Assert(Not memberImportsSyntax.IsDefault)
                Debug.Assert(memberImports.Length = memberImportsSyntax.Length)
                Debug.Assert(Not memberImports.Any(Function(i) i.NamespaceOrType.Kind = SymbolKind.ErrorType))

                Me.MemberImports = memberImports
                Me.MemberImportsSyntax = memberImportsSyntax
                Me.AliasImportsOpt = importAliasesOpt
                Me.XmlNamespacesOpt = xmlNamespacesOpt

                Me.OptionStrict = optionStrict
                Me.OptionInfer = optionInfer
                Me.OptionExplicit = optionExplicit
                Me.OptionCompareText = optionCompareText
            End Sub
        End Class

        Public Sub New(sourceModule As SourceModuleSymbol, tree As SyntaxTree)
            _sourceModule = sourceModule
            _syntaxTree = tree
        End Sub

        ' Get the declaration errors.
        Public ReadOnly Property DeclarationDiagnostics As DiagnosticBag
            Get
                Return _diagnosticBagDeclare
            End Get
        End Property

        ' Get a quick attribute checker that can be used for quick attributes checks, initialized with project-level
        ' aliases.
        Public ReadOnly Property QuickAttributeChecker As QuickAttributeChecker
            Get
                If _lazyQuickAttributeChecker Is Nothing Then
                    Interlocked.CompareExchange(_lazyQuickAttributeChecker, CreateQuickAttributeChecker(), Nothing)
                End If

                Return _lazyQuickAttributeChecker
            End Get
        End Property

        Private Function CreateQuickAttributeChecker() As QuickAttributeChecker
            ' First, initialize from the source module to get aliases from the options.
            Dim checker As New QuickAttributeChecker(_sourceModule.QuickAttributeChecker)

            ' Now process alias imports
            Dim compilationUnitSyntax = _syntaxTree.GetCompilationUnitRoot()
            For Each statement In compilationUnitSyntax.Imports
                For Each clause In statement.ImportsClauses
                    If clause.Kind = SyntaxKind.SimpleImportsClause Then

                        Dim simpleImportsClause = DirectCast(clause, SimpleImportsClauseSyntax)

                        If simpleImportsClause.Alias IsNot Nothing Then
                            checker.AddAlias(simpleImportsClause)
                        End If
                    End If
                Next
            Next

            checker.Seal()
            Return checker
        End Function

        ''''''''''''''''''''''''''''''''''''''
        ' Below here are accessors that use bound information.

        Private ReadOnly Property BoundInformation As BoundFileInformation
            Get
                Return GetBoundInformation(CancellationToken.None)
            End Get
        End Property

        Private Function GetBoundInformation(cancellationToken As CancellationToken) As BoundFileInformation
            If _lazyBoundInformation Is Nothing Then
                Dim diagBag = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)
                Dim lazyBoundInformation = BindFileInformation(diagBag.DiagnosticBag, cancellationToken)
                _sourceModule.AtomicStoreReferenceAndDiagnostics(_lazyBoundInformation, lazyBoundInformation, diagBag)
                diagBag.Free()
            End If

            Return _lazyBoundInformation
        End Function

        Private Sub EnsureImportsValidated()
            If _importsValidated = 0 Then
                Dim boundFileInformation = BoundInformation
                Dim diagBag = BindingDiagnosticBag.GetInstance()
                ValidateImports(_sourceModule.DeclaringCompilation, boundFileInformation.MemberImports, boundFileInformation.MemberImportsSyntax, boundFileInformation.AliasImportsOpt, diagBag)
                _sourceModule.AtomicStoreIntegerAndDiagnostics(_importsValidated, 1, 0, diagBag)
                diagBag.Free()
            End If
            Debug.Assert(_importsValidated = 1)
        End Sub

        Private Function BindFileInformation(diagBag As DiagnosticBag, cancellationToken As CancellationToken, Optional filterSpan As TextSpan? = Nothing) As BoundFileInformation

            ' The binder must be set up to only bind things in the global namespace, in order to bind imports 
            ' correctly. Note that a different binder would be needed for binding the file-level attributes.
            Dim binder = BinderBuilder.CreateBinderForSourceFileImports(_sourceModule, _syntaxTree)
            Dim compilationUnitSyntax = _syntaxTree.GetCompilationUnitRoot()

            Dim optionStrict As Boolean?
            Dim optionInfer As Boolean?
            Dim optionExplicit As Boolean?
            Dim optionCompareText As Boolean?

            BindOptions(compilationUnitSyntax.Options, diagBag, optionStrict, optionInfer, optionExplicit, optionCompareText, filterSpan)

            Dim importMembersOf As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim importMembersOfSyntax As ImmutableArray(Of SyntaxReference) = Nothing
            Dim importAliasesOpt As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim xmlNamespacesOpt As IReadOnlyDictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

            BindImports(compilationUnitSyntax.Imports, binder, diagBag, importMembersOf, importMembersOfSyntax, importAliasesOpt, xmlNamespacesOpt, cancellationToken, filterSpan)

            Return New BoundFileInformation(importMembersOf, importMembersOfSyntax, importAliasesOpt, xmlNamespacesOpt, optionStrict, optionInfer, optionExplicit, optionCompareText)
        End Function

        ' Bind the options and return the value of how options were specified.
        ' Errors are generated for duplicate options.
        Private Shared Sub BindOptions(optionsSyntax As SyntaxList(Of OptionStatementSyntax),
                                diagBag As DiagnosticBag,
                                ByRef optionStrict As Boolean?,
                                ByRef optionInfer As Boolean?,
                                ByRef optionExplicit As Boolean?,
                                ByRef optionCompareText As Boolean?,
                                Optional filterSpan As TextSpan? = Nothing)
            optionStrict = Nothing
            optionInfer = Nothing
            optionExplicit = Nothing
            optionCompareText = Nothing

            For Each optionStmtSyntax In optionsSyntax
                If filterSpan.HasValue AndAlso Not filterSpan.Value.IntersectsWith(optionStmtSyntax.FullSpan) Then
                    Continue For
                End If

                Select Case optionStmtSyntax.NameKeyword.Kind
                    Case SyntaxKind.StrictKeyword
                        If optionStrict.HasValue Then
                            Binder.ReportDiagnostic(diagBag, optionStmtSyntax, ERRID.ERR_DuplicateOption1, "Strict")
                        Else
                            optionStrict = Binder.DecodeOnOff(optionStmtSyntax.ValueKeyword)
                        End If

                    Case SyntaxKind.InferKeyword
                        If optionInfer.HasValue Then
                            Binder.ReportDiagnostic(diagBag, optionStmtSyntax, ERRID.ERR_DuplicateOption1, "Infer")
                        Else
                            optionInfer = Binder.DecodeOnOff(optionStmtSyntax.ValueKeyword)
                        End If

                    Case SyntaxKind.ExplicitKeyword
                        If optionExplicit.HasValue Then
                            Binder.ReportDiagnostic(diagBag, optionStmtSyntax, ERRID.ERR_DuplicateOption1, "Explicit")
                        Else
                            optionExplicit = Binder.DecodeOnOff(optionStmtSyntax.ValueKeyword)
                        End If

                    Case SyntaxKind.CompareKeyword
                        If optionCompareText.HasValue Then
                            Binder.ReportDiagnostic(diagBag, optionStmtSyntax, ERRID.ERR_DuplicateOption1, "Compare")
                        Else
                            optionCompareText = Binder.DecodeTextBinary(optionStmtSyntax.ValueKeyword)
                        End If
                End Select
            Next
        End Sub

        ' Bind and return the imports.
        ' Warnings and errors are emitted into the diagnostic bag, including detecting duplicates.
        ' Note that the binder has already been set up to only bind to things in the global namespace.
        Private Shared Sub BindImports(importsListSyntax As SyntaxList(Of ImportsStatementSyntax),
                                       binder As Binder,
                                       diagBag As DiagnosticBag,
                                       <Out> ByRef importMembersOf As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                                       <Out> ByRef importMembersOfSyntax As ImmutableArray(Of SyntaxReference),
                                       <Out> ByRef importAliasesOpt As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition),
                                       <Out> ByRef xmlNamespacesOpt As IReadOnlyDictionary(Of String, XmlNamespaceAndImportsClausePosition),
                                       cancellationToken As CancellationToken,
                                       Optional filterSpan As TextSpan? = Nothing)
            Dim membersBuilder = ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition).GetInstance()
            Dim membersSyntaxBuilder = ArrayBuilder(Of SyntaxReference).GetInstance()
            Dim data = New FileImportData(membersBuilder, membersSyntaxBuilder)

            Try
                For Each statement In importsListSyntax
                    If filterSpan.HasValue AndAlso Not filterSpan.Value.IntersectsWith(statement.FullSpan) Then
                        Continue For
                    End If

                    cancellationToken.ThrowIfCancellationRequested()
                    binder.Compilation.RecordImports(statement)

                    For Each clause In statement.ImportsClauses
                        If filterSpan.HasValue AndAlso Not filterSpan.Value.IntersectsWith(statement.FullSpan) Then
                            Continue For
                        End If

                        cancellationToken.ThrowIfCancellationRequested()
                        binder.BindImportClause(clause, data, diagBag)
                    Next
                Next

                importMembersOf = membersBuilder.ToImmutable()
                importMembersOfSyntax = membersSyntaxBuilder.ToImmutable()
                importAliasesOpt = If(data.Aliases.Count = 0, Nothing, data.Aliases)
                xmlNamespacesOpt = If(data.XmlNamespaces.Count > 0, data.XmlNamespaces, Nothing)
            Finally
                membersBuilder.Free()
                membersSyntaxBuilder.Free()
            End Try
        End Sub

        ''' <summary>
        ''' Data for Binder.BindImportClause that maintains flat lists
        ''' of members and member syntax references in addition to
        ''' the dictionaries needed by BindImportClause.
        ''' </summary>
        Private NotInheritable Class FileImportData
            Inherits ImportData

            Private ReadOnly _membersBuilder As ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition)
            Private ReadOnly _membersSyntaxBuilder As ArrayBuilder(Of SyntaxReference)

            Public Sub New(membersBuilder As ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition), membersSyntaxBuilder As ArrayBuilder(Of SyntaxReference))
                MyBase.New(New HashSet(Of NamespaceOrTypeSymbol), New Dictionary(Of String, AliasAndImportsClausePosition)(IdentifierComparison.Comparer), New Dictionary(Of String, XmlNamespaceAndImportsClausePosition))
                _membersBuilder = membersBuilder
                _membersSyntaxBuilder = membersSyntaxBuilder
            End Sub

            Public Overrides Sub AddMember(
                    syntaxRef As SyntaxReference,
                    member As NamespaceOrTypeSymbol,
                    importsClausePosition As Integer,
                    dependencies As IReadOnlyCollection(Of AssemblySymbol),
                    isProjectImportDeclaration As Boolean)

                ' Do not expose any locations for project level imports.  This matches the effective logic
                ' we have for aliases, which are given NoLocation.Singleton (which never translates to a
                ' DeclaringSyntaxReference).
                Dim pair = New NamespaceOrTypeAndImportsClausePosition(
                    member, importsClausePosition, If(isProjectImportDeclaration, Nothing, syntaxRef), dependencies.ToImmutableArray())
                Members.Add(member)
                _membersBuilder.Add(pair)
                _membersSyntaxBuilder.Add(syntaxRef)
            End Sub

            Public Overrides Sub AddAlias(syntaxRef As SyntaxReference, name As String, [alias] As AliasSymbol, importsClausePosition As Integer, dependencies As IReadOnlyCollection(Of AssemblySymbol))
                Aliases.Add(name, New AliasAndImportsClausePosition([alias], importsClausePosition, syntaxRef, dependencies.ToImmutableArray()))
            End Sub
        End Class

        ''' <summary>
        ''' Perform any validation of import statements that must occur
        ''' after the import statements have been added to the SourceFile.
        ''' Specifically, constraints are checked for generic type references.
        ''' </summary>
        Private Shared Sub ValidateImports(compilation As VisualBasicCompilation,
                                           memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                                           memberImportsSyntax As ImmutableArray(Of SyntaxReference),
                                           aliasImportsOpt As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition),
                                           diagnostics As BindingDiagnosticBag)
            ' TODO: Dev10 reports error on specific type parts rather than the import
            ' (reporting error on Object rather than C in C = A(Of Object) for instance).
            Dim clauseDiagnostics = BindingDiagnosticBag.GetInstance()

            For i = 0 To memberImports.Length - 1
                ValidateImportsClause(compilation, clauseDiagnostics, memberImports(i).NamespaceOrType,
                                      memberImports(i).Dependencies, memberImportsSyntax(i).GetLocation(),
                                      memberImports(i).ImportsClausePosition, diagnostics)
            Next

            If aliasImportsOpt IsNot Nothing Then
                For Each aliasImport In aliasImportsOpt.Values
                    ValidateImportsClause(compilation, clauseDiagnostics, aliasImport.Alias.Target,
                                          aliasImport.Dependencies, aliasImport.Alias.GetFirstLocation(),
                                          aliasImport.ImportsClausePosition, diagnostics)
                Next
            End If

            clauseDiagnostics.Free()
        End Sub

        Private Shared Sub ValidateImportsClause(
            compilation As VisualBasicCompilation,
            clauseDiagnostics As BindingDiagnosticBag,
            namespaceOrType As NamespaceOrTypeSymbol,
            dependencies As ImmutableArray(Of AssemblySymbol),
            location As Location,
            importsClausePosition As Integer,
            diagnostics As BindingDiagnosticBag
        )
            Dim type = TryCast(namespaceOrType, TypeSymbol)
            If type IsNot Nothing Then
                clauseDiagnostics.Clear()
                type.CheckAllConstraints(
                    compilation.LanguageVersion,
                    location, clauseDiagnostics, template:=New CompoundUseSiteInfo(Of AssemblySymbol)(diagnostics, compilation.Assembly))
                diagnostics.AddRange(clauseDiagnostics.DiagnosticBag)

                If VisualBasicCompilation.ReportUnusedImportsInTree(location.PossiblyEmbeddedOrMySourceTree) Then
                    If clauseDiagnostics.DependenciesBag.Count <> 0 Then
                        If Not dependencies.IsEmpty Then
                            clauseDiagnostics.AddDependencies(dependencies)
                        End If

                        dependencies = clauseDiagnostics.DependenciesBag.ToImmutableArray()
                    End If

                    compilation.RecordImportsClauseDependencies(location.PossiblyEmbeddedOrMySourceTree, importsClausePosition, dependencies)
                Else
                    diagnostics.AddDependencies(dependencies)
                    diagnostics.AddDependencies(clauseDiagnostics.DependenciesBag)
                End If
            Else
                Debug.Assert(dependencies.IsEmpty)
                If Not VisualBasicCompilation.ReportUnusedImportsInTree(location.PossiblyEmbeddedOrMySourceTree) Then
                    diagnostics.AddAssembliesUsedByNamespaceReference(DirectCast(namespaceOrType, NamespaceSymbol))
                End If
            End If
        End Sub

        ''' <summary>
        ''' Return the member imports for this file.
        ''' Doesn't contain error types.
        ''' </summary>
        Public ReadOnly Property MemberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition)
            Get
                Return BoundInformation.MemberImports
            End Get
        End Property

        ''' <summary>
        ''' Return the alias imports for this file. May return Nothing if there are no alias imports.
        ''' May contain aliases with error type targets.
        ''' </summary>
        Public ReadOnly Property AliasImportsOpt As IReadOnlyDictionary(Of String, AliasAndImportsClausePosition)
            Get
                Return BoundInformation.AliasImportsOpt
            End Get
        End Property

        ''' <summary>
        ''' Return the xmlns imports for this file. May return Nothing if there are no xmlns imports.
        ''' </summary>
        Public ReadOnly Property XmlNamespacesOpt As IReadOnlyDictionary(Of String, XmlNamespaceAndImportsClausePosition)
            Get
                Return BoundInformation.XmlNamespacesOpt
            End Get
        End Property

        ''' <summary>
        ''' Returns the value of the Option Strict declaration if there was one, otherwise Null.
        ''' </summary>
        Public ReadOnly Property OptionStrict As Boolean?
            Get
                Return BoundInformation.OptionStrict
            End Get
        End Property

        ''' <summary>
        ''' Returns the value of the Option Infer declaration if there was one, otherwise Null.
        ''' </summary>
        Public ReadOnly Property OptionInfer As Boolean?
            Get
                Return BoundInformation.OptionInfer
            End Get
        End Property

        ''' <summary>
        ''' Returns the value of the Option Explicit declaration if there was one, otherwise Null.
        ''' </summary>
        Public ReadOnly Property OptionExplicit As Boolean?
            Get
                Return BoundInformation.OptionExplicit
            End Get
        End Property

        ''' <summary>
        ''' Returns the value of the Option Compare Text/Binary declaration if there was one, otherwise Null. True means
        ''' Text, False means Binary.
        ''' </summary>
        Public ReadOnly Property OptionCompareText As Boolean?
            Get
                Return BoundInformation.OptionCompareText
            End Get
        End Property

        ''' <summary>
        ''' Force all declaration errors to be generated.
        ''' </summary>
        Friend Sub GenerateAllDeclarationErrors()
            ' Getting the bound information causes the declaration errors to be generated
            Dim unused1 = Me.BoundInformation
            EnsureImportsValidated()
        End Sub

        ''' <summary>
        ''' Get all declaration errors in the given filterSpan.
        ''' </summary>
        Friend Function GetDeclarationErrorsInSpan(filterSpan As TextSpan, cancellationToken As CancellationToken) As IEnumerable(Of Diagnostic)
            Dim diagBag = DiagnosticBag.GetInstance()
            BindFileInformation(diagBag, cancellationToken, filterSpan)
            Return diagBag.ToReadOnlyAndFree()
        End Function

        Public ReadOnly Property Parent As Cci.IImportScope Implements Cci.IImportScope.Parent
            Get
                Return Nothing
            End Get
        End Property

        Public Function Translate(moduleBuilder As Emit.PEModuleBuilder, diagnostics As DiagnosticBag) As Cci.IImportScope
            If Not moduleBuilder.TryGetTranslatedImports(Me, Nothing) Then
                moduleBuilder.GetOrAddTranslatedImports(Me, TranslateImports(moduleBuilder, diagnostics))
            End If

            Return Me
        End Function

        Public Function GetUsedNamespaces(context As EmitContext) As ImmutableArray(Of Cci.UsedNamespaceOrType) Implements Cci.IImportScope.GetUsedNamespaces
            Dim [imports] As ImmutableArray(Of Cci.UsedNamespaceOrType) = Nothing
            Dim result = DirectCast(context.Module, Emit.PEModuleBuilder).TryGetTranslatedImports(Me, [imports])
            ' The imports should have been translated during code gen.
            Debug.Assert(result)
            Debug.Assert(Not [imports].IsDefault)
            Return [imports]
        End Function

        Private Function TranslateImports(moduleBuilder As Emit.PEModuleBuilder, diagnostics As DiagnosticBag) As ImmutableArray(Of Cci.UsedNamespaceOrType)
            Return NamespaceScopeBuilder.BuildNamespaceScope(moduleBuilder,
                                                             XmlNamespacesOpt,
                                                             If(AliasImportsOpt IsNot Nothing, AliasImportsOpt.Values, Nothing),
                                                             MemberImports,
                                                             diagnostics)
        End Function
    End Class
End Namespace

