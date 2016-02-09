' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class CompilationContext

        Private Shared ReadOnly s_fullNameFormat As New SymbolDisplayFormat(
            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Friend Shared ReadOnly BackstopBinder As Binder = New BackstopBinder()

        Friend ReadOnly Compilation As VisualBasicCompilation
        Friend ReadOnly NamespaceBinder As Binder ' Internal for test purposes.

        Private ReadOnly _currentFrame As MethodSymbol
        Private ReadOnly _locals As ImmutableArray(Of LocalSymbol)
        Private ReadOnly _displayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable)
        Private ReadOnly _hoistedParameterNames As ImmutableHashSet(Of String)
        Private ReadOnly _localsForBinding As ImmutableArray(Of LocalSymbol)
        Private ReadOnly _syntax As ExecutableStatementSyntax
        Private ReadOnly _methodNotType As Boolean
        Private ReadOnly _voidType As NamedTypeSymbol

        ''' <summary>
        ''' Create a context to compile expressions within a method scope.
        ''' </summary>
        Friend Sub New(
            compilation As VisualBasicCompilation,
            currentFrame As MethodSymbol,
            locals As ImmutableArray(Of LocalSymbol),
            inScopeHoistedLocals As InScopeHoistedLocals,
            methodDebugInfo As MethodDebugInfo(Of TypeSymbol, LocalSymbol),
            syntax As ExecutableStatementSyntax)

            _syntax = syntax
            _currentFrame = currentFrame

            Debug.Assert(compilation.Options.RootNamespace = "") ' Default value.
            Debug.Assert(methodDebugInfo.ExternAliasRecords.IsDefaultOrEmpty)

            Dim originalCompilation = compilation

            If syntax IsNot Nothing Then
                compilation = compilation.AddSyntaxTrees(syntax.SyntaxTree)
            End If

            Dim defaultNamespaceName As String = methodDebugInfo.DefaultNamespaceName

            ' Note We don't need to try to bind this string because this is analogous to passing
            ' a command-line argument - as long as the syntax is valid, an appropriate symbol will
            ' be created for us.
            If defaultNamespaceName IsNot Nothing AndAlso TryParseDottedName(defaultNamespaceName, Nothing) Then
                compilation = compilation.WithOptions(compilation.Options.WithRootNamespace(defaultNamespaceName))
            End If

            If compilation Is originalCompilation Then
                compilation = compilation.Clone()
            End If

            Me.Compilation = compilation

            ' Each expression compile should use a unique compilation
            ' to ensure expression-specific synthesized members can be
            ' added (anonymous types, for instance).
            Debug.Assert(Me.Compilation IsNot originalCompilation)

            NamespaceBinder = CreateBinderChain(
                Me.Compilation,
                currentFrame.ContainingNamespace,
                methodDebugInfo.ImportRecordGroups)

            _voidType = Me.Compilation.GetSpecialType(SpecialType.System_Void)

            _methodNotType = Not locals.IsDefault

            If _methodNotType Then
                _locals = locals
                Dim displayClassVariableNamesInOrder As ImmutableArray(Of String) = Nothing
                GetDisplayClassVariables(currentFrame, locals, inScopeHoistedLocals, displayClassVariableNamesInOrder, _displayClassVariables, _hoistedParameterNames)
                Debug.Assert(displayClassVariableNamesInOrder.Length = _displayClassVariables.Count)
                _localsForBinding = GetLocalsForBinding(locals, displayClassVariableNamesInOrder, _displayClassVariables)
            Else
                _locals = ImmutableArray(Of LocalSymbol).Empty
                _displayClassVariables = ImmutableDictionary(Of String, DisplayClassVariable).Empty
                _localsForBinding = ImmutableArray(Of LocalSymbol).Empty
            End If

            ' Assert that the cheap check for "Me" is equivalent to the expensive check for "Me".
            Debug.Assert(
                _displayClassVariables.ContainsKey(StringConstants.HoistedMeName) =
                _displayClassVariables.Values.Any(Function(v) v.Kind = DisplayClassVariableKind.Me))
        End Sub

        Friend Function Compile(
            typeName As String,
            methodName As String,
            aliases As ImmutableArray(Of [Alias]),
            testData As Microsoft.CodeAnalysis.CodeGen.CompilationTestData,
            diagnostics As DiagnosticBag,
            <Out> ByRef resultProperties As ResultProperties) As CommonPEModuleBuilder

            Dim properties As ResultProperties = Nothing
            Dim objectType = Me.Compilation.GetSpecialType(SpecialType.System_Object)
            Dim synthesizedType = New EENamedTypeSymbol(
                Me.Compilation.SourceAssembly.GlobalNamespace,
                objectType,
                _syntax,
                _currentFrame,
                typeName,
                methodName,
                Me,
                Function(method, diags)
                    Dim hasDisplayClassMe = _displayClassVariables.ContainsKey(StringConstants.HoistedMeName)
                    Dim bindAsExpression = _syntax.Kind = SyntaxKind.PrintStatement
                    Dim binder = ExtendBinderChain(
                        aliases,
                        method,
                        NamespaceBinder,
                        hasDisplayClassMe,
                        _methodNotType,
                        allowImplicitDeclarations:=Not bindAsExpression)
                    Return If(bindAsExpression,
                        BindExpression(binder, DirectCast(_syntax, PrintStatementSyntax).Expression, diags, properties),
                        BindStatement(binder, _syntax, diags, properties))
                End Function)

            Dim moduleBuilder = CreateModuleBuilder(
                Me.Compilation,
                synthesizedType.Methods,
                additionalTypes:=ImmutableArray.Create(DirectCast(synthesizedType, NamedTypeSymbol)),
                testData:=testData,
                diagnostics:=diagnostics)

            Debug.Assert(moduleBuilder IsNot Nothing)

            Me.Compilation.Compile(
                moduleBuilder,
                win32Resources:=Nothing,
                xmlDocStream:=Nothing,
                emittingPdb:=False,
                diagnostics:=diagnostics,
                filterOpt:=Nothing,
                cancellationToken:=CancellationToken.None)

            If diagnostics.HasAnyErrors() Then
                resultProperties = Nothing
                Return Nothing
            End If

#If DEBUG Then
            Dim m = synthesizedType.GetMembers()(0)
            ' Should be no name mangling since the caller provided explicit names.
            Debug.Assert(m.ContainingType.MetadataName = typeName)
            Debug.Assert(m.MetadataName = methodName)
#End If

            resultProperties = properties
            Return moduleBuilder
        End Function

        Private Shared Function GetNextMethodName(builder As ArrayBuilder(Of MethodSymbol)) As String
            ' NOTE: These names are consumed by Concord, so there's no native precedent.
            Return String.Format("<>m{0}", builder.Count)
        End Function

        ''' <summary>
        ''' Generate a class containing methods that represent
        ''' the set of arguments and locals at the current scope.
        ''' </summary>
        Friend Function CompileGetLocals(
            typeName As String,
            localBuilder As ArrayBuilder(Of LocalAndMethod),
            argumentsOnly As Boolean,
            aliases As ImmutableArray(Of [Alias]),
            testData As Microsoft.CodeAnalysis.CodeGen.CompilationTestData,
            diagnostics As DiagnosticBag) As CommonPEModuleBuilder

            Dim objectType = Me.Compilation.GetSpecialType(SpecialType.System_Object)
            Dim allTypeParameters = GetAllTypeParameters(_currentFrame)
            Dim additionalTypes = ArrayBuilder(Of NamedTypeSymbol).GetInstance()

            Dim typeVariablesType As EENamedTypeSymbol = Nothing
            If Not argumentsOnly AndAlso allTypeParameters.Length > 0 Then
                ' Generate a generic type with matching type parameters.
                ' A null instance of this type will be used to represent
                ' the "Type variables" local.
                typeVariablesType = New EENamedTypeSymbol(
                    Me.Compilation.SourceModule.GlobalNamespace,
                    objectType,
                    _syntax,
                    _currentFrame,
                    ExpressionCompilerConstants.TypeVariablesClassName,
                    Function(m, t)
                        Dim constructor As New EEConstructorSymbol(t)
                        constructor.SetParameters(ImmutableArray(Of ParameterSymbol).Empty)
                        Return ImmutableArray.Create(Of MethodSymbol)(constructor)
                    End Function,
                    allTypeParameters,
                    Function(t1, t2) allTypeParameters.SelectAsArray(Function(tp, i, t) DirectCast(New SimpleTypeParameterSymbol(t, i, tp.GetUnmangledName()), TypeParameterSymbol), t2))
                additionalTypes.Add(typeVariablesType)
            End If

            Dim synthesizedType As New EENamedTypeSymbol(
                Me.Compilation.SourceModule.GlobalNamespace,
                objectType,
                _syntax,
                _currentFrame,
                typeName,
                Function(m, container)
                    Dim methodBuilder = ArrayBuilder(Of MethodSymbol).GetInstance()

                    If Not argumentsOnly Then
                        If aliases.Length > 0 Then
                            ' Pseudo-variables: $exception, $ReturnValue, etc.
                            Dim typeNameDecoder = New EETypeNameDecoder(Me.Compilation, DirectCast(_currentFrame.ContainingModule, PEModuleSymbol))
                            For Each [alias] As [Alias] In aliases
                                Dim methodName = GetNextMethodName(methodBuilder)
                                Dim syntax = SyntaxFactory.IdentifierName(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken))
                                Dim local = PlaceholderLocalSymbol.Create(typeNameDecoder, _currentFrame, [alias])
                                Dim aliasMethod = Me.CreateMethod(
                                    container,
                                    methodName,
                                    syntax,
                                    Function(method, diags)
                                        Dim expression = New BoundLocal(syntax, local, isLValue:=False, type:=local.Type)
                                        Return New BoundReturnStatement(syntax, expression, Nothing, Nothing).MakeCompilerGenerated()
                                    End Function)
                                localBuilder.Add(MakeLocalAndMethod(local, methodName, If(local.IsReadOnly, DkmClrCompilationResultFlags.ReadOnlyResult, DkmClrCompilationResultFlags.None)))
                                methodBuilder.Add(aliasMethod)
                            Next
                        End If

                        ' "Me" for non-shared methods that are not display class methods
                        ' or display class methods where the display class contains "$VB$Me".
                        If Not m.IsShared AndAlso (Not m.ContainingType.IsClosureOrStateMachineType() OrElse _displayClassVariables.ContainsKey(GeneratedNames.MakeStateMachineCapturedMeName())) Then
                            Dim methodName = GetNextMethodName(methodBuilder)
                            Dim method = Me.GetMeMethod(container, methodName)
                            localBuilder.Add(New VisualBasicLocalAndMethod("Me", "Me", methodName, DkmClrCompilationResultFlags.None)) ' NOTE: writable in Dev11.
                            methodBuilder.Add(method)
                        End If
                    End If

                    ' Hoisted method parameters (represented as locals in the EE).
                    If Not _hoistedParameterNames.IsEmpty Then
                        Dim localIndex As Integer = 0

                        For Each local In _localsForBinding
                            ' Since we are showing hoisted method parameters first, the parameters may appear out of order
                            ' in the Locals window if only some of the parameters are hoisted.  This is consistent with the
                            ' behavior of the old EE.
                            If _hoistedParameterNames.Contains(local.Name) Then
                                AppendLocalAndMethod(localBuilder, methodBuilder, local, container, localIndex, GetLocalResultFlags(local))
                            End If

                            localIndex += 1
                        Next
                    End If

                    ' Method parameters (except those that have been hoisted).
                    Dim parameterIndex = If(m.IsShared, 0, 1)
                    For Each parameter In m.Parameters
                        Dim parameterName As String = parameter.Name
                        If Not _hoistedParameterNames.Contains(parameterName) AndAlso GeneratedNames.GetKind(parameterName) = GeneratedNameKind.None Then
                            AppendParameterAndMethod(localBuilder, methodBuilder, parameter, container, parameterIndex)
                        End If

                        parameterIndex += 1
                    Next

                    If Not argumentsOnly Then
                        ' Locals.
                        Dim localIndex As Integer = 0
                        For Each local In _localsForBinding
                            If Not _hoistedParameterNames.Contains(local.Name) Then
                                AppendLocalAndMethod(localBuilder, methodBuilder, local, container, localIndex, GetLocalResultFlags(local))
                            End If

                            localIndex += 1
                        Next

                        ' "Type variables".
                        If typeVariablesType IsNot Nothing Then
                            Dim methodName = GetNextMethodName(methodBuilder)
                            Dim returnType = typeVariablesType.Construct(ImmutableArrayExtensions.Cast(Of TypeParameterSymbol, TypeSymbol)(allTypeParameters))
                            Dim method = Me.GetTypeVariableMethod(container, methodName, returnType)
                            localBuilder.Add(New VisualBasicLocalAndMethod(
                                             ExpressionCompilerConstants.TypeVariablesLocalName,
                                             ExpressionCompilerConstants.TypeVariablesLocalName,
                                             methodName,
                                             DkmClrCompilationResultFlags.ReadOnlyResult))
                            methodBuilder.Add(method)
                        End If
                    End If

                    Return methodBuilder.ToImmutableAndFree()
                End Function)

            additionalTypes.Add(synthesizedType)

            Dim moduleBuilder = CreateModuleBuilder(
                Me.Compilation,
                synthesizedType.Methods,
                additionalTypes:=additionalTypes.ToImmutableAndFree(),
                testData:=testData,
                diagnostics:=diagnostics)

            Debug.Assert(moduleBuilder IsNot Nothing)

            Me.Compilation.Compile(
                moduleBuilder,
                win32Resources:=Nothing,
                xmlDocStream:=Nothing,
                emittingPdb:=False,
                diagnostics:=diagnostics,
                filterOpt:=Nothing,
                cancellationToken:=CancellationToken.None)

            Return If(diagnostics.HasAnyErrors(), Nothing, moduleBuilder)
        End Function

        Private Sub AppendLocalAndMethod(
            localBuilder As ArrayBuilder(Of LocalAndMethod),
            methodBuilder As ArrayBuilder(Of MethodSymbol),
            local As LocalSymbol,
            container As EENamedTypeSymbol,
            localIndex As Integer,
            resultFlags As DkmClrCompilationResultFlags)

            Dim methodName = GetNextMethodName(methodBuilder)
            Dim method = Me.GetLocalMethod(container, methodName, local.Name, localIndex)
            localBuilder.Add(MakeLocalAndMethod(local, methodName, resultFlags))
            methodBuilder.Add(method)
        End Sub

        Private Sub AppendParameterAndMethod(
            localBuilder As ArrayBuilder(Of LocalAndMethod),
            methodBuilder As ArrayBuilder(Of MethodSymbol),
            parameter As ParameterSymbol,
            container As EENamedTypeSymbol,
            parameterIndex As Integer)

            ' Note: The native EE doesn't do this, but if we don't escape keyword identifiers,
            ' the ResultProvider needs to be able to disambiguate cases Like "Me" And "[Me]",
            ' which it can't do correctly without semantic information.
            Dim name = SyntaxHelpers.EscapeKeywordIdentifiers(parameter.Name)
            Dim methodName = GetNextMethodName(methodBuilder)
            Dim method = Me.GetParameterMethod(container, methodName, parameter.Name, parameterIndex)
            localBuilder.Add(New VisualBasicLocalAndMethod(name, name, methodName, DkmClrCompilationResultFlags.None))
            methodBuilder.Add(method)
        End Sub

        Private Shared Function MakeLocalAndMethod(local As LocalSymbol, methodName As String, flags As DkmClrCompilationResultFlags) As LocalAndMethod
            ' Note: The native EE doesn't do this, but if we don't escape keyword identifiers,
            ' the ResultProvider needs to be able to disambiguate cases Like "Me" And "[Me]",
            ' which it can't do correctly without semantic information.
            Dim escapedName = SyntaxHelpers.EscapeKeywordIdentifiers(local.Name)
            Dim displayName = If(TryCast(local, PlaceholderLocalSymbol)?.DisplayName, escapedName)
            Return New VisualBasicLocalAndMethod(escapedName, displayName, methodName, flags)
        End Function

        Private Shared Function CreateModuleBuilder(
            compilation As VisualBasicCompilation,
            methods As ImmutableArray(Of MethodSymbol),
            additionalTypes As ImmutableArray(Of NamedTypeSymbol),
            testData As Microsoft.CodeAnalysis.CodeGen.CompilationTestData,
            diagnostics As DiagnosticBag) As EEAssemblyBuilder

            ' Each assembly must have a unique name.
            Dim emitOptions = New EmitOptions(outputNameOverride:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtimeMetadataVersion = compilation.GetRuntimeMetadataVersion()
            Dim serializationProperties = compilation.ConstructModuleSerializationProperties(emitOptions, runtimeMetadataVersion)
            Return New EEAssemblyBuilder(compilation.SourceAssembly, emitOptions, methods, serializationProperties, additionalTypes, testData)
        End Function

        Friend Function CreateMethod(
            container As EENamedTypeSymbol,
            methodName As String,
            syntax As VisualBasicSyntaxNode,
            generateMethodBody As GenerateMethodBody) As EEMethodSymbol

            Return New EEMethodSymbol(
                Compilation,
                container,
                methodName,
                syntax.GetLocation(),
                _currentFrame,
                _locals,
                _localsForBinding,
                _displayClassVariables,
                _voidType,
                generateMethodBody)
        End Function

        Private Function GetLocalMethod(container As EENamedTypeSymbol, methodName As String, localName As String, localIndex As Integer) As EEMethodSymbol
            Dim syntax = SyntaxFactory.IdentifierName(localName)
            Return Me.CreateMethod(
                container,
                methodName,
                syntax,
                Function(method, diagnostics)
                    Dim local = method.LocalsForBinding(localIndex)
                    Dim expression = New BoundLocal(syntax, local, isLValue:=False, type:=local.Type)
                    Return New BoundReturnStatement(syntax, expression, Nothing, Nothing).MakeCompilerGenerated()
                End Function)
        End Function

        Private Function GetParameterMethod(container As EENamedTypeSymbol, methodName As String, parameterName As String, parameterIndex As Integer) As EEMethodSymbol
            Dim syntax = SyntaxFactory.IdentifierName(parameterName)
            Return Me.CreateMethod(
                container,
                methodName,
                syntax,
                Function(method, diagnostics)
                    Dim parameter = method.Parameters(parameterIndex)
                    Dim expression = New BoundParameter(syntax, parameter, isLValue:=False, type:=parameter.Type)
                    Return New BoundReturnStatement(syntax, expression, Nothing, Nothing).MakeCompilerGenerated()
                End Function)
        End Function

        Private Function GetMeMethod(container As EENamedTypeSymbol, methodName As String) As EEMethodSymbol
            Dim syntax = SyntaxFactory.MeExpression()
            Return Me.CreateMethod(
                container,
                methodName,
                syntax,
                Function(method, diagnostics)
                    Dim expression = New BoundMeReference(syntax, GetNonClosureOrStateMachineContainer(container.SubstitutedSourceType))
                    Return New BoundReturnStatement(syntax, expression, Nothing, Nothing).MakeCompilerGenerated()
                End Function)
        End Function

        Private Function GetTypeVariableMethod(container As EENamedTypeSymbol, methodName As String, typeVariablesType As NamedTypeSymbol) As EEMethodSymbol
            Dim syntax = SyntaxFactory.IdentifierName(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken))
            Return Me.CreateMethod(
                container,
                methodName,
                syntax,
                Function(method, diagnostics)
                    Dim type = method.TypeMap.SubstituteNamedType(typeVariablesType)
                    Dim expression = New BoundObjectCreationExpression(syntax, type.InstanceConstructors(0), ImmutableArray(Of BoundExpression).Empty, Nothing, type)
                    Return New BoundReturnStatement(syntax, expression, Nothing, Nothing).MakeCompilerGenerated()
                End Function)
        End Function

        Private Shared Function BindExpression(binder As Binder, syntax As ExpressionSyntax, diagnostics As DiagnosticBag, <Out> ByRef resultProperties As ResultProperties) As BoundStatement
            Dim expression = binder.BindExpression(syntax, diagnostics)

            Dim flags = DkmClrCompilationResultFlags.None
            If Not IsAssignableExpression(binder, expression) Then
                flags = flags Or DkmClrCompilationResultFlags.ReadOnlyResult
            End If

            Try
                If MayHaveSideEffectsVisitor.MayHaveSideEffects(expression) Then
                    flags = flags Or DkmClrCompilationResultFlags.PotentialSideEffect
                End If
            Catch ex As BoundTreeVisitor.CancelledByStackGuardException
                ex.AddAnError(diagnostics)
            End Try

            If IsStatement(expression) Then
                expression = binder.ReclassifyInvocationExpressionAsStatement(expression, diagnostics)
            Else
                expression = binder.MakeRValue(expression, diagnostics)
            End If

            Select Case expression.Type.SpecialType
                Case SpecialType.System_Void
                    Debug.Assert(expression.ConstantValueOpt Is Nothing)
                    resultProperties = expression.ExpressionSymbol.GetResultProperties(flags, isConstant:=False)
                    Return New BoundExpressionStatement(syntax, expression).MakeCompilerGenerated()
                Case SpecialType.System_Boolean
                    flags = flags Or DkmClrCompilationResultFlags.BoolResult
            End Select

            resultProperties = expression.ExpressionSymbol.GetResultProperties(flags, expression.ConstantValueOpt IsNot Nothing)
            Return New BoundReturnStatement(syntax, expression, Nothing, Nothing).MakeCompilerGenerated()
        End Function

        Private Shared Function IsAssignableExpression(binder As Binder, expression As BoundExpression) As Boolean
            Dim diagnostics = DiagnosticBag.GetInstance()
            Dim value = binder.ReclassifyAsValue(expression, diagnostics)
            Dim result = False
            If Binder.IsValidAssignmentTarget(value) AndAlso Not diagnostics.HasAnyErrors() Then
                Dim isError = False
                binder.AdjustAssignmentTarget(value.Syntax, value, diagnostics, isError)
                Debug.Assert(isError = diagnostics.HasAnyErrors())
                result = Not isError
            End If
            diagnostics.Free()
            Return result
        End Function

        Private Shared Function IsStatement(expression As BoundExpression) As Boolean
            Select Case expression.Kind
                Case BoundKind.Call
                    Return IsCallStatement(DirectCast(expression, BoundCall))
                Case BoundKind.ConditionalAccess
                    Dim [call] = TryCast(DirectCast(expression, BoundConditionalAccess).AccessExpression, BoundCall)
                    Return ([call] IsNot Nothing) AndAlso IsCallStatement([call])
                Case Else
                    Return False
            End Select
        End Function

        Private Shared Function IsCallStatement([call] As BoundCall) As Boolean
            Return [call].Method.IsSub
        End Function

        Private Shared Function BindStatement(binder As Binder, syntax As StatementSyntax, diagnostics As DiagnosticBag, <Out> ByRef resultProperties As ResultProperties) As BoundStatement
            resultProperties = New ResultProperties(DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            Return binder.BindStatement(syntax, diagnostics).MakeCompilerGenerated()
        End Function

        Private Shared Function CreateBinderChain(
            compilation As VisualBasicCompilation,
            [namespace] As NamespaceSymbol,
            importRecordGroups As ImmutableArray(Of ImmutableArray(Of ImportRecord))) As Binder

            Dim binder = BackstopBinder
            binder = New SuppressDiagnosticsBinder(binder)
            binder = New IgnoreAccessibilityBinder(binder)
            binder = New SourceModuleBinder(binder, DirectCast(compilation.Assembly.Modules(0), SourceModuleSymbol))

            If Not importRecordGroups.IsEmpty Then
                binder = BuildImportedSymbolsBinder(binder, New NamespaceBinder(binder, compilation.GlobalNamespace), importRecordGroups)
            End If

            Dim stack = ArrayBuilder(Of String).GetInstance()
            Dim containingNamespace = [namespace]
            While containingNamespace IsNot Nothing
                stack.Push(containingNamespace.Name)
                containingNamespace = containingNamespace.ContainingNamespace
            End While

            ' PERF: we used to call compilation.GetCompilationNamespace on every iteration,
            ' but that involved walking up to the global namespace, which we have to do
            ' anyway.  Instead, we'll inline the functionality into our own walk of the
            ' namespace chain.
            [namespace] = compilation.GlobalNamespace

            While stack.Count > 0
                Dim namespaceName = stack.Pop()
                If namespaceName.Length > 0 Then
                    ' We're re-getting the namespace, rather than using the one containing
                    ' the current frame method, because we want the merged namespace
                    [namespace] = [namespace].GetNestedNamespace(namespaceName)
                    Debug.Assert([namespace] IsNot Nothing,
                                 "We worked backwards from symbols to names, but no symbol exists for name '" + namespaceName + "'")
                Else
                    Debug.Assert([namespace] Is compilation.GlobalNamespace)
                End If
                binder = New NamespaceBinder(binder, [namespace])
            End While

            stack.Free()

            Return binder
        End Function

        Private Shared Function ExtendBinderChain(
            aliases As ImmutableArray(Of [Alias]),
            method As EEMethodSymbol,
            binder As Binder,
            hasDisplayClassMe As Boolean,
            methodNotType As Boolean,
            allowImplicitDeclarations As Boolean) As Binder

            Dim substitutedSourceMethod = GetSubstitutedSourceMethod(method.SubstitutedSourceMethod, hasDisplayClassMe)
            Dim substitutedSourceType = substitutedSourceMethod.ContainingType

            Dim stack = ArrayBuilder(Of NamedTypeSymbol).GetInstance()
            Dim type = substitutedSourceType
            While type IsNot Nothing
                stack.Push(type)
                type = type.ContainingType
            End While

            While stack.Count > 0
                substitutedSourceType = stack.Pop()
                binder = New EENamedTypeBinder(substitutedSourceType, binder)
            End While

            stack.Free()

            If substitutedSourceMethod.Arity > 0 Then
                binder = New MethodTypeParametersBinder(binder, substitutedSourceMethod.TypeArguments.SelectAsArray(Function(t) DirectCast(t, TypeParameterSymbol)))
            End If

            ' The "Type Context" is used when binding DebuggerDisplayAttribute expressions.
            ' We have chosen to explicitly disallow pseudo variables in that scenario.
            If methodNotType Then
                ' Method locals and parameters shadow pseudo-variables.
                Dim typeNameDecoder = New EETypeNameDecoder(binder.Compilation, DirectCast(substitutedSourceMethod.ContainingModule, PEModuleSymbol))
                binder = New PlaceholderLocalBinder(aliases, method, typeNameDecoder, allowImplicitDeclarations, binder)
            End If

            ' Even if there are no parameters or locals, this has the effect of setting
            ' the containing member to the substituted source method.
            binder = New ParametersAndLocalsBinder(binder, method, substitutedSourceMethod)

            Return binder
        End Function

        Private Shared Function BuildImportedSymbolsBinder(
            containingBinder As Binder,
            importBinder As Binder,
            importRecordGroups As ImmutableArray(Of ImmutableArray(Of ImportRecord))) As Binder

            Dim projectLevelImportsBuilder As ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition) = Nothing
            Dim fileLevelImportsBuilder As ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition) = Nothing

            Dim projectLevelAliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing
            Dim fileLevelAliases As Dictionary(Of String, AliasAndImportsClausePosition) = Nothing

            Dim projectLevelXmlImports As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing
            Dim fileLevelXmlImports As Dictionary(Of String, XmlNamespaceAndImportsClausePosition) = Nothing

            Debug.Assert(importRecordGroups.Length = 2) ' First file-level, then project-level.
            Dim projectLevelImportRecords = importRecordGroups(1)
            Dim fileLevelImportRecords = importRecordGroups(0)

            ' Use this to give the imports different positions
            Dim position = 0

            For Each importRecord As ImportRecord In projectLevelImportRecords
                If AddImportForRecord(
                    importRecord,
                    importBinder,
                    position,
                    projectLevelImportsBuilder,
                    projectLevelAliases,
                    projectLevelXmlImports) Then

                    position += 1
                End If
            Next

            For Each importRecord As ImportRecord In fileLevelImportRecords
                If AddImportForRecord(
                    importRecord,
                    importBinder,
                    position,
                    fileLevelImportsBuilder,
                    fileLevelAliases,
                    fileLevelXmlImports) Then

                    position += 1
                End If
            Next

            ' BinderBuilder.CreateBinderForSourceFile creates separate binders for the project- and file-level
            ' imports.  We'd do the same, but we don't have a SourceFileBinder to put in between and that
            ' violates some (very specific) assertions about the shape of the binder chain.  Instead, we will
            ' manually resolve ties and then create a single set of binders.

            Dim binder = containingBinder

            Dim importsBuilder As ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition)
            If projectLevelImportsBuilder Is Nothing Then
                importsBuilder = fileLevelImportsBuilder
            ElseIf fileLevelImportsBuilder Is Nothing Then
                importsBuilder = projectLevelImportsBuilder
            Else
                importsBuilder = fileLevelImportsBuilder
                importsBuilder.AddRange(projectLevelImportsBuilder)
                projectLevelImportsBuilder.Free()
            End If
            If importsBuilder IsNot Nothing Then
                Dim [imports] As ImmutableArray(Of NamespaceOrTypeAndImportsClausePosition) = importsBuilder.ToImmutableAndFree()
                binder = New TypesOfImportedNamespacesMembersBinder(binder, [imports])
                binder = New ImportedTypesAndNamespacesMembersBinder(binder, [imports])
            End If

            Dim aliases = MergeAliases(projectLevelAliases, fileLevelAliases)
            If aliases IsNot Nothing Then
                binder = New ImportAliasesBinder(binder, aliases)
            End If

            Dim xmlImports = MergeAliases(projectLevelXmlImports, fileLevelXmlImports)
            If xmlImports IsNot Nothing Then
                binder = New XmlNamespaceImportsBinder(binder, xmlImports)
            End If

            Return binder
        End Function

        Private Shared Function AddImportForRecord(
            importRecord As ImportRecord,
            importBinder As Binder,
            position As Integer,
            ByRef importsBuilder As ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition),
            ByRef aliases As Dictionary(Of String, AliasAndImportsClausePosition),
            ByRef xmlImports As Dictionary(Of String, XmlNamespaceAndImportsClausePosition)) As Boolean

            Dim targetString = importRecord.TargetString

            ' NB: It appears that imports of generic types are not included in the PDB, so we never have to worry about parsing them.
            ' NB: Unlike in C# PDBs, the assembly name will not be present, so we have to just bind the string.
            Dim targetSyntax As NameSyntax = Nothing
            If Not String.IsNullOrEmpty(targetString) AndAlso ' CurrentNamespace may be an empty string, new-format types may be null.
                    importRecord.TargetKind <> ImportTargetKind.XmlNamespace AndAlso
                    Not TryParseDottedName(targetString, targetSyntax) Then

                Debug.WriteLine($"Import record '{importRecord}' has syntactically invalid target '{targetString}'")
                Return False
            End If

            ' Check for syntactically invalid aliases.
            Dim [alias] = importRecord.Alias
            If Not String.IsNullOrEmpty([alias]) Then
                Dim aliasNameSyntax As NameSyntax = Nothing
                If Not TryParseDottedName([alias], aliasNameSyntax) OrElse aliasNameSyntax.Kind <> SyntaxKind.IdentifierName Then
                    Debug.WriteLine($"Import record '{importRecord}' has syntactically invalid alias '{[alias]}'")
                    Return False
                End If
            End If

            Select Case importRecord.TargetKind
                Case ImportTargetKind.Type
                    Dim typeSymbol As TypeSymbol
                    If importRecord.TargetType IsNot Nothing Then
                        typeSymbol = DirectCast(importRecord.TargetType, TypeSymbol)
                    Else
                        Debug.Assert(importRecord.Alias Is Nothing) ' Represented as ImportTargetKind.NamespaceOrType in old-format PDBs.

                        Dim unusedDiagnostics = DiagnosticBag.GetInstance()
                        typeSymbol = importBinder.BindTypeSyntax(targetSyntax, unusedDiagnostics)
                        unusedDiagnostics.Free()

                        Debug.Assert(typeSymbol IsNot Nothing)

                        If typeSymbol.Kind = SymbolKind.ErrorType Then
                            ' Type is unrecognized.  The import may have been
                            ' valid in the original source but unnecessary.
                            Return False ' Don't add anything for this import.
                        End If
                    End If

                    If [alias] IsNot Nothing Then
                        Dim aliasSymbol As New AliasSymbol(importBinder.Compilation, importBinder.ContainingNamespaceOrType, [alias], typeSymbol, NoLocation.Singleton)

                        If aliases Is Nothing Then
                            aliases = New Dictionary(Of String, AliasAndImportsClausePosition)()
                        End If

                        ' There's no real syntax, so there's no real position.  We'll give them separate numbers though.
                        aliases([alias]) = New AliasAndImportsClausePosition(aliasSymbol, position)
                    Else
                        If importsBuilder Is Nothing Then
                            importsBuilder = ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition).GetInstance()
                        End If

                        ' There's no real syntax, so there's no real position.  We'll give them separate numbers though.
                        importsBuilder.Add(New NamespaceOrTypeAndImportsClausePosition(typeSymbol, position))
                    End If

                ' Dev12 treats the current namespace the same as any other namespace (see ProcedureContext::LoadImportsAndDefaultNamespaceNormal).
                ' It seems pointless to add an import for the namespace in which we are binding expressions, but the native source gives
                ' the impression that other namespaces may take the same form in Everett PDBs.
                Case ImportTargetKind.CurrentNamespace, ImportTargetKind.Namespace ' Unaliased namespace or type
                    If targetString = "" Then
                        Debug.Assert(importRecord.TargetKind = ImportTargetKind.CurrentNamespace) ' The current namespace can be empty.
                        Return False
                    End If

                    Dim unusedDiagnostics = DiagnosticBag.GetInstance()
                    Dim namespaceOrTypeSymbol = importBinder.BindNamespaceOrTypeSyntax(targetSyntax, unusedDiagnostics)
                    unusedDiagnostics.Free()

                    Debug.Assert(namespaceOrTypeSymbol IsNot Nothing)

                    If namespaceOrTypeSymbol.Kind = SymbolKind.ErrorType Then
                        ' Namespace is unrecognized.  The import may have been
                        ' valid in the original source but unnecessary.
                        Return False ' Don't add anything for this import.
                    End If

                    ' Native PDBs: aliased namespace is stored as NamespaceOrType
                    ' Portable PDBs: aliased namespace is stored as Namespace
                    If [alias] Is Nothing Then
                        If importsBuilder Is Nothing Then
                            importsBuilder = ArrayBuilder(Of NamespaceOrTypeAndImportsClausePosition).GetInstance()
                        End If

                        ' There's no real syntax, so there's no real position.  We'll give them separate numbers though.
                        importsBuilder.Add(New NamespaceOrTypeAndImportsClausePosition(namespaceOrTypeSymbol, position))
                    Else
                        Dim aliasSymbol As New AliasSymbol(importBinder.Compilation, importBinder.ContainingNamespaceOrType, [alias], namespaceOrTypeSymbol, NoLocation.Singleton)

                        If aliases Is Nothing Then
                            aliases = New Dictionary(Of String, AliasAndImportsClausePosition)()
                        End If

                        ' There's no real syntax, so there's no real position.  We'll give them separate numbers though.
                        aliases([alias]) = New AliasAndImportsClausePosition(aliasSymbol, position)
                    End If

                Case ImportTargetKind.NamespaceOrType ' Aliased namespace or type (native PDB only)
                    Dim unusedDiagnostics = DiagnosticBag.GetInstance()
                    Dim namespaceOrTypeSymbol = importBinder.BindNamespaceOrTypeSyntax(targetSyntax, unusedDiagnostics)
                    unusedDiagnostics.Free()

                    Debug.Assert(namespaceOrTypeSymbol IsNot Nothing)

                    If namespaceOrTypeSymbol.Kind = SymbolKind.ErrorType Then
                        ' Type is unrecognized.  The import may have been
                        ' valid in the original source but unnecessary.
                        Return False ' Don't add anything for this import.
                    End If

                    Debug.Assert([alias] IsNot Nothing) ' Implied by TargetKind

                    Dim aliasSymbol As New AliasSymbol(importBinder.Compilation, importBinder.ContainingNamespaceOrType, [alias], namespaceOrTypeSymbol, NoLocation.Singleton)

                    If aliases Is Nothing Then
                        aliases = New Dictionary(Of String, AliasAndImportsClausePosition)()
                    End If

                    ' There's no real syntax, so there's no real position.  We'll give them separate numbers though.
                    aliases([alias]) = New AliasAndImportsClausePosition(aliasSymbol, position)

                Case ImportTargetKind.XmlNamespace
                    If xmlImports Is Nothing Then
                        xmlImports = New Dictionary(Of String, XmlNamespaceAndImportsClausePosition)()
                    End If

                    ' There's no real syntax, so there's no real position.  We'll give them separate numbers though.
                    xmlImports(importRecord.Alias) = New XmlNamespaceAndImportsClausePosition(importRecord.TargetString, position)
                Case ImportTargetKind.DefaultNamespace
                    ' Processed ahead of time so that it can be incorporated into the compilation before
                    ' constructing the binder chain.
                    Return False
                Case ImportTargetKind.MethodToken ' forwarding
                    ' One level of forwarding is pre-processed away, but invalid PDBs might contain
                    ' chains.  Just ignore them (as in Dev12).
                    Return False
                Case ImportTargetKind.Defunct
                    Return False
                Case ImportTargetKind.Assembly
                    ' VB doesn't have extern aliases.
                    Throw ExceptionUtilities.UnexpectedValue(importRecord.TargetKind)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(importRecord.TargetKind)
            End Select

            Return True
        End Function

        Private Shared Function MergeAliases(Of T)(projectLevel As Dictionary(Of String, T), fileLevel As Dictionary(Of String, T)) As Dictionary(Of String, T)
            If projectLevel Is Nothing Then
                Return fileLevel
            ElseIf fileLevel Is Nothing Then
                Return projectLevel
            End If

            ' File-level aliases win.
            For Each pair In projectLevel
                Dim [alias] As String = pair.Key
                If Not fileLevel.ContainsKey([alias]) Then
                    fileLevel.Add([alias], pair.Value)
                End If
            Next

            Return fileLevel
        End Function

        Private Shared Function SelectAndInitializeCollection(Of T)(
            scope As ImportScope,
            ByRef projectLevelCollection As T,
            ByRef fileLevelCollection As T,
            initializeCollection As Func(Of T)) As T

            If scope = ImportScope.Project Then
                If projectLevelCollection Is Nothing Then
                    projectLevelCollection = initializeCollection()
                End If

                Return projectLevelCollection
            Else
                Debug.Assert(scope = ImportScope.File OrElse scope = ImportScope.Unspecified)

                If fileLevelCollection Is Nothing Then
                    fileLevelCollection = initializeCollection()
                End If

                Return fileLevelCollection
            End If
        End Function

        ''' <summary>
        ''' We don't want to use the real scanner because we want to treat keywords as identifiers.
        ''' Since the inputs are so simple, we'll just do the scanning ourselves.
        ''' </summary>
        Friend Shared Function TryParseDottedName(input As String, <Out> ByRef output As NameSyntax) As Boolean
            Dim pooled = PooledStringBuilder.GetInstance()
            Try
                Dim builder = pooled.Builder

                output = Nothing
                For Each ch In input
                    If builder.Length = 0 Then

                        If Not SyntaxFacts.IsIdentifierStartCharacter(ch) Then
                            output = Nothing
                            Return False
                        End If

                        builder.Append(ch)
                    ElseIf ch = "."c Then
                        Dim identifierName = SyntaxFactory.IdentifierName(builder.ToString())

                        builder.Clear()

                        output = If(output Is Nothing,
                            DirectCast(identifierName, NameSyntax),
                            SyntaxFactory.QualifiedName(output, identifierName))
                    ElseIf SyntaxFacts.IsIdentifierPartCharacter(ch) Then
                        builder.Append(ch)
                    Else
                        output = Nothing
                        Return False
                    End If
                Next

                ' There must be at least one character in the last identifier.
                If builder.Length = 0 Then
                    output = Nothing
                    Return False
                End If

                Dim finalIdentifierName = SyntaxFactory.IdentifierName(builder.ToString())
                output = If(output Is Nothing,
                    DirectCast(finalIdentifierName, NameSyntax),
                    SyntaxFactory.QualifiedName(output, finalIdentifierName))

                Return True
            Finally
                pooled.Free()
            End Try
        End Function

        Friend ReadOnly Property MessageProvider As CommonMessageProvider
            Get
                Return Me.Compilation.MessageProvider
            End Get
        End Property

        Private Shared Function GetLocalsForBinding(
            locals As ImmutableArray(Of LocalSymbol),
            displayClassVariableNamesInOrder As ImmutableArray(Of String),
            displayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable)) As ImmutableArray(Of LocalSymbol)

            Dim builder = ArrayBuilder(Of LocalSymbol).GetInstance()
            For Each local In locals
                Dim name = local.Name
                If name IsNot Nothing AndAlso Not IsGeneratedLocalName(name) Then
                    builder.Add(local)
                End If
            Next

            For Each variableName In displayClassVariableNamesInOrder
                Dim variable = displayClassVariables(variableName)
                Select Case variable.Kind
                    Case DisplayClassVariableKind.Local,
                         DisplayClassVariableKind.Parameter
                        Debug.Assert(Not IsGeneratedLocalName(variable.Name)) ' Established by GetDisplayClassVariables.
                        builder.Add(New EEDisplayClassFieldLocalSymbol(variable))
                End Select
            Next

            Return builder.ToImmutableAndFree()
        End Function

        ''' <summary>
        ''' Return a mapping of captured variables (parameters, locals, and "Me") to locals.
        ''' The mapping is needed to expose the original local identifiers (those from source)
        ''' in the binder.
        ''' </summary>
        Private Shared Sub GetDisplayClassVariables(
            method As MethodSymbol,
            locals As ImmutableArray(Of LocalSymbol),
            inScopeHoistedLocals As InScopeHoistedLocals,
            <Out> ByRef displayClassVariableNamesInOrder As ImmutableArray(Of String),
            <Out> ByRef displayClassVariables As ImmutableDictionary(Of String, DisplayClassVariable),
            <Out> ByRef hoistedParameterNames As ImmutableHashSet(Of String))

            ' Calculated the shortest paths from locals to instances of display classes.
            ' There should not be two instances of the same display class immediately
            ' within any particular method.
            Dim displayClassTypes = PooledHashSet(Of NamedTypeSymbol).GetInstance()
            Dim displayClassInstances = ArrayBuilder(Of DisplayClassInstanceAndFields).GetInstance()

            ' Add any display class instances from locals (these will contain any hoisted locals).
            For Each local As LocalSymbol In locals
                Dim localName = local.Name
                If localName IsNot Nothing AndAlso IsDisplayClassInstanceLocalName(localName) Then
                    Dim instance As New DisplayClassInstanceFromLocal(DirectCast(local, EELocalSymbol))
                    displayClassTypes.Add(instance.Type)
                    displayClassInstances.Add(New DisplayClassInstanceAndFields(instance))
                End If
            Next

            For Each parameter As ParameterSymbol In method.Parameters
                If GeneratedNames.GetKind(parameter.Name) = GeneratedNameKind.TransparentIdentifier Then
                    Dim instance As New DisplayClassInstanceFromParameter(parameter)
                    displayClassTypes.Add(instance.Type)
                    displayClassInstances.Add(New DisplayClassInstanceAndFields(instance))
                End If
            Next

            Dim containingType = method.ContainingType
            Dim isIteratorOrAsyncMethod = False
            If containingType.IsClosureOrStateMachineType() Then
                If Not method.IsShared Then
                    ' Add "Me" display class instance.
                    Dim instance As New DisplayClassInstanceFromParameter(method.MeParameter)
                    displayClassTypes.Add(instance.Type)
                    displayClassInstances.Add(New DisplayClassInstanceAndFields(instance))
                End If

                isIteratorOrAsyncMethod = containingType.IsStateMachineType()
            End If

            If displayClassInstances.Any() Then
                ' Find any additional display class instances breadth first.
                Dim depth = 0
                While GetDisplayClassInstances(displayClassTypes, displayClassInstances, depth) > 0
                    depth += 1
                End While

                ' The locals are the set of all fields from the display classes.
                Dim displayClassVariableNamesInOrderBuilder = ArrayBuilder(Of String).GetInstance()
                Dim displayClassVariablesBuilder = PooledDictionary(Of String, DisplayClassVariable).GetInstance()

                Dim parameterNames = PooledHashSet(Of String).GetInstance()
                If isIteratorOrAsyncMethod Then
                    Debug.Assert(containingType.IsClosureOrStateMachineType())

                    For Each field In containingType.GetMembers.OfType(Of FieldSymbol)()
                        ' All iterator and async state machine fields in VB have mangled names.
                        ' The ones beginning with "$VB$Local_" are the hoisted parameters.
                        Dim fieldName = field.Name
                        Dim parameterName As String = Nothing
                        If GeneratedNames.TryParseHoistedUserVariableName(fieldName, parameterName) Then
                            parameterNames.Add(parameterName)
                        End If
                    Next
                Else
                    For Each parameter In method.Parameters
                        parameterNames.Add(parameter.Name)
                    Next
                End If

                Dim pooledHoistedParameterNames = PooledHashSet(Of String).GetInstance()
                For Each instance In displayClassInstances
                    GetDisplayClassVariables(
                        displayClassVariableNamesInOrderBuilder,
                        displayClassVariablesBuilder,
                        parameterNames,
                        inScopeHoistedLocals,
                        instance,
                        pooledHoistedParameterNames)
                Next

                hoistedParameterNames = pooledHoistedParameterNames.ToImmutableHashSet()
                pooledHoistedParameterNames.Free()
                parameterNames.Free()

                displayClassVariableNamesInOrder = displayClassVariableNamesInOrderBuilder.ToImmutableAndFree()
                displayClassVariables = displayClassVariablesBuilder.ToImmutableDictionary()
                displayClassVariablesBuilder.Free()
            Else
                hoistedParameterNames = ImmutableHashSet(Of String).Empty
                displayClassVariableNamesInOrder = ImmutableArray(Of String).Empty
                displayClassVariables = ImmutableDictionary(Of String, DisplayClassVariable).Empty
            End If

            displayClassTypes.Free()
            displayClassInstances.Free()
        End Sub

        Private Shared Function IsHoistedMeFieldName(fieldName As String) As Boolean
            Return fieldName.Equals(StringConstants.HoistedMeName, StringComparison.Ordinal)
        End Function

        Private Shared Function IsLambdaMethodName(methodName As String) As Boolean
            Return methodName.StartsWith(StringConstants.LambdaMethodNamePrefix, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Test whether the name is for a local holding an instance of a display class.
        ''' </summary>
        Private Shared Function IsDisplayClassInstanceLocalName(name As String) As Boolean
            Debug.Assert(name IsNot Nothing) ' Verified by caller.
            Return name.StartsWith(StringConstants.ClosureVariablePrefix, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Test whether the name is for a field holding an instance of a display class
        ''' (i.e. a hoisted display class instance local).
        ''' </summary>
        Private Shared Function IsDisplayClassInstanceFieldName(name As String) As Boolean
            Debug.Assert(name IsNot Nothing) ' Verified by caller.
            Return name.StartsWith(StringConstants.HoistedSpecialVariablePrefix & StringConstants.ClosureVariablePrefix, StringComparison.Ordinal) OrElse
                name.StartsWith(StringConstants.StateMachineHoistedUserVariablePrefix & StringConstants.ClosureVariablePrefix, StringComparison.Ordinal) OrElse
                name.StartsWith(StringConstants.HoistedSpecialVariablePrefix & StringConstants.DisplayClassPrefix, StringComparison.Ordinal) ' Async lambda case
        End Function

        Private Shared Function IsTransparentIdentifierField(field As FieldSymbol) As Boolean
            Dim fieldName = field.Name

            Dim unmangledName As String = Nothing
            If GeneratedNames.TryParseHoistedUserVariableName(fieldName, unmangledName) Then
                fieldName = unmangledName
            ElseIf field.IsAnonymousTypeField(unmangledName) Then
                fieldName = unmangledName
            End If

            Return GeneratedNames.GetKind(fieldName) = GeneratedNameKind.TransparentIdentifier
        End Function

        Private Shared Function IsGeneratedLocalName(name As String) As Boolean
            Debug.Assert(name IsNot Nothing) ' Verified by caller.
            ' If a local's name contains "$", then it is a generated local.
            Return name.IndexOf("$"c) >= 0
        End Function

        Private Shared Function GetLocalResultFlags(local As LocalSymbol) As DkmClrCompilationResultFlags
            Debug.Assert(local.IsConst OrElse Not local.IsReadOnly, "Didn't expect user-referenceable read-only local.")
            Return If(
                local.IsConst,
                DkmClrCompilationResultFlags.ReadOnlyResult,
                DkmClrCompilationResultFlags.None)
        End Function

        ''' <summary>
        ''' Return the set of display class instances that can be reached from the given local.
        ''' A particular display class may be reachable from multiple locals.  In those cases,
        ''' the instance from the shortest path (fewest intermediate fields) is returned.
        ''' </summary>
        Private Shared Function GetDisplayClassInstances(
            displayClassTypes As HashSet(Of NamedTypeSymbol),
            displayClassInstances As ArrayBuilder(Of DisplayClassInstanceAndFields),
            depth As Integer) As Integer

            Debug.Assert(displayClassInstances.All(Function(p) p.Depth <= depth))

            Dim atDepth = ArrayBuilder(Of DisplayClassInstanceAndFields).GetInstance()
            atDepth.AddRange(displayClassInstances.Where(Function(p) p.Depth = depth))
            Debug.Assert(atDepth.Count > 0)

            Dim n = 0
            For Each instance In atDepth
                n += GetDisplayClassInstances(displayClassTypes, displayClassInstances, instance)
            Next

            atDepth.Free()
            Return n
        End Function

        Private Shared Function GetDisplayClassInstances(
            displayClassTypes As HashSet(Of NamedTypeSymbol),
            displayClassInstances As ArrayBuilder(Of DisplayClassInstanceAndFields),
            instance As DisplayClassInstanceAndFields) As Integer

            ' Display class instance.  The display class fields are variables.
            Dim n = 0
            For Each member In instance.Type.GetMembers()
                If member.Kind <> SymbolKind.Field Then
                    Continue For
                End If
                Dim field = DirectCast(member, FieldSymbol)
                Dim fieldType = field.Type
                Dim fieldName As String = field.Name
                If IsDisplayClassInstanceFieldName(fieldName) OrElse
                    IsTransparentIdentifierField(field) Then
                    Debug.Assert(Not field.IsShared)
                    ' A local that is itself a display class instance.
                    If displayClassTypes.Add(DirectCast(fieldType, NamedTypeSymbol)) Then
                        Dim other = instance.FromField(field)
                        displayClassInstances.Add(other)
                        n += 1
                    End If
                End If
            Next
            Return n
        End Function

        Private Shared Sub GetDisplayClassVariables(
            displayClassVariableNamesInOrder As ArrayBuilder(Of String),
            displayClassVariablesBuilder As Dictionary(Of String, DisplayClassVariable),
            parameterNames As HashSet(Of String),
            inScopeHoistedLocals As InScopeHoistedLocals,
            instance As DisplayClassInstanceAndFields,
            hoistedParameterNames As HashSet(Of String))

            ' Display class instance.  The display class fields are variables.
            For Each member In instance.Type.GetMembers()
                If member.Kind <> SymbolKind.Field Then
                    Continue For
                End If

                Dim field = DirectCast(member, FieldSymbol)
                Dim fieldName = field.Name

                Dim unmangledName As String = Nothing
                If field.IsAnonymousTypeField(unmangledName) Then
                    fieldName = unmangledName
                End If

                Dim variableKind As DisplayClassVariableKind
                Dim variableName As String

                If fieldName.StartsWith(StringConstants.HoistedUserVariablePrefix, StringComparison.Ordinal) Then
                    Debug.Assert(Not field.IsShared)
                    variableKind = DisplayClassVariableKind.Local
                    variableName = fieldName.Substring(StringConstants.HoistedUserVariablePrefix.Length)
                ElseIf fieldName.StartsWith(StringConstants.HoistedSpecialVariablePrefix, StringComparison.Ordinal) Then
                    Debug.Assert(Not field.IsShared)
                    variableKind = DisplayClassVariableKind.Local
                    variableName = fieldName.Substring(StringConstants.HoistedSpecialVariablePrefix.Length)
                ElseIf fieldName.StartsWith(StringConstants.StateMachineHoistedUserVariablePrefix, StringComparison.Ordinal) Then
                    Debug.Assert(Not field.IsShared)
                    variableKind = DisplayClassVariableKind.Local
                    variableName = Nothing
                    Dim unusedIndex As Integer = Nothing
                    If Not inScopeHoistedLocals.IsInScope(fieldName) OrElse Not GeneratedNames.TryParseStateMachineHoistedUserVariableName(fieldName, variableName, unusedIndex) Then
                        Continue For
                    End If
                ElseIf IsHoistedMeFieldName(fieldName) Then
                    Debug.Assert(Not field.IsShared)
                    ' A reference to "Me".
                    variableKind = DisplayClassVariableKind.Me
                    variableName = fieldName ' As in C#, we retain the mangled name.  It shouldn't be used, other than as a dictionary key.
                ElseIf fieldName.StartsWith(StringConstants.LambdaCacheFieldPrefix, StringComparison.Ordinal) Then
                    Continue For
                ElseIf GeneratedNames.GetKind(fieldName) = GeneratedNameKind.TransparentIdentifier
                    ' A transparent identifier (field) in an anonymous type synthesized for a transparent identifier.
                    Debug.Assert(Not field.IsShared)
                    Continue For
                Else
                    variableKind = DisplayClassVariableKind.Local
                    variableName = fieldName
                End If

                If variableKind <> DisplayClassVariableKind.Me AndAlso IsGeneratedLocalName(variableName) Then
                    Continue For
                End If

                If variableKind = DisplayClassVariableKind.Local AndAlso parameterNames.Contains(variableName) Then
                    variableKind = DisplayClassVariableKind.Parameter
                    hoistedParameterNames.Add(variableName)
                End If

                If displayClassVariablesBuilder.ContainsKey(variableName) Then
                    ' Only expecting duplicates for async state machine
                    ' fields (that should be at the top-level).
                    Debug.Assert(displayClassVariablesBuilder(variableName).DisplayClassFields.Count() = 1)
                    Debug.Assert(instance.Fields.Count() >= 1) ' greater depth
                    ' There are two ways names could collide:
                    '   1) hoisted state machine locals in different scopes
                    '   2) hoisted state machine parameters that are also captured by lambdas
                    ' The former should be impossible since we dropped out-of-scope hoisted
                    ' locals above.  We assert that we are seeing the latter.
                    Debug.Assert((variableKind = DisplayClassVariableKind.Parameter) OrElse
                        (variableKind = DisplayClassVariableKind.Me))
                Else
                    displayClassVariableNamesInOrder.Add(variableName)
                    displayClassVariablesBuilder.Add(variableName, instance.ToVariable(variableName, variableKind, field))
                End If
            Next
        End Sub

        ''' <summary>
        ''' Identifies the method in which binding should occur.
        ''' </summary>
        ''' <param name="candidateSubstitutedSourceMethod">
        ''' The symbol of the method that is currently on top of the callstack, with
        ''' EE type parameters substituted in place of the original type parameters.
        ''' </param>
        ''' <param name="sourceMethodMustBeInstance">
        ''' True if "Me" is available via a display class in the current context
        ''' </param>
        ''' <returns>
        ''' If <paramref name="candidateSubstitutedSourceMethod"/> is compiler-generated,
        ''' then we will attempt to determine which user-derived method caused it to be
        ''' generated.  For example, if <paramref name="candidateSubstitutedSourceMethod"/>
        ''' is a state machine MoveNext method, then we will try to find the iterator or
        ''' async method for which it was generated.  if we are able to find the original
        ''' method, then we will substitute in the EE type parameters.  Otherwise, we will
        ''' return <paramref name="candidateSubstitutedSourceMethod"/>.
        ''' </returns>
        ''' <remarks>
        ''' In the event that the original method is overloaded, we may not be able to determine
        ''' which overload actually corresponds to the state machine.  In particular, we do not
        ''' have information about the signature of the original method (i.e. number of parameters,
        ''' parameter types and ref-kinds, return type).  However, we conjecture that this
        ''' level of uncertainty is acceptable, since parameters are managed by a separate binder
        ''' in the synthesized binder chain and we have enough information to check the other method
        ''' properties that are used during binding (e.g. static-ness, generic arity, type parameter
        ''' constraints).
        ''' </remarks>
        Friend Shared Function GetSubstitutedSourceMethod(
            candidateSubstitutedSourceMethod As MethodSymbol,
            sourceMethodMustBeInstance As Boolean) As MethodSymbol

            Dim candidateSubstitutedSourceType = candidateSubstitutedSourceMethod.ContainingType
            Dim candidateSourceTypeName = candidateSubstitutedSourceType.Name

            Dim desiredMethodName As String = Nothing
            If IsLambdaMethodName(candidateSubstitutedSourceMethod.Name) OrElse
                    GeneratedNames.TryParseStateMachineTypeName(candidateSourceTypeName, desiredMethodName) Then

                ' We could be in the MoveNext method of an async lambda.  If that is the case, we can't 
                ' figure out desiredMethodName by unmangling the name.
                If desiredMethodName IsNot Nothing AndAlso IsLambdaMethodName(desiredMethodName) Then
                    desiredMethodName = Nothing
                    Dim containing = candidateSubstitutedSourceType.ContainingType
                    Debug.Assert(containing IsNot Nothing)
                    If containing.IsClosureType() Then
                        candidateSubstitutedSourceType = containing
                        sourceMethodMustBeInstance = candidateSubstitutedSourceType.MemberNames.Contains(StringConstants.HoistedMeName, StringComparer.Ordinal)
                    End If
                End If

                Dim desiredTypeParameters = candidateSubstitutedSourceType.OriginalDefinition.TypeParameters

                ' Type containing the original iterator, async, or lambda-containing method.
                Dim substitutedSourceType = GetNonClosureOrStateMachineContainer(candidateSubstitutedSourceType)

                For Each candidateMethod In substitutedSourceType.GetMembers().OfType(Of MethodSymbol)()
                    If IsViableSourceMethod(candidateMethod, desiredMethodName, desiredTypeParameters, sourceMethodMustBeInstance) Then
                        Return If(desiredTypeParameters.Length = 0,
                            candidateMethod,
                            candidateMethod.Construct(candidateSubstitutedSourceType.TypeArguments))
                    End If
                Next

                Debug.Assert(False, String.Format("Why didn't we find a substituted source method for {0}?", candidateSubstitutedSourceMethod))
            End If

            Return candidateSubstitutedSourceMethod
        End Function

        Private Shared Function GetNonClosureOrStateMachineContainer(type As NamedTypeSymbol) As NamedTypeSymbol
            ' 1) Display class and state machine types are always nested within the types
            '    that use them so that they can access private members of those types).
            ' 2) The native compiler used to produce nested display classes for nested lambdas,
            '    so we may have to walk out more than one level.
            While type.IsClosureOrStateMachineType()
                type = type.ContainingType
            End While
            Debug.Assert(type IsNot Nothing)

            Return type
        End Function

        Private Shared Function IsViableSourceMethod(
            candidateMethod As MethodSymbol,
            desiredMethodName As String,
            desiredTypeParameters As ImmutableArray(Of TypeParameterSymbol),
            desiredMethodMustBeInstance As Boolean) As Boolean

            Return _
                Not candidateMethod.IsMustOverride AndAlso
                (Not (desiredMethodMustBeInstance AndAlso candidateMethod.IsShared)) AndAlso
                (desiredMethodName Is Nothing OrElse desiredMethodName = candidateMethod.Name) AndAlso
                HasDesiredConstraints(candidateMethod, desiredTypeParameters)
        End Function

        Private Shared Function HasDesiredConstraints(candidateMethod As MethodSymbol, desiredTypeParameters As ImmutableArray(Of TypeParameterSymbol)) As Boolean
            Dim arity = candidateMethod.Arity
            If arity <> desiredTypeParameters.Length Then
                Return False
            ElseIf arity = 0 Then
                Return True
            End If

            Dim indexedTypeParameters = IndexedTypeParameterSymbol.Take(arity).As(Of TypeSymbol)

            ' NOTE: Can't seem to construct a type map for just the method type parameters,
            ' so we also specify a trivial map for the type parameters of the (immediately)
            ' containing type.
            Dim candidateMethodDefinition As MethodSymbol = candidateMethod.OriginalDefinition
            Dim sourceTypeTypeParameters As ImmutableArray(Of TypeParameterSymbol) = candidateMethodDefinition.ContainingType.TypeParameters
            Dim candidateTypeMap = TypeSubstitution.Create(
                candidateMethodDefinition,
                sourceTypeTypeParameters.Concat(candidateMethodDefinition.TypeParameters),
                sourceTypeTypeParameters.As(Of TypeSymbol).Concat(indexedTypeParameters))
            Debug.Assert(candidateTypeMap.PairsIncludingParent.Length = arity)

            Dim desiredTypeMap = TypeSubstitution.Create(
                desiredTypeParameters(0).ContainingSymbol,
                desiredTypeParameters,
                indexedTypeParameters)
            Debug.Assert(desiredTypeMap.PairsIncludingParent.Length = arity)

            For i = 0 To arity - 1
                If Not MethodSignatureComparer.HaveSameConstraints(candidateMethodDefinition.TypeParameters(i), candidateTypeMap, desiredTypeParameters(i), desiredTypeMap) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Structure DisplayClassInstanceAndFields
            Friend ReadOnly Instance As DisplayClassInstance
            Friend ReadOnly Fields As ConsList(Of FieldSymbol)

            Friend Sub New(instance As DisplayClassInstance)
                MyClass.New(instance, ConsList(Of FieldSymbol).Empty)
                Debug.Assert(instance.Type.IsClosureOrStateMachineType() OrElse
                             GeneratedNames.GetKind(instance.Type.Name) = GeneratedNameKind.AnonymousType)
            End Sub

            Private Sub New(instance As DisplayClassInstance, fields As ConsList(Of FieldSymbol))
                Me.Instance = instance
                Me.Fields = fields
            End Sub

            Friend ReadOnly Property Type As NamedTypeSymbol
                Get
                    Return If(Me.Fields.Any(), DirectCast(Me.Fields.Head.Type, NamedTypeSymbol), Me.Instance.Type)
                End Get
            End Property

            Friend ReadOnly Property Depth As Integer
                Get
                    Return Me.Fields.Count()
                End Get
            End Property

            Friend Function FromField(field As FieldSymbol) As DisplayClassInstanceAndFields
                Debug.Assert(field.Type.IsClosureOrStateMachineType() OrElse
                             GeneratedNames.GetKind(field.Type.Name) = GeneratedNameKind.AnonymousType)
                Return New DisplayClassInstanceAndFields(Me.Instance, Me.Fields.Prepend(field))
            End Function

            Friend Function ToVariable(name As String, kind As DisplayClassVariableKind, field As FieldSymbol) As DisplayClassVariable
                Return New DisplayClassVariable(name, kind, Me.Instance, Me.Fields.Prepend(field))
            End Function
        End Structure
    End Class
End Namespace
