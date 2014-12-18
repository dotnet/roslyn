' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class SourceFile
        Private ReadOnly m_sourceModule As SourceModuleSymbol
        Private ReadOnly m_syntaxTree As SyntaxTree

        ' holds diagnostics related to source code in this particular source file, for 
        ' each stage.
        Private ReadOnly m_diagnosticBagDeclare As New DiagnosticBag()
        Private ReadOnly m_diagnosticBagCompile As New DiagnosticBag()
        Private ReadOnly m_diagnosticBagEmit As New DiagnosticBag()

        ' Lazily filled in.
        Private m_lazyBoundInformation As BoundFileInformation

        ' Set to nonzero when import validated errors have been reported.
        Private m_importsValidated As Integer

        ' lazily populate with quick attribute checker that is initialized with the imports.
        Private m_lazyQuickAttributeChecker As QuickAttributeChecker

        ''' <summary>
        ''' The bound information from a file.
        ''' </summary>
        Private NotInheritable Class BoundFileInformation
            Public ReadOnly MemberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) ' can be Nothing if no member imports.
            Public ReadOnly MemberImportsSyntax As ImmutableArray(Of SyntaxReference) ' can be Nothing if no member imports.
            Public ReadOnly AliasImports As Dictionary(Of String, AliasAndImportsClausePosition) ' can be Nothing if no alias imports.
            Public ReadOnly XmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) ' can be Nothing if no xmlns imports.

            ' HasValue is false if the given option wasn't present in the file.
            Public ReadOnly OptionStrict As Boolean?
            Public ReadOnly OptionInfer As Boolean?
            Public ReadOnly OptionExplicit As Boolean?
            Public ReadOnly OptionCompareText As Boolean?

            Public Sub New(importMembersOf As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                           importMembersOfSyntax As ImmutableArray(Of SyntaxReference),
                           importAliases As Dictionary(Of String, AliasAndImportsClausePosition),
                           xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition),
                           optionStrict As Boolean?,
                           optionInfer As Boolean?,
                           optionExplicit As Boolean?,
                           optionCompareText As Boolean?)

                Me.MemberImports = importMembersOf
                Me.MemberImportsSyntax = importMembersOfSyntax
                Me.AliasImports = importAliases
                Me.XmlNamespaces = xmlNamespaces

                Me.OptionStrict = optionStrict
                Me.OptionInfer = optionInfer
                Me.OptionExplicit = optionExplicit
                Me.OptionCompareText = optionCompareText
            End Sub
        End Class

        Public Sub New(sourceModule As SourceModuleSymbol, tree As SyntaxTree)
            m_sourceModule = sourceModule
            m_syntaxTree = tree
        End Sub

        ' Get the declaration errors.
        Public ReadOnly Property DeclarationErrors As DiagnosticBag
            Get
                Return m_diagnosticBagDeclare
            End Get
        End Property

        ' Add a diagnostic to this source file.
        Public Sub AddDiagnostic(d As Diagnostic, stage As CompilationStage)
            Select Case stage
                Case CompilationStage.Declare
                    m_diagnosticBagDeclare.Add(d)

                Case CompilationStage.Compile
                    m_diagnosticBagCompile.Add(d)

                Case CompilationStage.Emit
                    m_diagnosticBagEmit.Add(d)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(stage)
            End Select
        End Sub

        ' Get a quick attribute checker that can be used for quick attributes checks, initialized with project-level
        ' aliases.
        Public ReadOnly Property QuickAttributeChecker As QuickAttributeChecker
            Get
                If m_lazyQuickAttributeChecker Is Nothing Then
                    Interlocked.CompareExchange(m_lazyQuickAttributeChecker, CreateQuickAttributeChecker(), Nothing)
                End If

                Return m_lazyQuickAttributeChecker
            End Get
        End Property

        Private Function CreateQuickAttributeChecker() As QuickAttributeChecker
            ' First, initialize from the source module to get aliases from the options.
            Dim checker As New QuickAttributeChecker(m_sourceModule.QuickAttributeChecker)

            ' Now process alias imports
            Dim compilationUnitSyntax = m_syntaxTree.GetCompilationUnitRoot()
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
            If m_lazyBoundInformation Is Nothing Then
                Dim diagBag As New DiagnosticBag()
                Dim lazyBoundInformation = BindFileInformation(diagBag, cancellationToken)
                m_sourceModule.AtomicStoreReferenceAndDiagnostics(m_lazyBoundInformation, lazyBoundInformation, diagBag, CompilationStage.Declare)
            End If

            Return m_lazyBoundInformation
        End Function

        Private Sub EnsureImportsValidated()
            If m_importsValidated = 0 Then
                Dim boundFileInformation = BoundInformation
                Dim diagBag As New DiagnosticBag()
                ValidateImports(boundFileInformation.MemberImports, boundFileInformation.MemberImportsSyntax, boundFileInformation.AliasImports, diagBag)
                m_sourceModule.AtomicStoreIntegerAndDiagnostics(m_importsValidated, 1, 0, diagBag, CompilationStage.Declare)
            End If
            Debug.Assert(m_importsValidated = 1)
        End Sub

        Private Function BindFileInformation(diagBag As DiagnosticBag, cancellationToken As CancellationToken, Optional filterSpan As TextSpan? = Nothing) As BoundFileInformation
            ' The binder must be set up to only bind things in the global namespace, in order to bind imports 
            ' correctly. Note that a different binder would be needed for binding the file-level attributes.
            Dim binder = BinderBuilder.CreateBinderForSourceFileImports(m_sourceModule, m_syntaxTree)
            Dim compilationUnitSyntax = m_syntaxTree.GetCompilationUnitRoot()

            Dim optionStrict As Boolean?
            Dim optionInfer As Boolean?
            Dim optionExplicit As Boolean?
            Dim optionCompareText As Boolean?

            BindOptions(compilationUnitSyntax.Options, binder, diagBag, optionStrict, optionInfer, optionExplicit, optionCompareText, filterSpan)

            Dim importMembersOf As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim importMembersOfSyntax As ImmutableArray(Of SyntaxReference) = Nothing
            Dim importAliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

            BindImports(compilationUnitSyntax.Imports, binder, diagBag, importMembersOf, importMembersOfSyntax, importAliases, xmlNamespaces, cancellationToken, filterSpan)

            Return New BoundFileInformation(importMembersOf, importMembersOfSyntax, importAliases, xmlNamespaces, optionStrict, optionInfer, optionExplicit, optionCompareText)
        End Function

        ' Bind the options and return the value of how options were specified.
        ' Errors are generated for duplicate options.
        Private Shared Sub BindOptions(optionsSyntax As SyntaxList(Of OptionStatementSyntax),
                                binder As Binder,
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

                Select Case optionStmtSyntax.NameKeyword.VBKind
                    Case SyntaxKind.StrictKeyword
                        If optionStrict.HasValue Then
                            Binder.ReportDiagnostic(diagBag, optionStmtSyntax, ERRID.ERR_DuplicateOption1, "Strict")
                        Else
                            optionStrict = binder.DecodeOnOff(optionStmtSyntax.ValueKeyword)
                        End If

                    Case SyntaxKind.InferKeyword
                        If optionInfer.HasValue Then
                            Binder.ReportDiagnostic(diagBag, optionStmtSyntax, ERRID.ERR_DuplicateOption1, "Infer")
                        Else
                            optionInfer = binder.DecodeOnOff(optionStmtSyntax.ValueKeyword)
                        End If

                    Case SyntaxKind.ExplicitKeyword
                        If optionExplicit.HasValue Then
                            Binder.ReportDiagnostic(diagBag, optionStmtSyntax, ERRID.ERR_DuplicateOption1, "Explicit")
                        Else
                            optionExplicit = binder.DecodeOnOff(optionStmtSyntax.ValueKeyword)
                        End If

                    Case SyntaxKind.CompareKeyword
                        If optionCompareText.HasValue Then
                            Binder.ReportDiagnostic(diagBag, optionStmtSyntax, ERRID.ERR_DuplicateOption1, "Compare")
                        Else
                            optionCompareText = binder.DecodeTextBinary(optionStmtSyntax.ValueKeyword)
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
                                       <Out()> ByRef importMembersOf As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                                       <Out()> ByRef importMembersOfSyntax As ImmutableArray(Of SyntaxReference),
                                       <Out()> ByRef importAliases As Dictionary(Of String, AliasAndImportsClausePosition),
                                       <Out()> ByRef xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition),
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
                        Binder.BindImportClause(clause, data, diagBag)
                    Next
                Next

                importMembersOf = membersBuilder.ToImmutable()
                importMembersOfSyntax = membersSyntaxBuilder.ToImmutable()
                importAliases = If(data.Aliases.Count = 0, Nothing, data.Aliases)
                xmlNamespaces = If(data.XmlNamespaces.Count > 0, data.XmlNamespaces, Nothing)
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

            Public Overrides Sub AddMember(syntaxRef As SyntaxReference, member As NamespaceOrTypeSymbol, importsClausePosition As Integer)
                Dim pair = New NamespaceOrTypeAndImportsClausePosition(member, importsClausePosition)
                Members.Add(member)
                _membersBuilder.Add(pair)
                _membersSyntaxBuilder.Add(syntaxRef)
            End Sub

            Public Overrides Sub AddAlias(syntaxRef As SyntaxReference, name As String, [alias] As AliasSymbol, importsClausePosition As Integer)
                Aliases.Add(name, New AliasAndImportsClausePosition([alias], importsClausePosition))
            End Sub
        End Class

        ''' <summary>
        ''' Perform any validation of import statements that must occur
        ''' after the import statements have been added to the SourceFile.
        ''' Specifically, constraints are checked for generic type references.
        ''' </summary>
        Private Shared Sub ValidateImports(memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                                           memberImportsSyntax As ImmutableArray(Of SyntaxReference),
                                           aliasImports As Dictionary(Of String, AliasAndImportsClausePosition),
                                           diagnostics As DiagnosticBag)
            ' TODO: Dev10 reports error on specific type parts rather than the import
            ' (reporting error on Object rather than C in C = A(Of Object) for instance).

            If Not memberImports.IsDefault Then
                For i = 0 To memberImports.Length - 1
                    Dim type = TryCast(memberImports(i).NamespaceOrType, TypeSymbol)
                    If type IsNot Nothing Then
                        Dim location = memberImportsSyntax(i).GetLocation()
                        type.CheckAllConstraints(location, diagnostics)
                    End If
                Next
            End If

            If aliasImports IsNot Nothing Then
                For Each aliasImport In aliasImports.Values
                    Dim type = TryCast(aliasImport.Alias.Target, TypeSymbol)
                    If type IsNot Nothing Then
                        type.CheckAllConstraints(aliasImport.Alias.Locations(0), diagnostics)
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Return the member imports for this file. May return Nothing if there are no member imports.
        ''' </summary>
        Public ReadOnly Property MemberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition)
            Get
                Return BoundInformation.MemberImports
            End Get
        End Property

        ''' <summary>
        ''' Return the alias imports for this file. May return Nothing if there are no alias imports.
        ''' </summary>
        Public ReadOnly Property AliasImports As Dictionary(Of String, AliasAndImportsClausePosition)
            Get
                Return BoundInformation.AliasImports
            End Get
        End Property

        ''' <summary>
        ''' Return the xmlns imports for this file. May return Nothing if there are no xmlns imports.
        ''' </summary>
        Public ReadOnly Property XmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition)
            Get
                Return BoundInformation.XmlNamespaces
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
            Dim diagBag As DiagnosticBag = DiagnosticBag.GetInstance()
            BindFileInformation(diagBag, cancellationToken, filterSpan)
            Return diagBag.ToReadOnlyAndFree()
        End Function
    End Class
End Namespace

