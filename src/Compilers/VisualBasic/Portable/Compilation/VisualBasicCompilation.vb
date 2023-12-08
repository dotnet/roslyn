' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.Emit
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.InternalUtilities
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The Compilation object is an immutable representation of a single invocation of the
    ''' compiler. Although immutable, a Compilation is also on-demand, in that a compilation can be
    ''' created quickly, but will that compiler parts or all of the code in order to respond to
    ''' method or properties. Also, a compilation can produce a new compilation with a small change
    ''' from the current compilation. This is, in many cases, more efficient than creating a new
    ''' compilation from scratch, as the new compilation can share information from the old
    ''' compilation.
    ''' </summary>
    Public NotInheritable Class VisualBasicCompilation
        Inherits Compilation

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        '
        ' Changes to the public interface of this class should remain synchronized with the C#
        ' version. Do not make any changes to the public interface without making the corresponding
        ' change to the C# version.
        '
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ''' <summary>
        ''' most of time all compilation would use same MyTemplate. no reason to create (reparse) one for each compilation
        ''' as long as its parse option is same
        ''' </summary>
        Private Shared ReadOnly s_myTemplateCache As ConcurrentLruCache(Of VisualBasicParseOptions, SyntaxTree) =
            New ConcurrentLruCache(Of VisualBasicParseOptions, SyntaxTree)(capacity:=5)

        ''' <summary>
        ''' The SourceAssemblySymbol for this compilation. Do not access directly, use Assembly
        ''' property instead. This field is lazily initialized by ReferenceManager,
        ''' ReferenceManager.CacheLockObject must be locked while ReferenceManager "calculates" the
        ''' value and assigns it, several threads must not perform duplicate "calculation"
        ''' simultaneously.
        ''' </summary>
        Private _lazyAssemblySymbol As SourceAssemblySymbol

        ''' <summary>
        ''' Holds onto data related to reference binding.
        ''' The manager is shared among multiple compilations that we expect to have the same result of reference binding.
        ''' In most cases this can be determined without performing the binding. If the compilation however contains a circular 
        ''' metadata reference (a metadata reference that refers back to the compilation) we need to avoid sharing of the binding results.
        ''' We do so by creating a new reference manager for such compilation. 
        ''' </summary>
        Private _referenceManager As ReferenceManager

        ''' <summary>
        ''' The options passed to the constructor of the Compilation
        ''' </summary>
        Private ReadOnly _options As VisualBasicCompilationOptions

        ''' <summary>
        ''' The global namespace symbol. Lazily populated on first access.
        ''' </summary>
        Private _lazyGlobalNamespace As NamespaceSymbol

        ''' <summary>
        ''' The syntax trees explicitly given to the compilation at creation, in ordinal order.
        ''' </summary>
        Private ReadOnly _syntaxTrees As ImmutableArray(Of SyntaxTree)

        Private ReadOnly _syntaxTreeOrdinalMap As ImmutableDictionary(Of SyntaxTree, Integer)

        ''' <summary>
        ''' The syntax trees of this compilation plus all 'hidden' trees 
        ''' added to the compilation by compiler, e.g. Vb Core Runtime.
        ''' </summary>
        Private _lazyAllSyntaxTrees As ImmutableArray(Of SyntaxTree)

        ''' <summary>
        ''' A map between syntax trees and the root declarations in the declaration table.
        ''' Incrementally updated between compilation versions when source changes are made.
        ''' </summary>
        Private ReadOnly _rootNamespaces As ImmutableDictionary(Of SyntaxTree, DeclarationTableEntry)

        ''' <summary>
        ''' Imports appearing in <see cref="SyntaxTree"/>s in this compilation.
        ''' </summary>
        ''' <remarks>
        ''' Unlike in C#, we don't need to use a set because the <see cref="SourceFile"/> objects
        ''' that record the imports are persisted.
        ''' </remarks>
        Private _lazyImportInfos As ConcurrentQueue(Of ImportInfo)
        Private _lazyImportClauseDependencies As ConcurrentDictionary(Of (SyntaxTree As SyntaxTree, ImportsClausePosition As Integer), ImmutableArray(Of AssemblySymbol))

        ''' <summary>
        ''' Cache the CLS diagnostics for the whole compilation so they aren't computed repeatedly.
        ''' </summary>
        ''' <remarks>
        ''' NOTE: Presently, we do not cache the per-tree diagnostics.
        ''' </remarks>
        Private _lazyClsComplianceDiagnostics As ImmutableArray(Of Diagnostic)
        Private _lazyClsComplianceDependencies As ImmutableArray(Of AssemblySymbol)

        ''' <summary>
        ''' A SyntaxTree and the associated RootSingleNamespaceDeclaration for an embedded
        ''' syntax tree in the Compilation. Unlike the entries in m_rootNamespaces, the
        ''' SyntaxTree here is lazy since the tree cannot be evaluated until the references
        ''' have been resolved (as part of binding the source module), and at that point, the
        ''' SyntaxTree may be Nothing if the embedded tree is not needed for the Compilation.
        ''' </summary>
        Private Structure EmbeddedTreeAndDeclaration
            Public ReadOnly Tree As Lazy(Of SyntaxTree)
            Public ReadOnly DeclarationEntry As DeclarationTableEntry

            Public Sub New(treeOpt As Func(Of SyntaxTree), rootNamespaceOpt As Func(Of RootSingleNamespaceDeclaration))
                Me.Tree = New Lazy(Of SyntaxTree)(treeOpt)
                Me.DeclarationEntry = New DeclarationTableEntry(New Lazy(Of RootSingleNamespaceDeclaration)(rootNamespaceOpt), isEmbedded:=True)
            End Sub
        End Structure

        Private ReadOnly _embeddedTrees As ImmutableArray(Of EmbeddedTreeAndDeclaration)

        ''' <summary>
        ''' The declaration table that holds onto declarations from source. Incrementally updated
        ''' between compilation versions when source changes are made.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _declarationTable As DeclarationTable

        ''' <summary>
        ''' Manages anonymous types declared in this compilation. Unifies types that are structurally equivalent.
        ''' </summary>
        Private ReadOnly _anonymousTypeManager As AnonymousTypeManager

        ''' <summary>
        ''' Manages automatically embedded content.
        ''' </summary>
        Private _lazyEmbeddedSymbolManager As EmbeddedSymbolManager

        ''' <summary>
        ''' MyTemplate automatically embedded from resource in the compiler.
        ''' It doesn't feel like it should be managed by EmbeddedSymbolManager
        ''' because MyTemplate is treated as user code, i.e. can be extended via
        ''' partial declarations, doesn't require "on-demand" metadata generation, etc.
        ''' 
        ''' SyntaxTree.Dummy means uninitialized.
        ''' </summary>
        Private _lazyMyTemplate As SyntaxTree = VisualBasicSyntaxTree.Dummy

        Private ReadOnly _scriptClass As Lazy(Of ImplicitNamedTypeSymbol)

        ''' <summary>
        ''' Contains the main method of this assembly, if there is one.
        ''' </summary>
        Private _lazyEntryPoint As EntryPoint

        ''' <summary>
        ''' The set of trees for which a <see cref="CompilationUnitCompletedEvent"/> has been added to the queue.
        ''' </summary>
        Private _lazyCompilationUnitCompletedTrees As HashSet(Of SyntaxTree)

        ''' <summary>
        ''' The common language version among the trees of the compilation.
        ''' </summary>
        Private ReadOnly _languageVersion As LanguageVersion

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Public Overrides ReadOnly Property IsCaseSensitive As Boolean
            Get
                Return False
            End Get
        End Property

        Friend ReadOnly Property Declarations As DeclarationTable
            Get
                Return _declarationTable
            End Get
        End Property

        Friend ReadOnly Property MergedRootDeclaration As MergedNamespaceDeclaration
            Get
                Return Declarations.GetMergedRoot(Me)
            End Get
        End Property

        Public Shadows ReadOnly Property Options As VisualBasicCompilationOptions
            Get
                Return _options
            End Get
        End Property

        ''' <summary>
        ''' The language version that was used to parse the syntax trees of this compilation.
        ''' </summary>
        Public ReadOnly Property LanguageVersion As LanguageVersion
            Get
                Return _languageVersion
            End Get
        End Property

        Friend ReadOnly Property AnonymousTypeManager As AnonymousTypeManager
            Get
                Return Me._anonymousTypeManager
            End Get
        End Property

        Friend Overrides ReadOnly Property CommonAnonymousTypeManager As CommonAnonymousTypeManager
            Get
                Return Me._anonymousTypeManager
            End Get
        End Property

        ''' <summary>
        ''' SyntaxTree of MyTemplate for the compilation. Settable for testing purposes only.
        ''' </summary>
        Friend Property MyTemplate As SyntaxTree
            Get
                If _lazyMyTemplate Is VisualBasicSyntaxTree.Dummy Then
                    Dim compilationOptions = Me.Options
                    If compilationOptions.EmbedVbCoreRuntime OrElse compilationOptions.SuppressEmbeddedDeclarations Then
                        _lazyMyTemplate = Nothing
                    Else
                        ' first see whether we can use one from global cache
                        Dim parseOptions = If(compilationOptions.ParseOptions, VisualBasicParseOptions.Default)

                        Dim tree As SyntaxTree = Nothing

                        If s_myTemplateCache.TryGetValue(parseOptions, tree) Then
                            Debug.Assert(tree IsNot Nothing)
                            Debug.Assert(tree IsNot VisualBasicSyntaxTree.Dummy)
                            Debug.Assert(tree.IsMyTemplate)

                            Interlocked.CompareExchange(_lazyMyTemplate, tree, VisualBasicSyntaxTree.Dummy)
                        Else
                            ' we need to make one.
                            Dim text As String = EmbeddedResources.VbMyTemplateText

                            ' The My template regularly makes use of more recent language features.  Care is
                            ' taken to ensure these are compatible with 2.0 runtimes so there is no danger
                            ' with allowing the newer syntax here.
                            Dim options = parseOptions.WithLanguageVersion(LanguageVersion.Default)
                            tree = VisualBasicSyntaxTree.ParseText(
                                SourceText.From(text, encoding:=Nothing, SourceHashAlgorithms.Default),
                                isMyTemplate:=True,
                                options,
                                path:=Nothing)

                            If tree.GetDiagnostics().Any() Then
                                Throw ExceptionUtilities.Unreachable
                            End If

                            If Interlocked.CompareExchange(_lazyMyTemplate, tree, VisualBasicSyntaxTree.Dummy) Is VisualBasicSyntaxTree.Dummy Then
                                ' set global cache
                                s_myTemplateCache(parseOptions) = tree
                            End If
                        End If
                    End If
                    Debug.Assert(_lazyMyTemplate Is Nothing OrElse _lazyMyTemplate.IsMyTemplate)
                End If

                Return _lazyMyTemplate
            End Get
            Set(value As SyntaxTree)
                Debug.Assert(_lazyMyTemplate Is VisualBasicSyntaxTree.Dummy)
                Debug.Assert(value IsNot VisualBasicSyntaxTree.Dummy)
                Debug.Assert(value Is Nothing OrElse value.IsMyTemplate)

                If value?.GetDiagnostics().Any() Then
                    Throw ExceptionUtilities.Unreachable
                End If

                _lazyMyTemplate = value
            End Set
        End Property

        Friend ReadOnly Property EmbeddedSymbolManager As EmbeddedSymbolManager
            Get
                If _lazyEmbeddedSymbolManager Is Nothing Then
                    Dim embedded = If(Options.EmbedVbCoreRuntime, EmbeddedSymbolKind.VbCore, EmbeddedSymbolKind.None) Or
                                        If(IncludeInternalXmlHelper(), EmbeddedSymbolKind.XmlHelper, EmbeddedSymbolKind.None)
                    If embedded <> EmbeddedSymbolKind.None Then
                        embedded = embedded Or EmbeddedSymbolKind.EmbeddedAttribute
                    End If
                    Interlocked.CompareExchange(_lazyEmbeddedSymbolManager, New EmbeddedSymbolManager(embedded), Nothing)
                End If
                Return _lazyEmbeddedSymbolManager
            End Get
        End Property

