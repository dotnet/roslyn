// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class CompilationContext
    {
        private static readonly SymbolDisplayFormat s_fullNameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        internal readonly CSharpCompilation Compilation;
        internal readonly Binder NamespaceBinder; // Internal for test purposes.

        private readonly MethodSymbol _currentFrame;
        private readonly ImmutableArray<LocalSymbol> _locals;
        private readonly ImmutableDictionary<string, DisplayClassVariable> _displayClassVariables;
        private readonly ImmutableArray<string> _sourceMethodParametersInOrder;
        private readonly ImmutableArray<LocalSymbol> _localsForBinding;
        private readonly bool _methodNotType;

        /// <summary>
        /// Create a context to compile expressions within a method scope.
        /// </summary>
        internal CompilationContext(
            CSharpCompilation compilation,
            MethodSymbol currentFrame,
            MethodSymbol currentSourceMethod,
            ImmutableArray<LocalSymbol> locals,
            ImmutableSortedSet<int> inScopeHoistedLocalSlots,
            MethodDebugInfo<TypeSymbol, LocalSymbol> methodDebugInfo)
        {
            _currentFrame = currentFrame;
            _methodNotType = !locals.IsDefault;

            // NOTE: Since this is done within CompilationContext, it will not be cached.
            // CONSIDER: The values should be the same everywhere in the module, so they
            // could be cached.  
            // (Catch: what happens in a type context without a method def?)
            this.Compilation = GetCompilationWithExternAliases(compilation, methodDebugInfo.ExternAliasRecords);

            // Each expression compile should use a unique compilation
            // to ensure expression-specific synthesized members can be
            // added (anonymous types, for instance).
            Debug.Assert(this.Compilation != compilation);

            this.NamespaceBinder = CreateBinderChain(
                this.Compilation,
                (PEModuleSymbol)currentFrame.ContainingModule,
                currentFrame.ContainingNamespace,
                methodDebugInfo.ImportRecordGroups);

            if (_methodNotType)
            {
                _locals = locals;
                _sourceMethodParametersInOrder = GetSourceMethodParametersInOrder(currentFrame, currentSourceMethod);
                ImmutableArray<string> displayClassVariableNamesInOrder;
                GetDisplayClassVariables(
                    currentFrame,
                    _locals,
                    inScopeHoistedLocalSlots,
                    _sourceMethodParametersInOrder,
                    out displayClassVariableNamesInOrder,
                    out _displayClassVariables);
                Debug.Assert(displayClassVariableNamesInOrder.Length == _displayClassVariables.Count);
                _localsForBinding = GetLocalsForBinding(_locals, displayClassVariableNamesInOrder, _displayClassVariables);
            }
            else
            {
                _locals = ImmutableArray<LocalSymbol>.Empty;
                _displayClassVariables = ImmutableDictionary<string, DisplayClassVariable>.Empty;
                _localsForBinding = ImmutableArray<LocalSymbol>.Empty;
            }

            // Assert that the cheap check for "this" is equivalent to the expensive check for "this".
            Debug.Assert(
                (GetThisProxy(_displayClassVariables) != null) ==
                _displayClassVariables.Values.Any(v => v.Kind == DisplayClassVariableKind.This));
        }

        internal CommonPEModuleBuilder CompileExpressions(
            ImmutableArray<CSharpSyntaxNode> syntaxNodes,
            string typeNameBase,
            string methodName,
            DiagnosticBag diagnostics)
        {
            // Create a separate synthesized type for each evaluation method.
            // (Necessary for VB in particular since the EENamedTypeSymbol.Locations
            // is tied to the expression syntax in VB.)
            var synthesizedTypes = syntaxNodes.SelectAsArray(
                (syntax, i, arg) => (NamedTypeSymbol)CreateSynthesizedType(syntax, typeNameBase + i, methodName, ImmutableArray<Alias>.Empty),
                (object)null);
            if (synthesizedTypes.Length == 0)
            {
                return null;
            }
            var module = CreateModuleBuilder(
                this.Compilation,
                additionalTypes: synthesizedTypes,
                testData: null,
                diagnostics: diagnostics);
            Debug.Assert(module != null);
            this.Compilation.Compile(
                module,
                emittingPdb: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: CancellationToken.None);
            return diagnostics.HasAnyErrors() ? null : module;
        }

        internal CommonPEModuleBuilder CompileExpression(
            CSharpSyntaxNode syntax,
            string typeName,
            string methodName,
            ImmutableArray<Alias> aliases,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData,
            DiagnosticBag diagnostics,
            out EEMethodSymbol synthesizedMethod)
        {
            var synthesizedType = CreateSynthesizedType(syntax, typeName, methodName, aliases);
            var module = CreateModuleBuilder(
                this.Compilation,
                additionalTypes: ImmutableArray.Create((NamedTypeSymbol)synthesizedType),
                testData: testData,
                diagnostics: diagnostics);

            Debug.Assert(module != null);

            this.Compilation.Compile(
                module,
                emittingPdb: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: CancellationToken.None);

            if (diagnostics.HasAnyErrors())
            {
                synthesizedMethod = null;
                return null;
            }

            synthesizedMethod = GetSynthesizedMethod(synthesizedType);
            return module;
        }

        private EENamedTypeSymbol CreateSynthesizedType(
            CSharpSyntaxNode syntax,
            string typeName,
            string methodName,
            ImmutableArray<Alias> aliases)
        {
            var objectType = this.Compilation.GetSpecialType(SpecialType.System_Object);
            var synthesizedType = new EENamedTypeSymbol(
                this.Compilation.SourceModule.GlobalNamespace,
                objectType,
                syntax,
                _currentFrame,
                typeName,
                methodName,
                this,
                (EEMethodSymbol method, DiagnosticBag diags, out ImmutableArray<LocalSymbol> declaredLocals, out ResultProperties properties) =>
                {
                    var hasDisplayClassThis = GetThisProxy(_displayClassVariables) != null;
                    var binder = ExtendBinderChain(
                        syntax,
                        aliases,
                        method,
                        this.NamespaceBinder,
                        hasDisplayClassThis,
                        _methodNotType,
                        out declaredLocals);
                    var statementSyntax = syntax as StatementSyntax;

                    return (statementSyntax == null) ?
                        BindExpression(binder, (ExpressionSyntax)syntax, diags, out properties) :
                        BindStatement(binder, statementSyntax, diags, out properties);
                });
            return synthesizedType;
        }

        internal CommonPEModuleBuilder CompileAssignment(
            ExpressionSyntax syntax,
            string typeName,
            string methodName,
            ImmutableArray<Alias> aliases,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData,
            DiagnosticBag diagnostics,
            out EEMethodSymbol synthesizedMethod)
        {
            var objectType = this.Compilation.GetSpecialType(SpecialType.System_Object);
            var synthesizedType = new EENamedTypeSymbol(
                Compilation.SourceModule.GlobalNamespace,
                objectType,
                syntax,
                _currentFrame,
                typeName,
                methodName,
                this,
                (EEMethodSymbol method, DiagnosticBag diags, out ImmutableArray<LocalSymbol> declaredLocals, out ResultProperties properties) =>
                {
                    var hasDisplayClassThis = GetThisProxy(_displayClassVariables) != null;
                    var binder = ExtendBinderChain(
                        syntax,
                        aliases,
                        method,
                        this.NamespaceBinder,
                        hasDisplayClassThis,
                        methodNotType: true,
                        declaredLocals: out declaredLocals);
                    properties = new ResultProperties(DkmClrCompilationResultFlags.PotentialSideEffect);
                    return BindAssignment(binder, syntax, diags);
                });

            var module = CreateModuleBuilder(
                this.Compilation,
                additionalTypes: ImmutableArray.Create((NamedTypeSymbol)synthesizedType),
                testData: testData,
                diagnostics: diagnostics);

            Debug.Assert(module != null);

            this.Compilation.Compile(
                module,
                emittingPdb: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: CancellationToken.None);

            if (diagnostics.HasAnyErrors())
            {
                synthesizedMethod = null;
                return null;
            }

            synthesizedMethod = GetSynthesizedMethod(synthesizedType);
            return module;
        }

        private static EEMethodSymbol GetSynthesizedMethod(EENamedTypeSymbol synthesizedType)
        {
            return (EEMethodSymbol)synthesizedType.Methods[0];
        }

        private static string GetNextMethodName(ArrayBuilder<MethodSymbol> builder)
        {
            return string.Format("<>m{0}", builder.Count);
        }

        /// <summary>
        /// Generate a class containing methods that represent
        /// the set of arguments and locals at the current scope.
        /// </summary>
        internal CommonPEModuleBuilder CompileGetLocals(
            string typeName,
            ArrayBuilder<LocalAndMethod> localBuilder,
            bool argumentsOnly,
            ImmutableArray<Alias> aliases,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData,
            DiagnosticBag diagnostics)
        {
            var objectType = this.Compilation.GetSpecialType(SpecialType.System_Object);
            var allTypeParameters = _currentFrame.GetAllTypeParameters();
            var additionalTypes = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            EENamedTypeSymbol typeVariablesType = null;
            if (!argumentsOnly && (allTypeParameters.Length > 0))
            {
                // Generate a generic type with matching type parameters.
                // A null instance of the type will be used to represent the
                // "Type variables" local.
                typeVariablesType = new EENamedTypeSymbol(
                    this.Compilation.SourceModule.GlobalNamespace,
                    objectType,
                    _currentFrame,
                    ExpressionCompilerConstants.TypeVariablesClassName,
                    (m, t) => ImmutableArray.Create<MethodSymbol>(new EEConstructorSymbol(t)),
                    allTypeParameters,
                    (t1, t2) => allTypeParameters.SelectAsArray((tp, i, t) => (TypeParameterSymbol)new SimpleTypeParameterSymbol(t, i, tp.Name), t2));
                additionalTypes.Add(typeVariablesType);
            }

            var synthesizedType = new EENamedTypeSymbol(
                Compilation.SourceModule.GlobalNamespace,
                objectType,
                _currentFrame,
                typeName,
                (m, container) =>
                {
                    var methodBuilder = ArrayBuilder<MethodSymbol>.GetInstance();

                    if (!argumentsOnly)
                    {
                        // Pseudo-variables: $exception, $ReturnValue, etc.
                        if (aliases.Length > 0)
                        {
                            var sourceAssembly = Compilation.SourceAssembly;
                            var typeNameDecoder = new EETypeNameDecoder(Compilation, (PEModuleSymbol)_currentFrame.ContainingModule);
                            foreach (var alias in aliases)
                            {
                                if (alias.IsReturnValueWithoutIndex())
                                {
                                    Debug.Assert(aliases.Count(a => a.Kind == DkmClrAliasKind.ReturnValue) > 1);
                                    continue;
                                }

                                var local = PlaceholderLocalSymbol.Create(
                                    typeNameDecoder,
                                    _currentFrame,
                                    sourceAssembly,
                                    alias);
                                // Skip pseudo-variables with errors.
                                if (local.GetUseSiteDiagnostic()?.Severity == DiagnosticSeverity.Error)
                                {
                                    continue;
                                }
                                var methodName = GetNextMethodName(methodBuilder);
                                var syntax = SyntaxFactory.IdentifierName(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken));
                                var aliasMethod = this.CreateMethod(
                                    container,
                                    methodName,
                                    syntax,
                                    (EEMethodSymbol method, DiagnosticBag diags, out ImmutableArray<LocalSymbol> declaredLocals, out ResultProperties properties) =>
                                    {
                                        declaredLocals = ImmutableArray<LocalSymbol>.Empty;
                                        var expression = new BoundLocal(syntax, local, constantValueOpt: null, type: local.Type);
                                        properties = default(ResultProperties);
                                        return new BoundReturnStatement(syntax, RefKind.None, expression) { WasCompilerGenerated = true };
                                    });
                                var flags = local.IsWritableVariable ? DkmClrCompilationResultFlags.None : DkmClrCompilationResultFlags.ReadOnlyResult;
                                localBuilder.Add(MakeLocalAndMethod(local, aliasMethod, flags));
                                methodBuilder.Add(aliasMethod);
                            }
                        }

                        // "this" for non-static methods that are not display class methods or
                        // display class methods where the display class contains "<>4__this".
                        if ((!m.IsStatic && !IsDisplayClassType(m.ContainingType)) || GetThisProxy(_displayClassVariables) != null)
                        {
                            var methodName = GetNextMethodName(methodBuilder);
                            var method = this.GetThisMethod(container, methodName);
                            localBuilder.Add(new CSharpLocalAndMethod("this", "this", method, DkmClrCompilationResultFlags.None)); // Note: writable in dev11.
                            methodBuilder.Add(method);
                        }
                    }

                    var itemsAdded = PooledHashSet<string>.GetInstance();

                    // Method parameters
                    int parameterIndex = m.IsStatic ? 0 : 1;
                    foreach (var parameter in m.Parameters)
                    {
                        var parameterName = parameter.Name;
                        if (GeneratedNames.GetKind(parameterName) == GeneratedNameKind.None &&
                            !IsDisplayClassParameter(parameter))
                        {
                            itemsAdded.Add(parameterName);
                            AppendParameterAndMethod(localBuilder, methodBuilder, parameter, container, parameterIndex);
                        }

                        parameterIndex++;
                    }

                    // In case of iterator or async state machine, the 'm' method has no parameters
                    // but the source method can have parameters to iterate over.
                    if (itemsAdded.Count == 0 && _sourceMethodParametersInOrder.Length != 0)
                    {
                        var localsDictionary = PooledDictionary<string, (LocalSymbol, int)>.GetInstance();
                        int localIndex = 0;
                        foreach (var local in _localsForBinding)
                        {
                            localsDictionary.Add(local.Name, (local, localIndex));
                            localIndex++;
                        }

                        foreach (var argumentName in _sourceMethodParametersInOrder)
                        {
                            (LocalSymbol local, int localIndex) localSymbolAndIndex;
                            if (localsDictionary.TryGetValue(argumentName, out localSymbolAndIndex))
                            {
                                itemsAdded.Add(argumentName);
                                var local = localSymbolAndIndex.local;
                                AppendLocalAndMethod(localBuilder, methodBuilder, local, container, localSymbolAndIndex.localIndex, GetLocalResultFlags(local));
                            }
                        }

                        localsDictionary.Free();
                    }

                    if (!argumentsOnly)
                    {
                        // Locals which were not added as parameters or parameters of the source method.
                        int localIndex = 0;
                        foreach (var local in _localsForBinding)
                        {
                            if (!itemsAdded.Contains(local.Name))
                            {
                                AppendLocalAndMethod(localBuilder, methodBuilder, local, container, localIndex, GetLocalResultFlags(local));
                            }

                            localIndex++;
                        }

                        // "Type variables".
                        if ((object)typeVariablesType != null)
                        {
                            var methodName = GetNextMethodName(methodBuilder);
                            var returnType = typeVariablesType.Construct(allTypeParameters.Cast<TypeParameterSymbol, TypeSymbol>());
                            var method = this.GetTypeVariablesMethod(container, methodName, returnType);
                            localBuilder.Add(new CSharpLocalAndMethod(
                                ExpressionCompilerConstants.TypeVariablesLocalName,
                                ExpressionCompilerConstants.TypeVariablesLocalName,
                                method,
                                DkmClrCompilationResultFlags.ReadOnlyResult));
                            methodBuilder.Add(method);
                        }
                    }

                    itemsAdded.Free();
                    return methodBuilder.ToImmutableAndFree();
                });

            additionalTypes.Add(synthesizedType);

            var module = CreateModuleBuilder(
                this.Compilation,
                additionalTypes: additionalTypes.ToImmutableAndFree(),
                testData: testData,
                diagnostics: diagnostics);

            Debug.Assert(module != null);

            this.Compilation.Compile(
                module,
                emittingPdb: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: CancellationToken.None);

            return diagnostics.HasAnyErrors() ? null : module;
        }

        private void AppendLocalAndMethod(
            ArrayBuilder<LocalAndMethod> localBuilder,
            ArrayBuilder<MethodSymbol> methodBuilder,
            LocalSymbol local,
            EENamedTypeSymbol container,
            int localIndex,
            DkmClrCompilationResultFlags resultFlags)
        {
            var methodName = GetNextMethodName(methodBuilder);
            var method = this.GetLocalMethod(container, methodName, local.Name, localIndex);
            localBuilder.Add(MakeLocalAndMethod(local, method, resultFlags));
            methodBuilder.Add(method);
        }

        private void AppendParameterAndMethod(
            ArrayBuilder<LocalAndMethod> localBuilder,
            ArrayBuilder<MethodSymbol> methodBuilder,
            ParameterSymbol parameter,
            EENamedTypeSymbol container,
            int parameterIndex)
        {
            // Note: The native EE doesn't do this, but if we don't escape keyword identifiers,
            // the ResultProvider needs to be able to disambiguate cases like "this" and "@this",
            // which it can't do correctly without semantic information.
            var name = SyntaxHelpers.EscapeKeywordIdentifiers(parameter.Name);
            var methodName = GetNextMethodName(methodBuilder);
            var method = this.GetParameterMethod(container, methodName, name, parameterIndex);
            localBuilder.Add(new CSharpLocalAndMethod(name, name, method, DkmClrCompilationResultFlags.None));
            methodBuilder.Add(method);
        }

        private static LocalAndMethod MakeLocalAndMethod(LocalSymbol local, MethodSymbol method, DkmClrCompilationResultFlags flags)
        {
            // Note: The native EE doesn't do this, but if we don't escape keyword identifiers,
            // the ResultProvider needs to be able to disambiguate cases like "this" and "@this",
            // which it can't do correctly without semantic information.
            var escapedName = SyntaxHelpers.EscapeKeywordIdentifiers(local.Name);
            var displayName = (local as PlaceholderLocalSymbol)?.DisplayName ?? escapedName;
            return new CSharpLocalAndMethod(escapedName, displayName, method, flags);
        }

        private static EEAssemblyBuilder CreateModuleBuilder(
            CSharpCompilation compilation,
            ImmutableArray<NamedTypeSymbol> additionalTypes,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData,
            DiagnosticBag diagnostics)
        {
            // Each assembly must have a unique name.
            var emitOptions = new EmitOptions(outputNameOverride: ExpressionCompilerUtilities.GenerateUniqueName());

            string runtimeMetadataVersion = compilation.GetRuntimeMetadataVersion(emitOptions, diagnostics);
            var serializationProperties = compilation.ConstructModuleSerializationProperties(emitOptions, runtimeMetadataVersion);
            return new EEAssemblyBuilder(
                compilation.SourceAssembly,
                emitOptions,
                serializationProperties,
                additionalTypes,
                contextType => GetNonDisplayClassContainer(((EENamedTypeSymbol)contextType).SubstitutedSourceType),
                testData);
        }

        internal EEMethodSymbol CreateMethod(
            EENamedTypeSymbol container,
            string methodName,
            CSharpSyntaxNode syntax,
            GenerateMethodBody generateMethodBody)
        {
            return new EEMethodSymbol(
                container,
                methodName,
                syntax.Location,
                _currentFrame,
                _locals,
                _localsForBinding,
                _displayClassVariables,
                generateMethodBody);
        }

        private EEMethodSymbol GetLocalMethod(EENamedTypeSymbol container, string methodName, string localName, int localIndex)
        {
            var syntax = SyntaxFactory.IdentifierName(localName);
            return this.CreateMethod(container, methodName, syntax, (EEMethodSymbol method, DiagnosticBag diagnostics, out ImmutableArray<LocalSymbol> declaredLocals, out ResultProperties properties) =>
            {
                declaredLocals = ImmutableArray<LocalSymbol>.Empty;
                var local = method.LocalsForBinding[localIndex];
                var expression = new BoundLocal(syntax, local, constantValueOpt: local.GetConstantValue(null, null, diagnostics), type: local.Type);
                properties = default(ResultProperties);
                return new BoundReturnStatement(syntax, RefKind.None, expression) { WasCompilerGenerated = true };
            });
        }

        private EEMethodSymbol GetParameterMethod(EENamedTypeSymbol container, string methodName, string parameterName, int parameterIndex)
        {
            var syntax = SyntaxFactory.IdentifierName(parameterName);
            return this.CreateMethod(container, methodName, syntax, (EEMethodSymbol method, DiagnosticBag diagnostics, out ImmutableArray<LocalSymbol> declaredLocals, out ResultProperties properties) =>
            {
                declaredLocals = ImmutableArray<LocalSymbol>.Empty;
                var parameter = method.Parameters[parameterIndex];
                var expression = new BoundParameter(syntax, parameter);
                properties = default(ResultProperties);
                return new BoundReturnStatement(syntax, RefKind.None, expression) { WasCompilerGenerated = true };
            });
        }

        private EEMethodSymbol GetThisMethod(EENamedTypeSymbol container, string methodName)
        {
            var syntax = SyntaxFactory.ThisExpression();
            return this.CreateMethod(container, methodName, syntax, (EEMethodSymbol method, DiagnosticBag diagnostics, out ImmutableArray<LocalSymbol> declaredLocals, out ResultProperties properties) =>
            {
                declaredLocals = ImmutableArray<LocalSymbol>.Empty;
                var expression = new BoundThisReference(syntax, GetNonDisplayClassContainer(container.SubstitutedSourceType));
                properties = default(ResultProperties);
                return new BoundReturnStatement(syntax, RefKind.None, expression) { WasCompilerGenerated = true };
            });
        }

        private EEMethodSymbol GetTypeVariablesMethod(EENamedTypeSymbol container, string methodName, NamedTypeSymbol typeVariablesType)
        {
            var syntax = SyntaxFactory.IdentifierName(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken));
            return this.CreateMethod(container, methodName, syntax, (EEMethodSymbol method, DiagnosticBag diagnostics, out ImmutableArray<LocalSymbol> declaredLocals, out ResultProperties properties) =>
            {
                declaredLocals = ImmutableArray<LocalSymbol>.Empty;
                var type = method.TypeMap.SubstituteNamedType(typeVariablesType);
                var expression = new BoundObjectCreationExpression(syntax, type.InstanceConstructors[0], null);
                var statement = new BoundReturnStatement(syntax, RefKind.None, expression) { WasCompilerGenerated = true };
                properties = default(ResultProperties);
                return statement;
            });
        }

        private static BoundStatement BindExpression(Binder binder, ExpressionSyntax syntax, DiagnosticBag diagnostics, out ResultProperties resultProperties)
        {
            var flags = DkmClrCompilationResultFlags.None;

            // In addition to C# expressions, the native EE also supports
            // type names which are bound to a representation of the type
            // (but not System.Type) that the user can expand to see the
            // base type. Instead, we only allow valid C# expressions.
            var expression = IsDeconstruction(syntax)
                ? binder.BindDeconstruction((AssignmentExpressionSyntax)syntax, diagnostics, resultIsUsedOverride: true)
                : binder.BindRValueWithoutTargetType(syntax, diagnostics);
            if (diagnostics.HasAnyErrors())
            {
                resultProperties = default;
                return null;
            }

            try
            {
                if (MayHaveSideEffectsVisitor.MayHaveSideEffects(expression))
                {
                    flags |= DkmClrCompilationResultFlags.PotentialSideEffect;
                }
            }
            catch (BoundTreeVisitor.CancelledByStackGuardException ex)
            {
                ex.AddAnError(diagnostics);
                resultProperties = default(ResultProperties);
                return null;
            }

            var expressionType = expression.Type;
            if ((object)expressionType == null)
            {
                expression = binder.CreateReturnConversion(
                    syntax,
                    diagnostics,
                    expression,
                    RefKind.None,
                    binder.Compilation.GetSpecialType(SpecialType.System_Object));
                if (diagnostics.HasAnyErrors())
                {
                    resultProperties = default(ResultProperties);
                    return null;
                }
            }
            else if (expressionType.SpecialType == SpecialType.System_Void)
            {
                flags |= DkmClrCompilationResultFlags.ReadOnlyResult;
                Debug.Assert(expression.ConstantValue == null);
                resultProperties = expression.ExpressionSymbol.GetResultProperties(flags, isConstant: false);
                return new BoundExpressionStatement(syntax, expression) { WasCompilerGenerated = true };
            }
            else if (expressionType.SpecialType == SpecialType.System_Boolean)
            {
                flags |= DkmClrCompilationResultFlags.BoolResult;
            }

            if (!IsAssignableExpression(binder, expression))
            {
                flags |= DkmClrCompilationResultFlags.ReadOnlyResult;
            }

            resultProperties = expression.ExpressionSymbol.GetResultProperties(flags, expression.ConstantValue != null);
            return new BoundReturnStatement(syntax, RefKind.None, expression) { WasCompilerGenerated = true };
        }

        private static bool IsDeconstruction(ExpressionSyntax syntax)
        {
            if (syntax.Kind() != SyntaxKind.SimpleAssignmentExpression)
            {
                return false;
            }

            var node = (AssignmentExpressionSyntax)syntax;
            return node.Left.Kind() == SyntaxKind.TupleExpression || node.Left.Kind() == SyntaxKind.DeclarationExpression;
        }

        private static BoundStatement BindStatement(Binder binder, StatementSyntax syntax, DiagnosticBag diagnostics, out ResultProperties properties)
        {
            properties = new ResultProperties(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            return binder.BindStatement(syntax, diagnostics);
        }

        private static bool IsAssignableExpression(Binder binder, BoundExpression expression)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var result = binder.CheckValueKind(expression.Syntax, expression, Binder.BindValueKind.Assignable, checkingReceiver: false, diagnostics: diagnostics);
            diagnostics.Free();
            return result;
        }

        private static BoundStatement BindAssignment(Binder binder, ExpressionSyntax syntax, DiagnosticBag diagnostics)
        {
            var expression = binder.BindValue(syntax, diagnostics, Binder.BindValueKind.RValue);
            if (diagnostics.HasAnyErrors())
            {
                return null;
            }

            return new BoundExpressionStatement(expression.Syntax, expression) { WasCompilerGenerated = true };
        }

        private static Binder CreateBinderChain(
            CSharpCompilation compilation,
            PEModuleSymbol module,
            NamespaceSymbol @namespace,
            ImmutableArray<ImmutableArray<ImportRecord>> importRecordGroups)
        {
            var stack = ArrayBuilder<string>.GetInstance();
            while ((object)@namespace != null)
            {
                stack.Push(@namespace.Name);
                @namespace = @namespace.ContainingNamespace;
            }

            Binder binder = new BuckStopsHereBinder(compilation);
            var hasImports = !importRecordGroups.IsDefaultOrEmpty;
            var numImportStringGroups = hasImports ? importRecordGroups.Length : 0;
            var currentStringGroup = numImportStringGroups - 1;

            // PERF: We used to call compilation.GetCompilationNamespace on every iteration,
            // but that involved walking up to the global namespace, which we have to do
            // anyway.  Instead, we'll inline the functionality into our own walk of the
            // namespace chain.
            @namespace = compilation.GlobalNamespace;

            while (stack.Count > 0)
            {
                var namespaceName = stack.Pop();
                if (namespaceName.Length > 0)
                {
                    // We're re-getting the namespace, rather than using the one containing
                    // the current frame method, because we want the merged namespace.
                    @namespace = @namespace.GetNestedNamespace(namespaceName);
                    Debug.Assert((object)@namespace != null,
                        $"We worked backwards from symbols to names, but no symbol exists for name '{namespaceName}'");
                }
                else
                {
                    Debug.Assert((object)@namespace == (object)compilation.GlobalNamespace);
                }

                Imports imports = null;
                if (hasImports)
                {
                    if (currentStringGroup < 0)
                    {
                        Debug.WriteLine($"No import string group for namespace '{@namespace}'");
                        break;
                    }

                    var importsBinder = new InContainerBinder(@namespace, binder);
                    imports = BuildImports(compilation, module, importRecordGroups[currentStringGroup], importsBinder);
                    currentStringGroup--;
                }

                binder = new InContainerBinder(@namespace, binder, imports);
            }

            stack.Free();

            if (currentStringGroup >= 0)
            {
                // CONSIDER: We could lump these into the outermost namespace.  It's probably not worthwhile since
                // the usings are already for the wrong method.
                Debug.WriteLine($"Found {currentStringGroup + 1} import string groups without corresponding namespaces");
            }

            return binder;
        }

        private static CSharpCompilation GetCompilationWithExternAliases(CSharpCompilation compilation, ImmutableArray<ExternAliasRecord> externAliasRecords)
        {
            if (externAliasRecords.IsDefaultOrEmpty)
            {
                return compilation.Clone();
            }

            var updatedReferences = ArrayBuilder<MetadataReference>.GetInstance();
            var assembliesAndModulesBuilder = ArrayBuilder<Symbol>.GetInstance();
            foreach (var reference in compilation.References)
            {
                updatedReferences.Add(reference);
                assembliesAndModulesBuilder.Add(compilation.GetAssemblyOrModuleSymbol(reference));
            }
            Debug.Assert(assembliesAndModulesBuilder.Count == updatedReferences.Count);

            var assembliesAndModules = assembliesAndModulesBuilder.ToImmutableAndFree();

            foreach (var externAliasRecord in externAliasRecords)
            {
                var targetAssembly = externAliasRecord.TargetAssembly as AssemblySymbol;
                int index;
                if (targetAssembly != null)
                {
                    index = assembliesAndModules.IndexOf(targetAssembly);
                }
                else
                {
                    index = IndexOfMatchingAssembly((AssemblyIdentity)externAliasRecord.TargetAssembly, assembliesAndModules, compilation.Options.AssemblyIdentityComparer);
                }

                if (index < 0)
                {
                    Debug.WriteLine($"Unable to find corresponding assembly reference for extern alias '{externAliasRecord}'");
                    continue;
                }

                var externAlias = externAliasRecord.Alias;

                var assemblyReference = updatedReferences[index];
                var oldAliases = assemblyReference.Properties.Aliases;
                var newAliases = oldAliases.IsEmpty
                    ? ImmutableArray.Create(MetadataReferenceProperties.GlobalAlias, externAlias)
                    : oldAliases.Concat(ImmutableArray.Create(externAlias));

                // NOTE: Dev12 didn't emit custom debug info about "global", so we don't have
                // a good way to distinguish between a module aliased with both (e.g.) "X" and 
                // "global" and a module aliased with only "X".  As in Dev12, we assume that 
                // "global" is a valid alias to remain at least as permissive as source.
                // NOTE: In the event that this introduces ambiguities between two assemblies
                // (e.g. because one was "global" in source and the other was "X"), it should be
                // possible to disambiguate as long as each assembly has a distinct extern alias,
                // not necessarily used in source.
                Debug.Assert(newAliases.Contains(MetadataReferenceProperties.GlobalAlias));

                // Replace the value in the map with the updated reference.
                updatedReferences[index] = assemblyReference.WithAliases(newAliases);
            }

            compilation = compilation.WithReferences(updatedReferences);

            updatedReferences.Free();

            return compilation;
        }

        private static int IndexOfMatchingAssembly(AssemblyIdentity referenceIdentity, ImmutableArray<Symbol> assembliesAndModules, AssemblyIdentityComparer assemblyIdentityComparer)
        {
            for (int i = 0; i < assembliesAndModules.Length; i++)
            {
                if (assembliesAndModules[i] is AssemblySymbol assembly && assemblyIdentityComparer.ReferenceMatchesDefinition(referenceIdentity, assembly.Identity))
                {
                    return i;
                }
            }

            return -1;
        }

        private static Binder ExtendBinderChain(
            CSharpSyntaxNode syntax,
            ImmutableArray<Alias> aliases,
            EEMethodSymbol method,
            Binder binder,
            bool hasDisplayClassThis,
            bool methodNotType,
            out ImmutableArray<LocalSymbol> declaredLocals)
        {
            var substitutedSourceMethod = GetSubstitutedSourceMethod(method.SubstitutedSourceMethod, hasDisplayClassThis);
            var substitutedSourceType = substitutedSourceMethod.ContainingType;

            var stack = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            for (var type = substitutedSourceType; (object)type != null; type = type.ContainingType)
            {
                stack.Add(type);
            }

            while (stack.Count > 0)
            {
                substitutedSourceType = stack.Pop();

                binder = new InContainerBinder(substitutedSourceType, binder);
                if (substitutedSourceType.Arity > 0)
                {
                    binder = new WithTypeArgumentsBinder(substitutedSourceType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics, binder);
                }
            }

            stack.Free();

            if (substitutedSourceMethod.Arity > 0)
            {
                binder = new WithTypeArgumentsBinder(substitutedSourceMethod.TypeArgumentsWithAnnotations, binder);
            }

            // Method locals and parameters shadow pseudo-variables.
            // That is why we place PlaceholderLocalBinder and ExecutableCodeBinder before EEMethodBinder.
            if (methodNotType)
            {
                var typeNameDecoder = new EETypeNameDecoder(binder.Compilation, (PEModuleSymbol)substitutedSourceMethod.ContainingModule);
                binder = new PlaceholderLocalBinder(
                    syntax,
                    aliases,
                    method,
                    typeNameDecoder,
                    binder);
            }

            binder = new EEMethodBinder(method, substitutedSourceMethod, binder);

            if (methodNotType)
            {
                binder = new SimpleLocalScopeBinder(method.LocalsForBinding, binder);
            }

            Binder actualRootBinder = null;
            SyntaxNode declaredLocalsScopeDesignator = null;

            var executableBinder = new ExecutableCodeBinder(syntax, substitutedSourceMethod, binder,
                                              (rootBinder, declaredLocalsScopeDesignatorOpt) =>
                                              {
                                                  actualRootBinder = rootBinder;
                                                  declaredLocalsScopeDesignator = declaredLocalsScopeDesignatorOpt;
                                              });

            // We just need to trigger the process of building the binder map
            // so that the lambda above was executed.
            executableBinder.GetBinder(syntax);

            Debug.Assert(actualRootBinder != null);

            if (declaredLocalsScopeDesignator != null)
            {
                declaredLocals = actualRootBinder.GetDeclaredLocalsForScope(declaredLocalsScopeDesignator);
            }
            else
            {
                declaredLocals = ImmutableArray<LocalSymbol>.Empty;
            }

            return actualRootBinder;
        }

        private static Imports BuildImports(CSharpCompilation compilation, PEModuleSymbol module, ImmutableArray<ImportRecord> importRecords, InContainerBinder binder)
        {
            // We make a first pass to extract all of the extern aliases because other imports may depend on them.
            var externsBuilder = ArrayBuilder<AliasAndExternAliasDirective>.GetInstance();
            foreach (var importRecord in importRecords)
            {
                if (importRecord.TargetKind != ImportTargetKind.Assembly)
                {
                    continue;
                }

                var alias = importRecord.Alias;
                IdentifierNameSyntax aliasNameSyntax;
                if (!TryParseIdentifierNameSyntax(alias, out aliasNameSyntax))
                {
                    Debug.WriteLine($"Import record '{importRecord}' has syntactically invalid extern alias '{alias}'");
                    continue;
                }

                var externAliasSyntax = SyntaxFactory.ExternAliasDirective(aliasNameSyntax.Identifier);
                var aliasSymbol = new AliasSymbol(binder, externAliasSyntax); // Binder is only used to access compilation.
                externsBuilder.Add(new AliasAndExternAliasDirective(aliasSymbol, externAliasDirective: null)); // We have one, but we pass null for consistency.
            }

            var externs = externsBuilder.ToImmutableAndFree();

            if (externs.Any())
            {
                // NB: This binder (and corresponding Imports) is only used to bind the other imports.
                // We'll merge the externs into a final Imports object and return that to be used in
                // the actual binder chain.
                binder = new InContainerBinder(
                    binder.Container,
                    binder,
                    Imports.FromCustomDebugInfo(binder.Compilation, ImmutableDictionary<string, AliasAndUsingDirective>.Empty, ImmutableArray<NamespaceOrTypeAndUsingDirective>.Empty, externs));
            }

            var usingAliases = ImmutableDictionary.CreateBuilder<string, AliasAndUsingDirective>();
            var usingsBuilder = ArrayBuilder<NamespaceOrTypeAndUsingDirective>.GetInstance();

            foreach (var importRecord in importRecords)
            {
                switch (importRecord.TargetKind)
                {
                    case ImportTargetKind.Type:
                        {
                            TypeSymbol typeSymbol = (TypeSymbol)importRecord.TargetType;
                            Debug.Assert((object)typeSymbol != null);

                            if (typeSymbol.IsErrorType())
                            {
                                // Type is unrecognized. The import may have been
                                // valid in the original source but unnecessary.
                                continue; // Don't add anything for this import.
                            }
                            else if (importRecord.Alias == null && !typeSymbol.IsStatic)
                            {
                                // Only static types can be directly imported.
                                continue;
                            }

                            if (!TryAddImport(importRecord.Alias, typeSymbol, usingsBuilder, usingAliases, binder, importRecord))
                            {
                                continue;
                            }

                            break;
                        }
                    case ImportTargetKind.Namespace:
                        {
                            var namespaceName = importRecord.TargetString;
                            NameSyntax targetSyntax;
                            if (!SyntaxHelpers.TryParseDottedName(namespaceName, out targetSyntax))
                            {
                                // DevDiv #999086: Some previous version of VS apparently generated type aliases as "UA{alias} T{alias-qualified type name}". 
                                // Neither Roslyn nor Dev12 parses such imports.  However, Roslyn discards them, rather than interpreting them as "UA{alias}"
                                // (which will rarely work and never be correct).
                                Debug.WriteLine($"Import record '{importRecord}' has syntactically invalid target '{importRecord.TargetString}'");
                                continue;
                            }

                            NamespaceSymbol globalNamespace;
                            AssemblySymbol targetAssembly = (AssemblySymbol)importRecord.TargetAssembly;

                            if (targetAssembly != null)
                            {
                                if (targetAssembly.IsMissing)
                                {
                                    Debug.WriteLine($"Import record '{importRecord}' has invalid assembly reference '{targetAssembly.Identity}'");
                                    continue;
                                }

                                globalNamespace = targetAssembly.GlobalNamespace;
                            }
                            else if (importRecord.TargetAssemblyAlias != null)
                            {
                                IdentifierNameSyntax externAliasSyntax = null;
                                if (!TryParseIdentifierNameSyntax(importRecord.TargetAssemblyAlias, out externAliasSyntax))
                                {
                                    Debug.WriteLine($"Import record '{importRecord}' has syntactically invalid extern alias '{importRecord.TargetAssemblyAlias}'");
                                    continue;
                                }

                                var unusedDiagnostics = DiagnosticBag.GetInstance();
                                var aliasSymbol = (AliasSymbol)binder.BindNamespaceAliasSymbol(externAliasSyntax, unusedDiagnostics);
                                unusedDiagnostics.Free();

                                if ((object)aliasSymbol == null)
                                {
                                    Debug.WriteLine($"Import record '{importRecord}' requires unknown extern alias '{importRecord.TargetAssemblyAlias}'");
                                    continue;
                                }

                                globalNamespace = (NamespaceSymbol)aliasSymbol.Target;
                            }
                            else
                            {
                                globalNamespace = compilation.GlobalNamespace;
                            }

                            var namespaceSymbol = BindNamespace(namespaceName, globalNamespace);

                            if ((object)namespaceSymbol == null)
                            {
                                // Namespace is unrecognized. The import may have been
                                // valid in the original source but unnecessary.
                                continue; // Don't add anything for this import.
                            }

                            if (!TryAddImport(importRecord.Alias, namespaceSymbol, usingsBuilder, usingAliases, binder, importRecord))
                            {
                                continue;
                            }

                            break;
                        }
                    case ImportTargetKind.Assembly:
                        {
                            // Handled in first pass (above).
                            break;
                        }
                    default:
                        {
                            throw ExceptionUtilities.UnexpectedValue(importRecord.TargetKind);
                        }
                }
            }

            return Imports.FromCustomDebugInfo(binder.Compilation, usingAliases.ToImmutableDictionary(), usingsBuilder.ToImmutableAndFree(), externs);
        }

        private static NamespaceSymbol BindNamespace(string namespaceName, NamespaceSymbol globalNamespace)
        {
            var namespaceSymbol = globalNamespace;
            foreach (var name in namespaceName.Split('.'))
            {
                var members = namespaceSymbol.GetMembers(name);
                namespaceSymbol = members.Length == 1
                        ? members[0] as NamespaceSymbol
                        : null;

                if ((object)namespaceSymbol == null)
                {
                    break;
                }
            }
            return namespaceSymbol;
        }

        private static bool TryAddImport(
            string alias,
            NamespaceOrTypeSymbol targetSymbol,
            ArrayBuilder<NamespaceOrTypeAndUsingDirective> usingsBuilder,
            ImmutableDictionary<string, AliasAndUsingDirective>.Builder usingAliases,
            InContainerBinder binder,
            ImportRecord importRecord)
        {
            if (alias == null)
            {
                usingsBuilder.Add(new NamespaceOrTypeAndUsingDirective(targetSymbol, usingDirective: null));
            }
            else
            {
                IdentifierNameSyntax aliasSyntax;
                if (!TryParseIdentifierNameSyntax(alias, out aliasSyntax))
                {
                    Debug.WriteLine($"Import record '{importRecord}' has syntactically invalid alias '{alias}'");
                    return false;
                }

                var aliasSymbol = AliasSymbol.CreateCustomDebugInfoAlias(targetSymbol, aliasSyntax.Identifier, binder);
                usingAliases.Add(alias, new AliasAndUsingDirective(aliasSymbol, usingDirective: null));
            }

            return true;
        }

        private static bool TryParseIdentifierNameSyntax(string name, out IdentifierNameSyntax syntax)
        {
            Debug.Assert(name != null);

            if (name == MetadataReferenceProperties.GlobalAlias)
            {
                syntax = SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
                return true;
            }

            NameSyntax nameSyntax;
            if (!SyntaxHelpers.TryParseDottedName(name, out nameSyntax) || nameSyntax.Kind() != SyntaxKind.IdentifierName)
            {
                syntax = null;
                return false;
            }

            syntax = (IdentifierNameSyntax)nameSyntax;
            return true;
        }

        internal CommonMessageProvider MessageProvider
        {
            get { return this.Compilation.MessageProvider; }
        }

        private static DkmClrCompilationResultFlags GetLocalResultFlags(LocalSymbol local)
        {
            // CONSIDER: We might want to prevent the user from modifying pinned locals -
            // that's pretty dangerous.
            return local.IsConst
                ? DkmClrCompilationResultFlags.ReadOnlyResult
                : DkmClrCompilationResultFlags.None;
        }

        /// <summary>
        /// Generate the set of locals to use for binding. 
        /// </summary>
        private static ImmutableArray<LocalSymbol> GetLocalsForBinding(
            ImmutableArray<LocalSymbol> locals,
            ImmutableArray<string> displayClassVariableNamesInOrder,
            ImmutableDictionary<string, DisplayClassVariable> displayClassVariables)
        {
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();
            foreach (var local in locals)
            {
                var name = local.Name;
                if (name == null)
                {
                    continue;
                }

                if (GeneratedNames.GetKind(name) != GeneratedNameKind.None)
                {
                    continue;
                }

                // Although Roslyn doesn't name synthesized locals unless they are well-known to EE,
                // Dev12 did so we need to skip them here.
                if (GeneratedNames.IsSynthesizedLocalName(name))
                {
                    continue;
                }

                builder.Add(local);
            }

            foreach (var variableName in displayClassVariableNamesInOrder)
            {
                var variable = displayClassVariables[variableName];
                switch (variable.Kind)
                {
                    case DisplayClassVariableKind.Local:
                    case DisplayClassVariableKind.Parameter:
                        builder.Add(new EEDisplayClassFieldLocalSymbol(variable));
                        break;
                }
            }

            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<string> GetSourceMethodParametersInOrder(
            MethodSymbol method,
            MethodSymbol sourceMethod)
        {
            var containingType = method.ContainingType;
            bool isIteratorOrAsyncMethod = IsDisplayClassType(containingType) &&
                GeneratedNames.GetKind(containingType.Name) == GeneratedNameKind.StateMachineType;

            var parameterNamesInOrder = ArrayBuilder<string>.GetInstance();
            // For version before .NET 4.5, we cannot find the sourceMethod properly:
            // The source method coincides with the original method in the case.
            // Therefore, for iterators and async state machines, we have to get parameters from the containingType.
            // This does not guarantee the proper order of parameters.
            if (isIteratorOrAsyncMethod && method == sourceMethod)
            {
                Debug.Assert(IsDisplayClassType(containingType));
                foreach (var member in containingType.GetMembers())
                {
                    if (member.Kind != SymbolKind.Field)
                    {
                        continue;
                    }

                    var field = (FieldSymbol)member;
                    var fieldName = field.Name;
                    if (GeneratedNames.GetKind(fieldName) == GeneratedNameKind.None)
                    {
                        parameterNamesInOrder.Add(fieldName);
                    }
                }
            }
            else
            {
                if (sourceMethod != null)
                {
                    foreach (var p in sourceMethod.Parameters)
                    {
                        parameterNamesInOrder.Add(p.Name);
                    }
                }
            }

            return parameterNamesInOrder.ToImmutableAndFree();
        }

        /// <summary>
        /// Return a mapping of captured variables (parameters, locals, and
        /// "this") to locals. The mapping is needed to expose the original
        /// local identifiers (those from source) in the binder.
        /// </summary>
        private static void GetDisplayClassVariables(
            MethodSymbol method,
            ImmutableArray<LocalSymbol> locals,
            ImmutableSortedSet<int> inScopeHoistedLocalSlots,
            ImmutableArray<string> parameterNamesInOrder,
            out ImmutableArray<string> displayClassVariableNamesInOrder,
            out ImmutableDictionary<string, DisplayClassVariable> displayClassVariables)
        {
            // Calculate the shortest paths from locals to instances of display
            // classes. There should not be two instances of the same display
            // class immediately within any particular method.
            var displayClassInstances = ArrayBuilder<DisplayClassInstanceAndFields>.GetInstance();

            foreach (var parameter in method.Parameters)
            {
                if (GeneratedNames.GetKind(parameter.Name) == GeneratedNameKind.TransparentIdentifier ||
                    IsDisplayClassParameter(parameter))
                {
                    var instance = new DisplayClassInstanceFromParameter(parameter);
                    displayClassInstances.Add(new DisplayClassInstanceAndFields(instance));
                }
            }

            if (IsDisplayClassType(method.ContainingType) && !method.IsStatic)
            {
                // Add "this" display class instance.
                var instance = new DisplayClassInstanceFromParameter(method.ThisParameter);
                displayClassInstances.Add(new DisplayClassInstanceAndFields(instance));
            }

            var displayClassTypes = PooledHashSet<TypeSymbol>.GetInstance();
            foreach (var instance in displayClassInstances)
            {
                displayClassTypes.Add(instance.Instance.Type);
            }

            // Find any additional display class instances.
            GetAdditionalDisplayClassInstances(displayClassTypes, displayClassInstances, startIndex: 0);

            // Add any display class instances from locals (these will contain any hoisted locals).
            // Locals are only added after finding all display class instances reachable from
            // parameters because locals may be null (temporary locals in async state machine
            // for instance) so we prefer parameters to locals.
            int startIndex = displayClassInstances.Count;
            foreach (var local in locals)
            {
                var name = local.Name;
                if ((name != null) && (GeneratedNames.GetKind(name) == GeneratedNameKind.DisplayClassLocalOrField))
                {
                    var localType = local.Type;
                    if ((object)localType != null && displayClassTypes.Add(localType))
                    {
                        var instance = new DisplayClassInstanceFromLocal((EELocalSymbol)local);
                        displayClassInstances.Add(new DisplayClassInstanceAndFields(instance));
                    }
                }
            }
            GetAdditionalDisplayClassInstances(displayClassTypes, displayClassInstances, startIndex);

            displayClassTypes.Free();

            if (displayClassInstances.Any())
            {
                var parameterNames = PooledHashSet<string>.GetInstance();
                foreach (var name in parameterNamesInOrder)
                {
                    parameterNames.Add(name);
                }

                // The locals are the set of all fields from the display classes.
                var displayClassVariableNamesInOrderBuilder = ArrayBuilder<string>.GetInstance();
                var displayClassVariablesBuilder = PooledDictionary<string, DisplayClassVariable>.GetInstance();

                foreach (var instance in displayClassInstances)
                {
                    GetDisplayClassVariables(
                        displayClassVariableNamesInOrderBuilder,
                        displayClassVariablesBuilder,
                        parameterNames,
                        inScopeHoistedLocalSlots,
                        instance);
                }

                displayClassVariableNamesInOrder = displayClassVariableNamesInOrderBuilder.ToImmutableAndFree();
                displayClassVariables = displayClassVariablesBuilder.ToImmutableDictionary();
                displayClassVariablesBuilder.Free();
                parameterNames.Free();
            }
            else
            {
                displayClassVariableNamesInOrder = ImmutableArray<string>.Empty;
                displayClassVariables = ImmutableDictionary<string, DisplayClassVariable>.Empty;
            }

            displayClassInstances.Free();
        }

        private static void GetAdditionalDisplayClassInstances(
            HashSet<TypeSymbol> displayClassTypes,
            ArrayBuilder<DisplayClassInstanceAndFields> displayClassInstances,
            int startIndex)
        {
            // Find any additional display class instances breadth first.
            for (int i = startIndex; i < displayClassInstances.Count; i++)
            {
                GetDisplayClassInstances(displayClassTypes, displayClassInstances, displayClassInstances[i]);
            }
        }

        private static void GetDisplayClassInstances(
            HashSet<TypeSymbol> displayClassTypes,
            ArrayBuilder<DisplayClassInstanceAndFields> displayClassInstances,
            DisplayClassInstanceAndFields instance)
        {
            // Display class instance. The display class fields are variables.
            foreach (var member in instance.Type.GetMembers())
            {
                if (member.Kind != SymbolKind.Field)
                {
                    continue;
                }

                var field = (FieldSymbol)member;
                var fieldType = field.Type;
                var fieldName = field.Name;
                TryParseGeneratedName(fieldName, out var fieldKind, out var part);

                switch (fieldKind)
                {
                    case GeneratedNameKind.DisplayClassLocalOrField:
                    case GeneratedNameKind.TransparentIdentifier:
                        break;
                    case GeneratedNameKind.AnonymousTypeField:
                        if (GeneratedNames.GetKind(part) != GeneratedNameKind.TransparentIdentifier)
                        {
                            continue;
                        }
                        break;
                    case GeneratedNameKind.ThisProxyField:
                        if (GeneratedNames.GetKind(fieldType.Name) != GeneratedNameKind.LambdaDisplayClass)
                        {
                            continue;
                        }
                        // Async lambda case.
                        break;
                    default:
                        continue;
                }

                Debug.Assert(!field.IsStatic);

                // A hoisted local that is itself a display class instance.
                if (displayClassTypes.Add(fieldType))
                {
                    var other = instance.FromField(field);
                    displayClassInstances.Add(other);
                }
            }
        }

        /// <summary>
        /// Returns true if the parameter is a synthesized parameter representing
        /// a display class instance (used to pass hoisted symbols to local functions).
        /// </summary>
        private static bool IsDisplayClassParameter(ParameterSymbol parameter)
        {
            var type = parameter.Type;
            var result = type.Kind == SymbolKind.NamedType && IsDisplayClassType((NamedTypeSymbol)type);
            Debug.Assert(!result || parameter.MetadataName == "");
            return result;
        }

        private static void GetDisplayClassVariables(
            ArrayBuilder<string> displayClassVariableNamesInOrderBuilder,
            Dictionary<string, DisplayClassVariable> displayClassVariablesBuilder,
            HashSet<string> parameterNames,
            ImmutableSortedSet<int> inScopeHoistedLocalSlots,
            DisplayClassInstanceAndFields instance)
        {
            // Display class instance. The display class fields are variables.
            foreach (var member in instance.Type.GetMembers())
            {
                if (member.Kind != SymbolKind.Field)
                {
                    continue;
                }

                var field = (FieldSymbol)member;
                var fieldName = field.Name;

REPARSE:

                DisplayClassVariableKind variableKind;
                string variableName;
                TryParseGeneratedName(fieldName, out var fieldKind, out var part);

                switch (fieldKind)
                {
                    case GeneratedNameKind.AnonymousTypeField:
                        Debug.Assert(fieldName == field.Name); // This only happens once.
                        fieldName = part;
                        goto REPARSE;
                    case GeneratedNameKind.TransparentIdentifier:
                        // A transparent identifier (field) in an anonymous type synthesized for a transparent identifier.
                        Debug.Assert(!field.IsStatic);
                        continue;
                    case GeneratedNameKind.DisplayClassLocalOrField:
                        // A local that is itself a display class instance.
                        Debug.Assert(!field.IsStatic);
                        continue;
                    case GeneratedNameKind.HoistedLocalField:
                        // Filter out hoisted locals that are known to be out-of-scope at the current IL offset.
                        // Hoisted locals with invalid indices will be included since more information is better
                        // than less in error scenarios.
                        if (GeneratedNames.TryParseSlotIndex(fieldName, out int slotIndex) &&
                            !inScopeHoistedLocalSlots.Contains(slotIndex))
                        {
                            continue;
                        }

                        variableName = part;
                        variableKind = DisplayClassVariableKind.Local;
                        Debug.Assert(!field.IsStatic);
                        break;
                    case GeneratedNameKind.ThisProxyField:
                        // A reference to "this".
                        variableName = ""; // Should not be referenced by name.
                        variableKind = DisplayClassVariableKind.This;
                        Debug.Assert(!field.IsStatic);
                        break;
                    case GeneratedNameKind.None:
                        // A reference to a parameter or local.
                        variableName = fieldName;
                        if (parameterNames.Contains(variableName))
                        {
                            variableKind = DisplayClassVariableKind.Parameter;
                        }
                        else
                        {
                            variableKind = DisplayClassVariableKind.Local;
                        }
                        Debug.Assert(!field.IsStatic);
                        break;
                    default:
                        continue;
                }

                if (displayClassVariablesBuilder.ContainsKey(variableName))
                {
                    // Only expecting duplicates for async state machine
                    // fields (that should be at the top-level).
                    Debug.Assert(displayClassVariablesBuilder[variableName].DisplayClassFields.Count() == 1);

                    if (!instance.Fields.Any())
                    {
                        // Prefer parameters over locals.
                        Debug.Assert(instance.Instance is DisplayClassInstanceFromLocal);
                    }
                    else
                    {
                        Debug.Assert(instance.Fields.Count() >= 1); // greater depth
                        Debug.Assert((variableKind == DisplayClassVariableKind.Parameter) ||
                            (variableKind == DisplayClassVariableKind.This));

                        if (variableKind == DisplayClassVariableKind.Parameter && GeneratedNames.GetKind(instance.Type.Name) == GeneratedNameKind.LambdaDisplayClass)
                        {
                            displayClassVariablesBuilder[variableName] = instance.ToVariable(variableName, variableKind, field);
                        }
                    }
                }
                else if (variableKind != DisplayClassVariableKind.This || GeneratedNames.GetKind(instance.Type.ContainingType.Name) != GeneratedNameKind.LambdaDisplayClass)
                {
                    // In async lambdas, the hoisted "this" field in the state machine type will point to the display class instance, if there is one.
                    // In such cases, we want to add the display class "this" to the map instead (or nothing, if it lacks one).
                    displayClassVariableNamesInOrderBuilder.Add(variableName);
                    displayClassVariablesBuilder.Add(variableName, instance.ToVariable(variableName, variableKind, field));
                }
            }
        }

        private static bool TryParseGeneratedName(string name, out GeneratedNameKind kind, out string part)
        {
            bool result = GeneratedNames.TryParseGeneratedName(name, out kind, out int openBracketOffset, out int closeBracketOffset);
            switch (kind)
            {
                case GeneratedNameKind.AnonymousTypeField:
                case GeneratedNameKind.HoistedLocalField:
                    part = name.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                    break;
                default:
                    part = null;
                    break;
            }
            return result;
        }

        private static bool IsDisplayClassType(TypeSymbol type)
        {
            switch (GeneratedNames.GetKind(type.Name))
            {
                case GeneratedNameKind.LambdaDisplayClass:
                case GeneratedNameKind.StateMachineType:
                    return true;
                default:
                    return false;
            }
        }

        internal static DisplayClassVariable GetThisProxy(ImmutableDictionary<string, DisplayClassVariable> displayClassVariables)
        {
            return displayClassVariables.Values.FirstOrDefault(v => v.Kind == DisplayClassVariableKind.This);
        }

        private static NamedTypeSymbol GetNonDisplayClassContainer(NamedTypeSymbol type)
        {
            // 1) Display class and state machine types are always nested within the types
            //    that use them (so that they can access private members of those types).
            // 2) The native compiler used to produce nested display classes for nested lambdas,
            //    so we may have to walk out more than one level.
            while (IsDisplayClassType(type))
            {
                type = type.ContainingType;
            }
            Debug.Assert((object)type != null);

            return type;
        }

        /// <summary>
        /// Identifies the method in which binding should occur.
        /// </summary>
        /// <param name="candidateSubstitutedSourceMethod">
        /// The symbol of the method that is currently on top of the callstack, with
        /// EE type parameters substituted in place of the original type parameters.
        /// </param>
        /// <param name="sourceMethodMustBeInstance">
        /// True if "this" is available via a display class in the current context.
        /// </param>
        /// <returns>
        /// If <paramref name="candidateSubstitutedSourceMethod"/> is compiler-generated,
        /// then we will attempt to determine which user-defined method caused it to be
        /// generated.  For example, if <paramref name="candidateSubstitutedSourceMethod"/>
        /// is a state machine MoveNext method, then we will try to find the iterator or
        /// async method for which it was generated.  If we are able to find the original
        /// method, then we will substitute in the EE type parameters.  Otherwise, we will
        /// return <paramref name="candidateSubstitutedSourceMethod"/>.
        /// </returns>
        /// <remarks>
        /// In the event that the original method is overloaded, we may not be able to determine
        /// which overload actually corresponds to the state machine.  In particular, we do not
        /// have information about the signature of the original method (i.e. number of parameters,
        /// parameter types and ref-kinds, return type).  However, we conjecture that this
        /// level of uncertainty is acceptable, since parameters are managed by a separate binder
        /// in the synthesized binder chain and we have enough information to check the other method
        /// properties that are used during binding (e.g. static-ness, generic arity, type parameter
        /// constraints).
        /// </remarks>
        internal static MethodSymbol GetSubstitutedSourceMethod(
            MethodSymbol candidateSubstitutedSourceMethod,
            bool sourceMethodMustBeInstance)
        {
            var candidateSubstitutedSourceType = candidateSubstitutedSourceMethod.ContainingType;

            string desiredMethodName;
            if (GeneratedNames.TryParseSourceMethodNameFromGeneratedName(candidateSubstitutedSourceType.Name, GeneratedNameKind.StateMachineType, out desiredMethodName) ||
                GeneratedNames.TryParseSourceMethodNameFromGeneratedName(candidateSubstitutedSourceMethod.Name, GeneratedNameKind.LambdaMethod, out desiredMethodName) ||
                GeneratedNames.TryParseSourceMethodNameFromGeneratedName(candidateSubstitutedSourceMethod.Name, GeneratedNameKind.LocalFunction, out desiredMethodName))
            {
                // We could be in the MoveNext method of an async lambda.
                string tempMethodName;
                if (GeneratedNames.TryParseSourceMethodNameFromGeneratedName(desiredMethodName, GeneratedNameKind.LambdaMethod, out tempMethodName) ||
                    GeneratedNames.TryParseSourceMethodNameFromGeneratedName(desiredMethodName, GeneratedNameKind.LocalFunction, out tempMethodName))
                {
                    desiredMethodName = tempMethodName;
                    var containing = candidateSubstitutedSourceType.ContainingType;
                    Debug.Assert((object)containing != null);
                    if (GeneratedNames.GetKind(containing.Name) == GeneratedNameKind.LambdaDisplayClass)
                    {
                        candidateSubstitutedSourceType = containing;
                        sourceMethodMustBeInstance = candidateSubstitutedSourceType.MemberNames.Select(GeneratedNames.GetKind).Contains(GeneratedNameKind.ThisProxyField);
                    }
                }

                var desiredTypeParameters = candidateSubstitutedSourceType.OriginalDefinition.TypeParameters;

                // Type containing the original iterator, async, or lambda-containing method.
                var substitutedSourceType = GetNonDisplayClassContainer(candidateSubstitutedSourceType);

                foreach (var candidateMethod in substitutedSourceType.GetMembers().OfType<MethodSymbol>())
                {
                    if (IsViableSourceMethod(candidateMethod, desiredMethodName, desiredTypeParameters, sourceMethodMustBeInstance))
                    {
                        return desiredTypeParameters.Length == 0
                            ? candidateMethod
                            : candidateMethod.Construct(candidateSubstitutedSourceType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);
                    }
                }

                Debug.Assert(false, "Why didn't we find a substituted source method for " + candidateSubstitutedSourceMethod + "?");
            }

            return candidateSubstitutedSourceMethod;
        }

        private static bool IsViableSourceMethod(
            MethodSymbol candidateMethod,
            string desiredMethodName, ImmutableArray<TypeParameterSymbol> desiredTypeParameters, bool desiredMethodMustBeInstance)
        {
            return
candidateMethod is { IsAbstract: false, Name: desiredMethodName } && (!(desiredMethodMustBeInstance && candidateMethod.IsStatic)) && HaveSameConstraints(candidateMethod.TypeParameters, desiredTypeParameters);
        }

        private static bool HaveSameConstraints(ImmutableArray<TypeParameterSymbol> candidateTypeParameters, ImmutableArray<TypeParameterSymbol> desiredTypeParameters)
        {
            int arity = candidateTypeParameters.Length;
            if (arity != desiredTypeParameters.Length)
            {
                return false;
            }
            else if (arity == 0)
            {
                return true;
            }

            var indexedTypeParameters = IndexedTypeParameterSymbol.Take(arity);
            var candidateTypeMap = new TypeMap(candidateTypeParameters, indexedTypeParameters, allowAlpha: true);
            var desiredTypeMap = new TypeMap(desiredTypeParameters, indexedTypeParameters, allowAlpha: true);

            return MemberSignatureComparer.HaveSameConstraints(candidateTypeParameters, candidateTypeMap, desiredTypeParameters, desiredTypeMap);
        }

        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        private struct DisplayClassInstanceAndFields
        {
            internal readonly DisplayClassInstance Instance;
            internal readonly ConsList<FieldSymbol> Fields;

            internal DisplayClassInstanceAndFields(DisplayClassInstance instance) :
                this(instance, ConsList<FieldSymbol>.Empty)
            {
                Debug.Assert(IsDisplayClassType(instance.Type) ||
                    GeneratedNames.GetKind(instance.Type.Name) == GeneratedNameKind.AnonymousType);
            }

            private DisplayClassInstanceAndFields(DisplayClassInstance instance, ConsList<FieldSymbol> fields)
            {
                this.Instance = instance;
                this.Fields = fields;
            }

            internal TypeSymbol Type
            {
                get { return this.Fields.Any() ? this.Fields.Head.Type : this.Instance.Type; }
            }

            internal int Depth
            {
                get { return this.Fields.Count(); }
            }

            internal DisplayClassInstanceAndFields FromField(FieldSymbol field)
            {
                Debug.Assert(IsDisplayClassType(field.Type) ||
                    GeneratedNames.GetKind(field.Type.Name) == GeneratedNameKind.AnonymousType);
                return new DisplayClassInstanceAndFields(this.Instance, this.Fields.Prepend(field));
            }

            internal DisplayClassVariable ToVariable(string name, DisplayClassVariableKind kind, FieldSymbol field)
            {
                return new DisplayClassVariable(name, kind, this.Instance, this.Fields.Prepend(field));
            }

            private string GetDebuggerDisplay()
            {
                return Instance.GetDebuggerDisplay(Fields);
            }
        }
    }
}
