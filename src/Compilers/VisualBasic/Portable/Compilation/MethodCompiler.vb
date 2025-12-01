' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.ErrorReporting
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class MethodCompiler
        Inherits VisualBasicSymbolVisitor

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _cancellationToken As CancellationToken
        Private ReadOnly _emittingPdb As Boolean
        Private ReadOnly _diagnostics As BindingDiagnosticBag
        Private ReadOnly _hasDeclarationErrors As Boolean
        Private ReadOnly _moduleBeingBuiltOpt As PEModuleBuilder ' Nothing if compiling for diagnostics
        Private ReadOnly _filterOpt As Predicate(Of Symbol)      ' If not Nothing, limit analysis to specific symbols

        ' GetDiagnostics only needs to Bind. If we need to go further, _doEmitPhase needs to be set.
        ' It normally happens during actual compile, but also happens when getting emit diagnostics for
        ' testing purposes.
        Private ReadOnly _doEmitPhase As Boolean
        Private ReadOnly _doLoweringPhase As Boolean ' To collect used assemblies we are doing lowering, but not emit

        ' MethodCompiler employs concurrency by following flattened fork/join pattern.
        '
        ' For every item that we want to compile in parallel a new task is forked.
        ' compileTaskQueue is used to track and observe all the tasks.
        ' Once compileTaskQueue is empty, we know that there are no more tasks (and no more can be created)
        ' and that means we are done compiling. WaitForWorkers ensures this condition.
        '
        ' Note that while tasks may fork more tasks (nested types, lambdas, whatever else that may introduce more types),
        ' we do not want any child/parent relationship between spawned tasks and their creators.
        ' Creator has no real dependencies on the completion of its children and should finish and release any resources
        ' as soon as it can regardless of the tasks it may have spawned.
        '
        ' Stack is used so that the wait would observe the most recently added task and have
        ' more chances to do inlined execution.
        Private ReadOnly _compilerTasks As ConcurrentStack(Of Task)

        Private _lazyDebugDocumentProvider As DebugDocumentProvider

        ' Tracks whether any method body has hasErrors set, and used to avoid
        ' emitting if there are errors without corresponding diagnostics.
        ' NOTE: once the flag is set to true, it should never go back to false!!!
        Private _globalHasErrors As Boolean

        Private ReadOnly Property GlobalHasErrors As Boolean
            Get
                Return _globalHasErrors
            End Get
        End Property

        Private Sub SetGlobalErrorIfTrue(arg As Boolean)
            ' NOTE: this is not a volatile write
            '       for correctness we need only single threaded consistency.
            '       Within a single task - if we have got an error it may not be safe to continue with some lowerings.
            '       It is ok if other tasks will see the change after some delay or does not observe at all.
            '       Such races are unavoidable and will just result in performing some work that is safe to do
            '       but may no longer be needed.
            '       The final Join of compiling tasks cannot happen without interlocked operations and that
            '       will that the final state of the flag is synchronized after we are done compiling.
            If arg Then
                _globalHasErrors = True
            End If
        End Sub

        ' moduleBeingBuilt can be Nothing in order to just analyze methods for errors.
        Private Sub New(compilation As VisualBasicCompilation,
                       moduleBeingBuiltOpt As PEModuleBuilder,
                       emittingPdb As Boolean,
                       doLoweringPhase As Boolean,
                       doEmitPhase As Boolean,
                       hasDeclarationErrors As Boolean,
                       diagnostics As BindingDiagnosticBag,
                       filter As Predicate(Of Symbol),
                       cancellationToken As CancellationToken)

            Debug.Assert(diagnostics IsNot Nothing)
            Debug.Assert(diagnostics.AccumulatesDiagnostics)
            Debug.Assert(diagnostics.DependenciesBag Is Nothing OrElse TypeOf diagnostics.DependenciesBag Is ConcurrentSet(Of AssemblySymbol))

            _compilation = compilation
            _moduleBeingBuiltOpt = moduleBeingBuiltOpt
            _diagnostics = diagnostics
            _hasDeclarationErrors = hasDeclarationErrors
            _cancellationToken = cancellationToken
            _doLoweringPhase = doEmitPhase OrElse doLoweringPhase
            _doEmitPhase = doEmitPhase
            _emittingPdb = emittingPdb
            _filterOpt = filter

            If compilation.Options.ConcurrentBuild Then
                _compilerTasks = New ConcurrentStack(Of Task)()
            End If
        End Sub

        Private Function GetDebugDocumentProvider(instrumentations As MethodInstrumentation) As DebugDocumentProvider
            If _emittingPdb OrElse instrumentations.Kinds.Contains(InstrumentationKind.TestCoverage) Then
                Debug.Assert(_moduleBeingBuiltOpt IsNot Nothing)
                _lazyDebugDocumentProvider = Function(path As String, basePath As String) _moduleBeingBuiltOpt.DebugDocumentsBuilder.GetOrAddDebugDocument(path, basePath, AddressOf CreateDebugDocumentForFile)
            End If

            Return _lazyDebugDocumentProvider
        End Function

        Private Shared Function IsDefinedOrImplementedInSourceTree(symbol As Symbol, tree As SyntaxTree, span As TextSpan?) As Boolean
            If symbol.IsDefinedInSourceTree(tree, span) Then
                Return True
            End If

            Dim method = TryCast(symbol, SourceMemberMethodSymbol)
            If method IsNot Nothing AndAlso
                method.IsPartialDefinition Then

                Dim implementationPart = method.PartialImplementationPart
                If implementationPart IsNot Nothing Then
                    Return implementationPart.IsDefinedInSourceTree(tree, span)
                End If
            End If

            If symbol.Kind = SymbolKind.Method AndAlso symbol.IsImplicitlyDeclared AndAlso
               DirectCast(symbol, MethodSymbol).MethodKind = MethodKind.Constructor Then
                ' Include implicitly declared constructor if containing type is included
                Return IsDefinedOrImplementedInSourceTree(symbol.ContainingType, tree, span)
            End If

            Return False
        End Function

        ''' <summary>
        ''' Completes binding and performs analysis of bound trees for the purpose of obtaining diagnostics.
        '''
        ''' NOTE: This method does not perform lowering/rewriting/emit.
        '''       Errors from those stages require complete compile,
        '''       but generally are not interesting during editing.
        '''
        ''' NOTE: the bound tree produced by this method are not stored anywhere
        '''       and immediately lost after diagnostics of a particular tree is done.
        '''
        ''' </summary>
        Public Shared Sub GetCompileDiagnostics(compilation As VisualBasicCompilation,
                                                root As NamespaceSymbol,
                                                tree As SyntaxTree,
                                                filterSpanWithinTree As TextSpan?,
                                                hasDeclarationErrors As Boolean,
                                                diagnostics As BindingDiagnosticBag,
                                                doLoweringPhase As Boolean,
                                                cancellationToken As CancellationToken)

            Dim filter As Predicate(Of Symbol) = Nothing

            If tree IsNot Nothing Then
                filter = Function(sym) IsDefinedOrImplementedInSourceTree(sym, tree, filterSpanWithinTree)
            End If

            Dim compiler = New MethodCompiler(compilation,
                                              moduleBeingBuiltOpt:=If(doLoweringPhase,
                                                                      DirectCast(compilation.CreateModuleBuilder(
                                                                                     emitOptions:=EmitOptions.Default,
                                                                                     debugEntryPoint:=Nothing,
                                                                                     manifestResources:=Nothing,
                                                                                     sourceLinkStream:=Nothing,
                                                                                     embeddedTexts:=Nothing,
                                                                                     testData:=Nothing,
                                                                                     diagnostics:=diagnostics.DiagnosticBag,
                                                                                     cancellationToken:=cancellationToken), PEModuleBuilder),
                                                                      Nothing),
                                              emittingPdb:=False,
                                              doLoweringPhase:=doLoweringPhase,
                                              doEmitPhase:=False,
                                              hasDeclarationErrors:=hasDeclarationErrors,
                                              diagnostics:=diagnostics,
                                              filter:=filter,
                                              cancellationToken:=cancellationToken)

            root.Accept(compiler)

            If tree Is Nothing Then
                ' include entry point diagnostics if we are compiling the entire compilation, not just a single tree:
                Dim entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(cancellationToken)
                If entryPointAndDiagnostics IsNot Nothing Then
                    diagnostics.AddRange(entryPointAndDiagnostics.Diagnostics)
                End If
            End If

            compiler.WaitForWorkers()
        End Sub

        ''' <summary>
        ''' Compiles given compilation into provided module.
        '''
        ''' NOTE: it is ok for moduleBeingBuiltOpt to be Nothing.
        '''       In such case the only results of this method would be diagnostics for complete compile.
        '''
        ''' NOTE: the bound/lowered trees produced by this method are not stored anywhere and
        '''       immediately lost after obtaining method bodies and diagnostics for a particular
        '''       tree.
        ''' </summary>
        Friend Shared Sub CompileMethodBodies(compilation As VisualBasicCompilation,
                                              moduleBeingBuiltOpt As PEModuleBuilder,
                                              emittingPdb As Boolean,
                                              hasDeclarationErrors As Boolean,
                                              filter As Predicate(Of Symbol),
                                              diagnostics As BindingDiagnosticBag,
                                              Optional cancellationToken As CancellationToken = Nothing)
            Debug.Assert(diagnostics IsNot Nothing)
            Debug.Assert(diagnostics.AccumulatesDiagnostics)

            If compilation.PreviousSubmission IsNot Nothing Then
                ' In case there is a previous submission, we should ensure
                ' it has already created anonymous type/delegates templates

                ' NOTE: if there are any errors, we will pick up what was created anyway
                compilation.PreviousSubmission.EnsureAnonymousTypeTemplates(cancellationToken)

                ' TODO: revise to use a loop instead of a recursion
            End If

#If DEBUG Then
            compilation.EmbeddedSymbolManager.AssertMarkAllDeferredSymbolsAsReferencedIsCalled()
#End If

            Dim compiler = New MethodCompiler(compilation,
                                              moduleBeingBuiltOpt,
                                              emittingPdb,
                                              doLoweringPhase:=True,
                                              doEmitPhase:=True,
                                              hasDeclarationErrors:=hasDeclarationErrors,
                                              diagnostics:=diagnostics,
                                              filter:=filter,
                                              cancellationToken:=cancellationToken)

            compilation.SourceModule.GlobalNamespace.Accept(compiler)
            compiler.WaitForWorkers()

            If moduleBeingBuiltOpt IsNot Nothing Then
                Dim additionalTypes = moduleBeingBuiltOpt.GetAdditionalTopLevelTypes()
                If Not additionalTypes.IsEmpty Then
                    compiler.CompileSynthesizedMethods(additionalTypes)
                End If

                ' Create and compile HotReloadException type if emitting deltas even if it is not used.
                ' We might need to use it for deleted members, which we determine when indexing metadata.
                Dim hotReloadException = moduleBeingBuiltOpt.TryGetOrCreateSynthesizedHotReloadExceptionType()
                If hotReloadException IsNot Nothing Then
                    compiler.CompileSynthesizedMethods(ImmutableArray.Create(DirectCast(hotReloadException, NamedTypeSymbol)))
                End If

                compilation.AnonymousTypeManager.AssignTemplatesNamesAndCompile(compiler, moduleBeingBuiltOpt, diagnostics)
                compiler.WaitForWorkers()

                ' Process symbols from embedded code if needed.
                If compilation.EmbeddedSymbolManager.Embedded <> EmbeddedSymbolKind.None Then
                    compiler.ProcessEmbeddedMethods()
                End If

                ' Deleted definitions must be emitted before PrivateImplementationDetails are frozen since
                ' it may add new members to it. All changes to PrivateImplementationDetails are additions,
                ' so we don't need to create deleted method defs for those.
                moduleBeingBuiltOpt.CreateDeletedMemberDefinitions(diagnostics.DiagnosticBag)

                ' all threads that were adding methods must be finished now, we can freeze the class:
                Dim privateImplClass = moduleBeingBuiltOpt.FreezePrivateImplementationDetails()
                If privateImplClass IsNot Nothing Then
                    compiler.CompileSynthesizedMethods(privateImplClass)
                End If
            End If

            Dim entryPoint = GetEntryPoint(compilation, moduleBeingBuiltOpt, diagnostics, cancellationToken)
            If moduleBeingBuiltOpt IsNot Nothing Then
                If entryPoint IsNot Nothing AndAlso compilation.Options.OutputKind.IsApplication Then
                    moduleBeingBuiltOpt.SetPEEntryPoint(entryPoint, diagnostics.DiagnosticBag)
                End If

                If (compiler.GlobalHasErrors OrElse moduleBeingBuiltOpt.SourceModule.HasBadAttributes) AndAlso Not hasDeclarationErrors AndAlso Not diagnostics.HasAnyErrors Then
                    ' If there were errors but no diagnostics, explicitly add
                    ' a "Failed to emit module" error to prevent emitting.
                    Dim messageResourceName = If(compiler.GlobalHasErrors, NameOf(CodeAnalysisResources.UnableToDetermineSpecificCauseOfFailure), NameOf(CodeAnalysisResources.ModuleHasInvalidAttributes))
                    diagnostics.Add(ERRID.ERR_ModuleEmitFailure, NoLocation.Singleton, moduleBeingBuiltOpt.SourceModule.Name,
                        New LocalizableResourceString(messageResourceName, CodeAnalysisResources.ResourceManager, GetType(CodeAnalysisResources)))
                End If
            End If
        End Sub

        Private Shared Function GetEntryPoint(compilation As VisualBasicCompilation,
                                             moduleBeingBuilt As PEModuleBuilder,
                                             diagnostics As BindingDiagnosticBag,
                                             cancellationToken As CancellationToken) As MethodSymbol
            Debug.Assert(diagnostics.AccumulatesDiagnostics)
            Dim entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(cancellationToken)
            If entryPointAndDiagnostics Is Nothing Then
                Return Nothing
            End If

            Debug.Assert(Not entryPointAndDiagnostics.Diagnostics.IsDefault)
            diagnostics.AddRange(entryPointAndDiagnostics.Diagnostics)

            Dim entryPoint = entryPointAndDiagnostics.MethodSymbol
            Dim synthesizedEntryPoint = TryCast(entryPoint, SynthesizedEntryPointSymbol)
            If synthesizedEntryPoint IsNot Nothing AndAlso
                moduleBeingBuilt IsNot Nothing AndAlso
                Not diagnostics.HasAnyErrors Then
                Dim compilationState = New TypeCompilationState(compilation, moduleBeingBuilt, initializeComponentOpt:=Nothing)
                Dim body = synthesizedEntryPoint.CreateBody()

                Dim emittedBody = GenerateMethodBody(moduleBeingBuilt,
                                             synthesizedEntryPoint,
                                             methodOrdinal:=DebugId.UndefinedOrdinal,
                                             block:=body,
                                             lambdaDebugInfo:=ImmutableArray(Of EncLambdaInfo).Empty,
                                             orderedLambdaRuntimeRudeEdits:=ImmutableArray(Of LambdaRuntimeRudeEditInfo).Empty,
                                             closureDebugInfo:=ImmutableArray(Of EncClosureInfo).Empty,
                                             stateMachineStateDebugInfos:=ImmutableArray(Of StateMachineStateDebugInfo).Empty,
                                             stateMachineTypeOpt:=Nothing,
                                             variableSlotAllocatorOpt:=Nothing,
                                             diagnostics:=diagnostics,
                                             debugDocumentProvider:=Nothing,
                                             emittingPdb:=False,
                                             codeCoverageSpans:=ImmutableArray(Of SourceSpan).Empty)
                moduleBeingBuilt.SetMethodBody(synthesizedEntryPoint, emittedBody)
            End If

            Debug.Assert(entryPoint IsNot Nothing OrElse entryPointAndDiagnostics.Diagnostics.HasAnyErrors() OrElse Not compilation.Options.Errors.IsDefaultOrEmpty)
            Return entryPoint
        End Function

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

#Region "Embedded symbols processing"

        Private Sub ProcessEmbeddedMethods()
            Dim manager = _compilation.EmbeddedSymbolManager
            Dim processedSymbols As New ConcurrentSet(Of Symbol)(ReferenceEqualityComparer.Instance)

            Dim methodOrdinal = 0

            Dim builder = ArrayBuilder(Of Symbol).GetInstance
            Do
                ' We iterate all the symbols for embedded code that are CURRENTLY known
                ' to be referenced by user code or already emitted embedded code.
                '
                ' If this the first full emit of the current compilation and there are no
                ' concurrent emits this collection will consist of embedded symbols referenced
                ' from attributes on compilation source symbols (by directly using embedded
                ' attributes OR referencing embedded types from attributes' arguments via
                ' 'GetType(EmbeddedTypeName)' expressions).
                '
                ' The consecutive iterations may also see new embedded symbols referenced
                ' as we compile and emit embedded methods.
                '
                ' If there are concurrent emits in place more referenced symbols may be returned
                ' by GetCurrentReferencedSymbolsSnapshot than in simple case described above,
                ' thus reducing the number of iterations of the outer Do loop.
                '
                ' Note that GetCurrentReferencedSymbolsSnapshot actually makes a snapshot
                ' of the referenced symbols.
                manager.GetCurrentReferencedSymbolsSnapshot(builder, processedSymbols)
                If builder.Count = 0 Then
                    Exit Do
                End If

                For index = 0 To builder.Count - 1
                    Dim symbol As Symbol = builder(index)
                    processedSymbols.Add(symbol)

#If DEBUG Then
                    ' In DEBUG assert that the type does not have
                    ' field initializers except those in const fields
                    If symbol.Kind = SymbolKind.NamedType Then
                        Dim embeddedType = DirectCast(symbol, EmbeddedSymbolManager.EmbeddedNamedTypeSymbol)
                        AssertAllInitializersAreConstants(embeddedType.StaticInitializers)
                        AssertAllInitializersAreConstants(embeddedType.InstanceInitializers)
                    End If
#End If
                    ' Compile method
                    If symbol.Kind = SymbolKind.Method Then
                        Dim embeddedMethod = DirectCast(symbol, MethodSymbol)
                        EmbeddedSymbolManager.ValidateMethod(embeddedMethod)
                        VisitEmbeddedMethod(embeddedMethod)
                    End If
                Next

                builder.Clear()
            Loop
            builder.Free()

            ' Seal the referenced symbol collection
            manager.SealCollection()
        End Sub

        Private Sub VisitEmbeddedMethod(method As MethodSymbol)
            ' Lazily created collection of synthetic methods which
            ' may be created during compilation of methods
            Dim compilationState As TypeCompilationState = New TypeCompilationState(_compilation, _moduleBeingBuiltOpt, initializeComponentOpt:=Nothing)

            ' Containing type binder

            ' NOTE: we need to provide type binder for the constructor compilation,
            '       so we create it for each constructor, but it does not seem to be a
            '       problem since current embedded types have only one constructor per
            '       type; this might need to be revised later if this assumption changes
            Dim sourceTypeBinder As Binder = If(method.MethodKind = MethodKind.Ordinary, Nothing,
                                                    BinderBuilder.CreateBinderForType(
                                                        DirectCast(method.ContainingModule, SourceModuleSymbol),
                                                        method.ContainingType.GetFirstLocation().PossiblyEmbeddedOrMySourceTree(),
                                                        method.ContainingType))

            ' Since embedded method bodies don't produce synthesized methods (see the assertion below)
            ' there is no need to assign an ordinal to embedded methods.
            Const methodOrdinal As Integer = -1

            Dim withEventPropertyIdDispenser = 0
            Dim delegateRelaxationIdDispenser = 0

            Dim referencedConstructor As MethodSymbol = Nothing
            CompileMethod(method,
                          methodOrdinal,
                          withEventPropertyIdDispenser,
                          delegateRelaxationIdDispenser,
                          filter:=Nothing,
                          compilationState:=compilationState,
                          processedInitializers:=Binder.ProcessedFieldOrPropertyInitializers.Empty,
                          containingTypeBinder:=sourceTypeBinder,
                          previousSubmissionFields:=Nothing,
                          referencedConstructor:=referencedConstructor)

            ' Do not expect WithEvents
            Debug.Assert(withEventPropertyIdDispenser = 0)

            ' Do not expect delegate relaxation stubs
            Debug.Assert(delegateRelaxationIdDispenser = 0)

            ' Do not expect constructor --> constructor calls for embedded types
            Debug.Assert(referencedConstructor Is Nothing OrElse
                         Not referencedConstructor.ContainingType.Equals(method.ContainingType))

            ' Don't expect any synthetic methods created for embedded types
            Debug.Assert(Not compilationState.HasSynthesizedMethods)
        End Sub

        <Conditional("DEBUG")>
        Private Sub AssertAllInitializersAreConstants(initializers As ImmutableArray(Of ImmutableArray(Of FieldOrPropertyInitializer)))
            If Not initializers.IsDefaultOrEmpty Then
                For Each initializerGroup In initializers
                    If Not initializerGroup.IsEmpty Then
                        For Each initializer In initializerGroup
                            For Each fieldOrProperty In initializer.FieldsOrProperties
                                Debug.Assert(fieldOrProperty.Kind = SymbolKind.Field)
                                Debug.Assert(DirectCast(fieldOrProperty, FieldSymbol).IsConst)
                            Next
                        Next
                    End If
                Next
            End If
        End Sub

#End Region

        Private ReadOnly Property DoEmitPhase() As Boolean
            Get
                Return _doEmitPhase
            End Get
        End Property

        Private ReadOnly Property DoLoweringPhase As Boolean
            Get
                Return _doLoweringPhase
            End Get
        End Property

        Public Overrides Sub VisitNamespace(symbol As NamespaceSymbol)
            _cancellationToken.ThrowIfCancellationRequested()

            If Me._compilation.Options.ConcurrentBuild Then
                Dim worker As Task = CompileNamespaceAsync(symbol)
                _compilerTasks.Push(worker)
            Else
                CompileNamespace(symbol)
            End If
        End Sub

        Private Function CompileNamespaceAsync(symbol As NamespaceSymbol) As Task
            Return Task.Run(
                UICultureUtilities.WithCurrentUICulture(
                    Sub()
                        Try
                            CompileNamespace(symbol)
                        Catch e As Exception When FatalError.ReportAndPropagateUnlessCanceled(e)
                            Throw ExceptionUtilities.Unreachable
                        End Try
                    End Sub),
                Me._cancellationToken)
        End Function

        Private Sub CompileNamespace(symbol As NamespaceSymbol)
            If PassesFilter(_filterOpt, symbol) Then
                For Each member In symbol.GetMembersUnordered()
                    member.Accept(Me)
                Next
            End If
        End Sub

        Public Overrides Sub VisitNamedType(symbol As NamedTypeSymbol)
            _cancellationToken.ThrowIfCancellationRequested()
            If PassesFilter(_filterOpt, symbol) Then
                If Me._compilation.Options.ConcurrentBuild Then
                    Dim worker As Task = CompileNamedTypeAsync(symbol, _filterOpt)
                    _compilerTasks.Push(worker)
                Else
                    CompileNamedType(symbol, _filterOpt)
                End If
            End If
        End Sub

        Private Function CompileNamedTypeAsync(symbol As NamedTypeSymbol, filter As Predicate(Of Symbol)) As Task
            Return Task.Run(
                UICultureUtilities.WithCurrentUICulture(
                    Sub()
                        Try
                            CompileNamedType(symbol, filter)
                        Catch e As Exception When FatalError.ReportAndPropagateUnlessCanceled(e)
                            Throw ExceptionUtilities.Unreachable
                        End Try
                    End Sub),
                Me._cancellationToken)
        End Function

        Private Sub CompileNamedType(containingType As NamedTypeSymbol, filter As Predicate(Of Symbol))
            If containingType.IsEmbedded Then
                ' Don't process embedded types
                Return
            End If

            ' Find the constructor of a script class.
            Dim scriptCtor As SynthesizedConstructorBase = Nothing
            Dim scriptInitializer As SynthesizedInteractiveInitializerMethod = Nothing
            Dim scriptEntryPoint As SynthesizedEntryPointSymbol = Nothing
            Dim scriptCtorOrdinal = -1
            If containingType.IsScriptClass Then
                scriptCtor = containingType.GetScriptConstructor()
                scriptInitializer = containingType.GetScriptInitializer()
                scriptEntryPoint = containingType.GetScriptEntryPoint()
                Debug.Assert(scriptCtor IsNot Nothing)
                Debug.Assert(scriptInitializer IsNot Nothing)
            End If

            Dim processedStaticInitializers = Binder.ProcessedFieldOrPropertyInitializers.Empty
            Dim processedInstanceInitializers = Binder.ProcessedFieldOrPropertyInitializers.Empty
            Dim synthesizedSubmissionFields = If(containingType.IsSubmissionClass, New SynthesizedSubmissionFields(_compilation, containingType), Nothing)

            ' if this is a type symbol from source we'll try to bind the field initializers as well
            Dim sourceTypeSymbol = TryCast(containingType, SourceMemberContainerTypeSymbol)
            Dim initializeComponent As MethodSymbol = Nothing

            If sourceTypeSymbol IsNot Nothing AndAlso DoLoweringPhase Then
                initializeComponent = GetDesignerInitializeComponentMethod(sourceTypeSymbol)
            End If

            ' Lazily created collection of synthetic methods which
            ' may be created during compilation of methods
            Dim compilationState As TypeCompilationState = New TypeCompilationState(_compilation, _moduleBeingBuiltOpt, initializeComponent)

            ' Containing type binder
            Dim sourceTypeBinder As Binder = Nothing

            If sourceTypeSymbol IsNot Nothing Then

                Debug.Assert(sourceTypeSymbol.Locations.Length > 0)
                sourceTypeBinder = BinderBuilder.CreateBinderForType(
                                        DirectCast(sourceTypeSymbol.ContainingModule, SourceModuleSymbol),
                                        sourceTypeSymbol.GetFirstLocation().PossiblyEmbeddedOrMySourceTree,
                                        sourceTypeSymbol)

                processedStaticInitializers = New Binder.ProcessedFieldOrPropertyInitializers(Binder.BindFieldAndPropertyInitializers(sourceTypeSymbol,
                                                        sourceTypeSymbol.StaticInitializers,
                                                        scriptInitializer,
                                                        _diagnostics))

                processedInstanceInitializers = New Binder.ProcessedFieldOrPropertyInitializers(Binder.BindFieldAndPropertyInitializers(sourceTypeSymbol,
                                                        sourceTypeSymbol.InstanceInitializers,
                                                        scriptInitializer,
                                                        _diagnostics))

                ' TODO: any flow analysis for initializers?

                ' const fields of type date or decimal require a shared constructor. We decided that this constructor
                ' should not be part of the type's member list. If there is not already a shared constructor, we're
                ' creating one and call CompileMethod to rewrite the field initializers.
                Dim sharedDefaultConstructor = sourceTypeSymbol.CreateSharedConstructorsForConstFieldsIfRequired(sourceTypeBinder, _diagnostics)
                If sharedDefaultConstructor IsNot Nothing AndAlso PassesFilter(filter, sharedDefaultConstructor) Then

                    Dim sharedConstructorWithEventPropertyIdDispenser = 0
                    Dim sharedConstructorDelegateRelaxationIdDispenser = 0

                    CompileMethod(sharedDefaultConstructor,
                                  -1,
                                  sharedConstructorWithEventPropertyIdDispenser,
                                  sharedConstructorDelegateRelaxationIdDispenser,
                                  filter,
                                  compilationState,
                                  processedStaticInitializers,
                                  sourceTypeBinder,
                                  synthesizedSubmissionFields)

                    ' Default shared constructor shall not have any Handles clause
                    Debug.Assert(sharedConstructorWithEventPropertyIdDispenser = 0)

                    ' Default shared constructor shall not produce delegate relaxation stubs
                    Debug.Assert(sharedConstructorDelegateRelaxationIdDispenser = 0)

                    If _moduleBeingBuiltOpt IsNot Nothing Then
                        _moduleBeingBuiltOpt.AddSynthesizedDefinition(sourceTypeSymbol, sharedDefaultConstructor.GetCciAdapter())
                    End If
                End If
            End If

            ' Constructor --> Constructor calls to be used in cycles detection
            Dim constructorCallMap As Dictionary(Of MethodSymbol, MethodSymbol) = Nothing
            Dim members = containingType.GetMembers()

            ' Unique ids assigned to synthesized overrides of WithEvents properties.
            Dim withEventPropertyIdDispenser = 0

            ' Unique ids assigned to synthesized delegate relaxation stubs.
            Dim delegateRelaxationIdDispenser = 0

            For memberOrdinal = 0 To members.Length - 1
                Dim member = members(memberOrdinal)

                If Not PassesFilter(filter, member) Then
                    Continue For
                End If

                Select Case member.Kind
                    Case SymbolKind.NamedType
                        member.Accept(Me)

                    Case SymbolKind.Method
                        Dim method = DirectCast(member, MethodSymbol)
                        If method.IsScriptConstructor Then
                            Debug.Assert(scriptCtorOrdinal = -1)
                            Debug.Assert(scriptCtor Is method)
                            scriptCtorOrdinal = memberOrdinal
                            Continue For
                        End If

                        If method Is scriptEntryPoint Then
                            Continue For
                        End If

                        If method.IsPartial() Then
                            Dim impl = method.PartialImplementationPart
                            If impl IsNot method Then
                                If CType(method, SourceMethodSymbol).SetDiagnostics(ImmutableArray(Of Diagnostic).Empty) Then
                                    method.DeclaringCompilation.SymbolDeclaredEvent(method)
                                End If

                                If impl Is Nothing Then
                                    Continue For
                                End If

                                method = impl
                            End If
                        End If

                        ' pass correct processed initializers for the static or instance constructors,
                        ' otherwise pass nothing
                        Dim processedInitializers = Binder.ProcessedFieldOrPropertyInitializers.Empty

                        If method.MethodKind = MethodKind.SharedConstructor Then
                            processedInitializers = processedStaticInitializers
                        ElseIf method.MethodKind = MethodKind.Constructor OrElse method.IsScriptInitializer Then
                            processedInitializers = processedInstanceInitializers
                        End If

                        Dim referencedConstructor As MethodSymbol = Nothing

                        CompileMethod(method,
                                      memberOrdinal,
                                      withEventPropertyIdDispenser,
                                      delegateRelaxationIdDispenser,
                                      filter,
                                      compilationState,
                                      processedInitializers,
                                      sourceTypeBinder,
                                      synthesizedSubmissionFields,
                                      referencedConstructor)

                        ' If 'referencedConstructor' is returned by 'CompileMethod', the method just compiled
                        ' was a constructor which references the returned symbol. We might want to store
                        ' some of those constructors in 'constructorCallMap' to process later for cycle detection
                        If referencedConstructor IsNot Nothing Then

                            '  If base class constructor is called, the constructor cannot be part of a cycle
                            If referencedConstructor.ContainingType.Equals(containingType) Then
                                If constructorCallMap Is Nothing Then
                                    constructorCallMap = New Dictionary(Of MethodSymbol, MethodSymbol)
                                End If
                                constructorCallMap.Add(method, referencedConstructor)
                            End If

                        End If

                        ' Create exact signature stubs for interface implementations.
                        If DoLoweringPhase AndAlso _moduleBeingBuiltOpt IsNot Nothing Then
                            CreateExplicitInterfaceImplementationStubs(compilationState, method)
                        End If
                End Select
            Next

            Debug.Assert(containingType.IsScriptClass = (scriptCtorOrdinal >= 0))

            ' Detect and report cycles in constructor calls
            If constructorCallMap IsNot Nothing Then
                DetectAndReportCyclesInConstructorCalls(constructorCallMap, _diagnostics)
            End If

            ' Compile submission constructor last so that synthesized submission fields are collected from all script methods:
            If scriptCtor IsNot Nothing Then
                Debug.Assert(scriptCtorOrdinal >= 0)
                CompileMethod(scriptCtor,
                              scriptCtorOrdinal,
                              withEventPropertyIdDispenser,
                              delegateRelaxationIdDispenser,
                              filter,
                              compilationState,
                              Binder.ProcessedFieldOrPropertyInitializers.Empty,
                              sourceTypeBinder,
                              synthesizedSubmissionFields)

                If synthesizedSubmissionFields IsNot Nothing AndAlso _moduleBeingBuiltOpt IsNot Nothing Then
                    synthesizedSubmissionFields.AddToType(containingType, _moduleBeingBuiltOpt)
                End If
            End If

            ' Report warnings for constructors that do not call InitializeComponent
            If initializeComponent IsNot Nothing Then
                For Each member In containingType.GetMembers()
                    If member.IsShared OrElse Not member.IsFromCompilation(_compilation) OrElse member.Kind <> SymbolKind.Method Then
                        Continue For
                    End If

                    Dim sourceMethod = TryCast(member, SourceMemberMethodSymbol)

                    If sourceMethod IsNot Nothing AndAlso
                       sourceMethod.MethodKind = MethodKind.Constructor AndAlso
                       Not compilationState.CallsInitializeComponent(sourceMethod) Then
                        Dim location As Location = sourceMethod.NonMergedLocation
                        Debug.Assert(location IsNot Nothing)

                        If location IsNot Nothing Then
                            Binder.ReportDiagnostic(_diagnostics, location, ERRID.WRN_ExpectedInitComponentCall2, sourceMethod, sourceTypeSymbol)
                        End If
                    End If
                Next
            End If

            ' Add synthetic methods created for this type in above calls to CompileMethod
            If _moduleBeingBuiltOpt IsNot Nothing Then
                CompileSynthesizedMethods(compilationState)
            End If

            compilationState.Free()
        End Sub

        Private Sub CreateExplicitInterfaceImplementationStubs(compilationState As TypeCompilationState, method As MethodSymbol)
            ' It is not common to have signature mismatch, let's avoid any extra work
            ' and allocations until we know that we have a mismatch.
            Dim stubs As ArrayBuilder(Of SynthesizedInterfaceImplementationStubSymbol) = Nothing

            For Each implemented In method.ExplicitInterfaceImplementations
                If Not MethodSignatureComparer.CustomModifiersAndParametersAndReturnTypeSignatureComparer.Equals(method, implemented) AndAlso
                   MethodSignatureComparer.ParametersAndReturnTypeSignatureComparer.Equals(method, implemented) Then ' In some error scenarios we can reach here for incompatible signatures

                    If stubs Is Nothing Then
                        stubs = ArrayBuilder(Of SynthesizedInterfaceImplementationStubSymbol).GetInstance()
                    End If

                    Dim matchingStub As SynthesizedInterfaceImplementationStubSymbol = Nothing

                    For Each candidate In stubs
                        If MethodSignatureComparer.CustomModifiersAndParametersAndReturnTypeSignatureComparer.Equals(candidate, implemented) Then
                            matchingStub = candidate
                            Exit For
                        End If
                    Next

                    If matchingStub Is Nothing Then
                        matchingStub = New SynthesizedInterfaceImplementationStubSymbol(method, implemented)
                        stubs.Add(matchingStub)

                        Dim f = New SyntheticBoundNodeFactory(matchingStub, matchingStub, If(method.Syntax, VisualBasic.VisualBasicSyntaxTree.Dummy.GetRoot()), compilationState, BindingDiagnosticBag.Discarded)

                        Dim methodToInvoke As MethodSymbol
                        If method.IsGenericMethod Then
                            methodToInvoke = method.Construct(matchingStub.TypeArguments)
                        Else
                            methodToInvoke = method
                        End If

                        Dim arguments = ArrayBuilder(Of BoundExpression).GetInstance(matchingStub.ParameterCount)

                        For Each param In matchingStub.Parameters
                            Dim parameterExpression = f.Parameter(param)

                            If Not param.IsByRef Then
                                parameterExpression = parameterExpression.MakeRValue()
                            End If

                            arguments.Add(parameterExpression)
                        Next

                        Dim invocation = f.Call(f.Me, methodToInvoke, arguments.ToImmutableAndFree())
                        Dim body As BoundBlock

                        If method.IsSub Then
                            body = f.Block(f.ExpressionStatement(invocation), f.Return())
                        Else
                            body = f.Block(f.Return(invocation))
                        End If

                        f.CloseMethod(body)
                        _moduleBeingBuiltOpt.AddSynthesizedDefinition(method.ContainingType, DirectCast(matchingStub.GetCciAdapter(), Microsoft.Cci.IMethodDefinition))
                    End If

                    matchingStub.AddImplementedMethod(implemented)
                End If
            Next

            If stubs IsNot Nothing Then
                For Each stub In stubs
                    stub.Seal()
                Next

                stubs.Free()
            End If
        End Sub

        Private Shared Function GetDesignerInitializeComponentMethod(sourceTypeSymbol As SourceMemberContainerTypeSymbol) As MethodSymbol

            If sourceTypeSymbol.TypeKind = TypeKind.Class AndAlso sourceTypeSymbol.GetAttributes().IndexOfAttribute(AttributeDescription.DesignerGeneratedAttribute) > -1 Then
                For Each member As Symbol In sourceTypeSymbol.GetMembers("InitializeComponent")
                    If member.Kind = SymbolKind.Method Then
                        Dim method = DirectCast(member, MethodSymbol)

                        If method.IsSub AndAlso Not method.IsShared AndAlso Not method.IsGenericMethod AndAlso method.ParameterCount = 0 Then
                            Return method
                        End If
                    End If
                Next
            End If

            Return Nothing
        End Function

        Private Sub CompileSynthesizedMethods(privateImplClass As PrivateImplementationDetails)
            Debug.Assert(_moduleBeingBuiltOpt IsNot Nothing)

            Dim compilationState As New TypeCompilationState(_compilation, _moduleBeingBuiltOpt, initializeComponentOpt:=Nothing)
            For Each methodDef In privateImplClass.GetMethods(Nothing)
                Dim method = DirectCast(methodDef.GetInternalSymbol(), MethodSymbol)
                Dim diagnosticsThisMethod = BindingDiagnosticBag.GetInstance(_diagnostics)

                Dim boundBody = method.GetBoundMethodBody(compilationState, diagnosticsThisMethod)

                If DoEmitPhase AndAlso Not diagnosticsThisMethod.HasAnyErrors Then
                    Dim emittedBody = GenerateMethodBody(_moduleBeingBuiltOpt,
                                                         method,
                                                         methodOrdinal:=DebugId.UndefinedOrdinal,
                                                         block:=boundBody,
                                                         lambdaDebugInfo:=ImmutableArray(Of EncLambdaInfo).Empty,
                                                         orderedLambdaRuntimeRudeEdits:=ImmutableArray(Of LambdaRuntimeRudeEditInfo).Empty,
                                                         closureDebugInfo:=ImmutableArray(Of EncClosureInfo).Empty,
                                                         stateMachineStateDebugInfos:=ImmutableArray(Of StateMachineStateDebugInfo).Empty,
                                                         stateMachineTypeOpt:=Nothing,
                                                         variableSlotAllocatorOpt:=Nothing,
                                                         debugDocumentProvider:=GetDebugDocumentProvider(MethodInstrumentation.Empty),
                                                         diagnostics:=diagnosticsThisMethod,
                                                         emittingPdb:=False,
                                                         codeCoverageSpans:=ImmutableArray(Of SourceSpan).Empty)

                    ' error while generating IL
                    If emittedBody Is Nothing Then
                        _diagnostics.AddRange(diagnosticsThisMethod)
                        diagnosticsThisMethod.Free()
                        Exit For
                    End If

                    _moduleBeingBuiltOpt.SetMethodBody(method, emittedBody)
                End If

                _diagnostics.AddRange(diagnosticsThisMethod)
                diagnosticsThisMethod.Free()
            Next

            Debug.Assert(Not compilationState.HasSynthesizedMethods)
            compilationState.Free()
        End Sub

        Private Sub CompileSynthesizedMethods(additionalTypes As ImmutableArray(Of NamedTypeSymbol))
            Debug.Assert(_moduleBeingBuiltOpt IsNot Nothing)

            Dim lambdaDebugInfoBuilder = ArrayBuilder(Of EncLambdaInfo).GetInstance()
            Dim lambdaRuntimeRudeEditsBuilder = ArrayBuilder(Of LambdaRuntimeRudeEditInfo).GetInstance()
            Dim lambdaRuntimeRudeEdits = ArrayBuilder(Of LambdaRuntimeRudeEditInfo).GetInstance()
            Dim closureDebugInfoBuilder = ArrayBuilder(Of EncClosureInfo).GetInstance()
            Dim stateMachineStateDebugInfoBuilder = ArrayBuilder(Of StateMachineStateDebugInfo).GetInstance()
            Dim compilationState As New TypeCompilationState(_compilation, _moduleBeingBuiltOpt, initializeComponentOpt:=Nothing)

            For Each additionalType In additionalTypes
                Dim methodOrdinal As Integer = 0

                For Each method In additionalType.GetMethodsToEmit()
                    Dim diagnosticsThisMethod = BindingDiagnosticBag.GetInstance(_diagnostics)

                    Dim boundBody = method.GetBoundMethodBody(compilationState, diagnosticsThisMethod)

                    Dim emittedBody As MethodBody = Nothing

                    If Not diagnosticsThisMethod.HasAnyErrors Then
                        Dim lazyVariableSlotAllocator As VariableSlotAllocator = Nothing
                        Dim statemachineTypeOpt As StateMachineTypeSymbol = Nothing

                        Dim delegateRelaxationIdDispenser = 0
                        Dim codeCoverageSpans As ImmutableArray(Of SourceSpan) = ImmutableArray(Of SourceSpan).Empty
                        Dim instrumentation = _moduleBeingBuiltOpt.GetMethodBodyInstrumentations(method)

                        Dim rewrittenBody = Rewriter.LowerBodyOrInitializer(
                            method,
                            methodOrdinal,
                            boundBody,
                            previousSubmissionFields:=Nothing,
                            compilationState:=compilationState,
                            instrumentations:=instrumentation,
                            codeCoverageSpans:=codeCoverageSpans,
                            debugDocumentProvider:=GetDebugDocumentProvider(instrumentation),
                            diagnostics:=diagnosticsThisMethod,
                            lazyVariableSlotAllocator:=lazyVariableSlotAllocator,
                            lambdaDebugInfoBuilder:=lambdaDebugInfoBuilder,
                            lambdaRuntimeRudeEditsBuilder:=lambdaRuntimeRudeEditsBuilder,
                            closureDebugInfoBuilder:=closureDebugInfoBuilder,
                            stateMachineStateDebugInfoBuilder:=stateMachineStateDebugInfoBuilder,
                            delegateRelaxationIdDispenser:=delegateRelaxationIdDispenser,
                            stateMachineTypeOpt:=statemachineTypeOpt,
                            allowOmissionOfConditionalCalls:=_moduleBeingBuiltOpt.AllowOmissionOfConditionalCalls,
                            isBodySynthesized:=True)

                        If DoEmitPhase AndAlso Not diagnosticsThisMethod.HasAnyErrors Then
                            ' Synthesized methods have no ordinal stored in custom debug information
                            ' (only user-defined methods have ordinals).

                            lambdaRuntimeRudeEdits.Sort(Function(x, y) x.LambdaId.CompareTo(y.LambdaId))

                            emittedBody = GenerateMethodBody(_moduleBeingBuiltOpt,
                                                             method,
                                                             DebugId.UndefinedOrdinal,
                                                             rewrittenBody,
                                                             lambdaDebugInfoBuilder.ToImmutable(),
                                                             orderedLambdaRuntimeRudeEdits:=lambdaRuntimeRudeEdits.ToImmutable(),
                                                             closureDebugInfoBuilder.ToImmutable(),
                                                             stateMachineStateDebugInfoBuilder.ToImmutable(),
                                                             statemachineTypeOpt,
                                                             lazyVariableSlotAllocator,
                                                             debugDocumentProvider:=Nothing,
                                                             diagnostics:=diagnosticsThisMethod,
                                                             emittingPdb:=False,
                                                             codeCoverageSpans:=codeCoverageSpans)
                        End If
                    End If

                    _diagnostics.AddRange(diagnosticsThisMethod)
                    diagnosticsThisMethod.Free()

                    ' error while generating IL
                    If emittedBody Is Nothing Then
                        If DoEmitPhase Then
                            Exit For
                        End If
                    Else
                        _moduleBeingBuiltOpt.SetMethodBody(method, emittedBody)
                    End If

                    lambdaDebugInfoBuilder.Clear()
                    closureDebugInfoBuilder.Clear()
                    stateMachineStateDebugInfoBuilder.Clear()

                    methodOrdinal += 1
                Next
            Next

            If Not _diagnostics.HasAnyErrors() Then
                CompileSynthesizedMethods(compilationState)
            End If

            compilationState.Free()
            lambdaDebugInfoBuilder.Free()
            closureDebugInfoBuilder.Free()
            stateMachineStateDebugInfoBuilder.Free()
            lambdaRuntimeRudeEditsBuilder.Free()
        End Sub

        Private Sub CompileSynthesizedMethods(compilationState As TypeCompilationState)
            Debug.Assert(_moduleBeingBuiltOpt IsNot Nothing)

            If Not (DoEmitPhase AndAlso compilationState.HasSynthesizedMethods) Then
                Return
            End If

            For Each methodWithBody In compilationState.SynthesizedMethods
                If Not methodWithBody.Body.HasErrors Then
                    Dim method = methodWithBody.Method
                    Dim diagnosticsThisMethod = BindingDiagnosticBag.GetInstance(_diagnostics)

                    Dim emittedBody = GenerateMethodBody(_moduleBeingBuiltOpt,
                                                         method,
                                                         methodOrdinal:=DebugId.UndefinedOrdinal,
                                                         block:=methodWithBody.Body,
                                                         lambdaDebugInfo:=ImmutableArray(Of EncLambdaInfo).Empty,
                                                         orderedLambdaRuntimeRudeEdits:=ImmutableArray(Of LambdaRuntimeRudeEditInfo).Empty,
                                                         closureDebugInfo:=ImmutableArray(Of EncClosureInfo).Empty,
                                                         stateMachineStateDebugInfos:=methodWithBody.StateMachineStatesDebugInfo,
                                                         stateMachineTypeOpt:=methodWithBody.StateMachineType,
                                                         variableSlotAllocatorOpt:=Nothing,
                                                         debugDocumentProvider:=GetDebugDocumentProvider(MethodInstrumentation.Empty),
                                                         diagnostics:=diagnosticsThisMethod,
                                                         emittingPdb:=_emittingPdb,
                                                         codeCoverageSpans:=ImmutableArray(Of SourceSpan).Empty)

                    _diagnostics.AddRange(diagnosticsThisMethod)
                    diagnosticsThisMethod.Free()

                    ' error while generating IL
                    If emittedBody Is Nothing Then
                        Exit For
                    End If

                    _moduleBeingBuiltOpt.SetMethodBody(method, emittedBody)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Detects cycles in constructor invocations based on the 'constructor-calls-constructor'
        ''' map provided in 'constructorCallMap', reports errors if found.
        '''
        ''' NOTE: 'constructorCallMap' is being mutated by this method
        ''' </summary>
        Private Sub DetectAndReportCyclesInConstructorCalls(constructorCallMap As Dictionary(Of MethodSymbol, MethodSymbol), diagnostics As BindingDiagnosticBag)

            Debug.Assert(constructorCallMap.Count > 0)

            Dim constructorsInPath As New Dictionary(Of MethodSymbol, Integer)
            Dim constructorsPath = ArrayBuilder(Of MethodSymbol).GetInstance()

            Dim currentMethod As MethodSymbol = constructorCallMap.Keys.First()

            ' Cycle constructor calls
            Do
                '  Where this constructor points to?
                Dim currentMethodPointTo As MethodSymbol = Nothing
                If Not constructorCallMap.TryGetValue(currentMethod, currentMethodPointTo) Then

                    ' We didn't find anything, which means we maybe already processed 'currentMethod'
                    ' or it does not reference another constructor of this type, or there is something wrong with it;

                    ' In any case we can restart iteration, none of the constructors in path are part of cycles

                Else
                    ' 'currentMethod' references another constructor; we may safely remove 'currentMethod'
                    ' from 'constructorCallMap' because we won't need to process it again
                    constructorCallMap.Remove(currentMethod)

                    constructorsInPath.Add(currentMethod, constructorsPath.Count)
                    constructorsPath.Add(currentMethod)

                    Dim foundAt As Integer
                    If constructorsInPath.TryGetValue(currentMethodPointTo, foundAt) Then

                        ' We found a cycle which starts at 'foundAt' and goes to the end of constructorsPath
                        constructorsPath.Add(currentMethodPointTo)
                        ReportConstructorCycles(foundAt, constructorsPath.Count - 1, constructorsPath, diagnostics)

                        ' We can restart iteration, none of the constructors
                        ' in path may be part of other cycles

                    Else
                        ' No cycles so far, just move to the next constructor
                        currentMethod = currentMethodPointTo
                        Continue Do
                    End If

                End If

                ' Restart iteration
                constructorsInPath.Clear()
                constructorsPath.Clear()

                If constructorCallMap.Count = 0 Then
                    ' Nothing left
                    constructorsPath.Free()
                    Exit Sub
                End If

                currentMethod = constructorCallMap.Keys.First()
            Loop

            Throw ExceptionUtilities.Unreachable

        End Sub

        ''' <summary> All the constructors in the cycle will be reported </summary>
        Private Shared Sub ReportConstructorCycles(startsAt As Integer, endsAt As Integer,
                                            path As ArrayBuilder(Of MethodSymbol),
                                            diagnostics As BindingDiagnosticBag)

            ' Cycle is: constructorsCycle(startsAt) -->
            '               constructorsCycle(startsAt + 1) -->
            '                   ....
            '                       constructorsCycle(endsAt) = constructorsCycle(startsAt)
            '
            ' In case the constructor constructorsCycle(startsAt) calls itself, startsAt = endsAt + 1

            Debug.Assert(startsAt <= endsAt)
            Debug.Assert(path(startsAt).Equals(path(endsAt)))

            '  Generate cycle info
            Dim diagnosticInfos = ArrayBuilder(Of DiagnosticInfo).GetInstance()
            Dim referencingMethod As MethodSymbol = path(startsAt)
            For i = startsAt + 1 To endsAt
                Dim referencedMethod As MethodSymbol = path(i)
                diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_SubNewCycle2, referencingMethod, referencedMethod))
                referencingMethod = referencedMethod
            Next

            '  Report Errors for all constructors in the cycle
            For i = startsAt To endsAt - 1
                referencingMethod = path(i)

                '  Report an error
                diagnostics.Add(
                    New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_SubNewCycle1, referencingMethod,
                                                          New CompoundDiagnosticInfo(diagnosticInfos.ToArray())),
                                   referencingMethod.GetFirstLocation()))

                '  Rotate 'diagnosticInfos' for the next constructor
                If diagnosticInfos.Count > 1 Then
                    Dim diagnostic = diagnosticInfos(0)
                    diagnosticInfos.RemoveAt(0)
                    diagnosticInfos.Add(diagnostic)
                End If
            Next

            diagnosticInfos.Free()
        End Sub

        Friend Shared Function CanBindMethod(method As MethodSymbol) As Boolean
            If method.IsExternalMethod OrElse method.IsMustOverride Then
                Return False
            End If

            ' Synthesized struct constructors are not emitted.
            If method.IsDefaultValueTypeConstructor() Then
                Return False
            End If

            If method.IsPartialWithoutImplementation Then
                ' Exclude partial methods without implementation
                Return False
            End If

            If Not method.IsImplicitlyDeclared Then
                ' Only compile the method if the method has a body.
                Dim sourceMethod = TryCast(method, SourceMethodSymbol)
                If sourceMethod Is Nothing OrElse sourceMethod.BlockSyntax Is Nothing Then
                    Return False
                End If
            End If

            Return True
        End Function

        ''' <summary>
        ''' Compiles the method.
        ''' </summary>
        ''' <param name="referencedConstructor">
        ''' If the method being compiled is a constructor, CompileMethod returns in this parameter
        ''' the symbol of the constructor called from the one being compiled either explicitly or implicitly.
        ''' For structure constructors calling parameterless constructor returns the synthesized constructor symbol.
        ''' </param>
        Private Sub CompileMethod(
            method As MethodSymbol,
            methodOrdinal As Integer,
            ByRef withEventPropertyIdDispenser As Integer,
            ByRef delegateRelaxationIdDispenser As Integer,
            filter As Predicate(Of Symbol),
            compilationState As TypeCompilationState,
            processedInitializers As Binder.ProcessedFieldOrPropertyInitializers,
            containingTypeBinder As Binder,
            previousSubmissionFields As SynthesizedSubmissionFields,
            Optional ByRef referencedConstructor As MethodSymbol = Nothing
        )
            '' TODO: add filtering as follows
            'If filter IsNot Nothing AndAlso Not filter(method) Then
            '    Return
            'End If

            _cancellationToken.ThrowIfCancellationRequested()

            Debug.Assert(Not (method.IsPartial AndAlso method.PartialImplementationPart Is Nothing))

            Dim sourceMethod = TryCast(method, SourceMethodSymbol)
            'get cached diagnostics if not building and we have 'em
            If Not DoLoweringPhase AndAlso (sourceMethod IsNot Nothing) Then
                Debug.Assert(Me._diagnostics.DependenciesBag Is Nothing)
                Dim cachedDiagnostics = sourceMethod.Diagnostics
                If Not cachedDiagnostics.IsDefault Then
                    Me._diagnostics.AddRange(cachedDiagnostics)
                    Return
                End If
            End If

            If Not CanBindMethod(method) Then
                If sourceMethod IsNot Nothing AndAlso sourceMethod.SetDiagnostics(ImmutableArray(Of Diagnostic).Empty) Then
                    sourceMethod.DeclaringCompilation.SymbolDeclaredEvent(method)
                End If

                Return
            End If

            ' In order to avoid generating code for methods with errors, we create a diagnostic bag just for this method.
            Dim diagsForCurrentMethod = BindingDiagnosticBag.GetInstance(_diagnostics)

            Dim methodBinderOpt As Binder = Nothing
            Dim injectConstructorCall As Boolean
            Dim block = BindAndAnalyzeMethodBody(method, compilationState, diagsForCurrentMethod, containingTypeBinder, referencedConstructor, injectConstructorCall, methodBinderOpt)

            ' Initializers need to be flow-analyzed for warnings like 'Function '??' doesn't
            ' return a value on all code paths' or unreferenced variables.
            ' Because if there are any initializers they will eventually get to one or more constructors
            ' it does not matter which constructor is being used as a method symbol for their analysis,
            ' so we don't perform any analysis of which constructor symbol to pass to EnsureInitializersAnalyzed,
            ' but call this method for on all constructor symbols making sure instance/static initializers
            ' are analyzed on the first instance/static constructor processed
            processedInitializers.EnsureInitializersAnalyzed(method, diagsForCurrentMethod.DiagnosticBag)

            Dim hasErrors = _hasDeclarationErrors OrElse diagsForCurrentMethod.HasAnyErrors() OrElse processedInitializers.HasAnyErrors OrElse block.HasErrors
            SetGlobalErrorIfTrue(hasErrors)

            If sourceMethod IsNot Nothing Then
                Dim compilation = compilationState.Compilation

                compilation.RegisterPossibleUpcomingEventEnqueue()

                Try
                    If sourceMethod.SetDiagnostics(diagsForCurrentMethod.DiagnosticBag.ToReadOnly()) Then
                        If compilation.ShouldAddEvent(method) Then
                            If block Is Nothing Then
                                compilation.SymbolDeclaredEvent(sourceMethod)
                            Else
                                ' If compilation has a caching semantic model provider, then cache the already-computed bound tree
                                ' onto the semantic model and store it on the event.
                                Dim semanticModelWithCachedBoundNodes As SyntaxTreeSemanticModel = Nothing
                                Dim cachingSemanticModelProvider = TryCast(compilation.SemanticModelProvider, CachingSemanticModelProvider)
                                If cachingSemanticModelProvider IsNot Nothing Then
                                    Dim syntax = block.Syntax
                                    semanticModelWithCachedBoundNodes = CType(cachingSemanticModelProvider.GetSemanticModel(syntax.SyntaxTree, compilation), SyntaxTreeSemanticModel)
                                    Dim memberModel = CType(semanticModelWithCachedBoundNodes.GetMemberSemanticModel(syntax), MethodBodySemanticModel)
                                    If memberModel IsNot Nothing Then
                                        memberModel.CacheBoundNodes(block, syntax)
                                    End If
                                End If

                                compilation.EventQueue.TryEnqueue(New SymbolDeclaredCompilationEvent(compilation, method, semanticModelWithCachedBoundNodes))
                            End If
                        End If
                    End If
                Finally
                    compilation.UnregisterPossibleUpcomingEventEnqueue()
                End Try
            End If

            If Not DoLoweringPhase AndAlso sourceMethod IsNot Nothing Then
                Debug.Assert(Me._diagnostics.DependenciesBag Is Nothing)
                _diagnostics.AddRange(sourceMethod.Diagnostics)
                Return
            End If

            If DoLoweringPhase AndAlso Not hasErrors Then
                LowerAndEmitMethod(method,
                                   methodOrdinal,
                                   block,
                                   If(methodBinderOpt, containingTypeBinder),
                                   compilationState,
                                   diagsForCurrentMethod,
                                   processedInitializers,
                                   previousSubmissionFields,
                                   If(injectConstructorCall, referencedConstructor, Nothing),
                                   delegateRelaxationIdDispenser)

                ' if method happen to handle events of a base WithEvents, ensure that we have an overriding WithEvents property
                Dim handledEvents = method.HandledEvents
                If Not handledEvents.IsEmpty Then
                    CreateSyntheticWithEventOverridesIfNeeded(handledEvents,
                                                              delegateRelaxationIdDispenser,
                                                              withEventPropertyIdDispenser,
                                                              compilationState,
                                                              containingTypeBinder,
                                                              diagsForCurrentMethod,
                                                              previousSubmissionFields)
                End If
            End If

            ' Add the generated diagnostics into the full diagnostic bag.
            _diagnostics.AddRange(diagsForCurrentMethod)
            diagsForCurrentMethod.Free()
        End Sub

        ''' <summary>
        ''' If any of the "Handles" in the list have synthetic WithEvent override
        ''' as a container, then this method will (if not done already) inject
        ''' property/accessors symbol into the emit module and assign bodies to the accessors.
        ''' </summary>
        Private Sub CreateSyntheticWithEventOverridesIfNeeded(handledEvents As ImmutableArray(Of HandledEvent),
                                                              ByRef delegateRelaxationIdDispenser As Integer,
                                                              ByRef withEventPropertyIdDispenser As Integer,
                                                              compilationState As TypeCompilationState,
                                                              containingTypeBinder As Binder,
                                                              diagnostics As BindingDiagnosticBag,
                                                              previousSubmissionFields As SynthesizedSubmissionFields)

            Debug.Assert(_moduleBeingBuiltOpt Is Nothing OrElse _moduleBeingBuiltOpt.AllowOmissionOfConditionalCalls)

            For Each handledEvent In handledEvents
                If handledEvent.HandlesKind <> HandledEventKind.WithEvents Then
                    Continue For
                End If

                Dim prop = TryCast(handledEvent.hookupMethod.AssociatedSymbol, SynthesizedOverridingWithEventsProperty)
                If prop Is Nothing Then
                    Continue For
                End If

                Dim getter = prop.GetMethod
                If compilationState.HasMethodWrapper(getter) Then
                    Continue For
                End If

                Dim setter = prop.SetMethod
                Dim containingType = prop.ContainingType
                Debug.Assert(containingType Is getter.ContainingType AndAlso containingType Is setter.ContainingType)

                Dim getterBody = getter.GetBoundMethodBody(compilationState, diagnostics, containingTypeBinder)

                ' no need to rewrite getter, they are pretty simple and
                ' are already in a lowered form.
                compilationState.AddMethodWrapper(getter, getter, getterBody)
                _moduleBeingBuiltOpt.AddSynthesizedDefinition(containingType, getter.GetCciAdapter())

                ' setter needs to rewritten as it may require lambda conversions
                Dim setterBody = setter.GetBoundMethodBody(compilationState, diagnostics, containingTypeBinder)

                Dim lambdaDebugInfoBuilder = ArrayBuilder(Of EncLambdaInfo).GetInstance()
                Dim lambdaRuntimeRudeEditsBuilder = ArrayBuilder(Of LambdaRuntimeRudeEditInfo).GetInstance()
                Dim closureDebugInfoBuilder = ArrayBuilder(Of EncClosureInfo).GetInstance()
                Dim stateMachineStateDebugInfoBuilder = ArrayBuilder(Of StateMachineStateDebugInfo).GetInstance()
                Dim methodInstrumentations = _moduleBeingBuiltOpt.GetMethodBodyInstrumentations(setter)

                setterBody = Rewriter.LowerBodyOrInitializer(setter,
                                                             withEventPropertyIdDispenser,
                                                             setterBody,
                                                             previousSubmissionFields,
                                                             compilationState,
                                                             instrumentations:=methodInstrumentations,
                                                             codeCoverageSpans:=ImmutableArray(Of SourceSpan).Empty,
                                                             debugDocumentProvider:=GetDebugDocumentProvider(methodInstrumentations),
                                                             diagnostics:=diagnostics,
                                                             lazyVariableSlotAllocator:=Nothing,
                                                             lambdaDebugInfoBuilder:=lambdaDebugInfoBuilder,
                                                             lambdaRuntimeRudeEditsBuilder:=lambdaRuntimeRudeEditsBuilder,
                                                             closureDebugInfoBuilder:=closureDebugInfoBuilder,
                                                             stateMachineStateDebugInfoBuilder:=stateMachineStateDebugInfoBuilder,
                                                             delegateRelaxationIdDispenser:=delegateRelaxationIdDispenser,
                                                             stateMachineTypeOpt:=Nothing,
                                                             allowOmissionOfConditionalCalls:=True,
                                                             isBodySynthesized:=True)

                ' There shall be no lambdas and no awaits/yields in the synthesized accessor but delegate relaxation conversions:
                Debug.Assert(lambdaDebugInfoBuilder.IsEmpty())
                Debug.Assert(lambdaRuntimeRudeEditsBuilder.IsEmpty())
                Debug.Assert(closureDebugInfoBuilder.IsEmpty())
                Debug.Assert(stateMachineStateDebugInfoBuilder.IsEmpty())

                lambdaDebugInfoBuilder.Free()
                lambdaRuntimeRudeEditsBuilder.Free()
                closureDebugInfoBuilder.Free()
                stateMachineStateDebugInfoBuilder.Free()

                compilationState.AddMethodWrapper(setter, setter, setterBody)
                _moduleBeingBuiltOpt.AddSynthesizedDefinition(containingType, setter.GetCciAdapter())

                ' add property too
                _moduleBeingBuiltOpt.AddSynthesizedDefinition(containingType, prop.GetCciAdapter())
                withEventPropertyIdDispenser += 1
            Next
        End Sub

        ''' <summary>
        ''' Assuming the statement is a constructor call wrapped in bound expression
        ''' statement, get the method symbol being called
        ''' </summary>
        Private Shared Function TryGetMethodCalledInBoundExpressionStatement(stmt As BoundExpressionStatement) As MethodSymbol

            '  No statement provided or has errors
            If stmt Is Nothing OrElse stmt.HasErrors Then
                Return Nothing
            End If

            '  Statement is not a call
            Dim expression As BoundExpression = stmt.Expression
            If expression.Kind <> BoundKind.Call Then
                Return Nothing
            End If

            Return DirectCast(expression, BoundCall).Method
        End Function

        Private Sub LowerAndEmitMethod(
            method As MethodSymbol,
            methodOrdinal As Integer,
            block As BoundBlock,
            binderOpt As Binder,
            compilationState As TypeCompilationState,
            diagsForCurrentMethod As BindingDiagnosticBag,
            processedInitializers As Binder.ProcessedFieldOrPropertyInitializers,
            previousSubmissionFields As SynthesizedSubmissionFields,
            constructorToInject As MethodSymbol,
            ByRef delegateRelaxationIdDispenser As Integer
        )
            Debug.Assert(diagsForCurrentMethod.AccumulatesDiagnostics)

            Dim constructorInitializerOpt = If(constructorToInject Is Nothing,
                                               Nothing,
                                               BindDefaultConstructorInitializer(method, constructorToInject, diagsForCurrentMethod, binderOpt))

            If diagsForCurrentMethod.HasAnyErrors Then
                Return
            End If

            If constructorInitializerOpt IsNot Nothing AndAlso constructorInitializerOpt.HasErrors Then
                Return
            End If

            Dim body As BoundBlock
            If method.MethodKind = MethodKind.Constructor OrElse method.MethodKind = MethodKind.SharedConstructor Then
                If method.IsScriptConstructor Then
                    body = block
                Else
                    ' Turns field initializers into bound assignment statements and top-level script statements into bound statements in the beginning the body.
                    body = InitializerRewriter.BuildConstructorBody(compilationState, method, constructorInitializerOpt, processedInitializers, block)
                End If
            ElseIf method.IsScriptInitializer Then
                ' The body only includes bound initializers and a return statement. The rest is filled in later in this method.
                body = InitializerRewriter.BuildScriptInitializerBody(DirectCast(method, SynthesizedInteractiveInitializerMethod), processedInitializers, block)
            Else
                body = block
            End If

            Dim diagnostics As BindingDiagnosticBag = diagsForCurrentMethod

            If method.IsImplicitlyDeclared AndAlso
               method.AssociatedSymbol IsNot Nothing AndAlso
               method.AssociatedSymbol.IsMyGroupCollectionProperty Then
                diagnostics = BindingDiagnosticBag.GetInstance(diagsForCurrentMethod)
            End If

            Dim lazyVariableSlotAllocator As VariableSlotAllocator = Nothing
            Dim stateMachineTypeOpt As StateMachineTypeSymbol = Nothing
            Dim allowOmissionOfConditionalCalls = _moduleBeingBuiltOpt Is Nothing OrElse _moduleBeingBuiltOpt.AllowOmissionOfConditionalCalls
            Dim lambdaDebugInfoBuilder = ArrayBuilder(Of EncLambdaInfo).GetInstance()
            Dim lambdaRuntimeRudeEditsBuilder = ArrayBuilder(Of LambdaRuntimeRudeEditInfo).GetInstance()
            Dim lambdaRuntimeRudeEdits = ArrayBuilder(Of LambdaRuntimeRudeEditInfo).GetInstance()
            Dim closureDebugInfoBuilder = ArrayBuilder(Of EncClosureInfo).GetInstance()
            Dim stateMachineStateDebugInfoBuilder = ArrayBuilder(Of StateMachineStateDebugInfo).GetInstance()
            Dim codeCoverageSpans As ImmutableArray(Of SourceSpan) = ImmutableArray(Of SourceSpan).Empty
            Dim instrumentation = If(_moduleBeingBuiltOpt IsNot Nothing, _moduleBeingBuiltOpt.GetMethodBodyInstrumentations(method), Nothing)

            Try
                body = Rewriter.LowerBodyOrInitializer(method,
                                                       methodOrdinal,
                                                       body,
                                                       previousSubmissionFields,
                                                       compilationState,
                                                       instrumentation,
                                                       codeCoverageSpans,
                                                       GetDebugDocumentProvider(instrumentation),
                                                       diagnostics,
                                                       lazyVariableSlotAllocator,
                                                       lambdaDebugInfoBuilder,
                                                       lambdaRuntimeRudeEditsBuilder,
                                                       closureDebugInfoBuilder,
                                                       stateMachineStateDebugInfoBuilder,
                                                       delegateRelaxationIdDispenser,
                                                       stateMachineTypeOpt,
                                                       allowOmissionOfConditionalCalls,
                                                       isBodySynthesized:=False)

                ' The submission initializer has to be constructed after the body is rewritten (all previous submission references are visited):
                Dim submissionInitialization = If(method.IsSubmissionConstructor,
                SynthesizedSubmissionConstructorSymbol.MakeSubmissionInitialization(block.Syntax, method, previousSubmissionFields, _compilation),
                ImmutableArray(Of BoundStatement).Empty)
                Dim hasErrors = body.HasErrors OrElse diagsForCurrentMethod.HasAnyErrors OrElse (diagnostics IsNot diagsForCurrentMethod AndAlso diagnostics.HasAnyErrors)
                SetGlobalErrorIfTrue(hasErrors)

                ' Actual emitting is only done if we have a module in which to emit and no errors so far.
                If _moduleBeingBuiltOpt Is Nothing OrElse hasErrors Then
                    If diagnostics IsNot diagsForCurrentMethod Then
                        DirectCast(method.AssociatedSymbol, SynthesizedMyGroupCollectionPropertySymbol).RelocateDiagnostics(diagnostics.DiagnosticBag, diagsForCurrentMethod.DiagnosticBag)
                        diagsForCurrentMethod.AddDependencies(diagnostics)
                        diagnostics.Free()
                    End If

                    Return
                End If

                ' now we have everything we need to build complete submission
                If method.IsScriptConstructor Then
                    Dim boundStatements = ArrayBuilder(Of BoundStatement).GetInstance()
                    Debug.Assert(constructorInitializerOpt IsNot Nothing)
                    boundStatements.Add(constructorInitializerOpt)
                    boundStatements.AddRange(submissionInitialization)
                    boundStatements.Add(body)
                    body = New BoundBlock(body.Syntax, Nothing, ImmutableArray(Of LocalSymbol).Empty, boundStatements.ToImmutableAndFree(), body.HasErrors).MakeCompilerGenerated()
                End If

                If DoEmitPhase Then
                    ' NOTE: additional check for statement.HasErrors is needed to identify parse errors which didn't get into diagsForCurrentMethod

                    lambdaRuntimeRudeEdits.Sort(Function(x, y) x.LambdaId.CompareTo(y.LambdaId))

                    Dim methodBody As MethodBody = GenerateMethodBody(_moduleBeingBuiltOpt,
                                                                  method,
                                                                  methodOrdinal,
                                                                  body,
                                                                  lambdaDebugInfoBuilder.ToImmutable(),
                                                                  orderedLambdaRuntimeRudeEdits:=lambdaRuntimeRudeEdits.ToImmutable(),
                                                                  closureDebugInfoBuilder.ToImmutable(),
                                                                  stateMachineStateDebugInfoBuilder.ToImmutable(),
                                                                  stateMachineTypeOpt,
                                                                  lazyVariableSlotAllocator,
                                                                  GetDebugDocumentProvider(instrumentation),
                                                                  diagnostics,
                                                                  emittingPdb:=_emittingPdb,
                                                                  codeCoverageSpans:=codeCoverageSpans)

                    _moduleBeingBuiltOpt.SetMethodBody(If(method.PartialDefinitionPart, method), methodBody)
                End If

                If diagnostics IsNot diagsForCurrentMethod Then
                    DirectCast(method.AssociatedSymbol, SynthesizedMyGroupCollectionPropertySymbol).RelocateDiagnostics(diagnostics.DiagnosticBag, diagsForCurrentMethod.DiagnosticBag)
                    diagsForCurrentMethod.AddDependencies(diagnostics)
                    diagnostics.Free()
                End If
            Finally
                lambdaDebugInfoBuilder.Free()
                closureDebugInfoBuilder.Free()
                lambdaRuntimeRudeEditsBuilder.Free()
            End Try
        End Sub

        Friend Shared Function GenerateMethodBody(moduleBuilder As PEModuleBuilder,
                                                  method As MethodSymbol,
                                                  methodOrdinal As Integer,
                                                  block As BoundStatement,
                                                  lambdaDebugInfo As ImmutableArray(Of EncLambdaInfo),
                                                  orderedLambdaRuntimeRudeEdits As ImmutableArray(Of LambdaRuntimeRudeEditInfo),
                                                  closureDebugInfo As ImmutableArray(Of EncClosureInfo),
                                                  stateMachineStateDebugInfos As ImmutableArray(Of StateMachineStateDebugInfo),
                                                  stateMachineTypeOpt As StateMachineTypeSymbol,
                                                  variableSlotAllocatorOpt As VariableSlotAllocator,
                                                  debugDocumentProvider As DebugDocumentProvider,
                                                  diagnostics As BindingDiagnosticBag,
                                                  emittingPdb As Boolean,
                                                  codeCoverageSpans As ImmutableArray(Of SourceSpan)) As MethodBody
            Debug.Assert(diagnostics.AccumulatesDiagnostics)

            Dim compilation = moduleBuilder.Compilation
            Dim localSlotManager = New LocalSlotManager(variableSlotAllocatorOpt)
            Dim optimizations = compilation.Options.OptimizationLevel

            If method.IsEmbedded Then
                optimizations = OptimizationLevel.Release
            End If

            Dim builder As ILBuilder = New ILBuilder(moduleBuilder, localSlotManager, diagnostics.DiagnosticBag, optimizations, areLocalsZeroed:=True)

            Try
                Debug.Assert(Not diagnostics.HasAnyErrors)

                Dim moveNextBodyDebugInfoOpt As StateMachineMoveNextBodyDebugInfo = Nothing
                Dim codeGen = New CodeGen.CodeGenerator(method, block, builder, moduleBuilder, diagnostics.DiagnosticBag, optimizations, emittingPdb)

                If diagnostics.HasAnyErrors() Then
                    Return Nothing
                End If

                ' We need to save additional debugging information for MoveNext of an async state machine.
                Dim isAsyncStateMachine As Boolean
                Dim kickoffMethod As MethodSymbol

                Dim stateMachineMethod = TryCast(method, SynthesizedStateMachineMethod)
                If stateMachineMethod IsNot Nothing AndAlso method.Name = WellKnownMemberNames.MoveNextMethodName Then

                    kickoffMethod = stateMachineMethod.StateMachineType.KickoffMethod
                    Debug.Assert(kickoffMethod IsNot Nothing)

                    isAsyncStateMachine = kickoffMethod.IsAsync

                    ' Async Sub may be partial. Debug info needs to be associated with the emitted definition,
                    ' but the kickoff method is the method implementation (the part with body).
                    kickoffMethod = If(kickoffMethod.PartialDefinitionPart, kickoffMethod)
                Else
                    kickoffMethod = Nothing
                    isAsyncStateMachine = False
                End If

                If isAsyncStateMachine Then

                    Dim asyncCatchHandlerOffset As Integer = -1
                    Dim asyncYieldPoints As ImmutableArray(Of Integer) = Nothing
                    Dim asyncResumePoints As ImmutableArray(Of Integer) = Nothing
                    codeGen.Generate(asyncCatchHandlerOffset, asyncYieldPoints, asyncResumePoints)

                    ' The exception handler IL offset is used by the debugger to treat exceptions caught by the marked catch block as "user unhandled".
                    ' This is important for async void because async void exceptions generally result in the process being terminated,
                    ' but without anything useful on the call stack. Async Task methods on the other hand return exceptions as the result of the Task.
                    ' So it is undesirable to consider these exceptions "user unhandled" since there may well be user code that is awaiting the task.
                    ' This is a heuristic since it's possible that there is no user code awaiting the task.
                    moveNextBodyDebugInfoOpt = New AsyncMoveNextBodyDebugInfo(
                        kickoffMethod.GetCciAdapter(),
                        If(kickoffMethod.IsSub, asyncCatchHandlerOffset, -1),
                        asyncYieldPoints,
                        asyncResumePoints)
                Else
                    codeGen.Generate()

                    If kickoffMethod IsNot Nothing Then
                        moveNextBodyDebugInfoOpt = New IteratorMoveNextBodyDebugInfo(kickoffMethod.GetCciAdapter())
                    End If
                End If

                ' Compiler-generated MoveNext methods have hoisted local scopes.
                ' These are built by call to CodeGen.Generate.
                ' This information is not emitted to Windows PDBs.
                Dim stateMachineHoistedLocalScopes = If(kickoffMethod Is Nothing OrElse moduleBuilder.DebugInformationFormat = DebugInformationFormat.Pdb,
                    Nothing, builder.GetHoistedLocalScopes())

                ' Translate the imports even if we are not writing PDBs. The translation has an impact on generated metadata
                ' and we don't want to emit different metadata depending on whether or we emit with PDB stream.
                ' TODO (https://github.com/dotnet/roslyn/issues/2846): This will need to change for member initializers in partial class.
                Dim importScopeOpt = If(method.Syntax IsNot Nothing,
                                        moduleBuilder.SourceModule.TryGetSourceFile(method.Syntax.SyntaxTree)?.Translate(moduleBuilder, diagnostics.DiagnosticBag),
                                        Nothing)

                If diagnostics.HasAnyErrors() Then
                    Return Nothing
                End If

                ' We will only save the IL builders when running tests.
                moduleBuilder.TestData?.SetMethodILBuilder(method, builder.GetSnapshot())

                Dim stateMachineHoistedLocalSlots As ImmutableArray(Of EncHoistedLocalInfo) = Nothing
                Dim stateMachineAwaiterSlots As ImmutableArray(Of Cci.ITypeReference) = Nothing
                If optimizations = OptimizationLevel.Debug AndAlso stateMachineTypeOpt IsNot Nothing Then
                    Debug.Assert(method.IsAsync OrElse method.IsIterator)
                    GetStateMachineSlotDebugInfo(moduleBuilder, moduleBuilder.GetSynthesizedFields(stateMachineTypeOpt), variableSlotAllocatorOpt, diagnostics.DiagnosticBag, stateMachineHoistedLocalSlots, stateMachineAwaiterSlots)
                    Debug.Assert(Not diagnostics.HasAnyErrors())
                End If

                Dim localScopes = builder.GetAllScopes()

                Return New MethodBody(builder.RealizedIL,
                                      builder.MaxStack,
                                      If(method.PartialDefinitionPart, method).GetCciAdapter(),
                                      If(variableSlotAllocatorOpt?.MethodId, New DebugId(methodOrdinal, moduleBuilder.CurrentGenerationOrdinal)),
                                      builder.LocalSlotManager.LocalsInOrder(),
                                      builder.RealizedSequencePoints,
                                      debugDocumentProvider,
                                      builder.RealizedExceptionHandlers,
                                      areLocalsZeroed:=True,
                                      hasStackalloc:=False,
                                      localScopes,
                                      hasDynamicLocalVariables:=False,
                                      importScopeOpt:=importScopeOpt,
                                      lambdaDebugInfo:=lambdaDebugInfo,
                                      orderedLambdaRuntimeRudeEdits:=orderedLambdaRuntimeRudeEdits,
                                      closureDebugInfo:=closureDebugInfo,
                                      stateMachineTypeNameOpt:=stateMachineTypeOpt?.Name, ' TODO: remove or update AddedOrChangedMethodInfo
                                      stateMachineHoistedLocalScopes:=stateMachineHoistedLocalScopes,
                                      stateMachineHoistedLocalSlots:=stateMachineHoistedLocalSlots,
                                      stateMachineAwaiterSlots:=stateMachineAwaiterSlots,
                                      stateMachineStatesDebugInfo:=StateMachineStatesDebugInfo.Create(variableSlotAllocatorOpt, stateMachineStateDebugInfos),
                                      stateMachineMoveNextDebugInfoOpt:=moveNextBodyDebugInfoOpt,
                                      codeCoverageSpans:=codeCoverageSpans,
                                      isPrimaryConstructor:=False)
            Finally
                ' Free resources used by the basic blocks in the builder.
                builder.FreeBasicBlocks()
            End Try
        End Function

        Private Shared Sub GetStateMachineSlotDebugInfo(moduleBuilder As PEModuleBuilder,
                                                        fieldDefs As IEnumerable(Of Cci.IFieldDefinition),
                                                        variableSlotAllocatorOpt As VariableSlotAllocator,
                                                        diagnostics As DiagnosticBag,
                                                        ByRef hoistedVariableSlots As ImmutableArray(Of EncHoistedLocalInfo),
                                                        ByRef awaiterSlots As ImmutableArray(Of Cci.ITypeReference))

            Dim hoistedVariables = ArrayBuilder(Of EncHoistedLocalInfo).GetInstance()
            Dim awaiters = ArrayBuilder(Of Cci.ITypeReference).GetInstance()

            For Each def In fieldDefs
                Dim field = DirectCast(def.GetInternalSymbol(), StateMachineFieldSymbol)
                Dim index = field.SlotIndex

                If field.SlotDebugInfo.SynthesizedKind = SynthesizedLocalKind.AwaiterField Then
                    Debug.Assert(index >= 0)

                    While index >= awaiters.Count
                        awaiters.Add(Nothing)
                    End While

                    awaiters(index) = moduleBuilder.EncTranslateLocalVariableType(field.Type, diagnostics)
                ElseIf Not field.SlotDebugInfo.Id.IsNone Then
                    Debug.Assert(index >= 0 AndAlso field.SlotDebugInfo.SynthesizedKind.IsLongLived())

                    While index >= hoistedVariables.Count
                        ' Empty slots may be present if variables were deleted during EnC.
                        hoistedVariables.Add(New EncHoistedLocalInfo())
                    End While

                    hoistedVariables(index) = New EncHoistedLocalInfo(field.SlotDebugInfo, moduleBuilder.EncTranslateLocalVariableType(field.Type, diagnostics))
                End If
            Next

            ' Fill in empty slots for variables deleted during EnC that are not followed by an existing variable
            If variableSlotAllocatorOpt IsNot Nothing Then
                Dim previousAwaiterCount = variableSlotAllocatorOpt.PreviousAwaiterSlotCount
                While awaiters.Count < previousAwaiterCount
                    awaiters.Add(Nothing)
                End While

                Dim previousAwaiterSlotCount = variableSlotAllocatorOpt.PreviousHoistedLocalSlotCount
                While hoistedVariables.Count < previousAwaiterSlotCount
                    hoistedVariables.Add(New EncHoistedLocalInfo(True))
                End While
            End If

            hoistedVariableSlots = hoistedVariables.ToImmutableAndFree()
            awaiterSlots = awaiters.ToImmutableAndFree()
        End Sub

        Private Shared Function BindAndAnalyzeMethodBody(method As MethodSymbol,
                                                       compilationState As TypeCompilationState,
                                                       diagnostics As BindingDiagnosticBag,
                                                       containingTypeBinder As Binder,
                                                       ByRef referencedConstructor As MethodSymbol,
                                                       ByRef injectDefaultConstructorCall As Boolean,
                                                       ByRef methodBodyBinder As Binder) As BoundBlock

            Debug.Assert(diagnostics.AccumulatesDiagnostics)
            methodBodyBinder = Nothing

            Dim body = method.GetBoundMethodBody(compilationState, diagnostics, methodBodyBinder)
            Debug.Assert(body IsNot Nothing)

            Analyzer.AnalyzeMethodBody(method, body, diagnostics.DiagnosticBag)
            DiagnosticsPass.IssueDiagnostics(body, diagnostics.DiagnosticBag, method)

            Debug.Assert(method.IsFromCompilation(compilationState.Compilation))
            If Not method.IsShared AndAlso compilationState.InitializeComponentOpt IsNot Nothing AndAlso
               Not method.IsImplicitlyDeclared Then
                Try
                    InitializeComponentCallTreeBuilder.CollectCallees(compilationState, method, body)
                Catch ex As BoundTreeVisitor.CancelledByStackGuardException
                    ex.AddAnError(diagnostics)
                End Try
            End If

            '  Instance constructor should return the referenced constructor in 'referencedConstructor'
            If method.MethodKind = MethodKind.Constructor Then
                GetExplicitlyOrImplicitlyReferencedConstructor(method,
                                                               If(body IsNot Nothing AndAlso body.Statements.Length > 0, body.Statements(0), Nothing),
                                                               If(methodBodyBinder, containingTypeBinder),
                                                               diagnostics, referencedConstructor, injectDefaultConstructorCall)

                Debug.Assert(Not (injectDefaultConstructorCall AndAlso referencedConstructor IsNot Nothing) OrElse method.IsImplicitlyDeclared)
            Else
                referencedConstructor = Nothing
                injectDefaultConstructorCall = False
            End If

            Return body
        End Function

        Friend Shared Sub GetExplicitlyOrImplicitlyReferencedConstructor(method As MethodSymbol,
                                                                          theFirstStatementOpt As BoundStatement,
                                                                          binderForAccessibilityCheckOpt As Binder,
                                                                          diagnostics As BindingDiagnosticBag,
                                                                          ByRef referencedConstructor As MethodSymbol,
                                                                          ByRef injectDefaultConstructorCall As Boolean)
            referencedConstructor = Nothing

            ' class constructors must inject call to the base
            injectDefaultConstructorCall = Not method.ContainingType.IsValueType

            ' Try find explicitly called constructor, it should be the first statement in the block
            If theFirstStatementOpt IsNot Nothing Then

                '  Must be BoundExpressionStatement/BoundCall
                If theFirstStatementOpt.HasErrors Then
                    injectDefaultConstructorCall = False

                ElseIf theFirstStatementOpt.Kind = BoundKind.ExpressionStatement Then

                    Dim referencedMethod As MethodSymbol = TryGetMethodCalledInBoundExpressionStatement(DirectCast(theFirstStatementOpt, BoundExpressionStatement))
                    If referencedMethod IsNot Nothing AndAlso referencedMethod.MethodKind = MethodKind.Constructor Then
                        referencedConstructor = referencedMethod
                        injectDefaultConstructorCall = False
                    End If
                End If

            End If

            ' If we didn't find explicitly referenced constructor, use implicitly generated call
            If injectDefaultConstructorCall Then

                ' NOTE: We might generate an error in this call in case there is
                '       no parameterless constructor suitable for calling
                referencedConstructor = FindConstructorToCallByDefault(method, diagnostics, binderForAccessibilityCheckOpt)

                If referencedConstructor Is Nothing AndAlso method.ContainingType.BaseTypeNoUseSiteDiagnostics Is Nothing Then
                    ' possible if class System.Object in source doesn't have an explicit constructor
                    injectDefaultConstructorCall = False
                End If
            End If
        End Sub

        Private NotInheritable Class InitializeComponentCallTreeBuilder
            Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

            Private _calledMethods As HashSet(Of MethodSymbol)
            Private ReadOnly _containingType As NamedTypeSymbol

            Private Sub New(containingType As NamedTypeSymbol)
                _containingType = containingType
            End Sub

            Public Shared Sub CollectCallees(compilationState As TypeCompilationState, method As MethodSymbol, block As BoundBlock)
                Dim visitor As New InitializeComponentCallTreeBuilder(method.ContainingType)
                visitor.VisitBlock(block)

                If visitor._calledMethods IsNot Nothing Then
                    compilationState.AddToInitializeComponentCallTree(method, visitor._calledMethods.ToArray().AsImmutableOrNull())
                End If
            End Sub

            Public Overrides Function VisitCall(node As BoundCall) As BoundNode
                If node.ReceiverOpt IsNot Nothing AndAlso
                   (node.ReceiverOpt.Kind = BoundKind.MeReference OrElse node.ReceiverOpt.Kind = BoundKind.MyClassReference) AndAlso
                   Not node.Method.IsShared AndAlso node.Method.OriginalDefinition.ContainingType Is _containingType Then

                    If _calledMethods Is Nothing Then
                        _calledMethods = New HashSet(Of MethodSymbol)(ReferenceEqualityComparer.Instance)
                    End If

                    _calledMethods.Add(node.Method.OriginalDefinition)
                End If

                Return MyBase.VisitCall(node)
            End Function
        End Class

        ' This method may force completion of attributes to calculate if a symbol is Obsolete. Since this method is only called during
        ' lowering of default constructors, this should not cause any cycles.
        Private Shared Function FindConstructorToCallByDefault(constructor As MethodSymbol, diagnostics As BindingDiagnosticBag, Optional binderForAccessibilityCheckOpt As Binder = Nothing) As MethodSymbol
            Debug.Assert(constructor IsNot Nothing)
            Debug.Assert(constructor.MethodKind = MethodKind.Constructor)

            Dim containingType As NamedTypeSymbol = constructor.ContainingType
            Debug.Assert(Not containingType.IsValueType)

            If containingType.IsSubmissionClass Then
                ' TODO (tomat): report errors if not available
                Dim objectType = constructor.ContainingAssembly.GetSpecialType(SpecialType.System_Object)
                Return objectType.InstanceConstructors.Single()
            End If

            ' If the type is a structure, then invoke the default constructor on the current type.
            ' Otherwise, invoke the default constructor on the base type.
            Dim defaultConstructorType As NamedTypeSymbol = containingType.BaseTypeNoUseSiteDiagnostics
            If defaultConstructorType Is Nothing OrElse defaultConstructorType.IsErrorType Then
                ' possible if class System.Object in source doesn't have an explicit constructor
                Return Nothing
            End If

            ' If 'binderForAccessibilityCheckOpt' is not specified, containing type must be System.Object
            If binderForAccessibilityCheckOpt Is Nothing Then
                Debug.Assert(defaultConstructorType.IsObjectType)
            End If

            Dim candidate As MethodSymbol = Nothing
            Dim atLeastOneAccessibleCandidateFound As Boolean = False
            Dim useSiteInfo As New CompoundUseSiteInfo(Of AssemblySymbol)(diagnostics, containingType.ContainingAssembly)
            For Each m In defaultConstructorType.InstanceConstructors

                ' NOTE: Generic constructors are disallowed, but in case they
                '       show up because of bad metadata, ignore them.
                If m.IsGenericMethod Then
                    Continue For
                End If

                If binderForAccessibilityCheckOpt IsNot Nothing Then
                    ' Use binder to check accessibility
                    If Not binderForAccessibilityCheckOpt.IsAccessible(m, useSiteInfo:=useSiteInfo, accessThroughType:=containingType) Then
                        Continue For
                    End If
                Else
                    ' If there is no binder, just check if the method is public
                    If m.DeclaredAccessibility <> Accessibility.Public Then
                        Continue For
                    End If

                    ' NOTE: if there is no binder, we will only be able to emit a call to parameterless
                    '       constructor, but not for constructors with  optional parameters and/or ParamArray
                    If m.ParameterCount <> 0 Then
                        atLeastOneAccessibleCandidateFound = True  ' it is still accessible
                        Continue For
                    End If
                End If

                ' The constructor is accessible
                atLeastOneAccessibleCandidateFound = True

                ' Class constructors can be called with no parameters when the parameters are optional or paramarray.  However, for structures
                ' there cannot be any parameters.
                Dim canBeCalledWithNoParameters = If(containingType.IsReferenceType, m.CanBeCalledWithNoParameters(), m.ParameterCount = 0)

                If canBeCalledWithNoParameters Then

                    If candidate Is Nothing Then
                        candidate = m
                    Else
                        ' Too many candidates, use different errors for synthesized and regular constructors
                        If constructor.IsImplicitlyDeclared Then
                            ' Synthesized constructor
                            diagnostics.Add(New VBDiagnostic(
                                                ErrorFactory.ErrorInfo(ERRID.ERR_NoUniqueConstructorOnBase2, containingType, containingType.BaseTypeNoUseSiteDiagnostics),
                                                containingType.GetFirstLocation()))
                        Else
                            ' Regular constructor
                            diagnostics.Add(New VBDiagnostic(
                                                ErrorFactory.ErrorInfo(ERRID.ERR_RequiredNewCallTooMany2, defaultConstructorType, containingType),
                                                constructor.GetFirstLocation()))
                        End If

                        Return candidate
                    End If

                End If
            Next

            Dim locations As ImmutableArray(Of Location) = If(constructor.IsImplicitlyDeclared, containingType.Locations, constructor.Locations)
            diagnostics.Add(If(locations.IsDefaultOrEmpty, Location.None, locations(0)), useSiteInfo)

            ' Generate an error
            If candidate Is Nothing Then

                If atLeastOneAccessibleCandidateFound Then

                    ' Different errors for synthesized and regular constructors
                    If constructor.IsImplicitlyDeclared Then
                        ' Synthesized constructor
                        diagnostics.Add(New VBDiagnostic(
                                            ErrorFactory.ErrorInfo(ERRID.ERR_NoConstructorOnBase2, containingType, containingType.BaseTypeNoUseSiteDiagnostics),
                                            containingType.GetFirstLocation()))
                    Else
                        ' Regular constructor
                        diagnostics.Add(New VBDiagnostic(
                                            ErrorFactory.ErrorInfo(ERRID.ERR_RequiredNewCall2, defaultConstructorType, containingType),
                                            constructor.GetFirstLocation()))
                    End If

                Else
                    ' No accessible constructor
                    ' NOTE: Dev10 generates this error in 'Inherits' clause of the type, but it is not available
                    '       in *all* cases, so changing the error location to containingType's location
                    diagnostics.Add(New VBDiagnostic(
                                        ErrorFactory.ErrorInfo(ERRID.ERR_NoAccessibleConstructorOnBase, containingType.BaseTypeNoUseSiteDiagnostics),
                                        containingType.GetFirstLocation()))
                End If
            End If

            Debug.Assert(candidate <> constructor)

            ' If the candidate is Obsolete then report diagnostics.
            If candidate IsNot Nothing Then
                candidate.ForceCompleteObsoleteAttribute()

                If candidate.ObsoleteState = ThreeState.True Then
                    Dim data = candidate.ObsoleteAttributeData

                    ' If we have a synthesized constructor then give an error saying that there is no non-obsolete
                    ' base constructor. If we have a user-defined constructor then ask the user to explicitly call a
                    ' constructor so that they have a chance to call a non-obsolete base constructor.
                    If constructor.IsImplicitlyDeclared Then
                        ' Synthesized constructor.
                        If String.IsNullOrEmpty(data.Message) Then
                            diagnostics.Add(If(data.IsError, ERRID.ERR_NoNonObsoleteConstructorOnBase3, ERRID.WRN_NoNonObsoleteConstructorOnBase3),
                                            containingType.GetFirstLocation(),
                                            containingType,
                                            candidate,
                                            containingType.BaseTypeNoUseSiteDiagnostics)
                        Else
                            diagnostics.Add(If(data.IsError, ERRID.ERR_NoNonObsoleteConstructorOnBase4, ERRID.WRN_NoNonObsoleteConstructorOnBase4),
                                            containingType.GetFirstLocation(),
                                            containingType,
                                            candidate,
                                            containingType.BaseTypeNoUseSiteDiagnostics,
                                            data.Message)
                        End If
                    Else
                        ' Regular constructor.
                        If String.IsNullOrEmpty(data.Message) Then
                            diagnostics.Add(If(data.IsError, ERRID.ERR_RequiredNonObsoleteNewCall3, ERRID.WRN_RequiredNonObsoleteNewCall3),
                                            constructor.GetFirstLocation(),
                                            candidate,
                                            containingType.BaseTypeNoUseSiteDiagnostics,
                                            containingType)
                        Else
                            diagnostics.Add(If(data.IsError, ERRID.ERR_RequiredNonObsoleteNewCall4, ERRID.WRN_RequiredNonObsoleteNewCall4),
                                            constructor.GetFirstLocation(),
                                            candidate,
                                            containingType.BaseTypeNoUseSiteDiagnostics,
                                            containingType,
                                            data.Message)
                        End If
                    End If
                End If
            End If

            Return candidate
        End Function

        Friend Shared Function BindDefaultConstructorInitializer(constructor As MethodSymbol,
                                                                  constructorToCall As MethodSymbol,
                                                                  diagnostics As BindingDiagnosticBag,
                                                                  Optional binderOpt As Binder = Nothing) As BoundExpressionStatement

            Dim voidType As NamedTypeSymbol = constructor.ContainingAssembly.GetSpecialType(SpecialType.System_Void)
            ' NOTE: we can ignore use site errors in this place because they should have already be reported
            '       either in real or synthesized constructor

            Dim syntaxNode As SyntaxNode = constructor.Syntax

            Dim thisRef As New BoundMyBaseReference(syntaxNode, constructorToCall.ContainingType)
            thisRef.SetWasCompilerGenerated()

            Dim baseInvocation As BoundExpression = Nothing
            If constructorToCall.ParameterCount = 0 Then

                ' If this is parameterless constructor, we can build a call directly
                baseInvocation = New BoundCall(syntaxNode, constructorToCall, Nothing, thisRef, ImmutableArray(Of BoundExpression).Empty, Nothing, voidType)

            Else

                ' Otherwise we should bind invocation expression
                ' Binder must be passed in 'binderOpt'
                Debug.Assert(binderOpt IsNot Nothing)

                '  Build a method group
                Dim group As New BoundMethodGroup(constructor.Syntax,
                                                  typeArgumentsOpt:=Nothing,
                                                  methods:=ImmutableArray.Create(Of MethodSymbol)(constructorToCall),
                                                  resultKind:=LookupResultKind.Good,
                                                  receiverOpt:=thisRef,
                                                  qualificationKind:=QualificationKind.QualifiedViaValue)

                baseInvocation = binderOpt.BindInvocationExpression(constructor.Syntax,
                                                                    Nothing,
                                                                    TypeCharacter.None,
                                                                    group,
                                                                    ImmutableArray(Of BoundExpression).Empty,
                                                                    Nothing,
                                                                    diagnostics,
                                                                    callerInfoOpt:=Nothing,
                                                                    allowConstructorCall:=True)
            End If
            baseInvocation.SetWasCompilerGenerated()

            Dim statement As New BoundExpressionStatement(syntaxNode, baseInvocation)
            statement.SetWasCompilerGenerated()
            Return statement
        End Function

        Friend Shared Function BindDefaultConstructorInitializer(constructor As MethodSymbol, diagnostics As BindingDiagnosticBag) As BoundExpressionStatement
            ' NOTE: this method is only called from outside

            ' NOTE: Because we don't pass a binder into this method, we assume that (a) containing type of
            '       the constructor is a reference type inherited from System.Object (later is asserted
            '       in 'FindConstructorToCallByDefault'), and (b) System.Object must have Public parameterless
            '       constructor (asserted in 'BindDefaultConstructorInitializer')

            ' NOTE: We might generate an error in this call in case there is
            '       no parameterless constructor suitable for calling
            Dim baseConstructor As MethodSymbol = FindConstructorToCallByDefault(constructor, diagnostics)
            If baseConstructor Is Nothing Then
                Return Nothing
            End If

            Return BindDefaultConstructorInitializer(constructor, baseConstructor, diagnostics)
        End Function

        Private Shared Function CreateDebugDocumentForFile(normalizedPath As String) As Cci.DebugSourceDocument
            Return New Cci.DebugSourceDocument(normalizedPath, Cci.DebugSourceDocument.CorSymLanguageTypeBasic)
        End Function

        Private Shared Function PassesFilter(filterOpt As Predicate(Of Symbol), symbol As Symbol) As Boolean
            Return filterOpt Is Nothing OrElse filterOpt(symbol)
        End Function
    End Class
End Namespace