#Region "Constructors and Factories"

        ''' <summary>
        ''' Create a new compilation from scratch.
        ''' </summary>
        ''' <param name="assemblyName">Simple assembly name.</param>
        ''' <param name="syntaxTrees">The syntax trees with the source code for the new compilation.</param>
        ''' <param name="references">The references for the new compilation.</param>
        ''' <param name="options">The compiler options to use.</param>
        ''' <returns>A new compilation.</returns>
        Public Shared Function Create(
            assemblyName As String,
            Optional syntaxTrees As IEnumerable(Of SyntaxTree) = Nothing,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional options As VisualBasicCompilationOptions = Nothing
        ) As VisualBasicCompilation
            Return Create(assemblyName,
                          options,
                          If(syntaxTrees IsNot Nothing, syntaxTrees.Cast(Of SyntaxTree), Nothing),
                          references,
                          previousSubmission:=Nothing,
                          returnType:=Nothing,
                          hostObjectType:=Nothing,
                          isSubmission:=False)
        End Function

        ''' <summary> 
        ''' Creates a new compilation that can be used in scripting. 
        ''' </summary>
        Friend Shared Function CreateScriptCompilation(
            assemblyName As String,
            Optional syntaxTree As SyntaxTree = Nothing,
            Optional references As IEnumerable(Of MetadataReference) = Nothing,
            Optional options As VisualBasicCompilationOptions = Nothing,
            Optional previousScriptCompilation As VisualBasicCompilation = Nothing,
            Optional returnType As Type = Nothing,
            Optional globalsType As Type = Nothing) As VisualBasicCompilation

            CheckSubmissionOptions(options)
            ValidateScriptCompilationParameters(previousScriptCompilation, returnType, globalsType)

            Return Create(
                assemblyName,
                If(options, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).WithReferencesSupersedeLowerVersions(True),
                If((syntaxTree IsNot Nothing), {syntaxTree}, SpecializedCollections.EmptyEnumerable(Of SyntaxTree)()),
                references,
                previousScriptCompilation,
                returnType,
                globalsType,
                isSubmission:=True)
        End Function

        Private Shared Function Create(
            assemblyName As String,
            options As VisualBasicCompilationOptions,
            syntaxTrees As IEnumerable(Of SyntaxTree),
            references As IEnumerable(Of MetadataReference),
            previousSubmission As VisualBasicCompilation,
            returnType As Type,
            hostObjectType As Type,
            isSubmission As Boolean
        ) As VisualBasicCompilation
            Debug.Assert(Not isSubmission OrElse options.ReferencesSupersedeLowerVersions)

            If options Is Nothing Then
                options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)
            End If

            Dim validatedReferences = ValidateReferences(Of VisualBasicCompilationReference)(references)

            Dim c As VisualBasicCompilation = Nothing
            Dim embeddedTrees = CreateEmbeddedTrees(New Lazy(Of VisualBasicCompilation)(Function() c))
            Dim declMap = ImmutableDictionary.Create(Of SyntaxTree, DeclarationTableEntry)()
            Dim declTable = AddEmbeddedTrees(DeclarationTable.Empty, embeddedTrees)

            c = New VisualBasicCompilation(
                assemblyName,
                options,
                validatedReferences,
                ImmutableArray(Of SyntaxTree).Empty,
                ImmutableDictionary.Create(Of SyntaxTree, Integer)(),
                declMap,
                embeddedTrees,
                declTable,
                previousSubmission,
                returnType,
                hostObjectType,
                isSubmission,
                referenceManager:=Nothing,
                reuseReferenceManager:=False,
                eventQueue:=Nothing,
                semanticModelProvider:=Nothing)

            If syntaxTrees IsNot Nothing Then
                c = c.AddSyntaxTrees(syntaxTrees)
            End If

            Debug.Assert(c._lazyAssemblySymbol Is Nothing)

            Return c
        End Function

        Private Sub New(
            assemblyName As String,
            options As VisualBasicCompilationOptions,
            references As ImmutableArray(Of MetadataReference),
            syntaxTrees As ImmutableArray(Of SyntaxTree),
            syntaxTreeOrdinalMap As ImmutableDictionary(Of SyntaxTree, Integer),
            rootNamespaces As ImmutableDictionary(Of SyntaxTree, DeclarationTableEntry),
            embeddedTrees As ImmutableArray(Of EmbeddedTreeAndDeclaration),
            declarationTable As DeclarationTable,
            previousSubmission As VisualBasicCompilation,
            submissionReturnType As Type,
            hostObjectType As Type,
            isSubmission As Boolean,
            referenceManager As ReferenceManager,
            reuseReferenceManager As Boolean,
            semanticModelProvider As SemanticModelProvider,
            Optional eventQueue As AsyncQueue(Of CompilationEvent) = Nothing
        )
            MyBase.New(assemblyName, references, SyntaxTreeCommonFeatures(syntaxTrees), isSubmission, semanticModelProvider, eventQueue)

            Debug.Assert(rootNamespaces IsNot Nothing)
            Debug.Assert(declarationTable IsNot Nothing)

            Debug.Assert(syntaxTrees.All(Function(tree) syntaxTrees(syntaxTreeOrdinalMap(tree)) Is tree))
            Debug.Assert(syntaxTrees.SetEquals(rootNamespaces.Keys.AsImmutable(), EqualityComparer(Of SyntaxTree).Default))
            Debug.Assert(embeddedTrees.All(Function(treeAndDeclaration) declarationTable.Contains(treeAndDeclaration.DeclarationEntry)))

            _options = options
            _syntaxTrees = syntaxTrees
            _syntaxTreeOrdinalMap = syntaxTreeOrdinalMap
            _rootNamespaces = rootNamespaces
            _embeddedTrees = embeddedTrees
            _declarationTable = declarationTable
            _anonymousTypeManager = New AnonymousTypeManager(Me)
            _languageVersion = CommonLanguageVersion(syntaxTrees)

            _scriptClass = New Lazy(Of ImplicitNamedTypeSymbol)(AddressOf BindScriptClass)

            If isSubmission Then
                Debug.Assert(previousSubmission Is Nothing OrElse previousSubmission.HostObjectType Is hostObjectType)
                Me.ScriptCompilationInfo = New VisualBasicScriptCompilationInfo(previousSubmission, submissionReturnType, hostObjectType)
            Else
                Debug.Assert(previousSubmission Is Nothing AndAlso submissionReturnType Is Nothing AndAlso hostObjectType Is Nothing)
            End If

            If reuseReferenceManager Then
                referenceManager.AssertCanReuseForCompilation(Me)
                _referenceManager = referenceManager
            Else
                _referenceManager = New ReferenceManager(MakeSourceAssemblySimpleName(),
                                                              options.AssemblyIdentityComparer,
                                                              If(referenceManager IsNot Nothing, referenceManager.ObservedMetadata, Nothing))
            End If

            Debug.Assert(_lazyAssemblySymbol Is Nothing)
            If Me.EventQueue IsNot Nothing Then
                Me.EventQueue.TryEnqueue(New CompilationStartedEvent(Me))
            End If
        End Sub

        Friend Overrides Sub ValidateDebugEntryPoint(debugEntryPoint As IMethodSymbol, diagnostics As DiagnosticBag)
            Debug.Assert(debugEntryPoint IsNot Nothing)

            ' Debug entry point has to be a method definition from this compilation.
            Dim methodSymbol = TryCast(debugEntryPoint, MethodSymbol)
            If methodSymbol?.DeclaringCompilation IsNot Me OrElse Not methodSymbol.IsDefinition Then
                diagnostics.Add(ERRID.ERR_DebugEntryPointNotSourceMethodDefinition, Location.None)
            End If
        End Sub

        Private Function CommonLanguageVersion(syntaxTrees As ImmutableArray(Of SyntaxTree)) As LanguageVersion
            ' We don't check m_Options.ParseOptions.LanguageVersion for consistency, because
            ' it isn't consistent in practice.  In fact sometimes m_Options.ParseOptions is Nothing.
            Dim result As LanguageVersion? = Nothing
            For Each tree In syntaxTrees
                Dim version = CType(tree.Options, VisualBasicParseOptions).LanguageVersion
                If result Is Nothing Then
                    result = version
                ElseIf result <> version Then
                    Throw New ArgumentException(CodeAnalysisResources.InconsistentLanguageVersions, NameOf(syntaxTrees))
                End If
            Next

            Return If(result, LanguageVersion.Default.MapSpecifiedToEffectiveVersion)
        End Function

        ''' <summary>
        ''' Create a duplicate of this compilation with different symbol instances
        ''' </summary>
        Public Shadows Function Clone() As VisualBasicCompilation
            Return New VisualBasicCompilation(
                Me.AssemblyName,
                _options,
                Me.ExternalReferences,
                _syntaxTrees,
                _syntaxTreeOrdinalMap,
                _rootNamespaces,
                _embeddedTrees,
                _declarationTable,
                Me.PreviousSubmission,
                Me.SubmissionReturnType,
                Me.HostObjectType,
                Me.IsSubmission,
                _referenceManager,
                reuseReferenceManager:=True,
                Me.SemanticModelProvider,
                eventQueue:=Nothing) ' no event queue when cloning
        End Function

        Private Function UpdateSyntaxTrees(
            syntaxTrees As ImmutableArray(Of SyntaxTree),
            syntaxTreeOrdinalMap As ImmutableDictionary(Of SyntaxTree, Integer),
            rootNamespaces As ImmutableDictionary(Of SyntaxTree, DeclarationTableEntry),
            declarationTable As DeclarationTable,
            referenceDirectivesChanged As Boolean) As VisualBasicCompilation

            Return New VisualBasicCompilation(
                Me.AssemblyName,
                _options,
                Me.ExternalReferences,
                syntaxTrees,
                syntaxTreeOrdinalMap,
                rootNamespaces,
                _embeddedTrees,
                declarationTable,
                Me.PreviousSubmission,
                Me.SubmissionReturnType,
                Me.HostObjectType,
                Me.IsSubmission,
                _referenceManager,
                reuseReferenceManager:=Not referenceDirectivesChanged,
                Me.SemanticModelProvider)
        End Function

        ''' <summary>
        ''' Creates a new compilation with the specified name.
        ''' </summary>
        Public Shadows Function WithAssemblyName(assemblyName As String) As VisualBasicCompilation
            ' Can't reuse references since the source assembly name changed and the referenced symbols might 
            ' have internals-visible-to relationship with this compilation or they might had a circular reference 
            ' to this compilation.

            Return New VisualBasicCompilation(
                assemblyName,
                Me.Options,
                Me.ExternalReferences,
                _syntaxTrees,
                _syntaxTreeOrdinalMap,
                _rootNamespaces,
                _embeddedTrees,
                _declarationTable,
                Me.PreviousSubmission,
                Me.SubmissionReturnType,
                Me.HostObjectType,
                Me.IsSubmission,
                _referenceManager,
                reuseReferenceManager:=String.Equals(assemblyName, Me.AssemblyName, StringComparison.Ordinal),
                Me.SemanticModelProvider)
        End Function

        Public Shadows Function WithReferences(ParamArray newReferences As MetadataReference()) As VisualBasicCompilation
            Return WithReferences(DirectCast(newReferences, IEnumerable(Of MetadataReference)))
        End Function

        ''' <summary>
        ''' Creates a new compilation with the specified references.
        ''' </summary>
        ''' <remarks>
        ''' The new <see cref="VisualBasicCompilation"/> will query the given <see cref="MetadataReference"/> for the underlying 
        ''' metadata as soon as the are needed. 
        ''' 
        ''' The New compilation uses whatever metadata is currently being provided by the <see cref="MetadataReference"/>.
        ''' E.g. if the current compilation references a metadata file that has changed since the creation of the compilation
        ''' the New compilation is going to use the updated version, while the current compilation will be using the previous (it doesn't change).
        ''' </remarks>
        Public Shadows Function WithReferences(newReferences As IEnumerable(Of MetadataReference)) As VisualBasicCompilation
            Dim declTable = RemoveEmbeddedTrees(_declarationTable, _embeddedTrees)
            Dim c As VisualBasicCompilation = Nothing
            Dim embeddedTrees = CreateEmbeddedTrees(New Lazy(Of VisualBasicCompilation)(Function() c))
            declTable = AddEmbeddedTrees(declTable, embeddedTrees)

            ' References might have changed, don't reuse reference manager.
            ' Don't even reuse observed metadata - let the manager query for the metadata again.

            c = New VisualBasicCompilation(
                Me.AssemblyName,
                Me.Options,
                ValidateReferences(Of VisualBasicCompilationReference)(newReferences),
                _syntaxTrees,
                _syntaxTreeOrdinalMap,
                _rootNamespaces,
                embeddedTrees,
                declTable,
                Me.PreviousSubmission,
                Me.SubmissionReturnType,
                Me.HostObjectType,
                Me.IsSubmission,
                referenceManager:=Nothing,
                reuseReferenceManager:=False,
                Me.SemanticModelProvider)
            Return c
        End Function

        Public Shadows Function WithOptions(newOptions As VisualBasicCompilationOptions) As VisualBasicCompilation
            If newOptions Is Nothing Then
                Throw New ArgumentNullException(NameOf(newOptions))
            End If

            Dim c As VisualBasicCompilation = Nothing
            Dim embeddedTrees = _embeddedTrees
            Dim declTable = _declarationTable
            Dim declMap = Me._rootNamespaces

            If Not String.Equals(Me.Options.RootNamespace, newOptions.RootNamespace, StringComparison.Ordinal) Then
                ' If the root namespace was updated we have to update declaration table 
                ' entries for all the syntax trees of the compilation
                '
                ' NOTE: we use case-sensitive comparison so that the new compilation
                '       gets a root namespace with correct casing

                declMap = ImmutableDictionary.Create(Of SyntaxTree, DeclarationTableEntry)()
                declTable = DeclarationTable.Empty

                embeddedTrees = CreateEmbeddedTrees(New Lazy(Of VisualBasicCompilation)(Function() c))
                declTable = AddEmbeddedTrees(declTable, embeddedTrees)

                Dim discardedReferenceDirectivesChanged As Boolean = False

                For Each tree In _syntaxTrees
                    AddSyntaxTreeToDeclarationMapAndTable(tree, newOptions, Me.IsSubmission, declMap, declTable, discardedReferenceDirectivesChanged) ' declMap and declTable passed ByRef
                Next

            ElseIf Me.Options.EmbedVbCoreRuntime <> newOptions.EmbedVbCoreRuntime OrElse Me.Options.ParseOptions <> newOptions.ParseOptions Then
                declTable = RemoveEmbeddedTrees(declTable, _embeddedTrees)
                embeddedTrees = CreateEmbeddedTrees(New Lazy(Of VisualBasicCompilation)(Function() c))
                declTable = AddEmbeddedTrees(declTable, embeddedTrees)
            End If

            c = New VisualBasicCompilation(
                Me.AssemblyName,
                newOptions,
                Me.ExternalReferences,
                _syntaxTrees,
                _syntaxTreeOrdinalMap,
                declMap,
                embeddedTrees,
                declTable,
                Me.PreviousSubmission,
                Me.SubmissionReturnType,
                Me.HostObjectType,
                Me.IsSubmission,
                _referenceManager,
                reuseReferenceManager:=_options.CanReuseCompilationReferenceManager(newOptions),
                Me.SemanticModelProvider)
            Return c
        End Function

        ''' <summary>
        ''' Returns a new compilation with the given compilation set as the previous submission. 
        ''' </summary>
        Friend Shadows Function WithScriptCompilationInfo(info As VisualBasicScriptCompilationInfo) As VisualBasicCompilation
            If info Is ScriptCompilationInfo Then
                Return Me
            End If

            ' Metadata references are inherited from the previous submission,
            ' so we can only reuse the manager if we can guarantee that these references are the same.
            ' Check if the previous script compilation doesn't change. 

            ' TODO Consider comparing the metadata references if they have been bound already.
            ' https://github.com/dotnet/roslyn/issues/43397
            Dim reuseReferenceManager = ScriptCompilationInfo?.PreviousScriptCompilation Is info?.PreviousScriptCompilation

            Return New VisualBasicCompilation(
                Me.AssemblyName,
                Me.Options,
                Me.ExternalReferences,
                _syntaxTrees,
                _syntaxTreeOrdinalMap,
                _rootNamespaces,
                _embeddedTrees,
                _declarationTable,
                info?.PreviousScriptCompilation,
                info?.ReturnTypeOpt,
                info?.GlobalsType,
                info IsNot Nothing,
                _referenceManager,
                reuseReferenceManager,
                Me.SemanticModelProvider)
        End Function

        ''' <summary>
        ''' Returns a new compilation with the given semantic model provider.
        ''' </summary>
        Friend Overrides Function WithSemanticModelProvider(semanticModelProvider As SemanticModelProvider) As Compilation
            If Me.SemanticModelProvider Is semanticModelProvider Then
                Return Me
            End If

            Return New VisualBasicCompilation(
                Me.AssemblyName,
                Me.Options,
                Me.ExternalReferences,
                _syntaxTrees,
                _syntaxTreeOrdinalMap,
                _rootNamespaces,
                _embeddedTrees,
                _declarationTable,
                Me.PreviousSubmission,
                Me.SubmissionReturnType,
                Me.HostObjectType,
                Me.IsSubmission,
                _referenceManager,
                reuseReferenceManager:=True,
                semanticModelProvider)
        End Function

        ''' <summary>
        ''' Returns a new compilation with a given event queue.
        ''' </summary>
        Friend Overrides Function WithEventQueue(eventQueue As AsyncQueue(Of CompilationEvent)) As Compilation
            Return New VisualBasicCompilation(
                Me.AssemblyName,
                Me.Options,
                Me.ExternalReferences,
                _syntaxTrees,
                _syntaxTreeOrdinalMap,
                _rootNamespaces,
                _embeddedTrees,
                _declarationTable,
                Me.PreviousSubmission,
                Me.SubmissionReturnType,
                Me.HostObjectType,
                Me.IsSubmission,
                _referenceManager,
                reuseReferenceManager:=True,
                Me.SemanticModelProvider,
                eventQueue:=eventQueue)
        End Function

        Friend Overrides Sub SerializePdbEmbeddedCompilationOptions(builder As BlobBuilder)
            ' LanguageVersion should already be mapped to an effective version at this point
            Debug.Assert(LanguageVersion.MapSpecifiedToEffectiveVersion() = LanguageVersion)
            WriteValue(builder, CompilationOptionNames.LanguageVersion, LanguageVersion.ToDisplayString())
            WriteValue(builder, CompilationOptionNames.Checked, Options.CheckOverflow.ToString())
            WriteValue(builder, CompilationOptionNames.OptionStrict, Options.OptionStrict.ToString())
            WriteValue(builder, CompilationOptionNames.OptionInfer, Options.OptionInfer.ToString())
            WriteValue(builder, CompilationOptionNames.OptionCompareText, Options.OptionCompareText.ToString())
            WriteValue(builder, CompilationOptionNames.OptionExplicit, Options.OptionExplicit.ToString())
            WriteValue(builder, CompilationOptionNames.EmbedRuntime, Options.EmbedVbCoreRuntime.ToString())

            If Options.GlobalImports.Length > 0 Then
                WriteValue(builder, CompilationOptionNames.GlobalNamespaces, String.Join(";", Options.GlobalImports.Select(Function(x) x.Name)))
            End If

            If Not String.IsNullOrEmpty(Options.RootNamespace) Then
                WriteValue(builder, CompilationOptionNames.RootNamespace, Options.RootNamespace)
            End If

            If Options.ParseOptions IsNot Nothing Then
                Dim preprocessorStrings = Options.ParseOptions.PreprocessorSymbols.Select(
                    Function(p) As String
                        If TypeOf p.Value Is String Then
                            Return p.Key + "=""" + p.Value.ToString() + """"
                        ElseIf p.Value Is Nothing Then
                            Return p.Key
                        Else
                            Return p.Key + "=" + p.Value.ToString()
                        End If
                    End Function)
                WriteValue(builder, CompilationOptionNames.Define, String.Join(",", preprocessorStrings))
            End If
        End Sub

        Private Sub WriteValue(builder As BlobBuilder, key As String, value As String)
            builder.WriteUTF8(key)
            builder.WriteByte(0)
            builder.WriteUTF8(value)
            builder.WriteByte(0)
        End Sub
#End Region

#Region "Submission"

        Friend Shadows ReadOnly Property ScriptCompilationInfo As VisualBasicScriptCompilationInfo

        Friend Overrides ReadOnly Property CommonScriptCompilationInfo As ScriptCompilationInfo
            Get
                Return ScriptCompilationInfo
            End Get
        End Property

        Friend Shadows ReadOnly Property PreviousSubmission As VisualBasicCompilation
            Get
                Return ScriptCompilationInfo?.PreviousScriptCompilation
            End Get
        End Property

        Friend Overrides Function HasSubmissionResult() As Boolean
            Debug.Assert(IsSubmission)

            ' submission can be empty or comprise of a script file
            Dim tree = SyntaxTrees.SingleOrDefault()
            If tree Is Nothing Then
                Return False
            End If

            Dim root = tree.GetCompilationUnitRoot()
            If root.HasErrors Then
                Return False
            End If

            ' TODO: look for return statements
            ' https://github.com/dotnet/roslyn/issues/5773

            Dim lastStatement = root.Members.LastOrDefault()
            If lastStatement Is Nothing Then
                Return False
            End If

            Dim model = GetSemanticModel(tree)
            Select Case lastStatement.Kind
                Case SyntaxKind.PrintStatement
                    Dim expression = DirectCast(lastStatement, PrintStatementSyntax).Expression
                    Dim info = model.GetTypeInfo(expression)
                    ' always true, even for info.Type = Void
                    Return True

                Case SyntaxKind.ExpressionStatement
                    Dim expression = DirectCast(lastStatement, ExpressionStatementSyntax).Expression
                    Dim info = model.GetTypeInfo(expression)
                    Return info.Type.SpecialType <> SpecialType.System_Void

                Case SyntaxKind.CallStatement
                    Dim expression = DirectCast(lastStatement, CallStatementSyntax).Invocation
                    Dim info = model.GetTypeInfo(expression)
                    Return info.Type.SpecialType <> SpecialType.System_Void

                Case Else
                    Return False
            End Select
        End Function

        Friend Function GetSubmissionInitializer() As SynthesizedInteractiveInitializerMethod
            Return If(IsSubmission AndAlso ScriptClass IsNot Nothing,
                ScriptClass.GetScriptInitializer(),
                Nothing)
        End Function

        Protected Overrides ReadOnly Property CommonScriptGlobalsType As ITypeSymbol
            Get
                Return Nothing
            End Get
        End Property

#End Region

#Region "Syntax Trees"

        ''' <summary>
        ''' Get a read-only list of the syntax trees that this compilation was created with.
        ''' </summary>
        Public Shadows ReadOnly Property SyntaxTrees As ImmutableArray(Of SyntaxTree)
            Get
                Return _syntaxTrees
            End Get
        End Property

        ''' <summary>
        ''' Get a read-only list of the syntax trees that this compilation was created with PLUS
        ''' the trees that were automatically added to it, i.e. Vb Core Runtime tree.
        ''' </summary>
        Friend Shadows ReadOnly Property AllSyntaxTrees As ImmutableArray(Of SyntaxTree)
            Get
                If _lazyAllSyntaxTrees.IsDefault Then
                    Dim builder = ArrayBuilder(Of SyntaxTree).GetInstance()
                    builder.AddRange(_syntaxTrees)
                    For Each embeddedTree In _embeddedTrees
                        Dim tree = embeddedTree.Tree.Value
                        If tree IsNot Nothing Then
                            builder.Add(tree)
                        End If
                    Next
                    ImmutableInterlocked.InterlockedInitialize(_lazyAllSyntaxTrees, builder.ToImmutableAndFree())
                End If

                Return _lazyAllSyntaxTrees
            End Get
        End Property

        ''' <summary>
        ''' Is the passed in syntax tree in this compilation?
        ''' </summary>
        Public Shadows Function ContainsSyntaxTree(syntaxTree As SyntaxTree) As Boolean
            Return syntaxTree IsNot Nothing AndAlso _rootNamespaces.ContainsKey(syntaxTree)
        End Function

        Public Shadows Function AddSyntaxTrees(ParamArray trees As SyntaxTree()) As VisualBasicCompilation
            Return AddSyntaxTrees(DirectCast(trees, IEnumerable(Of SyntaxTree)))
        End Function

        Public Shadows Function AddSyntaxTrees(trees As IEnumerable(Of SyntaxTree)) As VisualBasicCompilation
            If trees Is Nothing Then
                Throw New ArgumentNullException(NameOf(trees))
            End If

            If Not trees.Any() Then
                Return Me
            End If

            ' We're using a try-finally for this builder because there's a test that 
            ' specifically checks for one or more of the argument exceptions below
            ' and we don't want to see console spew (even though we don't generally
            ' care about pool "leaks" in exceptional cases).  Alternatively, we
            ' could create a new ArrayBuilder.
            Dim builder = ArrayBuilder(Of SyntaxTree).GetInstance()
            Try
                builder.AddRange(_syntaxTrees)

                Dim referenceDirectivesChanged = False
                Dim oldTreeCount = _syntaxTrees.Length

                Dim ordinalMap = _syntaxTreeOrdinalMap
                Dim declMap = _rootNamespaces
                Dim declTable = _declarationTable
                Dim i = 0

                For Each tree As SyntaxTree In trees
                    If tree Is Nothing Then
                        Throw New ArgumentNullException(String.Format(VBResources.Trees0, i))
                    End If

                    If Not tree.HasCompilationUnitRoot Then
                        Throw New ArgumentException(String.Format(VBResources.TreesMustHaveRootNode, i))
                    End If

                    If tree.IsEmbeddedOrMyTemplateTree() Then
                        Throw New ArgumentException(VBResources.CannotAddCompilerSpecialTree)
                    End If

                    If declMap.ContainsKey(tree) Then
                        Throw New ArgumentException(VBResources.SyntaxTreeAlreadyPresent, String.Format(VBResources.Trees0, i))
                    End If

                    AddSyntaxTreeToDeclarationMapAndTable(tree, _options, Me.IsSubmission, declMap, declTable, referenceDirectivesChanged) ' declMap and declTable passed ByRef
                    builder.Add(tree)
                    ordinalMap = ordinalMap.Add(tree, oldTreeCount + i)
                    i += 1
                Next

                If IsSubmission AndAlso declMap.Count > 1 Then
                    Throw New ArgumentException(VBResources.SubmissionCanHaveAtMostOneSyntaxTree, NameOf(trees))
                End If

                Return UpdateSyntaxTrees(builder.ToImmutable(), ordinalMap, declMap, declTable, referenceDirectivesChanged)
            Finally
                builder.Free()
            End Try
        End Function

        Private Shared Sub AddSyntaxTreeToDeclarationMapAndTable(
                tree As SyntaxTree,
                compilationOptions As VisualBasicCompilationOptions,
                isSubmission As Boolean,
                ByRef declMap As ImmutableDictionary(Of SyntaxTree, DeclarationTableEntry),
                ByRef declTable As DeclarationTable,
                ByRef referenceDirectivesChanged As Boolean
            )

            Dim entry = New DeclarationTableEntry(New Lazy(Of RootSingleNamespaceDeclaration)(Function() ForTree(tree, compilationOptions, isSubmission)), isEmbedded:=False)
            declMap = declMap.Add(tree, entry) ' Callers are responsible for checking for existing entries.
            declTable = declTable.AddRootDeclaration(entry)
            referenceDirectivesChanged = referenceDirectivesChanged OrElse tree.HasReferenceDirectives
        End Sub

        Private Shared Function ForTree(tree As SyntaxTree, options As VisualBasicCompilationOptions, isSubmission As Boolean) As RootSingleNamespaceDeclaration
            Return DeclarationTreeBuilder.ForTree(tree, options.GetRootNamespaceParts(), If(options.ScriptClassName, ""), isSubmission)
        End Function

        Public Shadows Function RemoveSyntaxTrees(ParamArray trees As SyntaxTree()) As VisualBasicCompilation
            Return RemoveSyntaxTrees(DirectCast(trees, IEnumerable(Of SyntaxTree)))
        End Function

        Public Shadows Function RemoveSyntaxTrees(trees As IEnumerable(Of SyntaxTree)) As VisualBasicCompilation
            If trees Is Nothing Then
                Throw New ArgumentNullException(NameOf(trees))
            End If

            If Not trees.Any() Then
                Return Me
            End If

            Dim referenceDirectivesChanged = False
            Dim removeSet As New HashSet(Of SyntaxTree)()
            Dim declMap = _rootNamespaces
            Dim declTable = _declarationTable

            For Each tree As SyntaxTree In trees
                If tree.IsEmbeddedOrMyTemplateTree() Then
                    Throw New ArgumentException(VBResources.CannotRemoveCompilerSpecialTree)
                End If

                RemoveSyntaxTreeFromDeclarationMapAndTable(tree, declMap, declTable, referenceDirectivesChanged)
                removeSet.Add(tree)
            Next

            Debug.Assert(removeSet.Count > 0)

            ' We're going to have to revise the ordinals of all
            ' trees after the first one removed, so just build
            ' a new map.

            ' CONSIDER: an alternative approach would be to set the map to empty and
            ' re-calculate it the next time we need it.  This might save us time in the
            ' case where remove calls are made sequentially (rare?).

            Dim ordinalMap = ImmutableDictionary.Create(Of SyntaxTree, Integer)()
            Dim builder = ArrayBuilder(Of SyntaxTree).GetInstance()
            Dim i = 0

            For Each tree In _syntaxTrees
                If Not removeSet.Contains(tree) Then
                    builder.Add(tree)
                    ordinalMap = ordinalMap.Add(tree, i)
                    i += 1
                End If
            Next

            Return UpdateSyntaxTrees(builder.ToImmutableAndFree(), ordinalMap, declMap, declTable, referenceDirectivesChanged)
        End Function

        Private Shared Sub RemoveSyntaxTreeFromDeclarationMapAndTable(
                tree As SyntaxTree,
                ByRef declMap As ImmutableDictionary(Of SyntaxTree, DeclarationTableEntry),
            ByRef declTable As DeclarationTable,
            ByRef referenceDirectivesChanged As Boolean
            )
            Dim root As DeclarationTableEntry = Nothing
            If Not declMap.TryGetValue(tree, root) Then
                Throw New ArgumentException(String.Format(VBResources.SyntaxTreeNotFoundToRemove, tree))
            End If

            declTable = declTable.RemoveRootDeclaration(root)
            declMap = declMap.Remove(tree)
            referenceDirectivesChanged = referenceDirectivesChanged OrElse tree.HasReferenceDirectives
        End Sub

        Public Shadows Function RemoveAllSyntaxTrees() As VisualBasicCompilation
            Return UpdateSyntaxTrees(ImmutableArray(Of SyntaxTree).Empty,
                                     ImmutableDictionary.Create(Of SyntaxTree, Integer)(),
                                     ImmutableDictionary.Create(Of SyntaxTree, DeclarationTableEntry)(),
                                     AddEmbeddedTrees(DeclarationTable.Empty, _embeddedTrees),
                                     referenceDirectivesChanged:=_declarationTable.ReferenceDirectives.Any())
        End Function

        Public Shadows Function ReplaceSyntaxTree(oldTree As SyntaxTree, newTree As SyntaxTree) As VisualBasicCompilation
            If oldTree Is Nothing Then
                Throw New ArgumentNullException(NameOf(oldTree))
            End If

            If newTree Is Nothing Then
                Return Me.RemoveSyntaxTrees(oldTree)
            ElseIf newTree Is oldTree Then
                Return Me
            End If

            If Not newTree.HasCompilationUnitRoot Then
                Throw New ArgumentException(VBResources.TreeMustHaveARootNodeWithCompilationUnit, NameOf(newTree))
            End If

            Dim vbOldTree = oldTree
            Dim vbNewTree = newTree

            If vbOldTree.IsEmbeddedOrMyTemplateTree() Then
                Throw New ArgumentException(VBResources.CannotRemoveCompilerSpecialTree)
            End If

            If vbNewTree.IsEmbeddedOrMyTemplateTree() Then
                Throw New ArgumentException(VBResources.CannotAddCompilerSpecialTree)
            End If

            Dim declMap = _rootNamespaces

            If declMap.ContainsKey(vbNewTree) Then
                Throw New ArgumentException(VBResources.SyntaxTreeAlreadyPresent, NameOf(newTree))
            End If

            Dim declTable = _declarationTable
            Dim referenceDirectivesChanged = False

            ' TODO(tomat): Consider comparing #r's of the old and the new tree. If they are exactly the same we could still reuse.
            ' This could be a perf win when editing a script file in the IDE. The services create a new compilation every keystroke 
            ' that replaces the tree with a new one.

            RemoveSyntaxTreeFromDeclarationMapAndTable(vbOldTree, declMap, declTable, referenceDirectivesChanged)
            AddSyntaxTreeToDeclarationMapAndTable(vbNewTree, _options, Me.IsSubmission, declMap, declTable, referenceDirectivesChanged)

            Dim ordinalMap = _syntaxTreeOrdinalMap

            Debug.Assert(ordinalMap.ContainsKey(oldTree)) ' Checked by RemoveSyntaxTreeFromDeclarationMapAndTable
            Dim oldOrdinal = ordinalMap(oldTree)

            Dim newArray = _syntaxTrees.ToArray()
            newArray(oldOrdinal) = vbNewTree

            ' CONSIDER: should this be an operation on ImmutableDictionary?
            ordinalMap = ordinalMap.Remove(oldTree)
            ordinalMap = ordinalMap.Add(newTree, oldOrdinal)

            Return UpdateSyntaxTrees(newArray.AsImmutableOrNull(), ordinalMap, declMap, declTable, referenceDirectivesChanged)
        End Function

        Private Shared Function CreateEmbeddedTrees(compReference As Lazy(Of VisualBasicCompilation)) As ImmutableArray(Of EmbeddedTreeAndDeclaration)
            Return ImmutableArray.Create(
                New EmbeddedTreeAndDeclaration(
                    Function()
                        Dim compilation = compReference.Value
                        Return If(compilation.Options.EmbedVbCoreRuntime Or compilation.IncludeInternalXmlHelper,
                                  EmbeddedSymbolManager.EmbeddedSyntax,
                                  Nothing)
                    End Function,
                    Function()
                        Dim compilation = compReference.Value
                        Return If(compilation.Options.EmbedVbCoreRuntime Or compilation.IncludeInternalXmlHelper,
                                  ForTree(EmbeddedSymbolManager.EmbeddedSyntax, compilation.Options, isSubmission:=False),
                                  Nothing)
                    End Function),
                New EmbeddedTreeAndDeclaration(
                    Function()
                        Dim compilation = compReference.Value
                        Return If(compilation.Options.EmbedVbCoreRuntime,
                                  EmbeddedSymbolManager.VbCoreSyntaxTree,
                                  Nothing)
                    End Function,
                    Function()
                        Dim compilation = compReference.Value
                        Return If(compilation.Options.EmbedVbCoreRuntime,
                                  ForTree(EmbeddedSymbolManager.VbCoreSyntaxTree, compilation.Options, isSubmission:=False),
                                  Nothing)
                    End Function),
                New EmbeddedTreeAndDeclaration(
                    Function()
                        Dim compilation = compReference.Value
                        Return If(compilation.IncludeInternalXmlHelper(),
                                  EmbeddedSymbolManager.InternalXmlHelperSyntax,
                                  Nothing)
                    End Function,
                    Function()
                        Dim compilation = compReference.Value
                        Return If(compilation.IncludeInternalXmlHelper(),
                                  ForTree(EmbeddedSymbolManager.InternalXmlHelperSyntax, compilation.Options, isSubmission:=False),
                                  Nothing)
                    End Function),
                New EmbeddedTreeAndDeclaration(
                    Function()
                        Dim compilation = compReference.Value
                        Return compilation.MyTemplate
                    End Function,
                    Function()
                        Dim compilation = compReference.Value
                        Return If(compilation.MyTemplate IsNot Nothing,
                                  ForTree(compilation.MyTemplate, compilation.Options, isSubmission:=False),
                                  Nothing)
                    End Function))
        End Function

        Private Shared Function AddEmbeddedTrees(
            declTable As DeclarationTable,
            embeddedTrees As ImmutableArray(Of EmbeddedTreeAndDeclaration)
        ) As DeclarationTable

            For Each embeddedTree In embeddedTrees
                declTable = declTable.AddRootDeclaration(embeddedTree.DeclarationEntry)
            Next
            Return declTable
        End Function

        Private Shared Function RemoveEmbeddedTrees(
            declTable As DeclarationTable,
            embeddedTrees As ImmutableArray(Of EmbeddedTreeAndDeclaration)
        ) As DeclarationTable

            For Each embeddedTree In embeddedTrees
                declTable = declTable.RemoveRootDeclaration(embeddedTree.DeclarationEntry)
            Next
            Return declTable
        End Function

        ''' <summary>
        ''' Returns True if the set of references contains those assemblies needed for XML
        ''' literals.
        ''' If those assemblies are included, we should include the InternalXmlHelper
        ''' SyntaxTree in the Compilation so the helper methods are available for binding XML.
        ''' </summary>
        Private Function IncludeInternalXmlHelper() As Boolean
            ' In new flavors of the framework, types, that XML helpers depend upon, are
            ' defined in assemblies with different names. Let's not hardcode these names, 
            ' let's check for presence of types instead.
            Return Not Me.Options.SuppressEmbeddedDeclarations AndAlso
                   InternalXmlHelperDependencyIsSatisfied(WellKnownType.System_Linq_Enumerable) AndAlso
                   InternalXmlHelperDependencyIsSatisfied(WellKnownType.System_Xml_Linq_XElement) AndAlso
                   InternalXmlHelperDependencyIsSatisfied(WellKnownType.System_Xml_Linq_XName) AndAlso
                   InternalXmlHelperDependencyIsSatisfied(WellKnownType.System_Xml_Linq_XAttribute) AndAlso
                   InternalXmlHelperDependencyIsSatisfied(WellKnownType.System_Xml_Linq_XNamespace)
        End Function

        Private Function InternalXmlHelperDependencyIsSatisfied(type As WellKnownType) As Boolean

            Dim metadataName = MetadataTypeName.FromFullName(WellKnownTypes.GetMetadataName(type), useCLSCompliantNameArityEncoding:=True)
            Dim sourceAssembly = Me.SourceAssembly

            ' Lookup only in references. An attempt to lookup in assembly being built will get us in a cycle.
            ' We are explicitly ignoring scenario where the type might be defined in an added module.
            For Each reference As AssemblySymbol In sourceAssembly.SourceModule.GetReferencedAssemblySymbols()
                Debug.Assert(Not reference.IsMissing)
                Dim candidate As NamedTypeSymbol = reference.LookupDeclaredTopLevelMetadataType(metadataName)
                Debug.Assert(If(Not candidate?.IsErrorType(), True))

                If sourceAssembly.IsValidWellKnownType(candidate) AndAlso AssemblySymbol.IsAcceptableMatchForGetTypeByNameAndArity(candidate) Then
                    Return True
                End If
            Next

            Return False
        End Function

        ' TODO: This comparison probably will change to compiler command line order, or at least needs 
        ' TODO: to be resolved. See bug 8520.

        ''' <summary>
        ''' Compare two source locations, using their containing trees, and then by Span.First within a tree. 
        ''' Can be used to get a total ordering on declarations, for example.
        ''' </summary>
        Friend Overrides Function CompareSourceLocations(first As Location, second As Location) As Integer
            Return LexicalSortKey.Compare(first, second, Me)
        End Function

        ''' <summary>
        ''' Compare two source locations, using their containing trees, and then by Span.First within a tree. 
        ''' Can be used to get a total ordering on declarations, for example.
        ''' </summary>
        Friend Overrides Function CompareSourceLocations(first As SyntaxReference, second As SyntaxReference) As Integer
            Return LexicalSortKey.Compare(first, second, Me)
        End Function

        ''' <summary>
        ''' Compare two source locations, using their containing trees, and then by Span.First within a tree. 
        ''' Can be used to get a total ordering on declarations, for example.
        ''' </summary>
        Friend Overrides Function CompareSourceLocations(first As SyntaxNode, second As SyntaxNode) As Integer
            Return LexicalSortKey.Compare(first, second, Me)
        End Function

        Friend Overrides Function GetSyntaxTreeOrdinal(tree As SyntaxTree) As Integer
            Debug.Assert(Me.ContainsSyntaxTree(tree))
            Return _syntaxTreeOrdinalMap(tree)
        End Function

#End Region

#Region "References"
        Friend Overrides Function CommonGetBoundReferenceManager() As CommonReferenceManager
            Return GetBoundReferenceManager()
        End Function

        Friend Shadows Function GetBoundReferenceManager() As ReferenceManager
            If _lazyAssemblySymbol Is Nothing Then
                _referenceManager.CreateSourceAssemblyForCompilation(Me)
                Debug.Assert(_lazyAssemblySymbol IsNot Nothing)
            End If

            ' referenceManager can only be accessed after we initialized the lazyAssemblySymbol.
            ' In fact, initialization of the assembly symbol might change the reference manager.
            Return _referenceManager
        End Function

        ' for testing only:
        Friend Function ReferenceManagerEquals(other As VisualBasicCompilation) As Boolean
            Return _referenceManager Is other._referenceManager
        End Function

        Public Overrides ReadOnly Property DirectiveReferences As ImmutableArray(Of MetadataReference)
            Get
                Return GetBoundReferenceManager().DirectiveReferences
            End Get
        End Property

        Friend Overrides ReadOnly Property ReferenceDirectiveMap As IDictionary(Of (path As String, content As String), MetadataReference)
            Get
                Return GetBoundReferenceManager().ReferenceDirectiveMap
            End Get
        End Property

        ''' <summary>
        ''' Gets the <see cref="AssemblySymbol"/> or <see cref="ModuleSymbol"/> for a metadata reference used to create this compilation.
        ''' </summary>
        ''' <returns><see cref="AssemblySymbol"/> or <see cref="ModuleSymbol"/> corresponding to the given reference or Nothing if there is none.</returns>
        ''' <remarks>
        ''' Uses object identity when comparing two references. 
        ''' </remarks>
        Friend Shadows Function GetAssemblyOrModuleSymbol(reference As MetadataReference) As Symbol
            If (reference Is Nothing) Then
                Throw New ArgumentNullException(NameOf(reference))
            End If

            If reference.Properties.Kind = MetadataImageKind.Assembly Then
                Return GetBoundReferenceManager().GetReferencedAssemblySymbol(reference)
            Else
                Debug.Assert(reference.Properties.Kind = MetadataImageKind.Module)
                Dim index As Integer = GetBoundReferenceManager().GetReferencedModuleIndex(reference)
                Return If(index < 0, Nothing, Me.Assembly.Modules(index))
            End If
        End Function

        Friend Overrides Function GetSymbolInternal(Of TSymbol As {Class, ISymbolInternal})(symbol As ISymbol) As TSymbol
            Return DirectCast(symbol, TSymbol)
        End Function

        ''' <summary>
        ''' Gets the <see cref="MetadataReference"/> that corresponds to the assembly symbol.
        ''' </summary>
        Friend Shadows Function GetMetadataReference(assemblySymbol As AssemblySymbol) As MetadataReference
            Return Me.GetBoundReferenceManager().GetMetadataReference(assemblySymbol)
        End Function

        Private Protected Overrides Function CommonGetMetadataReference(assemblySymbol As IAssemblySymbol) As MetadataReference
            Dim symbol = TryCast(assemblySymbol, AssemblySymbol)
            If symbol IsNot Nothing Then
                Return GetMetadataReference(symbol)
            End If

            Return Nothing
        End Function

        Public Overrides ReadOnly Property ReferencedAssemblyNames As IEnumerable(Of AssemblyIdentity)
            Get
                Return [Assembly].Modules.SelectMany(Function(m) m.GetReferencedAssemblies())
            End Get
        End Property

        Friend Overrides ReadOnly Property ReferenceDirectives As IEnumerable(Of ReferenceDirective)
            Get
                Return _declarationTable.ReferenceDirectives
            End Get
        End Property

        Public Overrides Function ToMetadataReference(Optional aliases As ImmutableArray(Of String) = Nothing, Optional embedInteropTypes As Boolean = False) As CompilationReference
            Return New VisualBasicCompilationReference(Me, aliases, embedInteropTypes)
        End Function

        Public Shadows Function AddReferences(ParamArray references As MetadataReference()) As VisualBasicCompilation
            Return DirectCast(MyBase.AddReferences(references), VisualBasicCompilation)
        End Function

        Public Shadows Function AddReferences(references As IEnumerable(Of MetadataReference)) As VisualBasicCompilation
            Return DirectCast(MyBase.AddReferences(references), VisualBasicCompilation)
        End Function

        Public Shadows Function RemoveReferences(ParamArray references As MetadataReference()) As VisualBasicCompilation
            Return DirectCast(MyBase.RemoveReferences(references), VisualBasicCompilation)
        End Function

        Public Shadows Function RemoveReferences(references As IEnumerable(Of MetadataReference)) As VisualBasicCompilation
            Return DirectCast(MyBase.RemoveReferences(references), VisualBasicCompilation)
        End Function

        Public Shadows Function RemoveAllReferences() As VisualBasicCompilation
            Return DirectCast(MyBase.RemoveAllReferences(), VisualBasicCompilation)
        End Function

        Public Shadows Function ReplaceReference(oldReference As MetadataReference, newReference As MetadataReference) As VisualBasicCompilation
            Return DirectCast(MyBase.ReplaceReference(oldReference, newReference), VisualBasicCompilation)
        End Function

        ''' <summary>
        ''' Determine if enum arrays can be initialized using block initialization.
        ''' </summary>
        ''' <returns>True if it's safe to use block initialization for enum arrays.</returns>
        ''' <remarks>
        ''' In NetFx 4.0, block array initializers do not work on all combinations of {32/64 X Debug/Retail} when array elements are enums.
        ''' This is fixed in 4.5 thus enabling block array initialization for a very common case.
        ''' We look for the presence of <see cref="System.Runtime.GCLatencyMode.SustainedLowLatency"/> which was introduced in .NET Framework 4.5
        ''' </remarks>
        Friend ReadOnly Property EnableEnumArrayBlockInitialization As Boolean
            Get
                Dim sustainedLowLatency = GetWellKnownTypeMember(WellKnownMember.System_Runtime_GCLatencyMode__SustainedLowLatency)
                Return sustainedLowLatency IsNot Nothing AndAlso sustainedLowLatency.ContainingAssembly = Assembly.CorLibrary
            End Get
        End Property

