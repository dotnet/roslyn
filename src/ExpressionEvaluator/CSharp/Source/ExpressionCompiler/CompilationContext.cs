// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
        private readonly ImmutableHashSet<string> _hoistedParameterNames;
        private readonly ImmutableArray<LocalSymbol> _localsForBinding;
        private readonly CSharpSyntaxNode _syntax;
        private readonly bool _methodNotType;

        /// <summary>
        /// Create a context to compile expressions within a method scope.
        /// </summary>
        internal CompilationContext(
            CSharpCompilation compilation,
            MethodSymbol currentFrame,
            ImmutableArray<LocalSymbol> locals,
            InScopeHoistedLocals inScopeHoistedLocals,
            MethodDebugInfo<TypeSymbol, LocalSymbol> methodDebugInfo,
            CSharpSyntaxNode syntax)
        {
            Debug.Assert((syntax == null) || (syntax is ExpressionSyntax) || (syntax is LocalDeclarationStatementSyntax));

            // TODO: syntax.SyntaxTree should probably be added to the compilation,
            // but it isn't rooted by a CompilationUnitSyntax so it doesn't work (yet).
            _currentFrame = currentFrame;
            _syntax = syntax;
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
                ImmutableArray<string> displayClassVariableNamesInOrder;
                GetDisplayClassVariables(
                    currentFrame,
                    _locals,
                    inScopeHoistedLocals,
                    out displayClassVariableNamesInOrder,
                    out _displayClassVariables,
                    out _hoistedParameterNames);
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
                _displayClassVariables.ContainsKey(GeneratedNames.ThisProxyFieldName()) ==
                _displayClassVariables.Values.Any(v => v.Kind == DisplayClassVariableKind.This));
        }

        internal CommonPEModuleBuilder CompileExpression(
            string typeName,
            string methodName,
            ImmutableArray<Alias> aliases,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties)
        {
            var properties = default(ResultProperties);
            var objectType = this.Compilation.GetSpecialType(SpecialType.System_Object);
            var synthesizedType = new EENamedTypeSymbol(
                this.Compilation.SourceModule.GlobalNamespace,
                objectType,
                _syntax,
                _currentFrame,
                typeName,
                methodName,
                this,
                (method, diags) =>
                {
                    var hasDisplayClassThis = _displayClassVariables.ContainsKey(GeneratedNames.ThisProxyFieldName());
                    var binder = ExtendBinderChain(
                        _syntax,
                        aliases,
                        method,
                        this.NamespaceBinder,
                        hasDisplayClassThis,
                        _methodNotType);
                    var statementSyntax = _syntax as StatementSyntax;
                    return (statementSyntax == null) ?
                        BindExpression(binder, (ExpressionSyntax)_syntax, diags, out properties) :
                        BindStatement(binder, statementSyntax, diags, out properties);
                });

            var module = CreateModuleBuilder(
                this.Compilation,
                synthesizedType.Methods,
                additionalTypes: ImmutableArray.Create((NamedTypeSymbol)synthesizedType),
                synthesizedType: synthesizedType,
                testData: testData,
                diagnostics: diagnostics);

            Debug.Assert(module != null);

            this.Compilation.Compile(
                module,
                win32Resources: null,
                xmlDocStream: null,
                emittingPdb: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: CancellationToken.None);

            if (diagnostics.HasAnyErrors())
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            // Should be no name mangling since the caller provided explicit names.
            Debug.Assert(synthesizedType.MetadataName == typeName);
            Debug.Assert(synthesizedType.GetMembers()[0].MetadataName == methodName);

            resultProperties = properties;
            return module;
        }

        internal CommonPEModuleBuilder CompileAssignment(
            string typeName,
            string methodName,
            ImmutableArray<Alias> aliases,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData,
            DiagnosticBag diagnostics,
            out ResultProperties resultProperties)
        {
            var objectType = this.Compilation.GetSpecialType(SpecialType.System_Object);
            var synthesizedType = new EENamedTypeSymbol(
                Compilation.SourceModule.GlobalNamespace,
                objectType,
                _syntax,
                _currentFrame,
                typeName,
                methodName,
                this,
                (method, diags) =>
                {
                    var hasDisplayClassThis = _displayClassVariables.ContainsKey(GeneratedNames.ThisProxyFieldName());
                    var binder = ExtendBinderChain(
                        _syntax,
                        aliases,
                        method,
                        this.NamespaceBinder,
                        hasDisplayClassThis,
                        methodNotType: true);
                    return BindAssignment(binder, (ExpressionSyntax)_syntax, diags);
                });

            var module = CreateModuleBuilder(
                this.Compilation,
                synthesizedType.Methods,
                additionalTypes: ImmutableArray.Create((NamedTypeSymbol)synthesizedType),
                synthesizedType: synthesizedType,
                testData: testData,
                diagnostics: diagnostics);

            Debug.Assert(module != null);

            this.Compilation.Compile(
                module,
                win32Resources: null,
                xmlDocStream: null,
                emittingPdb: false,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: CancellationToken.None);

            if (diagnostics.HasAnyErrors())
            {
                resultProperties = default(ResultProperties);
                return null;
            }

            // Should be no name mangling since the caller provided explicit names.
            Debug.Assert(synthesizedType.MetadataName == typeName);
            Debug.Assert(synthesizedType.GetMembers()[0].MetadataName == methodName);

            resultProperties = new ResultProperties(DkmClrCompilationResultFlags.PotentialSideEffect);
            return module;
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
                    _syntax,
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
                _syntax,
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
                                var methodName = GetNextMethodName(methodBuilder);
                                var syntax = SyntaxFactory.IdentifierName(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken));
                                var aliasMethod = this.CreateMethod(container, methodName, syntax, (method, diags) =>
                                {
                                    var expression = new BoundLocal(syntax, local, constantValueOpt: null, type: local.Type.TypeSymbol);
                                    return new BoundReturnStatement(syntax, expression) { WasCompilerGenerated = true };
                                });
                                var flags = local.IsWritable ? DkmClrCompilationResultFlags.None : DkmClrCompilationResultFlags.ReadOnlyResult;
                                localBuilder.Add(MakeLocalAndMethod(local, aliasMethod, flags));
                                methodBuilder.Add(aliasMethod);
                            }
                        }

                        // "this" for non-static methods that are not display class methods or
                        // display class methods where the display class contains "<>4__this".
                        if (!m.IsStatic && (!IsDisplayClassType(m.ContainingType) || _displayClassVariables.ContainsKey(GeneratedNames.ThisProxyFieldName())))
                        {
                            var methodName = GetNextMethodName(methodBuilder);
                            var method = this.GetThisMethod(container, methodName);
                            localBuilder.Add(new CSharpLocalAndMethod("this", "this", method, DkmClrCompilationResultFlags.None)); // Note: writable in dev11.
                            methodBuilder.Add(method);
                        }
                    }

                    // Hoisted method parameters (represented as locals in the EE).
                    if (!_hoistedParameterNames.IsEmpty)
                    {
                        int localIndex = 0;
                        foreach (var local in _localsForBinding)
                        {
                            // Since we are showing hoisted method parameters first, the parameters may appear out of order
                            // in the Locals window if only some of the parameters are hoisted.  This is consistent with the
                            // behavior of the old EE.
                            if (_hoistedParameterNames.Contains(local.Name))
                            {
                                AppendLocalAndMethod(localBuilder, methodBuilder, local, container, localIndex, GetLocalResultFlags(local));
                            }

                            localIndex++;
                        }
                    }

                    // Method parameters (except those that have been hoisted).
                    int parameterIndex = m.IsStatic ? 0 : 1;
                    foreach (var parameter in m.Parameters)
                    {
                        var parameterName = parameter.Name;
                        if (!_hoistedParameterNames.Contains(parameterName) && GeneratedNames.GetKind(parameterName) == GeneratedNameKind.None)
                        {
                            AppendParameterAndMethod(localBuilder, methodBuilder, parameter, container, parameterIndex);
                        }

                        parameterIndex++;
                    }

                    if (!argumentsOnly)
                    {
                        // Locals.
                        int localIndex = 0;
                        foreach (var local in _localsForBinding)
                        {
                            if (!_hoistedParameterNames.Contains(local.Name))
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

                    return methodBuilder.ToImmutableAndFree();
                });

            additionalTypes.Add(synthesizedType);

            var module = CreateModuleBuilder(
                this.Compilation,
                synthesizedType.Methods,
                additionalTypes: additionalTypes.ToImmutableAndFree(),
                synthesizedType: synthesizedType,
                testData: testData,
                diagnostics: diagnostics);

            Debug.Assert(module != null);

            this.Compilation.Compile(
                module,
                win32Resources: null,
                xmlDocStream: null,
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
            ImmutableArray<MethodSymbol> methods,
            ImmutableArray<NamedTypeSymbol> additionalTypes,
            EENamedTypeSymbol synthesizedType,
            Microsoft.CodeAnalysis.CodeGen.CompilationTestData testData,
            DiagnosticBag diagnostics)
        {
            // Each assembly must have a unique name.
            var emitOptions = new EmitOptions(outputNameOverride: ExpressionCompilerUtilities.GenerateUniqueName());

            var dynamicOperationContextType = GetNonDisplayClassContainer(synthesizedType.SubstitutedSourceType);

            string runtimeMetadataVersion = compilation.GetRuntimeMetadataVersion(emitOptions, diagnostics);
            var serializationProperties = compilation.ConstructModuleSerializationProperties(emitOptions, runtimeMetadataVersion);
            return new EEAssemblyBuilder(compilation.SourceAssembly, emitOptions, methods, serializationProperties, additionalTypes, dynamicOperationContextType, testData);
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
            return this.CreateMethod(container, methodName, syntax, (method, diagnostics) =>
            {
                var local = method.LocalsForBinding[localIndex];
                var expression = new BoundLocal(syntax, local, constantValueOpt: local.GetConstantValue(null, null, diagnostics), type: local.Type.TypeSymbol);
                return new BoundReturnStatement(syntax, expression) { WasCompilerGenerated = true };
            });
        }

        private EEMethodSymbol GetParameterMethod(EENamedTypeSymbol container, string methodName, string parameterName, int parameterIndex)
        {
            var syntax = SyntaxFactory.IdentifierName(parameterName);
            return this.CreateMethod(container, methodName, syntax, (method, diagnostics) =>
            {
                var parameter = method.Parameters[parameterIndex];
                var expression = new BoundParameter(syntax, parameter);
                return new BoundReturnStatement(syntax, expression) { WasCompilerGenerated = true };
            });
        }

        private EEMethodSymbol GetThisMethod(EENamedTypeSymbol container, string methodName)
        {
            var syntax = SyntaxFactory.ThisExpression();
            return this.CreateMethod(container, methodName, syntax, (method, diagnostics) =>
            {
                var expression = new BoundThisReference(syntax, GetNonDisplayClassContainer(container.SubstitutedSourceType));
                return new BoundReturnStatement(syntax, expression) { WasCompilerGenerated = true };
            });
        }

        private EEMethodSymbol GetTypeVariablesMethod(EENamedTypeSymbol container, string methodName, NamedTypeSymbol typeVariablesType)
        {
            var syntax = SyntaxFactory.IdentifierName(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken));
            return this.CreateMethod(container, methodName, syntax, (method, diagnostics) =>
            {
                var type = method.TypeMap.SubstituteNamedType(typeVariablesType);
                var expression = new BoundObjectCreationExpression(syntax, type.InstanceConstructors[0]);
                var statement = new BoundReturnStatement(syntax, expression) { WasCompilerGenerated = true };
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
            var expression = binder.BindValue(syntax, diagnostics, Binder.BindValueKind.RValue);
            if (diagnostics.HasAnyErrors())
            {
                resultProperties = default(ResultProperties);
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
            return new BoundReturnStatement(syntax, expression) { WasCompilerGenerated = true };
        }

        private static BoundStatement BindStatement(Binder binder, StatementSyntax syntax, DiagnosticBag diagnostics, out ResultProperties properties)
        {
            properties = new ResultProperties(DkmClrCompilationResultFlags.PotentialSideEffect | DkmClrCompilationResultFlags.ReadOnlyResult);
            return binder.BindStatement(syntax, diagnostics);
        }

        private static bool IsAssignableExpression(Binder binder, BoundExpression expression)
        {
            // NOTE: Surprisingly, binder.CheckValueKind will return true (!) for readonly fields 
            // in contexts where they cannot be assigned - it simply reports a diagnostic.
            // Presumably, this is done to avoid producing a confusing error message about the
            // field not being an lvalue.
            var diagnostics = DiagnosticBag.GetInstance();
            var result = binder.CheckValueKind(expression, Binder.BindValueKind.Assignment, diagnostics) &&
                !diagnostics.HasAnyErrors();
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
                var assembly = assembliesAndModules[i] as AssemblySymbol;
                if (assembly != null && assemblyIdentityComparer.ReferenceMatchesDefinition(referenceIdentity, assembly.Identity))
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
            bool methodNotType)
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
                    binder = new WithTypeArgumentsBinder(substitutedSourceType.TypeArguments, binder);
                }
            }

            stack.Free();

            if (substitutedSourceMethod.Arity > 0)
            {
                binder = new WithTypeArgumentsBinder(substitutedSourceMethod.TypeArguments, binder);
            }

            if (methodNotType)
            {
                // Method locals and parameters shadow pseudo-variables.
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

            return binder;
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

        /// <summary>
        /// Return a mapping of captured variables (parameters, locals, and
        /// "this") to locals. The mapping is needed to expose the original
        /// local identifiers (those from source) in the binder.
        /// </summary>
        private static void GetDisplayClassVariables(
            MethodSymbol method,
            ImmutableArray<LocalSymbol> locals,
            InScopeHoistedLocals inScopeHoistedLocals,
            out ImmutableArray<string> displayClassVariableNamesInOrder,
            out ImmutableDictionary<string, DisplayClassVariable> displayClassVariables,
            out ImmutableHashSet<string> hoistedParameterNames)
        {
            // Calculated the shortest paths from locals to instances of display
            // classes. There should not be two instances of the same display
            // class immediately within any particular method.
            var displayClassTypes = PooledHashSet<NamedTypeSymbol>.GetInstance();
            var displayClassInstances = ArrayBuilder<DisplayClassInstanceAndFields>.GetInstance();

            // Add any display class instances from locals (these will contain any hoisted locals).
            foreach (var local in locals)
            {
                var name = local.Name;
                if ((name != null) && (GeneratedNames.GetKind(name) == GeneratedNameKind.DisplayClassLocalOrField))
                {
                    var instance = new DisplayClassInstanceFromLocal((EELocalSymbol)local);
                    displayClassTypes.Add(instance.Type);
                    displayClassInstances.Add(new DisplayClassInstanceAndFields(instance));
                }
            }

            foreach (var parameter in method.Parameters)
            {
                if (GeneratedNames.GetKind(parameter.Name) == GeneratedNameKind.TransparentIdentifier)
                {
                    var instance = new DisplayClassInstanceFromParameter(parameter);
                    displayClassTypes.Add(instance.Type);
                    displayClassInstances.Add(new DisplayClassInstanceAndFields(instance));
                }
            }

            var containingType = method.ContainingType;
            bool isIteratorOrAsyncMethod = false;
            if (IsDisplayClassType(containingType))
            {
                if (!method.IsStatic)
                {
                    // Add "this" display class instance.
                    var instance = new DisplayClassInstanceFromParameter(method.ThisParameter);
                    displayClassTypes.Add(instance.Type);
                    displayClassInstances.Add(new DisplayClassInstanceAndFields(instance));
                }

                isIteratorOrAsyncMethod = GeneratedNames.GetKind(containingType.Name) == GeneratedNameKind.StateMachineType;
            }

            if (displayClassInstances.Any())
            {
                // Find any additional display class instances breadth first.
                for (int depth = 0; GetDisplayClassInstances(displayClassTypes, displayClassInstances, depth) > 0; depth++)
                {
                }

                // The locals are the set of all fields from the display classes.
                var displayClassVariableNamesInOrderBuilder = ArrayBuilder<string>.GetInstance();
                var displayClassVariablesBuilder = PooledDictionary<string, DisplayClassVariable>.GetInstance();

                var parameterNames = PooledHashSet<string>.GetInstance();
                if (isIteratorOrAsyncMethod)
                {
                    Debug.Assert(IsDisplayClassType(containingType));

                    foreach (var field in containingType.GetMembers().OfType<FieldSymbol>())
                    {
                        // All iterator and async state machine fields (including hoisted locals) have mangled names, except
                        // for hoisted parameters (whose field names are always the same as the original source parameters).
                        var fieldName = field.Name;
                        if (GeneratedNames.GetKind(fieldName) == GeneratedNameKind.None)
                        {
                            parameterNames.Add(fieldName);
                        }
                    }
                }
                else
                {
                    foreach (var p in method.Parameters)
                    {
                        parameterNames.Add(p.Name);
                    }
                }

                var pooledHoistedParameterNames = PooledHashSet<string>.GetInstance();
                foreach (var instance in displayClassInstances)
                {
                    GetDisplayClassVariables(
                        displayClassVariableNamesInOrderBuilder,
                        displayClassVariablesBuilder,
                        parameterNames,
                        inScopeHoistedLocals,
                        instance,
                        pooledHoistedParameterNames);
                }

                hoistedParameterNames = pooledHoistedParameterNames.ToImmutableHashSet<string>();
                pooledHoistedParameterNames.Free();
                parameterNames.Free();

                displayClassVariableNamesInOrder = displayClassVariableNamesInOrderBuilder.ToImmutableAndFree();
                displayClassVariables = displayClassVariablesBuilder.ToImmutableDictionary();
                displayClassVariablesBuilder.Free();
            }
            else
            {
                hoistedParameterNames = ImmutableHashSet<string>.Empty;
                displayClassVariableNamesInOrder = ImmutableArray<string>.Empty;
                displayClassVariables = ImmutableDictionary<string, DisplayClassVariable>.Empty;
            }

            displayClassTypes.Free();
            displayClassInstances.Free();
        }

        /// <summary>
        /// Return the set of display class instances that can be reached
        /// from the given local. A particular display class may be reachable
        /// from multiple locals. In those cases, the instance from the
        /// shortest path (fewest intermediate fields) is returned.
        /// </summary>
        private static int GetDisplayClassInstances(
            HashSet<NamedTypeSymbol> displayClassTypes,
            ArrayBuilder<DisplayClassInstanceAndFields> displayClassInstances,
            int depth)
        {
            Debug.Assert(displayClassInstances.All(p => p.Depth <= depth));

            var atDepth = ArrayBuilder<DisplayClassInstanceAndFields>.GetInstance();
            atDepth.AddRange(displayClassInstances.Where(p => p.Depth == depth));
            Debug.Assert(atDepth.Count > 0);

            int n = 0;
            foreach (var instance in atDepth)
            {
                n += GetDisplayClassInstances(displayClassTypes, displayClassInstances, instance);
            }

            atDepth.Free();
            return n;
        }

        private static int GetDisplayClassInstances(
            HashSet<NamedTypeSymbol> displayClassTypes,
            ArrayBuilder<DisplayClassInstanceAndFields> displayClassInstances,
            DisplayClassInstanceAndFields instance)
        {
            // Display class instance. The display class fields are variables.
            int n = 0;
            foreach (var member in instance.Type.GetMembers())
            {
                if (member.Kind != SymbolKind.Field)
                {
                    continue;
                }
                var field = (FieldSymbol)member;
                var fieldType = field.Type.TypeSymbol;
                var fieldKind = GeneratedNames.GetKind(field.Name);
                if (fieldKind == GeneratedNameKind.DisplayClassLocalOrField ||
                    fieldKind == GeneratedNameKind.TransparentIdentifier ||
                    IsTransparentIdentifierFieldInAnonymousType(field) ||
                    (fieldKind == GeneratedNameKind.ThisProxyField && GeneratedNames.GetKind(fieldType.Name) == GeneratedNameKind.LambdaDisplayClass)) // Async lambda case.
                {
                    Debug.Assert(!field.IsStatic);
                    // A local that is itself a display class instance.
                    if (displayClassTypes.Add((NamedTypeSymbol)fieldType))
                    {
                        var other = instance.FromField(field);
                        displayClassInstances.Add(other);
                        n++;
                    }
                }
            }
            return n;
        }

        private static bool IsTransparentIdentifierFieldInAnonymousType(FieldSymbol field)
        {
            string fieldName = field.Name;

            if (GeneratedNames.GetKind(fieldName) != GeneratedNameKind.AnonymousTypeField)
            {
                return false;
            }

            GeneratedNameKind kind;
            int openBracketOffset;
            int closeBracketOffset;
            if (!GeneratedNames.TryParseGeneratedName(fieldName, out kind, out openBracketOffset, out closeBracketOffset))
            {
                return false;
            }

            fieldName = fieldName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);

            return GeneratedNames.GetKind(fieldName) == GeneratedNameKind.TransparentIdentifier;
        }

        private static void GetDisplayClassVariables(
            ArrayBuilder<string> displayClassVariableNamesInOrderBuilder,
            Dictionary<string, DisplayClassVariable> displayClassVariablesBuilder,
            HashSet<string> parameterNames,
            InScopeHoistedLocals inScopeHoistedLocals,
            DisplayClassInstanceAndFields instance,
            HashSet<string> hoistedParameterNames)
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
                GeneratedNameKind fieldKind;
                int openBracketOffset;
                int closeBracketOffset;
                GeneratedNames.TryParseGeneratedName(fieldName, out fieldKind, out openBracketOffset, out closeBracketOffset);

                switch (fieldKind)
                {
                    case GeneratedNameKind.AnonymousTypeField:
                        Debug.Assert(fieldName == field.Name); // This only happens once.
                        fieldName = fieldName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
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
                        if (!inScopeHoistedLocals.IsInScope(fieldName))
                        {
                            continue;
                        }

                        variableName = fieldName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                        variableKind = DisplayClassVariableKind.Local;
                        Debug.Assert(!field.IsStatic);
                        break;
                    case GeneratedNameKind.ThisProxyField:
                        // A reference to "this".
                        variableName = fieldName;
                        variableKind = DisplayClassVariableKind.This;
                        Debug.Assert(!field.IsStatic);
                        break;
                    case GeneratedNameKind.None:
                        // A reference to a parameter or local.
                        variableName = fieldName;
                        if (parameterNames.Contains(variableName))
                        {
                            variableKind = DisplayClassVariableKind.Parameter;
                            hoistedParameterNames.Add(variableName);
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
                    Debug.Assert(instance.Fields.Count() >= 1); // greater depth
                    Debug.Assert((variableKind == DisplayClassVariableKind.Parameter) ||
                        (variableKind == DisplayClassVariableKind.This));
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

        private static bool IsDisplayClassType(NamedTypeSymbol type)
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
                            : candidateMethod.Construct(candidateSubstitutedSourceType.TypeArguments);
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
                !candidateMethod.IsAbstract &&
                (!(desiredMethodMustBeInstance && candidateMethod.IsStatic)) &&
                candidateMethod.Name == desiredMethodName &&
                HaveSameConstraints(candidateMethod.TypeParameters, desiredTypeParameters);
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

            internal NamedTypeSymbol Type
            {
                get { return this.Fields.Any() ? (NamedTypeSymbol)this.Fields.Head.Type.TypeSymbol : this.Instance.Type; }
            }

            internal int Depth
            {
                get { return this.Fields.Count(); }
            }

            internal DisplayClassInstanceAndFields FromField(FieldSymbol field)
            {
                Debug.Assert(IsDisplayClassType((NamedTypeSymbol)field.Type.TypeSymbol) ||
                    GeneratedNames.GetKind(field.Type.TypeSymbol.Name) == GeneratedNameKind.AnonymousType);
                return new DisplayClassInstanceAndFields(this.Instance, this.Fields.Prepend(field));
            }

            internal DisplayClassVariable ToVariable(string name, DisplayClassVariableKind kind, FieldSymbol field)
            {
                return new DisplayClassVariable(name, kind, this.Instance, this.Fields.Prepend(field));
            }
        }
    }
}
