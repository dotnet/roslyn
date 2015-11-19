' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents the primary module of an assembly being built by compiler.
    ''' </summary>
    ''' <remarks></remarks>
    Partial Friend NotInheritable Class SourceModuleSymbol
        Inherits NonMissingModuleSymbol
        Implements IAttributeTargetSymbol

        ''' <summary>
        ''' Owning assembly.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _assemblySymbol As SourceAssemblySymbol

        ' The declaration table for all the source files.
        Private ReadOnly _declarationTable As DeclarationTable

        ' Options that control compilation
        Private ReadOnly _options As VisualBasicCompilationOptions

        ' Module attributes
        Private _lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        Private _lazyContainsExtensionMethods As Byte = ThreeState.Unknown

        Private _lazyAssembliesToEmbedTypesFrom As ImmutableArray(Of AssemblySymbol)

        Private _lazyContainsExplicitDefinitionOfNoPiaLocalTypes As ThreeState = ThreeState.Unknown

        Private _locations As ImmutableArray(Of Location)

        ' holds diagnostics not related to source code 
        ' in any particular source file, for each stage.
        Private ReadOnly _diagnosticBagDeclare As New DiagnosticBag()
        'Private m_diagnosticBagCompile As New DiagnosticBag()
        'Private m_diagnosticBagEmit As New DiagnosticBag()

        Private _hasBadAttributes As Boolean

        Friend ReadOnly Property Options As VisualBasicCompilationOptions
            Get
                Return _options
            End Get
        End Property

        ' A concurrent dictionary that hold SourceFile objects for each source file, created
        ' on demand. Each source file object holds information about the source that is specific to a particular
        ' compilation and doesn't survive edits in any way (unlike the declaration table, which is incrementally
        ' updated between compilations as edits occur.)
        Private ReadOnly _sourceFileMap As New ConcurrentDictionary(Of SyntaxTree, SourceFile)

        ' lazily populated with the global namespace symbol
        Private _lazyGlobalNamespace As SourceNamespaceSymbol

        ' lazily populated with the bound imports
        Private _lazyBoundImports As BoundImports

        ' lazily populate with quick attribute checker that is initialized with the imports.
        Private _lazyQuickAttributeChecker As QuickAttributeChecker

        ' lazily populate with diagnostics validating linked assemblies.
        Private _lazyLinkedAssemblyDiagnostics As ImmutableArray(Of Diagnostic)

        Private _lazyTypesWithDefaultInstanceAlias As Dictionary(Of NamedTypeSymbol, SynthesizedMyGroupCollectionPropertySymbol)
        Private Shared ReadOnly s_noTypesWithDefaultInstanceAlias As New Dictionary(Of NamedTypeSymbol, SynthesizedMyGroupCollectionPropertySymbol)()

        Friend Sub New(assemblySymbol As SourceAssemblySymbol,
                       declarationTable As DeclarationTable,
                       options As VisualBasicCompilationOptions,
                       nameAndExtension As String)
            Debug.Assert(assemblySymbol IsNot Nothing)

            _assemblySymbol = assemblySymbol
            _declarationTable = declarationTable
            _options = options
            _nameAndExtension = nameAndExtension
        End Sub

        Friend Overrides ReadOnly Property Ordinal As Integer
            Get
                Return 0
            End Get
        End Property

        Friend Overrides ReadOnly Property Machine As System.Reflection.PortableExecutable.Machine
            Get
                Select Case DeclaringCompilation.Options.Platform
                    Case Platform.Arm
                        Return System.Reflection.PortableExecutable.Machine.ARMThumb2
                    Case Platform.X64
                        Return System.Reflection.PortableExecutable.Machine.AMD64
                    Case Platform.Itanium
                        Return System.Reflection.PortableExecutable.Machine.IA64
                    Case Else
                        Return System.Reflection.PortableExecutable.Machine.I386
                End Select
            End Get
        End Property

        Friend Overrides ReadOnly Property Bit32Required As Boolean
            Get
                Return DeclaringCompilation.Options.Platform = Platform.X86
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _assemblySymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _assemblySymbol
            End Get
        End Property

        Public ReadOnly Property ContainingSourceAssembly As SourceAssemblySymbol
            Get
                Return _assemblySymbol
            End Get
        End Property

        ''' <summary>
        ''' This override is essential - it's a base case of the recursive definition.
        ''' </summary>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return _assemblySymbol.DeclaringCompilation
            End Get
        End Property

        Private ReadOnly _nameAndExtension As String

        Public Overrides ReadOnly Property Name As String
            Get
                Return _nameAndExtension
            End Get
        End Property

        ' Get the SourceFile object associated with a root declaration.
        Friend Function GetSourceFile(tree As SyntaxTree) As SourceFile
            Debug.Assert(tree IsNot Nothing)
            Debug.Assert(tree.IsEmbeddedOrMyTemplateTree() OrElse _assemblySymbol.DeclaringCompilation.SyntaxTrees.Contains(tree))

            Dim srcFile As SourceFile = Nothing
            If _sourceFileMap.TryGetValue(tree, srcFile) Then
                Return srcFile
            Else
                srcFile = New SourceFile(Me, tree)
                Return _sourceFileMap.GetOrAdd(tree, srcFile)
            End If
        End Function

        ' Gets the global namespace for that merges the global namespace across all source files.
        Public Overrides ReadOnly Property GlobalNamespace As NamespaceSymbol
            Get
                If _lazyGlobalNamespace Is Nothing Then
                    Dim globalNS = New SourceNamespaceSymbol(_declarationTable.MergedRoot, Nothing, Me)
                    Interlocked.CompareExchange(_lazyGlobalNamespace, globalNS, Nothing)
                End If

                Return _lazyGlobalNamespace
            End Get
        End Property

        ' Get the "root" or default namespace that all source types are declared inside. This may be the global namespace
        ' or may be another namespace. This is a non-merged, source only namespace.
        Friend ReadOnly Property RootNamespace As NamespaceSymbol
            Get
                Dim result = Me.GlobalNamespace.LookupNestedNamespace(Me.Options.GetRootNamespaceParts())
                Debug.Assert(result IsNot Nothing, "Something is deeply wrong with the declaration table or the symbol table")
                Return result
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                If _locations.IsDefault Then
                    Dim locs = _declarationTable.AllRootNamespaces().SelectAsArray(Function(n) n.Location)

                    ImmutableInterlocked.InterlockedCompareExchange(_locations, locs, Nothing)
                End If

                Return _locations
            End Get
        End Property

        Friend ReadOnly Property SyntaxTrees As IEnumerable(Of SyntaxTree)
            Get
                Return _assemblySymbol.DeclaringCompilation.AllSyntaxTrees
            End Get
        End Property

        Public ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Module
            End Get
        End Property

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        ''' <remarks>
        ''' NOTE: This method should always be kept as a NotOverridable method.
        ''' If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        ''' </remarks>
        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetAttributesBag().Attributes
        End Function

        Private Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            If _lazyCustomAttributesBag Is Nothing OrElse Not _lazyCustomAttributesBag.IsSealed Then
                Dim mergedAttributes = DirectCast(Me.ContainingAssembly, SourceAssemblySymbol).GetAttributeDeclarations()
                LoadAndValidateAttributes(OneOrMany.Create(mergedAttributes), _lazyCustomAttributesBag)
            End If
            Return _lazyCustomAttributesBag
        End Function

        Friend Function GetDecodedWellKnownAttributeData() As CommonModuleWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me._lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonModuleWellKnownAttributeData)
        End Function

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

        Friend ReadOnly Property AnyReferencedAssembliesAreLinked As Boolean
            Get
                Return GetAssembliesToEmbedTypesFrom().Length > 0
            End Get
        End Property

        Friend Function MightContainNoPiaLocalTypes() As Boolean
            Return AnyReferencedAssembliesAreLinked OrElse
                ContainsExplicitDefinitionOfNoPiaLocalTypes
        End Function

        Friend Function GetAssembliesToEmbedTypesFrom() As ImmutableArray(Of AssemblySymbol)
            If _lazyAssembliesToEmbedTypesFrom.IsDefault Then
                AssertReferencesInitialized()
                Dim assemblies = ArrayBuilder(Of AssemblySymbol).GetInstance()

                For Each assembly In GetReferencedAssemblySymbols()
                    If assembly.IsLinked Then
                        assemblies.Add(assembly)
                    End If
                Next

                ImmutableInterlocked.InterlockedInitialize(_lazyAssembliesToEmbedTypesFrom, assemblies.ToImmutableAndFree())
            End If

            Debug.Assert(Not _lazyAssembliesToEmbedTypesFrom.IsDefault)
            Return _lazyAssembliesToEmbedTypesFrom
        End Function

        Friend ReadOnly Property ContainsExplicitDefinitionOfNoPiaLocalTypes As Boolean
            Get
                If _lazyContainsExplicitDefinitionOfNoPiaLocalTypes = ThreeState.Unknown Then
                    ' TODO: This will recursively visit all top level types and bind attributes on them.
                    '       This might be very expensive to do, but explicitly declared local types are 
                    '       very uncommon. We should consider optimizing this by analyzing syntax first, 
                    '       for example, the way VB handles ExtensionAttribute, etc.
                    _lazyContainsExplicitDefinitionOfNoPiaLocalTypes = NamespaceContainsExplicitDefinitionOfNoPiaLocalTypes(GlobalNamespace).ToThreeState()
                End If

                Debug.Assert(_lazyContainsExplicitDefinitionOfNoPiaLocalTypes <> ThreeState.Unknown)
                Return _lazyContainsExplicitDefinitionOfNoPiaLocalTypes = ThreeState.True
            End Get
        End Property

        Private Shared Function NamespaceContainsExplicitDefinitionOfNoPiaLocalTypes(ns As NamespaceSymbol) As Boolean
            For Each s In ns.GetMembersUnordered()
                Select Case s.Kind
                    Case SymbolKind.Namespace
                        If NamespaceContainsExplicitDefinitionOfNoPiaLocalTypes(DirectCast(s, NamespaceSymbol)) Then
                            Return True
                        End If
                    Case SymbolKind.NamedType
                        If DirectCast(s, NamedTypeSymbol).IsExplicitDefinitionOfNoPiaLocalType Then
                            Return True
                        End If
                End Select
            Next
            Return False
        End Function

        Private Function CreateQuickAttributeChecker() As QuickAttributeChecker
            ' First, initialize for the predefined attributes.
            Dim checker As New QuickAttributeChecker()
            checker.AddName("ExtensionAttribute", QuickAttributes.Extension)
            checker.AddName("ObsoleteAttribute", QuickAttributes.Obsolete)
            checker.AddName("DeprecatedAttribute", QuickAttributes.Obsolete)
            checker.AddName("MyGroupCollectionAttribute", QuickAttributes.MyGroupCollection)

            ' Now process alias imports
            For Each globalImport In Options.GlobalImports
                If globalImport.Clause.Kind = SyntaxKind.SimpleImportsClause Then
                    Dim simpleImportsClause = DirectCast(globalImport.Clause, SimpleImportsClauseSyntax)

                    If simpleImportsClause.Alias IsNot Nothing Then
                        checker.AddAlias(simpleImportsClause)
                    End If
                End If
            Next

            checker.Seal()
            Return checker
        End Function

        ' Make sure the project level imports are bound.
        Private Sub EnsureImportsAreBound(cancellationToken As CancellationToken)
            If _lazyBoundImports Is Nothing Then
                If Interlocked.CompareExchange(_lazyBoundImports, BindImports(cancellationToken), Nothing) Is Nothing Then
                    ValidateImports(_lazyBoundImports.MemberImports, _lazyBoundImports.MemberImportsInfo, _lazyBoundImports.AliasImports, _lazyBoundImports.AliasImportsInfo, _lazyBoundImports.Diagnostics)
                End If
            End If
        End Sub

        ' Bind the project level imports.
        Private Function BindImports(cancellationToken As CancellationToken) As BoundImports
            Dim diagBag As New DiagnosticBag

            Dim membersMap = New HashSet(Of NamespaceOrTypeSymbol)
            Dim aliasesMap = New Dictionary(Of String, AliasAndImportsClausePosition)(IdentifierComparison.Comparer)
            Dim membersBuilder = ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition).GetInstance()
            Dim membersInfoBuilder = ArrayBuilder(Of GlobalImportInfo).GetInstance()
            Dim aliasesBuilder = ArrayBuilder(Of AliasAndImportsClausePosition).GetInstance()
            Dim aliasesInfoBuilder = ArrayBuilder(Of GlobalImportInfo).GetInstance()
            Dim xmlNamespaces = New Dictionary(Of String, XmlNamespaceAndImportsClausePosition)

            Try
                For Each globalImport In Options.GlobalImports
                    cancellationToken.ThrowIfCancellationRequested()

                    Dim data = New ModuleImportData(globalImport, membersMap, aliasesMap, membersBuilder, membersInfoBuilder, aliasesBuilder, aliasesInfoBuilder, xmlNamespaces)
                    Dim diagBagForThisImport = DiagnosticBag.GetInstance()
                    Dim binder As binder = BinderBuilder.CreateBinderForProjectImports(Me, VisualBasicSyntaxTree.Dummy)
                    binder.BindImportClause(globalImport.Clause, data, diagBagForThisImport)

                    ' Map diagnostics to new ones.
                    ' Note, it is safe to resolve diagnostics here because we suppress obsolete diagnostics
                    ' in ProjectImportsBinder.
                    For Each d As Diagnostic In diagBagForThisImport.AsEnumerable()
                        ' NOTE: Dev10 doesn't report 'ERR_DuplicateImport1' for project level imports. 
                        If d.Code <> ERRID.ERR_DuplicateImport1 Then
                            diagBag.Add(globalImport.MapDiagnostic(d))
                        End If
                    Next

                    diagBagForThisImport.Free()
                Next

                Return New BoundImports(
                    membersBuilder.ToImmutable(),
                    membersInfoBuilder.ToImmutable(),
                    aliasesMap,
                    aliasesBuilder.ToImmutable(),
                    aliasesInfoBuilder.ToImmutable(),
                    If(xmlNamespaces.Count > 0, xmlNamespaces, Nothing),
                    diagBag)
            Finally
                membersBuilder.Free()
                membersInfoBuilder.Free()
                aliasesBuilder.Free()
                aliasesInfoBuilder.Free()
            End Try
        End Function

        ''' <summary>
        ''' Data for Binder.BindImportClause that maintains flat lists of members, aliases,
        ''' and corresponding syntax references in addition to the dictionaries needed by
        ''' BindImportClause. The syntax references, instances of GlobalImportInfo, are used
        ''' later, when validating constraints, to generate Locations for constraint errors.
        ''' </summary>
        Private NotInheritable Class ModuleImportData
            Inherits ImportData

            Private ReadOnly _globalImport As GlobalImport
            Private ReadOnly _membersBuilder As ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition)
            Private ReadOnly _membersInfoBuilder As ArrayBuilder(Of GlobalImportInfo)
            Private ReadOnly _aliasesBuilder As ArrayBuilder(Of AliasAndImportsClausePosition)
            Private ReadOnly _aliasesInfoBuilder As ArrayBuilder(Of GlobalImportInfo)

            Public Sub New(globalImport As GlobalImport,
                           membersMap As HashSet(Of NamespaceOrTypeSymbol),
                           aliasesMap As Dictionary(Of String, AliasAndImportsClausePosition),
                           membersBuilder As ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition),
                           membersInfoBuilder As ArrayBuilder(Of GlobalImportInfo),
                           aliasesBuilder As ArrayBuilder(Of AliasAndImportsClausePosition),
                           aliasesInfoBuilder As ArrayBuilder(Of GlobalImportInfo),
                           xmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition))
                MyBase.New(membersMap, aliasesMap, xmlNamespaces)
                _globalImport = globalImport
                _membersBuilder = membersBuilder
                _membersInfoBuilder = membersInfoBuilder
                _aliasesBuilder = aliasesBuilder
                _aliasesInfoBuilder = aliasesInfoBuilder
            End Sub

            Public Overrides Sub AddMember(syntaxRef As SyntaxReference, member As NamespaceOrTypeSymbol, importsClausePosition As Integer)
                Dim pair = New NamespaceOrTypeAndImportsClausePosition(member, importsClausePosition)
                Members.Add(member)
                _membersBuilder.Add(pair)
                _membersInfoBuilder.Add(New GlobalImportInfo(_globalImport, syntaxRef))
            End Sub

            Public Overrides Sub AddAlias(syntaxRef As SyntaxReference, name As String, [alias] As AliasSymbol, importsClausePosition As Integer)
                Dim pair = New AliasAndImportsClausePosition([alias], importsClausePosition)
                Aliases.Add(name, pair)
                _aliasesBuilder.Add(pair)
                _aliasesInfoBuilder.Add(New GlobalImportInfo(_globalImport, syntaxRef))
            End Sub
        End Class

        ''' <summary>
        ''' Perform any validation of import statements that must occur
        ''' after the import statements have been added to the module.
        ''' </summary>
        Private Shared Sub ValidateImports(
                                         memberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition),
                                         memberImportsInfo As ImmutableArray(Of GlobalImportInfo),
                                         aliasImports As ImmutableArray(Of AliasAndImportsClausePosition),
                                         aliasImportsInfo As ImmutableArray(Of GlobalImportInfo),
                                         diagnostics As DiagnosticBag)
            ' TODO: Dev10 reports error on specific type parts rather than the import
            ' (reporting error on Object rather than C in C = A(Of Object) for instance).

            If Not memberImports.IsDefault Then
                For i = 0 To memberImports.Length - 1
                    Dim type = TryCast(memberImports(i).NamespaceOrType, TypeSymbol)
                    If type IsNot Nothing Then
                        ValidateImport(type, memberImportsInfo(i), diagnostics)
                    End If
                Next
            End If

            If Not aliasImports.IsDefault Then
                For i = 0 To aliasImports.Length - 1
                    Dim type = TryCast(aliasImports(i).Alias.Target, TypeSymbol)
                    If type IsNot Nothing Then
                        ValidateImport(type, aliasImportsInfo(i), diagnostics)
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Perform validation of an import statement that must occur
        ''' after the statement has been added to the module. Specifically,
        ''' constraints are checked for generic type references.
        ''' </summary>
        Private Shared Sub ValidateImport(type As TypeSymbol, info As GlobalImportInfo, diagnostics As DiagnosticBag)
            Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
            Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing
            type.CheckAllConstraints(diagnosticsBuilder, useSiteDiagnosticsBuilder)

            If useSiteDiagnosticsBuilder IsNot Nothing Then
                diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
            End If

            For Each pair In diagnosticsBuilder
                diagnostics.Add(info.Import.MapDiagnostic(New VBDiagnostic(pair.DiagnosticInfo, info.SyntaxReference.GetLocation())))
            Next
            diagnosticsBuilder.Free()
        End Sub

        ' Get the project-level member imports, or Nothing if none.
        Friend ReadOnly Property MemberImports As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition)
            Get
                EnsureImportsAreBound(CancellationToken.None)
                Return _lazyBoundImports.MemberImports
            End Get
        End Property

        Friend ReadOnly Property AliasImports As ImmutableArray(Of AliasAndImportsClausePosition)
            Get
                EnsureImportsAreBound(CancellationToken.None)
                Return _lazyBoundImports.AliasImports
            End Get
        End Property

        ' Get the project level alias imports, or Nothing if none.
        Friend ReadOnly Property AliasImportsMap As Dictionary(Of String, AliasAndImportsClausePosition)
            Get
                EnsureImportsAreBound(CancellationToken.None)
                Return _lazyBoundImports.AliasImportsMap
            End Get
        End Property

        ' Get the project level xmlns imports, or Nothing if none.
        Friend ReadOnly Property XmlNamespaces As Dictionary(Of String, XmlNamespaceAndImportsClausePosition)
            Get
                EnsureImportsAreBound(CancellationToken.None)
                Return _lazyBoundImports.XmlNamespaces
            End Get
        End Property

        ''' <summary>
        ''' Get all the declaration errors in a single tree.
        ''' </summary>
        Friend Function GetDeclarationErrorsInTree(tree As SyntaxTree,
                                                   filterSpanWithinTree As TextSpan?,
                                                   locationFilter As Func(Of IEnumerable(Of Diagnostic), SyntaxTree, TextSpan?, IEnumerable(Of Diagnostic)),
                                                   cancellationToken As CancellationToken) As ImmutableArray(Of Diagnostic)
            Dim builder = ArrayBuilder(Of Diagnostic).GetInstance()

            ' Force source file to generate errors for Imports and the like.
            Dim sourceFile As SourceFile = GetSourceFile(tree)
            If filterSpanWithinTree.HasValue Then
                Dim diagnostics = sourceFile.GetDeclarationErrorsInSpan(filterSpanWithinTree.Value, cancellationToken)
                diagnostics = locationFilter(diagnostics, tree, filterSpanWithinTree)
                builder.AddRange(diagnostics)
            Else
                sourceFile.GenerateAllDeclarationErrors()
            End If

            ' Force bind module and assembly attributes
            Me.GetAttributes()
            Me.ContainingAssembly.GetAttributes()

            ' Force all types to generate errors.
            Dim tasks As ConcurrentStack(Of Task) = If(ContainingSourceAssembly.DeclaringCompilation.Options.ConcurrentBuild, New ConcurrentStack(Of Task)(), Nothing)

            ' Force all types that were declared in this tree to generate errors. We may also generate declaration
            ' errors for other parts of partials; that's OK and those errors will be retained, but won't be in the
            ' diagnostic bag for this particular file.
            VisitAllSourceTypesAndNamespaces(Sub(typeOrNamespace As NamespaceOrTypeSymbol)
                                                 If Not typeOrNamespace.IsDefinedInSourceTree(tree, filterSpanWithinTree) Then
                                                     Return
                                                 End If

                                                 If typeOrNamespace.IsNamespace Then
                                                     DirectCast(typeOrNamespace, SourceNamespaceSymbol).GenerateDeclarationErrorsInTree(tree, filterSpanWithinTree, cancellationToken)
                                                 Else
                                                     ' synthetic event delegates are not source types so use NamedTypeSymbol. 
                                                     Dim sourceType = DirectCast(typeOrNamespace, NamedTypeSymbol)
                                                     sourceType.GenerateDeclarationErrors(cancellationToken)
                                                 End If
                                             End Sub,
                tasks, cancellationToken)

            If tasks IsNot Nothing Then
                Dim curTask As Task = Nothing
                While tasks.TryPop(curTask)
                    curTask.GetAwaiter().GetResult()
                End While
            End If

            ' Get all the errors that were generated. 
            Dim declarationDiagnostics = sourceFile.DeclarationErrors.AsEnumerable()

            ' Filter diagnostics outside the tree/span of interest.
            If locationFilter IsNot Nothing Then
                Debug.Assert(tree IsNot Nothing)
                declarationDiagnostics = locationFilter(declarationDiagnostics, tree, filterSpanWithinTree)
            End If

            For Each d As Diagnostic In declarationDiagnostics
                builder.Add(d)
            Next

            Return builder.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Get all the declaration errors.
        ''' </summary>
        Friend Function GetAllDeclarationErrors(cancellationToken As CancellationToken, ByRef hasExtensionMethods As Boolean) As ImmutableArray(Of Diagnostic)
            ' Bind project level imports
            EnsureImportsAreBound(cancellationToken)

            ' Force all source files to generate errors for imports and the like.
            If ContainingSourceAssembly.DeclaringCompilation.Options.ConcurrentBuild Then
                Dim trees = ArrayBuilder(Of SyntaxTree).GetInstance()
                trees.AddRange(SyntaxTrees)

                Dim options = New ParallelOptions() With {.CancellationToken = cancellationToken}
                Parallel.For(0, trees.Count, options,
                    UICultureUtilities.WithCurrentUICulture(
                        Sub(i As Integer)
                            cancellationToken.ThrowIfCancellationRequested()
                            GetSourceFile(trees(i)).GenerateAllDeclarationErrors()
                        End Sub))
                trees.Free()
            Else
                For Each tree In SyntaxTrees
                    cancellationToken.ThrowIfCancellationRequested()
                    GetSourceFile(tree).GenerateAllDeclarationErrors()
                Next
            End If

            ' Force bind module and assembly attributes
            Dim unused = Me.GetAttributes()
            unused = Me.ContainingAssembly.GetAttributes()

            EnsureLinkedAssembliesAreValidated(cancellationToken)

            ' Force all types to generate errors.
            Dim tasks As ConcurrentStack(Of Task) = If(ContainingSourceAssembly.DeclaringCompilation.Options.ConcurrentBuild,
                                                       New ConcurrentStack(Of Task)(), Nothing)

            VisitAllSourceTypesAndNamespaces(Sub(typeOrNamespace As NamespaceOrTypeSymbol)
                                                 If typeOrNamespace.IsNamespace Then
                                                     DirectCast(typeOrNamespace, SourceNamespaceSymbol).GenerateDeclarationErrors(cancellationToken)
                                                 Else
                                                     ' synthetic event delegates are not source types so use NamedTypeSymbol. 
                                                     Dim sourceType = DirectCast(typeOrNamespace, NamedTypeSymbol)
                                                     sourceType.GenerateDeclarationErrors(cancellationToken)
                                                 End If
                                             End Sub,
                tasks, cancellationToken)

            If tasks IsNot Nothing Then
                Dim curTask As Task = Nothing
                While tasks.TryPop(curTask)
                    curTask.GetAwaiter().GetResult()
                End While
            End If

            ' At this point we should have recorded presence of extension methods.
            If _lazyContainsExtensionMethods = ThreeState.Unknown Then
                _lazyContainsExtensionMethods = ThreeState.False
            End If

            hasExtensionMethods = _lazyContainsExtensionMethods = ThreeState.True

            ' Accumulate all the errors that were generated.
            Dim builder = DiagnosticBag.GetInstance()
            builder.AddRange(Me._diagnosticBagDeclare)
            builder.AddRange(Me._lazyBoundImports.Diagnostics)
            builder.AddRange(Me._lazyLinkedAssemblyDiagnostics)

            For Each tree In SyntaxTrees
                builder.AddRange(GetSourceFile(tree).DeclarationErrors)
            Next

            Return builder.ToReadOnlyAndFree(Of Diagnostic)()
        End Function

        ' Visit all of the source types within this source module.
        Private Sub VisitAllSourceTypesAndNamespaces(visitor As Action(Of NamespaceOrTypeSymbol), tasks As ConcurrentStack(Of Task), cancellationToken As CancellationToken)
            VisitTypesAndNamespacesWithin(Me.GlobalNamespace, visitor, tasks, cancellationToken)
        End Sub

        ' Visit all source types and namespaces within this source namespace or type, inclusive of this source namespace or type
        Private Sub VisitTypesAndNamespacesWithin(ns As NamespaceOrTypeSymbol, visitor As Action(Of NamespaceOrTypeSymbol), tasks As ConcurrentStack(Of Task), cancellationToken As CancellationToken)
            Dim stack = ArrayBuilder(Of NamespaceOrTypeSymbol).GetInstance
            Try
                stack.Push(ns)
                While stack.Count > 0
                    cancellationToken.ThrowIfCancellationRequested()
                    Dim symbol = stack.Pop()

                    If tasks IsNot Nothing Then
                        Dim worker As Task = Task.Run(
                            UICultureUtilities.WithCurrentUICulture(
                                Sub()
                                    Try
                                        visitor(symbol)
                                    Catch e As Exception When FatalError.ReportUnlessCanceled(e)
                                        Throw ExceptionUtilities.Unreachable
                                    End Try
                                End Sub),
                            cancellationToken)
                        tasks.Push(worker)
                    Else
                        visitor(symbol)
                    End If

                    For Each child As Symbol In If(symbol.IsNamespace, symbol.GetMembers(), symbol.GetTypeMembers().Cast(Of Symbol))
                        stack.Push(DirectCast(child, NamespaceOrTypeSymbol))
                    Next
                End While
            Finally
                stack.Free()
            End Try
        End Sub

        Private Sub EnsureLinkedAssembliesAreValidated(cancellationToken As CancellationToken)
            If _lazyLinkedAssemblyDiagnostics.IsDefault Then
                Dim diagnostics = DiagnosticBag.GetInstance()
                ValidateLinkedAssemblies(diagnostics, cancellationToken)
                ImmutableInterlocked.InterlockedInitialize(_lazyLinkedAssemblyDiagnostics, diagnostics.ToReadOnlyAndFree)
            End If
        End Sub

        Private Sub ValidateLinkedAssemblies(diagnostics As DiagnosticBag, cancellationToken As CancellationToken)
            For Each assembly In GetReferencedAssemblySymbols()
                cancellationToken.ThrowIfCancellationRequested()

                If assembly.IsMissing OrElse Not assembly.IsLinked Then
                    Continue For
                End If

                Dim hasGuidAttribute = False
                Dim hasImportedFromTypeLibOrPrimaryInteropAssemblyAttribute = False

                For Each attrData In assembly.GetAttributes()
                    If attrData.IsTargetAttribute(assembly, AttributeDescription.GuidAttribute) Then
                        If attrData.CommonConstructorArguments.Length = 1 Then
                            Dim value = attrData.CommonConstructorArguments(0).Value
                            If value Is Nothing OrElse TypeOf value Is String Then
                                hasGuidAttribute = True
                            End If
                        End If

                    ElseIf attrData.IsTargetAttribute(assembly, AttributeDescription.ImportedFromTypeLibAttribute) Then
                        If attrData.CommonConstructorArguments.Length = 1 Then
                            hasImportedFromTypeLibOrPrimaryInteropAssemblyAttribute = True
                        End If

                    ElseIf attrData.IsTargetAttribute(assembly, AttributeDescription.PrimaryInteropAssemblyAttribute) Then
                        If attrData.CommonConstructorArguments.Length = 2 Then
                            hasImportedFromTypeLibOrPrimaryInteropAssemblyAttribute = True
                        End If

                    End If

                    If hasGuidAttribute AndAlso hasImportedFromTypeLibOrPrimaryInteropAssemblyAttribute Then
                        Exit For
                    End If
                Next

                If Not hasGuidAttribute Then
                    diagnostics.Add(ERRID.ERR_PIAHasNoAssemblyGuid1,
                                    NoLocation.Singleton,
                                    assembly,
                                    AttributeDescription.GuidAttribute.FullName)
                End If

                If Not hasImportedFromTypeLibOrPrimaryInteropAssemblyAttribute Then
                    diagnostics.Add(ERRID.ERR_PIAHasNoTypeLibAttribute1,
                                    NoLocation.Singleton,
                                    assembly,
                                    AttributeDescription.ImportedFromTypeLibAttribute.FullName,
                                    AttributeDescription.PrimaryInteropAssemblyAttribute.FullName)
                End If
            Next
        End Sub

        ' This lock is used in the implementation of AtomicStoreReferenceAndDiagnostics
        Private ReadOnly _diagnosticLock As New Object()

        ' Checks if the given diagnostic bag has all lazy obsolete diagnostics.
        Private Shared Function HasAllLazyDiagnostics(diagBag As DiagnosticBag) As Boolean
            Debug.Assert(diagBag IsNot Nothing)
            Debug.Assert(Not diagBag.IsEmptyWithoutResolution())

            For Each diag In diagBag.AsEnumerable()
                Dim cdiag = TryCast(diag, DiagnosticWithInfo)
                If cdiag Is Nothing OrElse Not cdiag.HasLazyInfo Then
                    Return False
                End If
            Next

            Return True
        End Function

        ''' <summary>
        ''' Atomically store value into variable, and store the diagnostics away for later retrieval.
        ''' When this routine returns, variable is non-null. If this routine stored value into variable,
        ''' then the diagnostic bag is saved away before the variable is stored and it returns True.
        ''' Otherwise it returns False.
        ''' </summary>
        Friend Function AtomicStoreReferenceAndDiagnostics(Of T As Class)(ByRef variable As T,
                                                                     value As T,
                                                                     diagBag As DiagnosticBag,
                                                                     stage As CompilationStage,
                                                                     Optional comparand As T = Nothing) As Boolean
            Debug.Assert(value IsNot comparand)

            If diagBag Is Nothing OrElse diagBag.IsEmptyWithoutResolution Then
                Return Interlocked.CompareExchange(variable, value, comparand) Is comparand AndAlso comparand Is Nothing
            Else
                Dim stored = False
                SyncLock _diagnosticLock
                    If variable Is comparand Then
                        StoreDiagnostics(diagBag, stage)
                        stored = Interlocked.CompareExchange(variable, value, comparand) Is comparand
                        If Not stored AndAlso Not HasAllLazyDiagnostics(diagBag) Then

                            ' If this gets hit, then someone wrote to variable without going through this
                            ' routine, or else someone wrote to variable with this routine but an empty
                            ' diagnostic bag (they went through the above If part). Either is a bug.
                            Throw ExceptionUtilities.Unreachable
                        End If
                    End If
                End SyncLock
                Return stored AndAlso comparand Is Nothing
            End If
        End Function

        ' Atomically store value into variable, and store the diagnostics away for later retrieval.
        ' When this routine returns, variable is not equal to comparand. If this routine stored value into variable,
        ' then the diagnostic bag is saved away before the variable is stored.
        Friend Sub AtomicStoreIntegerAndDiagnostics(ByRef variable As Integer,
                                                    value As Integer,
                                                    comparand As Integer,
                                                    diagBag As DiagnosticBag,
                                                    stage As CompilationStage)
            If diagBag Is Nothing OrElse diagBag.IsEmptyWithoutResolution Then
                Interlocked.CompareExchange(variable, value, comparand)
            Else
                SyncLock _diagnosticLock
                    If variable = comparand Then
                        StoreDiagnostics(diagBag, stage)
                        If Interlocked.CompareExchange(variable, value, comparand) <> comparand AndAlso
                            Not HasAllLazyDiagnostics(diagBag) Then

                            ' If this gets hit, then someone wrote to variable without going through this
                            ' routine, or else someone wrote to variable with this routine but an empty
                            ' diagnostic bag (they went through the above If part). Either is a bug.
                            Throw ExceptionUtilities.Unreachable
                        End If
                    End If
                End SyncLock
            End If
        End Sub

        ' Atomically set flag value into variable, and store the diagnostics away for later retrieval.
        ' When this routine returns, variable is not equal to comparand. If this routine stored value into variable,
        ' then the diagnostic bag is saved away before the variable is stored.
        Friend Function AtomicSetFlagAndStoreDiagnostics(ByRef variable As Integer,
                                               mask As Integer,
                                               comparand As Integer,
                                               diagBag As DiagnosticBag,
                                               stage As CompilationStage) As Boolean
            If diagBag Is Nothing OrElse diagBag.IsEmptyWithoutResolution Then
                Return ThreadSafeFlagOperations.Set(variable, mask)
            Else
                SyncLock _diagnosticLock
                    Dim change = (variable And mask) = comparand
                    If change Then
                        StoreDiagnostics(diagBag, stage)

                        If Not ThreadSafeFlagOperations.Set(variable, mask) AndAlso
                             Not HasAllLazyDiagnostics(diagBag) Then

                            ' If this gets hit, then someone wrote to variable without going through this
                            ' routine, or else someone wrote to variable with this routine but an empty
                            ' diagnostic bag (they went through the above If part). Either is a bug.
                            Throw ExceptionUtilities.Unreachable
                        End If
                    End If
                    Return change
                End SyncLock
            End If
        End Function

        ' Atomically set flag value into variable, and raise the symbol declared event.
        ' When this routine returns, variable is not equal to comparand. If this routine stored value into variable,
        ' then the symbol declared event was raised for the symbol.
        Friend Function AtomicSetFlagAndRaiseSymbolDeclaredEvent(ByRef variable As Integer,
                                               mask As Integer,
                                               comparand As Integer,
                                               symbol As Symbol) As Boolean
            Debug.Assert(Me.DeclaringCompilation.EventQueue IsNot Nothing)

            SyncLock _diagnosticLock
                Dim change = (variable And mask) = comparand
                If change Then
                    Me.DeclaringCompilation.SymbolDeclaredEvent(symbol)

                    If Not ThreadSafeFlagOperations.Set(variable, mask) Then
                        ' If this gets hit, then someone wrote to variable without going through this
                        ' routine, which is a bug.
                        Throw ExceptionUtilities.Unreachable
                    End If
                End If
                Return change
            End SyncLock
        End Function

        Friend Function AtomicStoreArrayAndDiagnostics(Of T)(ByRef variable As ImmutableArray(Of T),
                                                             value As ImmutableArray(Of T),
                                                             diagBag As DiagnosticBag,
                                                             stage As CompilationStage) As Boolean
            Debug.Assert(Not value.IsDefault)

            If diagBag Is Nothing OrElse diagBag.IsEmptyWithoutResolution Then
                Return ImmutableInterlocked.InterlockedInitialize(variable, value)
            Else
                SyncLock _diagnosticLock
                    If variable.IsDefault Then
                        StoreDiagnostics(diagBag, stage)
                        Dim stored = ImmutableInterlocked.InterlockedInitialize(variable, value)
                        If Not stored AndAlso Not HasAllLazyDiagnostics(diagBag) Then

                            ' If this gets hit, then someone wrote to variable without going through this
                            ' routine, or else someone wrote to variable with this routine but an empty
                            ' diagnostic bag (they went through the above If part). Either is a bug.
                            Throw ExceptionUtilities.Unreachable
                        End If
                        Return stored
                    Else
                        Return False
                    End If
                End SyncLock
            End If
        End Function

        Friend Sub AtomicStoreAttributesAndDiagnostics(attributesBag As CustomAttributesBag(Of VisualBasicAttributeData),
                                                       attributesToStore As ImmutableArray(Of VisualBasicAttributeData),
                                                       diagBag As DiagnosticBag)
            Debug.Assert(attributesBag IsNot Nothing)
            Debug.Assert(Not attributesToStore.IsDefault)

            RecordPresenceOfBadAttributes(attributesToStore)

            If diagBag Is Nothing OrElse diagBag.IsEmptyWithoutResolution Then
                attributesBag.SetAttributes(attributesToStore)
            Else
                SyncLock _diagnosticLock
                    If Not attributesBag.IsSealed Then
                        StoreDiagnostics(diagBag, CompilationStage.Declare)

                        If Not attributesBag.SetAttributes(attributesToStore) AndAlso
                            Not HasAllLazyDiagnostics(diagBag) Then

                            ' If this gets hit, then someone set attributes in the bag without going through this
                            ' routine, or else someone set attributes in the bag with this routine but an empty
                            ' diagnostic bag (they went through the above If part). Either is a bug.
                            Throw ExceptionUtilities.Unreachable
                        End If

                        Debug.Assert(attributesBag.IsSealed)
                    End If
                End SyncLock
            End If
        End Sub

        Private Sub RecordPresenceOfBadAttributes(attributes As ImmutableArray(Of VisualBasicAttributeData))
            If Not _hasBadAttributes Then
                For Each attribute In attributes
                    If attribute.HasErrors Then
                        _hasBadAttributes = True
                        Exit For
                    End If
                Next
            End If
        End Sub

        Friend ReadOnly Property HasBadAttributes As Boolean
            Get
                Return _hasBadAttributes
            End Get
        End Property

        ' same as AtomicStoreReferenceAndDiagnostics, but without storing any references
        Friend Sub AddDiagnostics(diagBag As DiagnosticBag, stage As CompilationStage)
            If Not diagBag.IsEmptyWithoutResolution Then
                SyncLock _diagnosticLock
                    StoreDiagnostics(diagBag, stage)
                End SyncLock
            End If
        End Sub

        ' Given a diagnostic bag, store the diagnostics into the correct bags.
        ' NOTE: This is called from AtomicStoreReferenceAndDiagnostics, which takes a lock.
        ' NOTE: It is important that it doesn't do any operation that wants to acquire another lock.
        ' NOTE: Copy without resolving diagnostics.
        Private Sub StoreDiagnostics(diagBag As DiagnosticBag, stage As CompilationStage)
            If Not diagBag.IsEmptyWithoutResolution Then
                For Each d As Diagnostic In diagBag.AsEnumerableWithoutResolution()
                    Dim loc = d.Location
                    If loc.IsInSource Then
                        Dim tree = DirectCast(loc.SourceTree, VisualBasicSyntaxTree)
                        Dim sourceFile = GetSourceFile(tree)
                        sourceFile.AddDiagnostic(d, stage)
                    Else
                        Me.AddDiagnostic(d, stage)
                    End If
                Next
            End If
        End Sub

        ' Add a diagnostic to this source file.
        Private Sub AddDiagnostic(d As Diagnostic, stage As CompilationStage)
            Select Case stage
                Case CompilationStage.Declare
                    _diagnosticBagDeclare.Add(d)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(stage)
            End Select
        End Sub

        Friend Overrides ReadOnly Property TypeNames As ICollection(Of String)
            Get
                Return _declarationTable.TypeNames
            End Get
        End Property

        Friend Overrides ReadOnly Property NamespaceNames As ICollection(Of String)
            Get
                Return _declarationTable.NamespaceNames
            End Get
        End Property

        Friend Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                ' Only primary module of an assembly marked with an Extension attribute
                ' can contain extension methods recognized by the language (Dev10 behavior).
                If _lazyContainsExtensionMethods = ThreeState.Unknown Then
                    If Not (_assemblySymbol.Modules(0) Is Me) Then
                        _lazyContainsExtensionMethods = ThreeState.False
                    End If
                End If

                Return _lazyContainsExtensionMethods <> ThreeState.False
            End Get
        End Property

        Friend Sub RecordPresenceOfExtensionMethods()
            Debug.Assert(_lazyContainsExtensionMethods <> ThreeState.False)
            _lazyContainsExtensionMethods = ThreeState.True
        End Sub

        Friend Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)

            Dim attrData = arguments.Attribute
            Debug.Assert(Not attrData.HasErrors)
            Debug.Assert(arguments.SymbolPart = AttributeLocation.None)

            If attrData.IsTargetAttribute(Me, AttributeDescription.DefaultCharSetAttribute) Then
                Dim charSet As CharSet = attrData.GetConstructorArgument(Of CharSet)(0, SpecialType.System_Enum)
                If Not CommonModuleWellKnownAttributeData.IsValidCharSet(charSet) Then
                    arguments.Diagnostics.Add(ERRID.ERR_BadAttribute1, arguments.AttributeSyntaxOpt.ArgumentList.Arguments(0).GetLocation(), attrData.AttributeClass)
                Else
                    arguments.GetOrCreateData(Of CommonModuleWellKnownAttributeData)().DefaultCharacterSet = charSet
                End If
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.DebuggableAttribute) Then
                arguments.GetOrCreateData(Of CommonModuleWellKnownAttributeData).HasDebuggableAttribute = True
            End If

            MyBase.DecodeWellKnownAttribute(arguments)
        End Sub

        Friend Overrides ReadOnly Property HasAssemblyCompilationRelaxationsAttribute() As Boolean
            Get
                Dim decodedData As CommonAssemblyWellKnownAttributeData(Of NamedTypeSymbol) = DirectCast(Me.ContainingAssembly, SourceAssemblySymbol).GetSourceDecodedWellKnownAttributeData()
                Return decodedData IsNot Nothing AndAlso decodedData.HasCompilationRelaxationsAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property HasAssemblyRuntimeCompatibilityAttribute() As Boolean
            Get
                Dim decodedData As CommonAssemblyWellKnownAttributeData(Of NamedTypeSymbol) = DirectCast(Me.ContainingAssembly, SourceAssemblySymbol).GetSourceDecodedWellKnownAttributeData()
                Return decodedData IsNot Nothing AndAlso decodedData.HasRuntimeCompatibilityAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property DefaultMarshallingCharSet As CharSet?
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return If(data IsNot Nothing AndAlso data.HasDefaultCharSetAttribute, data.DefaultCharacterSet, DirectCast(Nothing, CharSet?))
            End Get
        End Property

        Public Function GetMyGroupCollectionPropertyWithDefaultInstanceAlias(classType As NamedTypeSymbol) As SynthesizedMyGroupCollectionPropertySymbol
            Debug.Assert(classType.IsDefinition AndAlso Not classType.IsGenericType)

            If _lazyTypesWithDefaultInstanceAlias Is Nothing Then
                _lazyTypesWithDefaultInstanceAlias = GetTypesWithDefaultInstanceAlias()
            End If

            Dim result As SynthesizedMyGroupCollectionPropertySymbol = Nothing

            If _lazyTypesWithDefaultInstanceAlias IsNot s_noTypesWithDefaultInstanceAlias AndAlso
               _lazyTypesWithDefaultInstanceAlias.TryGetValue(classType, result) Then
                Return result
            End If

            Return Nothing
        End Function

        Private Function GetTypesWithDefaultInstanceAlias() As Dictionary(Of NamedTypeSymbol, SynthesizedMyGroupCollectionPropertySymbol)
            Dim result As Dictionary(Of NamedTypeSymbol, SynthesizedMyGroupCollectionPropertySymbol) = Nothing

            If _assemblySymbol.DeclaringCompilation.MyTemplate IsNot Nothing Then
                GetTypesWithDefaultInstanceAlias(GlobalNamespace, result)
            End If

            If result Is Nothing Then
                result = s_noTypesWithDefaultInstanceAlias
            End If

            Return result
        End Function

        Private Shared Sub GetTypesWithDefaultInstanceAlias(
            namespaceOrType As NamespaceOrTypeSymbol,
            <[In], Out> ByRef result As Dictionary(Of NamedTypeSymbol, SynthesizedMyGroupCollectionPropertySymbol)
        )
            For Each member As Symbol In namespaceOrType.GetMembersUnordered()
                Select Case member.Kind
                    Case SymbolKind.Property
                        If member.IsMyGroupCollectionProperty Then
                            Dim prop = DirectCast(member, SynthesizedMyGroupCollectionPropertySymbol)

                            ' See Semantics::GetDefaultInstanceBaseNameForMyGroupMember
                            If prop.DefaultInstanceAlias.Length > 0 Then
                                Dim targetType = DirectCast(prop.Type, NamedTypeSymbol)

                                If result Is Nothing Then
                                    result = New Dictionary(Of NamedTypeSymbol, SynthesizedMyGroupCollectionPropertySymbol)(ReferenceEqualityComparer.Instance)
                                ElseIf result.ContainsKey(targetType) Then
                                    ' ambiguity
                                    result(targetType) = Nothing
                                    Exit Select
                                End If

                                result.Add(targetType, prop)
                            End If
                        End If

                    Case SymbolKind.NamedType
                        Dim named = TryCast(member, SourceNamedTypeSymbol)

                        If named IsNot Nothing Then
                            For Each syntaxRef As SyntaxReference In named.SyntaxReferences
                                If syntaxRef.SyntaxTree.IsMyTemplate Then
                                    GetTypesWithDefaultInstanceAlias(named, result)
                                    Exit For
                                End If
                            Next
                        End If

                    Case SymbolKind.Namespace
                        GetTypesWithDefaultInstanceAlias(DirectCast(member, NamespaceSymbol), result)
                End Select
            Next
        End Sub
    End Class
End Namespace