#End Region

#Region "Symbols"

        Friend ReadOnly Property SourceAssembly As SourceAssemblySymbol
            Get
                GetBoundReferenceManager()
                Return _lazyAssemblySymbol
            End Get
        End Property

        ''' <summary>
        ''' Gets the AssemblySymbol that represents the assembly being created.
        ''' </summary>
        Friend Shadows ReadOnly Property Assembly As AssemblySymbol
            Get
                Return Me.SourceAssembly
            End Get
        End Property

        ''' <summary>
        ''' Get a ModuleSymbol that refers to the module being created by compiling all of the code. By
        ''' getting the GlobalNamespace property of that module, all of the namespace and types defined in source code 
        ''' can be obtained.
        ''' </summary>
        Friend Shadows ReadOnly Property SourceModule As ModuleSymbol
            Get
                Return Me.Assembly.Modules(0)
            End Get
        End Property

        ''' <summary>
        ''' Gets the merged root namespace that contains all namespaces and types defined in source code or in 
        ''' referenced metadata, merged into a single namespace hierarchy. This namespace hierarchy is how the compiler
        ''' binds types that are referenced in code.
        ''' </summary>
        Friend Shadows ReadOnly Property GlobalNamespace As NamespaceSymbol
            Get
                If _lazyGlobalNamespace Is Nothing Then
                    Interlocked.CompareExchange(_lazyGlobalNamespace, MergedNamespaceSymbol.CreateGlobalNamespace(Me), Nothing)
                End If

                Return _lazyGlobalNamespace
            End Get
        End Property

        ''' <summary>
        ''' Get the "root" or default namespace that all source types are declared inside. This may be the 
        ''' global namespace or may be another namespace. 
        ''' </summary>
        Friend ReadOnly Property RootNamespace As NamespaceSymbol
            Get
                Return DirectCast(Me.SourceModule, SourceModuleSymbol).RootNamespace
            End Get
        End Property

        ''' <summary>
        ''' Given a namespace symbol, returns the corresponding namespace symbol with Compilation extent
        ''' that refers to that namespace in this compilation. Returns Nothing if there is no corresponding 
        ''' namespace. This should not occur if the namespace symbol came from an assembly referenced by this
        ''' compilation. 
        ''' </summary>
        Friend Shadows Function GetCompilationNamespace(namespaceSymbol As INamespaceSymbol) As NamespaceSymbol
            If namespaceSymbol Is Nothing Then
                Throw New ArgumentNullException(NameOf(namespaceSymbol))
            End If

            Dim vbNs = TryCast(namespaceSymbol, NamespaceSymbol)
            If vbNs IsNot Nothing AndAlso vbNs.Extent.Kind = NamespaceKind.Compilation AndAlso vbNs.Extent.Compilation Is Me Then
                ' If we already have a namespace with the right extent, use that.
                Return vbNs
            ElseIf namespaceSymbol.ContainingNamespace Is Nothing Then
                ' If is the root namespace, return the merged root namespace
                Debug.Assert(namespaceSymbol.Name = "", "Namespace with Nothing container should be root namespace with empty name")
                Return GlobalNamespace
            Else
                Dim containingNs = GetCompilationNamespace(namespaceSymbol.ContainingNamespace)
                If containingNs Is Nothing Then
                    Return Nothing
                End If

                ' Get the child namespace of the given name, if any.
                Return containingNs.GetMembers(namespaceSymbol.Name).OfType(Of NamespaceSymbol)().FirstOrDefault()
            End If
        End Function

        Friend Shadows Function GetEntryPoint(cancellationToken As CancellationToken) As MethodSymbol
            Dim entryPoint As EntryPoint = GetEntryPointAndDiagnostics(cancellationToken)
            Return If(entryPoint Is Nothing, Nothing, entryPoint.MethodSymbol)
        End Function

        Friend Function GetEntryPointAndDiagnostics(cancellationToken As CancellationToken) As EntryPoint
            If Not Me.Options.OutputKind.IsApplication() AndAlso ScriptClass Is Nothing Then
                Return Nothing
            End If

            If Me.Options.MainTypeName IsNot Nothing AndAlso Not Me.Options.MainTypeName.IsValidClrTypeName() Then
                Debug.Assert(Not Me.Options.Errors.IsDefaultOrEmpty)
                Return New EntryPoint(Nothing, ImmutableArray(Of Diagnostic).Empty)
            End If

            If _lazyEntryPoint Is Nothing Then
                Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
                Dim entryPoint = FindEntryPoint(cancellationToken, diagnostics)
                Interlocked.CompareExchange(_lazyEntryPoint, New EntryPoint(entryPoint, diagnostics), Nothing)
            End If

            Return _lazyEntryPoint
        End Function

        Private Function FindEntryPoint(cancellationToken As CancellationToken, ByRef sealedDiagnostics As ImmutableArray(Of Diagnostic)) As MethodSymbol
            Dim diagnostics = DiagnosticBag.GetInstance()
            Dim entryPointCandidates = ArrayBuilder(Of MethodSymbol).GetInstance()

            Try
                Dim mainType As SourceMemberContainerTypeSymbol

                Dim mainTypeName As String = Me.Options.MainTypeName
                Dim globalNamespace As NamespaceSymbol = Me.SourceModule.GlobalNamespace

                Dim errorTarget As Object

                If mainTypeName IsNot Nothing Then
                    ' Global code is the entry point, ignore all other Mains.
                    If ScriptClass IsNot Nothing Then
                        ' CONSIDER: we could use the symbol instead of just the name.
                        diagnostics.Add(ERRID.WRN_MainIgnored, NoLocation.Singleton, mainTypeName)
                        Return ScriptClass.GetScriptEntryPoint()
                    End If

                    Dim mainTypeOrNamespace = globalNamespace.GetNamespaceOrTypeByQualifiedName(mainTypeName.Split("."c)).OfType(Of NamedTypeSymbol)().OfMinimalArity()
                    If mainTypeOrNamespace Is Nothing Then
                        diagnostics.Add(ERRID.ERR_StartupCodeNotFound1, NoLocation.Singleton, mainTypeName)
                        Return Nothing
                    End If

                    mainType = TryCast(mainTypeOrNamespace, SourceMemberContainerTypeSymbol)
                    If mainType Is Nothing OrElse (mainType.TypeKind <> TYPEKIND.Class AndAlso mainType.TypeKind <> TYPEKIND.Structure AndAlso mainType.TypeKind <> TYPEKIND.Module) Then
                        diagnostics.Add(ERRID.ERR_StartupCodeNotFound1, NoLocation.Singleton, mainType)
                        Return Nothing
                    End If

                    ' Dev10 reports ERR_StartupCodeNotFound1 but that doesn't make much sense
                    If mainType.IsGenericType Then
                        diagnostics.Add(ERRID.ERR_GenericSubMainsFound1, NoLocation.Singleton, mainType)
                        Return Nothing
                    End If

                    errorTarget = mainType

                    ' NOTE: unlike in C#, we're not going search the member list of mainType directly.
                    ' Instead, we're going to mimic dev10's behavior by doing a lookup for "Main",
                    ' starting in mainType.  Among other things, this implies that the entrypoint
                    ' could be in a base class and that it could be hidden by a non-method member
                    ' named "Main".

                    Dim binder As Binder = BinderBuilder.CreateBinderForType(mainType.ContainingSourceModule, mainType.SyntaxReferences(0).SyntaxTree, mainType)
                    Dim lookupResult As LookupResult = lookupResult.GetInstance()
                    Dim entryPointLookupOptions As LookupOptions = LookupOptions.AllMethodsOfAnyArity Or LookupOptions.IgnoreExtensionMethods
                    binder.LookupMember(lookupResult, mainType, WellKnownMemberNames.EntryPointMethodName, arity:=0, options:=entryPointLookupOptions, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)

                    If (Not lookupResult.IsGoodOrAmbiguous) OrElse lookupResult.Symbols(0).Kind <> SymbolKind.Method Then
                        diagnostics.Add(ERRID.ERR_StartupCodeNotFound1, NoLocation.Singleton, mainType)
                        lookupResult.Free()
                        Return Nothing
                    End If

                    For Each candidate In lookupResult.Symbols
                        ' The entrypoint cannot be in another assembly.
                        ' NOTE: filter these out here, rather than below, so that we
                        ' report "not found", rather than "invalid", as in dev10.
                        If candidate.ContainingAssembly = Me.Assembly Then
                            entryPointCandidates.Add(DirectCast(candidate, MethodSymbol))
                        End If
                    Next

                    lookupResult.Free()

                Else
                    mainType = Nothing

                    errorTarget = Me.AssemblyName
                    For Each candidate In Me.GetSymbolsWithName(WellKnownMemberNames.EntryPointMethodName, SymbolFilter.Member, cancellationToken)
                        Dim method = TryCast(candidate, MethodSymbol)
                        If method?.IsEntryPointCandidate = True Then
                            entryPointCandidates.Add(method)
                        End If
                    Next

                    ' Global code is the entry point, ignore all other Mains.
                    If ScriptClass IsNot Nothing Then
                        For Each main In entryPointCandidates
                            diagnostics.Add(ERRID.WRN_MainIgnored, main.Locations.First(), main)
                        Next
                        Return ScriptClass.GetScriptEntryPoint()
                    End If
                End If

                If entryPointCandidates.Count = 0 Then
                    diagnostics.Add(ERRID.ERR_StartupCodeNotFound1, NoLocation.Singleton, errorTarget)
                    Return Nothing
                End If

                Dim hasViableGenericEntryPoints As Boolean = False
                Dim viableEntryPoints = ArrayBuilder(Of MethodSymbol).GetInstance()

                For Each candidate In entryPointCandidates
                    If Not candidate.IsViableMainMethod Then
                        Continue For
                    End If

                    If candidate.IsGenericMethod OrElse candidate.ContainingType.IsGenericType Then
                        hasViableGenericEntryPoints = True
                    Else
                        viableEntryPoints.Add(candidate)
                    End If
                Next

                Dim entryPoint As MethodSymbol = Nothing
                If viableEntryPoints.Count = 0 Then
                    If hasViableGenericEntryPoints Then
                        diagnostics.Add(ERRID.ERR_GenericSubMainsFound1, NoLocation.Singleton, errorTarget)
                    Else
                        diagnostics.Add(ERRID.ERR_InValidSubMainsFound1, NoLocation.Singleton, errorTarget)
                    End If
                ElseIf viableEntryPoints.Count > 1 Then
                    viableEntryPoints.Sort(LexicalOrderSymbolComparer.Instance)
                    diagnostics.Add(ERRID.ERR_MoreThanOneValidMainWasFound2,
                                        NoLocation.Singleton,
                                        Me.AssemblyName,
                                        New FormattedSymbolList(viableEntryPoints.ToArray(), CustomSymbolDisplayFormatter.ErrorMessageFormatNoModifiersNoReturnType))
                Else
                    entryPoint = viableEntryPoints(0)

                    If entryPoint.IsAsync Then
                        ' The rule we follow:
                        ' First determine the Sub Main using pre-async rules, and give the pre-async errors if there were 0 or >1 results
                        ' If there was exactly one result, but it was async, then give an error. Otherwise proceed.
                        ' This doesn't follow the same pattern as "error due to being generic". That's because
                        ' maybe one day we'll want to allow Async Sub Main but without breaking back-compat.                    
                        Dim sourceMethod = TryCast(entryPoint, SourceMemberMethodSymbol)
                        Debug.Assert(sourceMethod IsNot Nothing)

                        If sourceMethod IsNot Nothing Then
                            Dim location As Location = sourceMethod.NonMergedLocation
                            Debug.Assert(location IsNot Nothing)

                            If location IsNot Nothing Then
                                Binder.ReportDiagnostic(diagnostics, location, ERRID.ERR_AsyncSubMain)
                            End If
                        End If
                    End If
                End If

                viableEntryPoints.Free()
                Return entryPoint

            Finally
                entryPointCandidates.Free()
                sealedDiagnostics = diagnostics.ToReadOnlyAndFree()
            End Try
        End Function

        Friend Class EntryPoint
            Public ReadOnly MethodSymbol As MethodSymbol
            Public ReadOnly Diagnostics As ImmutableArray(Of Diagnostic)

            Public Sub New(methodSymbol As MethodSymbol, diagnostics As ImmutableArray(Of Diagnostic))
                Me.MethodSymbol = methodSymbol
                Me.Diagnostics = diagnostics
            End Sub
        End Class

        ''' <summary>
        ''' Returns the list of member imports that apply to all syntax trees in this compilation.
        ''' </summary>
        Friend ReadOnly Property MemberImports As ImmutableArray(Of NamespaceOrTypeSymbol)
            Get
                Return DirectCast(Me.SourceModule, SourceModuleSymbol).MemberImports.SelectAsArray(Function(m) m.NamespaceOrType)
            End Get
        End Property

        ''' <summary>
        ''' Returns the list of alias imports that apply to all syntax trees in this compilation.
        ''' </summary>
        Friend ReadOnly Property AliasImports As ImmutableArray(Of AliasSymbol)
            Get
                Return DirectCast(Me.SourceModule, SourceModuleSymbol).AliasImports.SelectAsArray(Function(a) a.Alias)
            End Get
        End Property

        Friend Overrides Sub ReportUnusedImports(diagnostics As DiagnosticBag, cancellationToken As CancellationToken)
            Dim builder = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)
            ReportUnusedImports(filterTree:=Nothing, builder, cancellationToken)
            diagnostics.AddRange(builder.DiagnosticBag)
            builder.Free()
        End Sub

        Private Overloads Sub ReportUnusedImports(filterTree As SyntaxTree, diagnostics As BindingDiagnosticBag, cancellationToken As CancellationToken)
            If _lazyImportInfos IsNot Nothing AndAlso (filterTree Is Nothing OrElse ReportUnusedImportsInTree(filterTree)) Then
                Dim unusedBuilder As ArrayBuilder(Of TextSpan) = Nothing

                For Each info As ImportInfo In _lazyImportInfos
                    cancellationToken.ThrowIfCancellationRequested()

                    Dim infoTree As SyntaxTree = info.Tree
                    If (filterTree Is Nothing OrElse filterTree Is infoTree) AndAlso ReportUnusedImportsInTree(infoTree) Then
                        Dim clauseSpans = info.ClauseSpans
                        Dim numClauseSpans = clauseSpans.Length

                        If numClauseSpans = 1 Then
                            ' Do less work in common case (one clause per statement).
                            If Not Me.IsImportDirectiveUsed(infoTree, clauseSpans(0).Start) Then
                                diagnostics.Add(ERRID.HDN_UnusedImportStatement, infoTree.GetLocation(info.StatementSpan))
                            Else
                                AddImportsDependencies(diagnostics, infoTree, clauseSpans(0))
                            End If
                        Else
                            If unusedBuilder IsNot Nothing Then
                                unusedBuilder.Clear()
                            End If

                            For Each clauseSpan In info.ClauseSpans
                                If Not Me.IsImportDirectiveUsed(infoTree, clauseSpan.Start) Then
                                    If unusedBuilder Is Nothing Then
                                        unusedBuilder = ArrayBuilder(Of TextSpan).GetInstance()
                                    End If
                                    unusedBuilder.Add(clauseSpan)
                                Else
                                    AddImportsDependencies(diagnostics, infoTree, clauseSpan)
                                End If
                            Next

                            If unusedBuilder IsNot Nothing AndAlso unusedBuilder.Count > 0 Then
                                If unusedBuilder.Count = numClauseSpans Then
                                    diagnostics.Add(ERRID.HDN_UnusedImportStatement, infoTree.GetLocation(info.StatementSpan))
                                Else
                                    For Each clauseSpan In unusedBuilder
                                        diagnostics.Add(ERRID.HDN_UnusedImportClause, infoTree.GetLocation(clauseSpan))
                                    Next
                                End If
                            End If
                        End If
                    End If
                Next

                If unusedBuilder IsNot Nothing Then
                    unusedBuilder.Free()
                End If
            End If

            CompleteTrees(filterTree)
        End Sub

        Private Sub AddImportsDependencies(diagnostics As BindingDiagnosticBag, infoTree As SyntaxTree, clauseSpan As TextSpan)
            Dim dependencies As ImmutableArray(Of AssemblySymbol) = Nothing

            If diagnostics.AccumulatesDependencies AndAlso _lazyImportClauseDependencies IsNot Nothing AndAlso
               _lazyImportClauseDependencies.TryGetValue((infoTree, clauseSpan.Start), dependencies) Then
                diagnostics.AddDependencies(dependencies)
            End If
        End Sub

        Friend Overrides Sub CompleteTrees(filterTree As SyntaxTree)
            ' By definition, a tree Is complete when all of its compiler diagnostics have been reported.
            ' Since unused imports are the last thing we compute And report, a tree Is complete when
            ' the unused imports have been reported.
            If EventQueue IsNot Nothing Then
                If filterTree IsNot Nothing Then
                    CompleteTree(filterTree)
                Else
                    For Each tree As SyntaxTree In SyntaxTrees
                        CompleteTree(tree)
                    Next
                End If
            End If
        End Sub

        Private Sub CompleteTree(tree As SyntaxTree)
            If tree.IsEmbeddedOrMyTemplateTree Then
                ' The syntax trees added to AllSyntaxTrees by the compiler
                ' do not count toward completion.
                Return
            End If

            Debug.Assert(AllSyntaxTrees.Contains(tree))

            If _lazyCompilationUnitCompletedTrees Is Nothing Then
                Interlocked.CompareExchange(_lazyCompilationUnitCompletedTrees, New HashSet(Of SyntaxTree)(), Nothing)
            End If

            SyncLock _lazyCompilationUnitCompletedTrees
                If _lazyCompilationUnitCompletedTrees.Add(tree) Then
                    ' signal the end of the compilation unit
                    EventQueue.TryEnqueue(New CompilationUnitCompletedEvent(Me, tree))

                    If _lazyCompilationUnitCompletedTrees.Count = SyntaxTrees.Length Then
                        ' if that was the last tree, signal the end of compilation
                        CompleteCompilationEventQueue_NoLock()
                    End If
                End If
            End SyncLock
        End Sub

        Friend Function ShouldAddEvent(symbol As Symbol) As Boolean
            Return EventQueue IsNot Nothing AndAlso symbol.IsInSource()
        End Function

        Friend Sub SymbolDeclaredEvent(symbol As Symbol)
            If ShouldAddEvent(symbol) Then
                EventQueue.TryEnqueue(New SymbolDeclaredCompilationEvent(Me, symbol))
            End If
        End Sub

        Friend Sub RecordImportsClauseDependencies(syntaxTree As SyntaxTree, importsClausePosition As Integer, dependencies As ImmutableArray(Of AssemblySymbol))
            If Not dependencies.IsDefaultOrEmpty Then
                LazyInitializer.EnsureInitialized(_lazyImportClauseDependencies).TryAdd((syntaxTree, importsClausePosition), dependencies)
            End If
        End Sub

        Friend Sub RecordImports(syntax As ImportsStatementSyntax)
            LazyInitializer.EnsureInitialized(_lazyImportInfos).Enqueue(New ImportInfo(syntax))
        End Sub

        Private Structure ImportInfo
            Public ReadOnly Tree As SyntaxTree
            Public ReadOnly StatementSpan As TextSpan
            Public ReadOnly ClauseSpans As ImmutableArray(Of TextSpan)

            ' CONSIDER: ClauseSpans will usually be a singleton.  If we're
            ' creating too much garbage, it might be worthwhile to store
            ' a single clause span in a separate field.

            Public Sub New(syntax As ImportsStatementSyntax)
                Me.Tree = syntax.SyntaxTree
                Me.StatementSpan = syntax.Span

                Dim builder = ArrayBuilder(Of TextSpan).GetInstance()

                For Each clause In syntax.ImportsClauses
                    builder.Add(clause.Span)
                Next

                Me.ClauseSpans = builder.ToImmutableAndFree()
            End Sub

        End Structure

        Friend ReadOnly Property DeclaresTheObjectClass As Boolean
            Get
                Return SourceAssembly.DeclaresTheObjectClass
            End Get
        End Property

        Friend Function MightContainNoPiaLocalTypes() As Boolean
            Return SourceAssembly.MightContainNoPiaLocalTypes()
        End Function

        ' NOTE(cyrusn): There is a bit of a discoverability problem with this method and the same
        ' named method in SyntaxTreeSemanticModel.  Technically, i believe these are the appropriate
        ' locations for these methods.  This method has no dependencies on anything but the
        ' compilation, while the other method needs a bindings object to determine what bound node
        ' an expression syntax binds to.  Perhaps when we document these methods we should explain
        ' where a user can find the other.

        ''' <summary>
        ''' Determine what kind of conversion, if any, there is between the types 
        ''' "source" and "destination".
        ''' </summary>
        Public Shadows Function ClassifyConversion(source As ITypeSymbol, destination As ITypeSymbol) As Conversion
            If source Is Nothing Then
                Throw New ArgumentNullException(NameOf(source))
            End If

            If destination Is Nothing Then
                Throw New ArgumentNullException(NameOf(destination))
            End If

            Dim vbsource = source.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(source))
            Dim vbdest = destination.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(destination))

            If vbsource.IsErrorType() OrElse vbdest.IsErrorType() Then
                Return New Conversion(Nothing) ' No conversion
            End If

            Return New Conversion(Conversions.ClassifyConversion(vbsource, vbdest, CompoundUseSiteInfo(Of AssemblySymbol).Discarded))
        End Function

        Public Overrides Function ClassifyCommonConversion(source As ITypeSymbol, destination As ITypeSymbol) As CommonConversion
            Return ClassifyConversion(source, destination).ToCommonConversion()
        End Function

        Friend Overrides Function ClassifyConvertibleConversion(source As IOperation, destination As ITypeSymbol, ByRef constantValue As ConstantValue) As IConvertibleConversion
            constantValue = Nothing

            If destination Is Nothing Then
                Return New Conversion(Nothing) ' No conversion
            End If

            Dim sourceType As ITypeSymbol = source.Type

            Dim sourceConstantValue As ConstantValue = source.GetConstantValue()
            If sourceType Is Nothing Then
                If sourceConstantValue IsNot Nothing AndAlso sourceConstantValue.IsNothing AndAlso destination.IsReferenceType Then
                    constantValue = sourceConstantValue
                    Return New Conversion(New KeyValuePair(Of ConversionKind, MethodSymbol)(ConversionKind.WideningNothingLiteral, Nothing))
                End If

                Return New Conversion(Nothing) ' No conversion
            End If

            Dim result As Conversion = ClassifyConversion(sourceType, destination)

            If result.IsReference AndAlso sourceConstantValue IsNot Nothing AndAlso sourceConstantValue.IsNothing Then
                constantValue = sourceConstantValue
            End If

            Return result
        End Function

        ''' <summary>
        ''' A symbol representing the implicit Script class. This is null if the class is not
        ''' defined in the compilation.
        ''' </summary>
        Friend Shadows ReadOnly Property ScriptClass As NamedTypeSymbol
            Get
                Return SourceScriptClass
            End Get
        End Property

        Friend ReadOnly Property SourceScriptClass As ImplicitNamedTypeSymbol
            Get
                Return _scriptClass.Value
            End Get
        End Property

        ''' <summary>
        ''' Resolves a symbol that represents script container (Script class). 
        ''' Uses the full name of the container class stored in <see cref="CompilationOptions.ScriptClassName"/>  to find the symbol.
        ''' </summary> 
        ''' <returns>
        ''' The Script class symbol or null if it is not defined.
        ''' </returns>
        Private Function BindScriptClass() As ImplicitNamedTypeSymbol
            Return DirectCast(CommonBindScriptClass(), ImplicitNamedTypeSymbol)
        End Function

        ''' <summary>
        ''' Get symbol for predefined type from Cor Library referenced by this compilation.
        ''' </summary>
        Friend Shadows Function GetSpecialType(typeId As SpecialType) As NamedTypeSymbol
            Dim result = Assembly.GetSpecialType(typeId)
            Debug.Assert(result.SpecialType = typeId)
            Return result
        End Function

        ''' <summary>
        ''' Get symbol for predefined type member from Cor Library referenced by this compilation.
        ''' </summary>
        Friend Shadows Function GetSpecialTypeMember(memberId As SpecialMember) As Symbol
            Return Assembly.GetSpecialTypeMember(memberId)
        End Function

        Friend Overrides Function CommonGetSpecialTypeMember(specialMember As SpecialMember) As ISymbolInternal
            Return GetSpecialTypeMember(specialMember)
        End Function

        Friend Function GetTypeByReflectionType(type As Type) As TypeSymbol
            ' TODO: See CSharpCompilation.GetTypeByReflectionType
            Return GetSpecialType(SpecialType.System_Object)
        End Function

        ''' <summary>
        ''' Lookup a type within the compilation's assembly and all referenced assemblies
        ''' using its canonical CLR metadata name (names are compared case-sensitively).
        ''' </summary>
        ''' <param name="fullyQualifiedMetadataName">
        ''' </param>
        ''' <returns>
        ''' Symbol for the type or null if type cannot be found or is ambiguous. 
        ''' </returns>
        Friend Shadows Function GetTypeByMetadataName(fullyQualifiedMetadataName As String) As NamedTypeSymbol
            Return Me.Assembly.GetTypeByMetadataName(fullyQualifiedMetadataName, includeReferences:=True, isWellKnownType:=False, conflicts:=Nothing)
        End Function

        Friend Shadows ReadOnly Property ObjectType As NamedTypeSymbol
            Get
                Return Assembly.ObjectType
            End Get
        End Property

        Friend Shadows Function CreateArrayTypeSymbol(elementType As TypeSymbol, Optional rank As Integer = 1) As ArrayTypeSymbol
            If elementType Is Nothing Then
                Throw New ArgumentNullException(NameOf(elementType))
            End If

            If rank < 1 Then
                Throw New ArgumentException(NameOf(rank))
            End If

            Return ArrayTypeSymbol.CreateVBArray(elementType, Nothing, rank, Me)
        End Function

        Friend ReadOnly Property HasTupleNamesAttributes As Boolean
            Get
                Dim constructorSymbol = TryCast(GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames), MethodSymbol)
                Return constructorSymbol IsNot Nothing AndAlso
                       Binder.GetUseSiteInfoForWellKnownTypeMember(constructorSymbol, WellKnownMember.System_Runtime_CompilerServices_TupleElementNamesAttribute__ctorTransformNames,
                                                                   embedVBRuntimeUsed:=False).DiagnosticInfo Is Nothing
            End Get
        End Property

        Private Protected Overrides Function IsSymbolAccessibleWithinCore(symbol As ISymbol, within As ISymbol, throughType As ITypeSymbol) As Boolean
            Dim symbol0 = symbol.EnsureVbSymbolOrNothing(Of Symbol)(NameOf(symbol))
            Dim within0 = within.EnsureVbSymbolOrNothing(Of Symbol)(NameOf(within))
            Dim throughType0 = throughType.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(throughType))
            Return If(within0.Kind = SymbolKind.Assembly,
                AccessCheck.IsSymbolAccessible(symbol0, DirectCast(within0, AssemblySymbol), useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded),
                AccessCheck.IsSymbolAccessible(symbol0, DirectCast(within0, NamedTypeSymbol), throughType0, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded))
        End Function

        <Obsolete("Compilation.IsSymbolAccessibleWithin is not designed for use within the compilers", True)>
        Friend Shadows Function IsSymbolAccessibleWithin(symbol As ISymbol, within As ISymbol, Optional throughType As ITypeSymbol = Nothing) As Boolean
            Throw New NotImplementedException
        End Function

#End Region

#Region "Binding"

        '''<summary> 
        ''' Get a fresh SemanticModel.  Note that each invocation gets a fresh SemanticModel, each of
        ''' which has a cache.  Therefore, one effectively clears the cache by discarding the
        ''' SemanticModel.
        '''</summary> 
        Public Shadows Function GetSemanticModel(syntaxTree As SyntaxTree, Optional ignoreAccessibility As Boolean = False) As SemanticModel
            Dim model As SemanticModel = Nothing
            If SemanticModelProvider IsNot Nothing Then
                model = SemanticModelProvider.GetSemanticModel(syntaxTree, Me, ignoreAccessibility)
                Debug.Assert(model IsNot Nothing)
            End If

            Return If(model, CreateSemanticModel(syntaxTree, ignoreAccessibility))
        End Function

        Friend Overrides Function CreateSemanticModel(syntaxTree As SyntaxTree, ignoreAccessibility As Boolean) As SemanticModel
            Return New SyntaxTreeSemanticModel(Me, DirectCast(Me.SourceModule, SourceModuleSymbol), syntaxTree, ignoreAccessibility)
        End Function

        Friend ReadOnly Property FeatureStrictEnabled As Boolean
            Get
                Return Me.Feature("strict") IsNot Nothing
            End Get
        End Property

#End Region

#Region "Diagnostics"

        Friend Overrides ReadOnly Property MessageProvider As CommonMessageProvider
            Get
                Return VisualBasic.MessageProvider.Instance
            End Get
        End Property

        ''' <summary>
        ''' Get all diagnostics for the entire compilation. This includes diagnostics from parsing, declarations, and
        ''' the bodies of methods. Getting all the diagnostics is potentially a length operations, as it requires parsing and
        ''' compiling all the code. The set of diagnostics is not caches, so each call to this method will recompile all
        ''' methods.
        ''' </summary>
        ''' <param name="cancellationToken">Cancellation token to allow cancelling the operation.</param>
        Public Overrides Function GetDiagnostics(Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Return GetDiagnostics(DefaultDiagnosticsStage, True, cancellationToken)
        End Function

        ''' <summary>
        ''' Get parse diagnostics for the entire compilation. This includes diagnostics from parsing BUT NOT from declarations and
        ''' the bodies of methods or initializers. The set of parse diagnostics is cached, so calling this method a second time
        ''' should be fast.
        ''' </summary>
        Public Overrides Function GetParseDiagnostics(Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Return GetDiagnostics(CompilationStage.Parse, False, cancellationToken)
        End Function

        ''' <summary>
        ''' Get declarations diagnostics for the entire compilation. This includes diagnostics from declarations, BUT NOT
        ''' the bodies of methods or initializers. The set of declaration diagnostics is cached, so calling this method a second time
        ''' should be fast.
        ''' </summary>
        ''' <param name="cancellationToken">Cancellation token to allow cancelling the operation.</param>
        Public Overrides Function GetDeclarationDiagnostics(Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Return GetDiagnostics(CompilationStage.Declare, False, cancellationToken)
        End Function

        ''' <summary>
        ''' Get method body diagnostics for the entire compilation. This includes diagnostics only from 
        ''' the bodies of methods and initializers. These diagnostics are NOT cached, so calling this method a second time
        ''' repeats significant work.
        ''' </summary>
        ''' <param name="cancellationToken">Cancellation token to allow cancelling the operation.</param>
        Public Overrides Function GetMethodBodyDiagnostics(Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Return GetDiagnostics(CompilationStage.Compile, False, cancellationToken)
        End Function

        ''' <summary>
        ''' Get all errors in the compilation, up through the given compilation stage. Note that this may
        ''' require significant work by the compiler, as all source code must be compiled to the given
        ''' level in order to get the errors. Errors on Options should be inspected by the user prior to constructing the compilation.
        ''' </summary>
        ''' <returns>
        ''' Returns all errors. The errors are not sorted in any particular order, and the client
        ''' should sort the errors as desired.
        ''' </returns>
        Friend Overloads Function GetDiagnostics(stage As CompilationStage, Optional includeEarlierStages As Boolean = True, Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            Dim diagnostics = DiagnosticBag.GetInstance()
            GetDiagnostics(stage, includeEarlierStages, diagnostics, cancellationToken)
            Return diagnostics.ToReadOnlyAndFree()
        End Function

        Friend Overrides Sub GetDiagnostics(stage As CompilationStage,
                                             includeEarlierStages As Boolean,
                                             diagnostics As DiagnosticBag,
                                             Optional cancellationToken As CancellationToken = Nothing)

            Dim builder = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)

            GetDiagnosticsWithoutFiltering(stage, includeEarlierStages, builder, cancellationToken)

            ' Before returning diagnostics, we filter some of them
            ' to honor the compiler options (e.g., /nowarn and /warnaserror)
            FilterAndAppendDiagnostics(diagnostics, builder.DiagnosticBag, cancellationToken)
            builder.Free()
        End Sub

        Private Sub GetDiagnosticsWithoutFiltering(stage As CompilationStage,
                                                   includeEarlierStages As Boolean,
                                                   builder As BindingDiagnosticBag,
                                                   Optional cancellationToken As CancellationToken = Nothing)

            Debug.Assert(builder.AccumulatesDiagnostics)

            ' Add all parsing errors.
            If (stage = CompilationStage.Parse OrElse stage > CompilationStage.Parse AndAlso includeEarlierStages) Then

                ' Embedded trees shouldn't have any errors, let's avoid making decision if they should be added too early.
                ' Otherwise IDE performance might be affect.
                If Options.ConcurrentBuild Then
                    RoslynParallel.For(
                        0,
                        SyntaxTrees.Length,
                        UICultureUtilities.WithCurrentUICulture(
                            Sub(i As Integer)
                                builder.AddRange(SyntaxTrees(i).GetDiagnostics(cancellationToken))
                            End Sub),
                        cancellationToken)
                Else
                    For Each tree In SyntaxTrees
                        cancellationToken.ThrowIfCancellationRequested()
                        builder.AddRange(tree.GetDiagnostics(cancellationToken))
                    Next
                End If

                Dim parseOptionsReported = New HashSet(Of ParseOptions)
                If Options.ParseOptions IsNot Nothing Then
                    parseOptionsReported.Add(Options.ParseOptions) ' This is reported in Options.Errors at CompilationStage.Declare
                End If

                For Each tree In SyntaxTrees
                    cancellationToken.ThrowIfCancellationRequested()
                    If Not tree.Options.Errors.IsDefaultOrEmpty AndAlso parseOptionsReported.Add(tree.Options) Then
                        Dim location = tree.GetLocation(TextSpan.FromBounds(0, 0))
                        For Each err In tree.Options.Errors
                            builder.Add(err.WithLocation(location))
                        Next
                    End If
                Next
            End If

            ' Add declaration errors
            If (stage = CompilationStage.Declare OrElse stage > CompilationStage.Declare AndAlso includeEarlierStages) Then
                CheckAssemblyName(builder.DiagnosticBag)
                builder.AddRange(Options.Errors)
                builder.AddRange(GetBoundReferenceManager().Diagnostics)
                SourceAssembly.GetAllDeclarationErrors(builder, cancellationToken)
                AddClsComplianceDiagnostics(builder, cancellationToken)

                If EventQueue IsNot Nothing AndAlso SyntaxTrees.Length = 0 Then
                    EnsureCompilationEventQueueCompleted()
                End If
            End If

            ' Add method body compilation errors.
            If (stage = CompilationStage.Compile OrElse stage > CompilationStage.Compile AndAlso includeEarlierStages) Then
                ' Note: this phase does not need to be parallelized because 
                '       it is already implemented in method compiler
                Dim methodBodyDiagnostics = If(builder.AccumulatesDependencies, BindingDiagnosticBag.GetConcurrentInstance(), BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False))

                GetDiagnosticsForAllMethodBodies(builder.HasAnyErrors(), methodBodyDiagnostics, doLowering:=False, cancellationToken)
                builder.AddRange(methodBodyDiagnostics)
                methodBodyDiagnostics.Free()
            End If
        End Sub

        Private Sub AddClsComplianceDiagnostics(diagnostics As BindingDiagnosticBag, cancellationToken As CancellationToken, Optional filterTree As SyntaxTree = Nothing, Optional filterSpanWithinTree As TextSpan? = Nothing)
            If filterTree IsNot Nothing Then
                ClsComplianceChecker.CheckCompliance(Me, diagnostics, cancellationToken, filterTree, filterSpanWithinTree)
                Return
            End If

            Debug.Assert(filterSpanWithinTree Is Nothing)
            If _lazyClsComplianceDiagnostics.IsDefault OrElse _lazyClsComplianceDependencies.IsDefault Then
                Dim builder = BindingDiagnosticBag.GetInstance()
                ClsComplianceChecker.CheckCompliance(Me, builder, cancellationToken)
                Dim result As ImmutableBindingDiagnostic(Of AssemblySymbol) = builder.ToReadOnlyAndFree()
                ImmutableInterlocked.InterlockedInitialize(_lazyClsComplianceDependencies, result.Dependencies)
                ImmutableInterlocked.InterlockedInitialize(_lazyClsComplianceDiagnostics, result.Diagnostics)
            End If

            Debug.Assert(Not _lazyClsComplianceDependencies.IsDefault)
            Debug.Assert(Not _lazyClsComplianceDiagnostics.IsDefault)

            diagnostics.AddRange(New ImmutableBindingDiagnostic(Of AssemblySymbol)(_lazyClsComplianceDiagnostics, _lazyClsComplianceDependencies), allowMismatchInDependencyAccumulation:=True)
        End Sub

        Private Shared Iterator Function FilterDiagnosticsByLocation(diagnostics As IEnumerable(Of Diagnostic), tree As SyntaxTree, filterSpanWithinTree As TextSpan?) As IEnumerable(Of Diagnostic)
            For Each diagnostic In diagnostics
                If diagnostic.HasIntersectingLocation(tree, filterSpanWithinTree) Then
                    Yield diagnostic
                End If
            Next
        End Function

        Friend Function GetDiagnosticsForSyntaxTree(stage As CompilationStage,
                                              tree As SyntaxTree,
                                              filterSpanWithinTree As TextSpan?,
                                              includeEarlierStages As Boolean,
                                              Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of Diagnostic)
            If Not SyntaxTrees.Contains(tree) Then
                Throw New ArgumentException("Cannot GetDiagnosticsForSyntax for a tree that is not part of the compilation", NameOf(tree))
            End If

            Dim builder = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)

            If (stage = CompilationStage.Parse OrElse stage > CompilationStage.Parse AndAlso includeEarlierStages) Then
                ' Add all parsing errors.
                cancellationToken.ThrowIfCancellationRequested()
                Dim syntaxDiagnostics = tree.GetDiagnostics(cancellationToken)
                syntaxDiagnostics = FilterDiagnosticsByLocation(syntaxDiagnostics, tree, filterSpanWithinTree)
                builder.AddRange(syntaxDiagnostics)
            End If

            ' Add declaring errors
            If (stage = CompilationStage.Declare OrElse stage > CompilationStage.Declare AndAlso includeEarlierStages) Then
                Dim declarationDiags = DirectCast(SourceModule, SourceModuleSymbol).GetDeclarationErrorsInTree(tree, filterSpanWithinTree, AddressOf FilterDiagnosticsByLocation, cancellationToken)
                Dim filteredDiags = FilterDiagnosticsByLocation(declarationDiags, tree, filterSpanWithinTree)
                builder.AddRange(filteredDiags)
                AddClsComplianceDiagnostics(builder, cancellationToken, tree, filterSpanWithinTree)
            End If

            ' Add method body declaring errors.
            If (stage = CompilationStage.Compile OrElse stage > CompilationStage.Compile AndAlso includeEarlierStages) Then
                Dim methodBodyDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)
                GetDiagnosticsForMethodBodiesInTree(tree, filterSpanWithinTree, builder.HasAnyErrors(), methodBodyDiagnostics, cancellationToken)

                ' This diagnostics can include diagnostics for initializers that do not belong to the tree.
                ' Let's filter them out.
                If Not methodBodyDiagnostics.DiagnosticBag.IsEmptyWithoutResolution Then
                    Dim allDiags = methodBodyDiagnostics.DiagnosticBag.AsEnumerableWithoutResolution()
                    Dim filteredDiags = FilterDiagnosticsByLocation(allDiags, tree, filterSpanWithinTree)
                    For Each diag In filteredDiags
                        builder.Add(diag)
                    Next
                End If

                methodBodyDiagnostics.Free()
            End If

            Dim result = DiagnosticBag.GetInstance()
            FilterAndAppendDiagnostics(result, builder.DiagnosticBag, cancellationToken)
            builder.Free()
            Return result.ToReadOnlyAndFree(Of Diagnostic)()
        End Function

        ' Get diagnostics by compiling all method bodies.
        Private Sub GetDiagnosticsForAllMethodBodies(hasDeclarationErrors As Boolean, diagnostics As BindingDiagnosticBag, doLowering As Boolean, cancellationToken As CancellationToken)
            MethodCompiler.GetCompileDiagnostics(Me, SourceModule.GlobalNamespace, Nothing, Nothing, hasDeclarationErrors, diagnostics, doLowering, cancellationToken)
            DocumentationCommentCompiler.WriteDocumentationCommentXml(Me, Nothing, Nothing, diagnostics, cancellationToken)
            Me.ReportUnusedImports(Nothing, diagnostics, cancellationToken)
        End Sub

        ' Get diagnostics by compiling all method bodies in the given tree.
        Private Sub GetDiagnosticsForMethodBodiesInTree(tree As SyntaxTree, filterSpanWithinTree As TextSpan?, hasDeclarationErrors As Boolean, diagnostics As BindingDiagnosticBag, cancellationToken As CancellationToken)
            Dim sourceMod = DirectCast(SourceModule, SourceModuleSymbol)

            MethodCompiler.GetCompileDiagnostics(Me,
                                                 SourceModule.GlobalNamespace,
                                                 tree,
                                                 filterSpanWithinTree,
                                                 hasDeclarationErrors,
                                                 diagnostics,
                                                 doLoweringPhase:=False,
                                                 cancellationToken)

            DocumentationCommentCompiler.WriteDocumentationCommentXml(Me, Nothing, Nothing, diagnostics, cancellationToken, tree, filterSpanWithinTree)

            ' Report unused import diagnostics only if computing diagnostics for the entire tree.
            ' Otherwise we cannot determine if a particular directive is used outside of the given sub-span within the tree.
            If Not filterSpanWithinTree.HasValue OrElse filterSpanWithinTree.Value = tree.GetRoot(cancellationToken).FullSpan Then
                Me.ReportUnusedImports(tree, diagnostics, cancellationToken)
            End If
        End Sub

        Friend Overrides Function CreateAnalyzerDriver(analyzers As ImmutableArray(Of DiagnosticAnalyzer), analyzerManager As AnalyzerManager, severityFilter As SeverityFilter) As AnalyzerDriver
            Dim getKind As Func(Of SyntaxNode, SyntaxKind) = Function(node As SyntaxNode) node.Kind
            Dim isComment As Func(Of SyntaxTrivia, Boolean) = Function(trivia As SyntaxTrivia) trivia.Kind() = SyntaxKind.CommentTrivia
            Return New AnalyzerDriver(Of SyntaxKind)(analyzers, getKind, analyzerManager, severityFilter, isComment)
        End Function

#End Region

#Region "Resources"
        Protected Overrides Sub AppendDefaultVersionResource(resourceStream As Stream)
            Dim fileVersion As String = If(SourceAssembly.FileVersion, SourceAssembly.Identity.Version.ToString())

            'for some parameters, alink used to supply whitespace instead of null.
            Win32ResourceConversions.AppendVersionToResourceStream(resourceStream,
                Not Me.Options.OutputKind.IsApplication(),
                fileVersion:=fileVersion,
                originalFileName:=Me.SourceModule.Name,
                internalName:=Me.SourceModule.Name,
                productVersion:=If(SourceAssembly.InformationalVersion, fileVersion),
                assemblyVersion:=SourceAssembly.Identity.Version,
                fileDescription:=If(SourceAssembly.Title, " "),
                legalCopyright:=If(SourceAssembly.Copyright, " "),
                legalTrademarks:=SourceAssembly.Trademark,
                productName:=SourceAssembly.Product,
                comments:=SourceAssembly.Description,
                companyName:=SourceAssembly.Company)
        End Sub
#End Region

#Region "Emit"

        Friend Overrides ReadOnly Property LinkerMajorVersion As Byte
            Get
                Return &H50
            End Get
        End Property

        Friend Overrides ReadOnly Property IsDelaySigned As Boolean
            Get
                Return SourceAssembly.IsDelaySigned
            End Get
        End Property

        Friend Overrides ReadOnly Property StrongNameKeys As StrongNameKeys
            Get
                Return SourceAssembly.StrongNameKeys
            End Get
        End Property

        Friend Overrides Function CreateModuleBuilder(
            emitOptions As EmitOptions,
            debugEntryPoint As IMethodSymbol,
            sourceLinkStream As Stream,
            embeddedTexts As IEnumerable(Of EmbeddedText),
            manifestResources As IEnumerable(Of ResourceDescription),
            testData As CompilationTestData,
            diagnostics As DiagnosticBag,
            cancellationToken As CancellationToken) As CommonPEModuleBuilder

            Return CreateModuleBuilder(
                emitOptions,
                debugEntryPoint,
                sourceLinkStream,
                embeddedTexts,
                manifestResources,
                testData,
                diagnostics,
                ImmutableArray(Of NamedTypeSymbol).Empty,
                cancellationToken)
        End Function

        Friend Overloads Function CreateModuleBuilder(
            emitOptions As EmitOptions,
            debugEntryPoint As IMethodSymbol,
            sourceLinkStream As Stream,
            embeddedTexts As IEnumerable(Of EmbeddedText),
            manifestResources As IEnumerable(Of ResourceDescription),
            testData As CompilationTestData,
            diagnostics As DiagnosticBag,
            additionalTypes As ImmutableArray(Of NamedTypeSymbol),
            cancellationToken As CancellationToken) As CommonPEModuleBuilder

            Debug.Assert(Not IsSubmission OrElse HasCodeToEmit() OrElse
                         (emitOptions = EmitOptions.Default AndAlso debugEntryPoint Is Nothing AndAlso sourceLinkStream Is Nothing AndAlso
                          embeddedTexts Is Nothing AndAlso manifestResources Is Nothing AndAlso testData Is Nothing))

            ' Get the runtime metadata version from the cor library. If this fails we have no reasonable value to give.
            Dim runtimeMetadataVersion = GetRuntimeMetadataVersion()

            Dim moduleSerializationProperties = ConstructModuleSerializationProperties(emitOptions, runtimeMetadataVersion)
            If manifestResources Is Nothing Then
                manifestResources = SpecializedCollections.EmptyEnumerable(Of ResourceDescription)()
            End If

            ' if there is no stream to write to, then there is no need for a module
            Dim moduleBeingBuilt As PEModuleBuilder
            If Options.OutputKind.IsNetModule() Then
                Debug.Assert(additionalTypes.IsEmpty)

                moduleBeingBuilt = New PENetModuleBuilder(
                    DirectCast(Me.SourceModule, SourceModuleSymbol),
                    emitOptions,
                    moduleSerializationProperties,
                    manifestResources)
            Else
                Dim kind = If(Options.OutputKind.IsValid(), Options.OutputKind, OutputKind.DynamicallyLinkedLibrary)
                moduleBeingBuilt = New PEAssemblyBuilder(
                        SourceAssembly,
                        emitOptions,
                        kind,
                        moduleSerializationProperties,
                        manifestResources,
                        additionalTypes)
            End If

            If debugEntryPoint IsNot Nothing Then
                moduleBeingBuilt.SetDebugEntryPoint(DirectCast(debugEntryPoint, MethodSymbol), diagnostics)
            End If

            moduleBeingBuilt.SourceLinkStreamOpt = sourceLinkStream

            If embeddedTexts IsNot Nothing Then
                moduleBeingBuilt.EmbeddedTexts = embeddedTexts
            End If

            If testData IsNot Nothing Then
                moduleBeingBuilt.SetTestData(testData)
            End If

            Return moduleBeingBuilt
        End Function

        Friend Overrides Function CompileMethods(
            moduleBuilder As CommonPEModuleBuilder,
            emittingPdb As Boolean,
            diagnostics As DiagnosticBag,
            filterOpt As Predicate(Of ISymbolInternal),
            cancellationToken As CancellationToken) As Boolean

            Dim emitMetadataOnly = moduleBuilder.EmitOptions.EmitMetadataOnly

            ' The diagnostics should include syntax and declaration errors. We insert these before calling Emitter.Emit, so that we don't emit
            ' metadata if there are declaration errors or method body errors (but we do insert all errors from method body binding...)
            Dim hasDeclarationErrors = Not FilterAndAppendDiagnostics(diagnostics, GetDiagnostics(CompilationStage.Declare, True, cancellationToken), exclude:=Nothing, cancellationToken)

            Dim moduleBeingBuilt = DirectCast(moduleBuilder, PEModuleBuilder)

            Me.EmbeddedSymbolManager.MarkAllDeferredSymbolsAsReferenced(Me)

            ' The translation of global imports assumes absence of error symbols.
            ' We don't need to translate them if there are any declaration errors since 
            ' we are not going to emit the metadata.
            If Not hasDeclarationErrors Then
                moduleBeingBuilt.TranslateImports(diagnostics)
            End If

            If emitMetadataOnly Then
                If hasDeclarationErrors Then
                    Return False
                End If

                If moduleBeingBuilt.SourceModule.HasBadAttributes Then
                    ' If there were errors but no declaration diagnostics, explicitly add a "Failed to emit module" error.
                    diagnostics.Add(ERRID.ERR_ModuleEmitFailure, NoLocation.Singleton, moduleBeingBuilt.SourceModule.Name,
                        New LocalizableResourceString(NameOf(CodeAnalysisResources.ModuleHasInvalidAttributes), CodeAnalysisResources.ResourceManager, GetType(CodeAnalysisResources)))
                    Return False
                End If

                SynthesizedMetadataCompiler.ProcessSynthesizedMembers(Me, moduleBeingBuilt, cancellationToken)
            Else
                ' start generating PDB checksums if we need to emit PDBs
                If (emittingPdb OrElse moduleBuilder.EmitOptions.InstrumentationKinds.Contains(InstrumentationKind.TestCoverage)) AndAlso
                   Not CreateDebugDocuments(moduleBeingBuilt.DebugDocumentsBuilder, moduleBeingBuilt.EmbeddedTexts, diagnostics) Then
                    Return False
                End If

                ' Perform initial bind of method bodies in spite of earlier errors. This is the same
                ' behavior as when calling GetDiagnostics()

                ' Use a temporary bag so we don't have to refilter pre-existing diagnostics.
                Dim methodBodyDiagnosticBag = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)

                MethodCompiler.CompileMethodBodies(
                    Me,
                    moduleBeingBuilt,
                    emittingPdb,
                    hasDeclarationErrors,
                    filterOpt,
                    methodBodyDiagnosticBag,
                    cancellationToken)

                Dim hasMethodBodyErrors As Boolean = Not FilterAndAppendDiagnostics(diagnostics, methodBodyDiagnosticBag.DiagnosticBag, cancellationToken)
                methodBodyDiagnosticBag.Free()

                If hasDeclarationErrors OrElse hasMethodBodyErrors Then
                    Return False
                End If
            End If

            cancellationToken.ThrowIfCancellationRequested()

            ' TODO (tomat): XML doc comments diagnostics
            Return True
        End Function

        Friend Overrides Function GenerateResources(
            moduleBuilder As CommonPEModuleBuilder,
            win32Resources As Stream,
            useRawWin32Resources As Boolean,
            diagnostics As DiagnosticBag,
            cancellationToken As CancellationToken) As Boolean

            cancellationToken.ThrowIfCancellationRequested()

            ' Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            Dim resourceDiagnostics = DiagnosticBag.GetInstance()

            SetupWin32Resources(moduleBuilder, win32Resources, useRawWin32Resources, resourceDiagnostics)

            ' give the name of any added modules, but not the name of the primary module.
            ReportManifestResourceDuplicates(
                moduleBuilder.ManifestResources,
                SourceAssembly.Modules.Skip(1).Select(Function(x) x.Name),
                AddedModulesResourceNames(resourceDiagnostics),
                resourceDiagnostics)

            Return FilterAndAppendAndFreeDiagnostics(diagnostics, resourceDiagnostics, cancellationToken)
        End Function

        Friend Overrides Function GenerateDocumentationComments(
            xmlDocStream As Stream,
            outputNameOverride As String,
            diagnostics As DiagnosticBag,
            cancellationToken As CancellationToken) As Boolean

            cancellationToken.ThrowIfCancellationRequested()

            ' Use a temporary bag so we don't have to refilter pre-existing diagnostics.
            Dim xmlDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics:=True, withDependencies:=False)

            Dim assemblyName = FileNameUtilities.ChangeExtension(outputNameOverride, extension:=Nothing)
            DocumentationCommentCompiler.WriteDocumentationCommentXml(Me, assemblyName, xmlDocStream, xmlDiagnostics, cancellationToken)

            Dim result = FilterAndAppendDiagnostics(diagnostics, xmlDiagnostics.DiagnosticBag, cancellationToken)
            xmlDiagnostics.Free()
            Return result
        End Function

        Private Iterator Function AddedModulesResourceNames(diagnostics As DiagnosticBag) As IEnumerable(Of String)
            Dim modules As ImmutableArray(Of ModuleSymbol) = SourceAssembly.Modules

            For i As Integer = 1 To modules.Length - 1
                Dim m = DirectCast(modules(i), Symbols.Metadata.PE.PEModuleSymbol)

                Try
                    For Each resource In m.Module.GetEmbeddedResourcesOrThrow()
                        Yield resource.Name
                    Next
                Catch mrEx As BadImageFormatException
                    diagnostics.Add(ERRID.ERR_UnsupportedModule1, NoLocation.Singleton, m)
                End Try
            Next
        End Function

        Friend Overrides Function EmitDifference(
            baseline As EmitBaseline,
            edits As IEnumerable(Of SemanticEdit),
            isAddedSymbol As Func(Of ISymbol, Boolean),
            metadataStream As Stream,
            ilStream As Stream,
            pdbStream As Stream,
            testData As CompilationTestData,
            cancellationToken As CancellationToken) As EmitDifferenceResult

            Return EmitHelpers.EmitDifference(
                Me,
                baseline,
                edits,
                isAddedSymbol,
                metadataStream,
                ilStream,
                pdbStream,
                testData,
                cancellationToken)
        End Function

        Friend Function GetRuntimeMetadataVersion() As String
            Dim corLibrary = TryCast(Assembly.CorLibrary, Symbols.Metadata.PE.PEAssemblySymbol)
            Return If(corLibrary Is Nothing, String.Empty, corLibrary.Assembly.ManifestModule.MetadataVersion)
        End Function

        Friend Overrides Sub AddDebugSourceDocumentsForChecksumDirectives(
            documentsBuilder As DebugDocumentsBuilder,
            tree As SyntaxTree,
            diagnosticBag As DiagnosticBag)

            Dim checksumDirectives = tree.GetRoot().GetDirectives(Function(d) d.Kind = SyntaxKind.ExternalChecksumDirectiveTrivia AndAlso
                                                                              Not d.ContainsDiagnostics)

            For Each directive In checksumDirectives
                Dim checksumDirective As ExternalChecksumDirectiveTriviaSyntax = DirectCast(directive, ExternalChecksumDirectiveTriviaSyntax)
                Dim path = checksumDirective.ExternalSource.ValueText

                Dim checkSumText = checksumDirective.Checksum.ValueText
                Dim normalizedPath = documentsBuilder.NormalizeDebugDocumentPath(path, basePath:=tree.FilePath)
                Dim existingDoc = documentsBuilder.TryGetDebugDocumentForNormalizedPath(normalizedPath)

                If existingDoc IsNot Nothing Then
                    ' directive matches a file path on an actual tree.
                    ' Dev12 compiler just ignores the directive in this case which means that
                    ' checksum of the actual tree always wins and no warning is given.
                    ' We will continue doing the same.
                    If existingDoc.IsComputedChecksum Then
                        Continue For
                    End If

                    Dim sourceInfo = existingDoc.GetSourceInfo()

                    If CheckSumMatches(checkSumText, sourceInfo.Checksum) Then
                        Dim guid As Guid = guid.Parse(checksumDirective.Guid.ValueText)
                        If guid = sourceInfo.ChecksumAlgorithmId Then
                            ' all parts match, nothing to do
                            Continue For
                        End If
                    End If

                    ' did not match to an existing document
                    ' produce a warning and ignore the directive
                    diagnosticBag.Add(ERRID.WRN_MultipleDeclFileExtChecksum, New SourceLocation(checksumDirective), path)

                Else
                    Dim newDocument = New DebugSourceDocument(
                        normalizedPath,
                        DebugSourceDocument.CorSymLanguageTypeBasic,
                        MakeCheckSumBytes(checksumDirective.Checksum.ValueText),
                        Guid.Parse(checksumDirective.Guid.ValueText))

                    documentsBuilder.AddDebugDocument(newDocument)
                End If
            Next
        End Sub

        Private Shared Function CheckSumMatches(bytesText As String, bytes As ImmutableArray(Of Byte)) As Boolean
            If bytesText.Length <> bytes.Length * 2 Then
                Return False
            End If

            For i As Integer = 0 To bytesText.Length \ 2 - 1
                ' 1A  in text becomes   0x1A
                Dim b As Integer = SyntaxFacts.IntegralLiteralCharacterValue(bytesText(i * 2)) * 16 +
                                   SyntaxFacts.IntegralLiteralCharacterValue(bytesText(i * 2 + 1))

                If b <> bytes(i) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Shared Function MakeCheckSumBytes(bytesText As String) As ImmutableArray(Of Byte)
            Dim builder As ArrayBuilder(Of Byte) = ArrayBuilder(Of Byte).GetInstance()

            For i As Integer = 0 To bytesText.Length \ 2 - 1
                ' 1A  in text becomes   0x1A
                Dim b As Byte = CByte(SyntaxFacts.IntegralLiteralCharacterValue(bytesText(i * 2)) * 16 +
                                      SyntaxFacts.IntegralLiteralCharacterValue(bytesText(i * 2 + 1)))

                builder.Add(b)
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Friend Overrides ReadOnly Property DebugSourceDocumentLanguageId As Guid
            Get
                Return DebugSourceDocument.CorSymLanguageTypeBasic
            End Get
        End Property

        Friend Overrides Function HasCodeToEmit() As Boolean
            For Each syntaxTree In SyntaxTrees
                Dim unit = syntaxTree.GetCompilationUnitRoot()
                If unit.Members.Count > 0 Then
                    Return True
                End If
            Next

            Return False
        End Function

#End Region

#Region "Common Members"

        Protected Overrides Function CommonWithReferences(newReferences As IEnumerable(Of MetadataReference)) As Compilation
            Return WithReferences(newReferences)
        End Function

        Protected Overrides Function CommonWithAssemblyName(assemblyName As String) As Compilation
            Return WithAssemblyName(assemblyName)
        End Function

        Protected Overrides Function CommonWithScriptCompilationInfo(info As ScriptCompilationInfo) As Compilation
            Return WithScriptCompilationInfo(DirectCast(info, VisualBasicScriptCompilationInfo))
        End Function

        Protected Overrides ReadOnly Property CommonAssembly As IAssemblySymbol
            Get
                Return Me.Assembly
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonGlobalNamespace As INamespaceSymbol
            Get
                Return Me.GlobalNamespace
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonOptions As CompilationOptions
            Get
                Return Options
            End Get
        End Property

        Protected Overrides Function CommonGetSemanticModel(syntaxTree As SyntaxTree, ignoreAccessibility As Boolean) As SemanticModel
            Return Me.GetSemanticModel(syntaxTree, ignoreAccessibility)
        End Function

        Protected Overrides ReadOnly Property CommonSyntaxTrees As ImmutableArray(Of SyntaxTree)
            Get
                Return Me.SyntaxTrees
            End Get
        End Property

        Protected Overrides Function CommonAddSyntaxTrees(trees As IEnumerable(Of SyntaxTree)) As Compilation
            Dim array = TryCast(trees, SyntaxTree())
            If array IsNot Nothing Then
                Return Me.AddSyntaxTrees(array)
            End If

            If trees Is Nothing Then
                Throw New ArgumentNullException(NameOf(trees))
            End If

            Return Me.AddSyntaxTrees(trees.Cast(Of SyntaxTree)())
        End Function

        Protected Overrides Function CommonRemoveSyntaxTrees(trees As IEnumerable(Of SyntaxTree)) As Compilation
            Dim array = TryCast(trees, SyntaxTree())
            If array IsNot Nothing Then
                Return Me.RemoveSyntaxTrees(array)
            End If

            If trees Is Nothing Then
                Throw New ArgumentNullException(NameOf(trees))
            End If

            Return Me.RemoveSyntaxTrees(trees.Cast(Of SyntaxTree)())
        End Function

        Protected Overrides Function CommonRemoveAllSyntaxTrees() As Compilation
            Return Me.RemoveAllSyntaxTrees()
        End Function

        Protected Overrides Function CommonReplaceSyntaxTree(oldTree As SyntaxTree, newTree As SyntaxTree) As Compilation
            Return Me.ReplaceSyntaxTree(oldTree, newTree)
        End Function

        Protected Overrides Function CommonWithOptions(options As CompilationOptions) As Compilation
            Return Me.WithOptions(DirectCast(options, VisualBasicCompilationOptions))
        End Function

        Protected Overrides Function CommonContainsSyntaxTree(syntaxTree As SyntaxTree) As Boolean
            Return Me.ContainsSyntaxTree(syntaxTree)
        End Function

        Protected Overrides Function CommonGetAssemblyOrModuleSymbol(reference As MetadataReference) As ISymbol
            Return Me.GetAssemblyOrModuleSymbol(reference)
        End Function

        Protected Overrides Function CommonClone() As Compilation
            Return Me.Clone()
        End Function

        Protected Overrides ReadOnly Property CommonSourceModule As IModuleSymbol
            Get
                Return Me.SourceModule
            End Get
        End Property

        Private Protected Overrides Function CommonGetSpecialType(specialType As SpecialType) As INamedTypeSymbolInternal
            Return Me.GetSpecialType(specialType)
        End Function

        Protected Overrides Function CommonGetCompilationNamespace(namespaceSymbol As INamespaceSymbol) As INamespaceSymbol
            Return Me.GetCompilationNamespace(namespaceSymbol)
        End Function

        Protected Overrides Function CommonGetTypeByMetadataName(metadataName As String) As INamedTypeSymbol
            Return Me.GetTypeByMetadataName(metadataName)
        End Function

        Protected Overrides ReadOnly Property CommonScriptClass As INamedTypeSymbol
            Get
                Return Me.ScriptClass
            End Get
        End Property

        Protected Overrides Function CommonCreateErrorTypeSymbol(container As INamespaceOrTypeSymbol, name As String, arity As Integer) As INamedTypeSymbol
            Return New ExtendedErrorTypeSymbol(
                       container.EnsureVbSymbolOrNothing(Of NamespaceOrTypeSymbol)(NameOf(container)),
                       name, arity)
        End Function

        Protected Overrides Function CommonCreateErrorNamespaceSymbol(container As INamespaceSymbol, name As String) As INamespaceSymbol
            Return New MissingNamespaceSymbol(
                       container.EnsureVbSymbolOrNothing(Of NamespaceSymbol)(NameOf(container)),
                       name)
        End Function

        Protected Overrides Function CommonCreateArrayTypeSymbol(elementType As ITypeSymbol, rank As Integer, elementNullableAnnotation As NullableAnnotation) As IArrayTypeSymbol
            Return CreateArrayTypeSymbol(elementType.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(elementType)), rank)
        End Function

        Protected Overrides Function CommonCreateTupleTypeSymbol(elementTypes As ImmutableArray(Of ITypeSymbol),
                                                                 elementNames As ImmutableArray(Of String),
                                                                 elementLocations As ImmutableArray(Of Location),
                                                                 elementNullableAnnotations As ImmutableArray(Of NullableAnnotation)) As INamedTypeSymbol
            Dim typesBuilder = ArrayBuilder(Of TypeSymbol).GetInstance(elementTypes.Length)
            For i As Integer = 0 To elementTypes.Length - 1
                typesBuilder.Add(elementTypes(i).EnsureVbSymbolOrNothing(Of TypeSymbol)($"{NameOf(elementTypes)}[{i}]"))
            Next

            'no location for the type declaration
            Return TupleTypeSymbol.Create(locationOpt:=Nothing,
                                          elementTypes:=typesBuilder.ToImmutableAndFree(),
                                          elementLocations:=elementLocations,
                                          elementNames:=elementNames, compilation:=Me,
                                          shouldCheckConstraints:=False, errorPositions:=Nothing)
        End Function

        Protected Overrides Function CommonCreateTupleTypeSymbol(
                underlyingType As INamedTypeSymbol,
                elementNames As ImmutableArray(Of String),
                elementLocations As ImmutableArray(Of Location),
                elementNullableAnnotations As ImmutableArray(Of NullableAnnotation)) As INamedTypeSymbol
            Dim csharpUnderlyingTuple = underlyingType.EnsureVbSymbolOrNothing(Of NamedTypeSymbol)(NameOf(underlyingType))

            Dim cardinality As Integer
            If Not csharpUnderlyingTuple.IsTupleCompatible(cardinality) Then
                Throw New ArgumentException(CodeAnalysisResources.TupleUnderlyingTypeMustBeTupleCompatible, NameOf(underlyingType))
            End If

            elementNames = CheckTupleElementNames(cardinality, elementNames)
            CheckTupleElementLocations(cardinality, elementLocations)
            CheckTupleElementNullableAnnotations(cardinality, elementNullableAnnotations)

            Return TupleTypeSymbol.Create(
                locationOpt:=Nothing,
                tupleCompatibleType:=underlyingType.EnsureVbSymbolOrNothing(Of NamedTypeSymbol)(NameOf(underlyingType)),
                elementLocations:=elementLocations,
                elementNames:=elementNames,
                errorPositions:=Nothing)
        End Function

        Protected Overrides Function CommonCreatePointerTypeSymbol(elementType As ITypeSymbol) As IPointerTypeSymbol
            Throw New NotSupportedException(VBResources.ThereAreNoPointerTypesInVB)
        End Function

        Protected Overrides Function CommonCreateFunctionPointerTypeSymbol(
                returnType As ITypeSymbol,
                refKind As RefKind,
                parameterTypes As ImmutableArray(Of ITypeSymbol),
                parameterRefKinds As ImmutableArray(Of RefKind),
                callingConvention As System.Reflection.Metadata.SignatureCallingConvention,
                callingConventionTypes As ImmutableArray(Of INamedTypeSymbol)) As IFunctionPointerTypeSymbol
            Throw New NotSupportedException(VBResources.ThereAreNoFunctionPointerTypesInVB)
        End Function

        Protected Overrides Function CommonCreateNativeIntegerTypeSymbol(signed As Boolean) As INamedTypeSymbol
            Throw New NotSupportedException(VBResources.ThereAreNoNativeIntegerTypesInVB)
        End Function

        Protected Overrides Function CommonCreateAnonymousTypeSymbol(
                memberTypes As ImmutableArray(Of ITypeSymbol),
                memberNames As ImmutableArray(Of String),
                memberLocations As ImmutableArray(Of Location),
                memberIsReadOnly As ImmutableArray(Of Boolean),
                memberNullableAnnotations As ImmutableArray(Of CodeAnalysis.NullableAnnotation)) As INamedTypeSymbol

            Dim i = 0
            For Each t In memberTypes
                t.EnsureVbSymbolOrNothing(Of TypeSymbol)($"{NameOf(memberTypes)}({i})")

                i = i + 1
            Next

            Dim fields = ArrayBuilder(Of AnonymousTypeField).GetInstance()

            For i = 0 To memberTypes.Length - 1
                Dim type = memberTypes(i)
                Dim name = memberNames(i)
                Dim loc = If(memberLocations.IsDefault, Location.None, memberLocations(i))
                Dim isReadOnly = memberIsReadOnly.IsDefault OrElse memberIsReadOnly(i)
                fields.Add(New AnonymousTypeField(name, DirectCast(type, TypeSymbol), loc, isReadOnly))
            Next

            Dim descriptor = New AnonymousTypeDescriptor(
                fields.ToImmutableAndFree(), Location.None, isImplicitlyDeclared:=False)
            Return Me.AnonymousTypeManager.ConstructAnonymousTypeSymbol(descriptor)
        End Function

        Protected Overrides Function CommonCreateBuiltinOperator(
                name As String,
                returnType As ITypeSymbol,
                leftType As ITypeSymbol,
                rightType As ITypeSymbol) As IMethodSymbol

            Dim vbReturnType = returnType.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(returnType))
            Dim vbLeftType = leftType.EnsureVbSymbolOrNothing(Of NamedTypeSymbol)(NameOf(leftType))
            Dim vbRightType = rightType.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(rightType))

            Dim nameToCheck = name
            Select Case name
                Case WellKnownMemberNames.CheckedAdditionOperatorName
                    nameToCheck = WellKnownMemberNames.AdditionOperatorName
                Case WellKnownMemberNames.CheckedDivisionOperatorName
                    nameToCheck = WellKnownMemberNames.IntegerDivisionOperatorName
                Case WellKnownMemberNames.CheckedMultiplyOperatorName
                    nameToCheck = WellKnownMemberNames.MultiplyOperatorName
                Case WellKnownMemberNames.CheckedSubtractionOperatorName
                    nameToCheck = WellKnownMemberNames.SubtractionOperatorName
            End Select

            Dim opInfo = OverloadResolution.GetOperatorInfo(nameToCheck)
            If Not opInfo.IsBinary Then
                Throw New ArgumentException(String.Format(CodeAnalysisResources.BadBuiltInOps1, name), NameOf(name))
            End If

            CheckBinaryBuiltInOperator(name, vbReturnType, vbLeftType, vbRightType, opInfo)

            Return New SynthesizedIntrinsicOperatorSymbol(vbLeftType, name, vbRightType, vbReturnType)
        End Function

        Private Shared Sub CheckBinaryBuiltInOperator(
                name As String,
                returnType As TypeSymbol,
                leftType As NamedTypeSymbol,
                rightType As TypeSymbol,
                opInfo As OverloadResolution.OperatorInfo)

            ' Built in enum binary operators
            If leftType.IsEnumType() AndAlso
               leftType.Equals(rightType, TypeCompareKind.ConsiderEverything) AndAlso
               leftType.Equals(returnType, TypeCompareKind.ConsiderEverything) Then
                If opInfo.BinaryOperatorKind = BinaryOperatorKind.Xor OrElse
                   opInfo.BinaryOperatorKind = BinaryOperatorKind.And OrElse
                   opInfo.BinaryOperatorKind = BinaryOperatorKind.Or Then
                    Return
                End If
            End If

            ' Quick table access to determine if these types are legal.
            If returnType.SpecialType <> SpecialType.None AndAlso
               leftType.SpecialType <> SpecialType.None AndAlso
               rightType.SpecialType <> SpecialType.None Then

                Dim resolved = OverloadResolution.ResolveNotLiftedIntrinsicBinaryOperator(opInfo.BinaryOperatorKind, leftType.SpecialType, rightType.SpecialType)
                If resolved <> SpecialType.None Then
                    ' Quick access table strangely maps `string Like string` to the `string` return type. remap it to 'bool'
                    ' here as that's what the operator actually is.
                    '
                    ' Similarly, the relations table doesn't include useful info.  it always has the original type,
                    ' not the expected 'bool' return type.
                    If resolved <> SpecialType.System_Object Then
                        If opInfo.BinaryOperatorKind = BinaryOperatorKind.Equals OrElse
                           opInfo.BinaryOperatorKind = BinaryOperatorKind.NotEquals OrElse
                           opInfo.BinaryOperatorKind = BinaryOperatorKind.LessThanOrEqual OrElse
                           opInfo.BinaryOperatorKind = BinaryOperatorKind.GreaterThanOrEqual OrElse
                           opInfo.BinaryOperatorKind = BinaryOperatorKind.LessThan OrElse
                           opInfo.BinaryOperatorKind = BinaryOperatorKind.GreaterThan OrElse
                           opInfo.BinaryOperatorKind = BinaryOperatorKind.Like Then

                            resolved = SpecialType.System_Boolean
                        End If
                    End If

                    If returnType.SpecialType = resolved Then
                        Return
                    End If
                End If
            End If

            Throw New ArgumentException(String.Format(CodeAnalysisResources.BadBuiltInOps3, $"{returnType.ToDisplayString()} operator {name}({leftType.ToDisplayString()}, {rightType.ToDisplayString()})"))
        End Sub

        Protected Overrides Function CommonCreateBuiltinOperator(
                name As String,
                returnType As ITypeSymbol,
                operandType As ITypeSymbol) As IMethodSymbol

            Dim vbReturnType = returnType.EnsureVbSymbolOrNothing(Of TypeSymbol)(NameOf(returnType))
            Dim vbOperandType = returnType.EnsureVbSymbolOrNothing(Of NamedTypeSymbol)(NameOf(operandType))

            Dim nameToCheck = If(name = WellKnownMemberNames.CheckedUnaryNegationOperatorName, WellKnownMemberNames.UnaryNegationOperatorName, name)

            Dim opInfo = OverloadResolution.GetOperatorInfo(nameToCheck)
            If Not opInfo.IsUnary Then
                Throw New ArgumentException(String.Format(CodeAnalysisResources.BadBuiltInOps1, name), NameOf(name))
            End If

            CheckUnaryBuiltInOperator(name, vbReturnType, vbOperandType, opInfo)

            Return New SynthesizedIntrinsicOperatorSymbol(vbOperandType, name, vbReturnType)
        End Function

        Private Shared Sub CheckUnaryBuiltInOperator(
                name As String,
                returnType As TypeSymbol,
                operandType As NamedTypeSymbol,
                opInfo As OverloadResolution.OperatorInfo)

            ' Enums support the `Not` operator.
            If operandType.IsEnumType() AndAlso
               opInfo.UnaryOperatorKind = UnaryOperatorKind.Not AndAlso
               returnType.Equals(operandType, TypeCompareKind.ConsiderEverything) Then
                Return
            End If

            ' Quick table access to determine if these types are legal.
            If returnType.SpecialType <> SpecialType.None AndAlso
               operandType.SpecialType <> SpecialType.None Then

                If opInfo.UnaryOperatorKind = UnaryOperatorKind.Not OrElse
                   opInfo.UnaryOperatorKind = UnaryOperatorKind.Plus OrElse
                   opInfo.UnaryOperatorKind = UnaryOperatorKind.Minus Then

                    Dim resolved = OverloadResolution.ResolveNotLiftedIntrinsicUnaryOperator(opInfo.UnaryOperatorKind, operandType.SpecialType)
                    If resolved <> SpecialType.None AndAlso
                       returnType.SpecialType = resolved Then
                        Return
                    End If
                End If
            End If

            Throw New ArgumentException(String.Format(CodeAnalysisResources.BadBuiltInOps3, $"{returnType.ToDisplayString()} operator {name}({operandType.ToDisplayString()})"))
        End Sub

        Protected Overrides ReadOnly Property CommonDynamicType As ITypeSymbol
            Get
                Throw New NotSupportedException(VBResources.ThereIsNoDynamicTypeInVB)
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonObjectType As INamedTypeSymbol
            Get
                Return Me.ObjectType
            End Get
        End Property

        Protected Overrides Function CommonGetEntryPoint(cancellationToken As CancellationToken) As IMethodSymbol
            Return Me.GetEntryPoint(cancellationToken)
        End Function

        ''' <summary>
        ''' Return true if there is a source declaration symbol name that meets given predicate.
        ''' </summary>
        Public Overrides Function ContainsSymbolsWithName(predicate As Func(Of String, Boolean), Optional filter As SymbolFilter = SymbolFilter.TypeAndMember, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            If predicate Is Nothing Then
                Throw New ArgumentNullException(NameOf(predicate))
            End If

            If filter = SymbolFilter.None Then
                Throw New ArgumentException(VBResources.NoNoneSearchCriteria, NameOf(filter))
            End If

            Return DeclarationTable.ContainsName(MergedRootDeclaration, predicate, filter, cancellationToken)
        End Function

        ''' <summary>
        ''' Return source declaration symbols whose name meets given predicate.
        ''' </summary>
        Public Overrides Function GetSymbolsWithName(predicate As Func(Of String, Boolean), Optional filter As SymbolFilter = SymbolFilter.TypeAndMember, Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of ISymbol)
            If predicate Is Nothing Then
                Throw New ArgumentNullException(NameOf(predicate))
            End If

            If filter = SymbolFilter.None Then
                Throw New ArgumentException(VBResources.NoNoneSearchCriteria, NameOf(filter))
            End If

            Return New PredicateSymbolSearcher(Me, filter, predicate, cancellationToken).GetSymbolsWithName()
        End Function

#Disable Warning RS0026 ' Do not add multiple public overloads with optional parameters
        ''' <summary>
        ''' Return true if there is a source declaration symbol name that matches the provided name.
        ''' This may be faster than <see cref="ContainsSymbolsWithName(Func(Of String, Boolean),
        ''' SymbolFilter, CancellationToken)"/> when predicate is just a simple string check.
        ''' <paramref name="name"/> is case insensitive.
        ''' </summary>
        Public Overrides Function ContainsSymbolsWithName(name As String, Optional filter As SymbolFilter = SymbolFilter.TypeAndMember, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            If name Is Nothing Then
                Throw New ArgumentNullException(NameOf(name))
            End If

            If filter = SymbolFilter.None Then
                Throw New ArgumentException(VBResources.NoNoneSearchCriteria, NameOf(filter))
            End If

            Return DeclarationTable.ContainsName(MergedRootDeclaration, name, filter, cancellationToken)
        End Function

        Public Overrides Function GetSymbolsWithName(name As String, Optional filter As SymbolFilter = SymbolFilter.TypeAndMember, Optional cancellationToken As CancellationToken = Nothing) As IEnumerable(Of ISymbol)
            If name Is Nothing Then
                Throw New ArgumentNullException(NameOf(name))
            End If

            If filter = SymbolFilter.None Then
                Throw New ArgumentException(VBResources.NoNoneSearchCriteria, NameOf(filter))
            End If

            Return New NameSymbolSearcher(Me, filter, name, cancellationToken).GetSymbolsWithName()
        End Function
#Enable Warning RS0026 ' Do not add multiple public overloads with optional parameters

        Friend Overrides Function IsUnreferencedAssemblyIdentityDiagnosticCode(code As Integer) As Boolean
            Select Case code
                Case ERRID.ERR_UnreferencedAssemblyEvent3,
                     ERRID.ERR_UnreferencedAssembly3
                    Return True

                Case Else
                    Return False
            End Select
        End Function

        Private Protected Overrides Function SupportsRuntimeCapabilityCore(capability As RuntimeCapability) As Boolean
            Return Me.Assembly.SupportsRuntimeCapability(capability)
        End Function

#End Region

        Private MustInherit Class AbstractSymbolSearcher
            Private ReadOnly _cache As PooledDictionary(Of Declaration, NamespaceOrTypeSymbol)
            Private ReadOnly _compilation As VisualBasicCompilation
            Private ReadOnly _includeNamespace As Boolean
            Private ReadOnly _includeType As Boolean
            Private ReadOnly _includeMember As Boolean
            Private ReadOnly _cancellationToken As CancellationToken

            Public Sub New(compilation As VisualBasicCompilation, filter As SymbolFilter, cancellationToken As CancellationToken)
                _cache = PooledDictionary(Of Declaration, NamespaceOrTypeSymbol).GetInstance()
                _compilation = compilation

                _includeNamespace = (filter And SymbolFilter.Namespace) = SymbolFilter.Namespace
                _includeType = (filter And SymbolFilter.Type) = SymbolFilter.Type
                _includeMember = (filter And SymbolFilter.Member) = SymbolFilter.Member

                _cancellationToken = cancellationToken
            End Sub

            Protected MustOverride Function Matches(name As String) As Boolean
            Protected MustOverride Function ShouldCheckTypeForMembers(typeDeclaration As MergedTypeDeclaration) As Boolean

            Public Function GetSymbolsWithName() As IEnumerable(Of ISymbol)
                Dim result = New HashSet(Of ISymbol)()
                Dim spine = ArrayBuilder(Of MergedNamespaceOrTypeDeclaration).GetInstance()

                AppendSymbolsWithName(spine, _compilation.MergedRootDeclaration, result)

                spine.Free()
                _cache.Free()

                Return result
            End Function

            Private Sub AppendSymbolsWithName(
                spine As ArrayBuilder(Of MergedNamespaceOrTypeDeclaration), current As MergedNamespaceOrTypeDeclaration, [set] As HashSet(Of ISymbol))

                If current.Kind = DeclarationKind.Namespace Then
                    If _includeNamespace AndAlso Matches(current.Name) Then
                        Dim container = GetSpineSymbol(spine)
                        Dim symbol = GetSymbol(container, current)
                        If symbol IsNot Nothing Then
                            [set].Add(symbol)
                        End If
                    End If
                Else
                    If _includeType AndAlso Matches(current.Name) Then
                        Dim container = GetSpineSymbol(spine)
                        Dim symbol = GetSymbol(container, current)
                        If symbol IsNot Nothing Then
                            [set].Add(symbol)
                        End If
                    End If

                    If _includeMember Then
                        Dim typeDeclaration = DirectCast(current, MergedTypeDeclaration)
                        If ShouldCheckTypeForMembers(typeDeclaration) Then
                            AppendMemberSymbolsWithName(spine, typeDeclaration, [set])
                        End If
                    End If
                End If

                spine.Add(current)
                For Each child In current.Children
                    Dim mergedNamespaceOrType = TryCast(child, MergedNamespaceOrTypeDeclaration)
                    If mergedNamespaceOrType IsNot Nothing Then
                        If _includeMember OrElse _includeType OrElse child.Kind = DeclarationKind.Namespace Then
                            AppendSymbolsWithName(spine, mergedNamespaceOrType, [set])
                        End If
                    End If
                Next

                spine.RemoveAt(spine.Count - 1)
            End Sub

            Private Sub AppendMemberSymbolsWithName(
                spine As ArrayBuilder(Of MergedNamespaceOrTypeDeclaration), mergedType As MergedTypeDeclaration, [set] As HashSet(Of ISymbol))

                _cancellationToken.ThrowIfCancellationRequested()
                spine.Add(mergedType)

                Dim container As NamespaceOrTypeSymbol = Nothing
                For Each name In mergedType.MemberNames
                    If Matches(name) Then
                        container = If(container, GetSpineSymbol(spine))
                        If container IsNot Nothing Then
                            [set].UnionWith(container.GetMembers(name))
                        End If
                    End If
                Next

                spine.RemoveAt(spine.Count - 1)
            End Sub

            Private Function GetSpineSymbol(spine As ArrayBuilder(Of MergedNamespaceOrTypeDeclaration)) As NamespaceOrTypeSymbol
                If spine.Count = 0 Then
                    Return Nothing
                End If

                Dim symbol = GetCachedSymbol(spine(spine.Count - 1))
                If symbol IsNot Nothing Then
                    Return symbol
                End If

                Dim current = TryCast(Me._compilation.GlobalNamespace, NamespaceOrTypeSymbol)
                For i = 1 To spine.Count - 1
                    current = GetSymbol(current, spine(i))
                Next

                Return current
            End Function

            Private Function GetCachedSymbol(declaration As MergedNamespaceOrTypeDeclaration) As NamespaceOrTypeSymbol
                Dim symbol As NamespaceOrTypeSymbol = Nothing
                If Me._cache.TryGetValue(declaration, symbol) Then
                    Return symbol
                End If

                Return Nothing
            End Function

            Private Function GetSymbol(container As NamespaceOrTypeSymbol, declaration As MergedNamespaceOrTypeDeclaration) As NamespaceOrTypeSymbol
                If container Is Nothing Then
                    Return Me._compilation.GlobalNamespace
                End If

                Dim symbol = GetCachedSymbol(declaration)
                If symbol IsNot Nothing Then
                    Return symbol
                End If

                If declaration.Kind = DeclarationKind.Namespace Then
                    AddCache(container.GetMembers(declaration.Name).OfType(Of NamespaceOrTypeSymbol)())
                Else
                    AddCache(container.GetTypeMembers(declaration.Name))
                End If

                Return GetCachedSymbol(declaration)
            End Function

            Private Sub AddCache(symbols As IEnumerable(Of NamespaceOrTypeSymbol))
                For Each symbol In symbols
                    Dim mergedNamespace = TryCast(symbol, MergedNamespaceSymbol)
                    If mergedNamespace IsNot Nothing Then
                        Me._cache(mergedNamespace.ConstituentNamespaces.OfType(Of SourceNamespaceSymbol).First().MergedDeclaration) = symbol
                        Continue For
                    End If

                    Dim sourceNamespace = TryCast(symbol, SourceNamespaceSymbol)
                    If sourceNamespace IsNot Nothing Then
                        Me._cache(sourceNamespace.MergedDeclaration) = sourceNamespace
                        Continue For
                    End If

                    Dim sourceType = TryCast(symbol, SourceMemberContainerTypeSymbol)
                    If sourceType IsNot Nothing Then
                        Me._cache(sourceType.TypeDeclaration) = sourceType
                    End If
                Next
            End Sub
        End Class

        Private Class PredicateSymbolSearcher
            Inherits AbstractSymbolSearcher

            Private ReadOnly _predicate As Func(Of String, Boolean)

            Public Sub New(
                compilation As VisualBasicCompilation, filter As SymbolFilter, predicate As Func(Of String, Boolean), cancellationToken As CancellationToken)
                MyBase.New(compilation, filter, cancellationToken)

                _predicate = predicate
            End Sub

            Protected Overrides Function ShouldCheckTypeForMembers(current As MergedTypeDeclaration) As Boolean
                Return True
            End Function

            Protected Overrides Function Matches(name As String) As Boolean
                Return _predicate(name)
            End Function
        End Class

        Private Class NameSymbolSearcher
            Inherits AbstractSymbolSearcher

            Private ReadOnly _name As String

            Public Sub New(
                compilation As VisualBasicCompilation, filter As SymbolFilter, name As String, cancellationToken As CancellationToken)
                MyBase.New(compilation, filter, cancellationToken)

                _name = name
            End Sub

            Protected Overrides Function ShouldCheckTypeForMembers(current As MergedTypeDeclaration) As Boolean
                For Each typeDecl In current.Declarations
                    If typeDecl.MemberNames.Contains(_name) Then
                        Return True
                    End If
                Next

                Return False
            End Function

            Protected Overrides Function Matches(name As String) As Boolean
                Return IdentifierComparison.Equals(_name, name)
            End Function
        End Class
    End Class
End Namespace
