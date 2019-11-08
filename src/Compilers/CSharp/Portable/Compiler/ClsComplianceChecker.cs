// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Traverses the symbol table checking for CLS compliance.
    /// </summary>
    internal partial class ClsComplianceChecker : CSharpSymbolVisitor
    {
        private readonly CSharpCompilation _compilation;
        private readonly SyntaxTree _filterTree; //if not null, limit analysis to types residing in this tree
        private readonly TextSpan? _filterSpanWithinTree; //if filterTree and filterSpanWithinTree is not null, limit analysis to types residing within this span in the filterTree.
        private readonly ConcurrentQueue<Diagnostic> _diagnostics;
        private readonly CancellationToken _cancellationToken;

        private readonly ConcurrentDictionary<Symbol, Compliance> _declaredOrInheritedCompliance;

        /// <seealso cref="MethodCompiler._compilerTasks"/>
        private readonly ConcurrentStack<Task> _compilerTasks;

        private ClsComplianceChecker(
            CSharpCompilation compilation,
            SyntaxTree filterTree,
            TextSpan? filterSpanWithinTree,
            ConcurrentQueue<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _filterTree = filterTree;
            _filterSpanWithinTree = filterSpanWithinTree;
            _diagnostics = diagnostics;
            _cancellationToken = cancellationToken;

            _declaredOrInheritedCompliance = new ConcurrentDictionary<Symbol, Compliance>(SymbolEqualityComparer.ConsiderEverything);

            if (ConcurrentAnalysis)
            {
                _compilerTasks = new ConcurrentStack<Task>();
            }
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="ClsComplianceChecker"/> is allowed to analyze in parallel.
        /// </summary>
        private bool ConcurrentAnalysis => _filterTree == null && _compilation.Options.ConcurrentBuild;

        /// <summary>
        /// Traverses the symbol table checking for CLS compliance.
        /// </summary>
        /// <param name="compilation">Compilation that owns the symbol table.</param>
        /// <param name="diagnostics">Will be supplemented with documentation comment diagnostics.</param>
        /// <param name="cancellationToken">To stop traversing the symbol table early.</param>
        /// <param name="filterTree">Only report diagnostics from this syntax tree, if non-null.</param>
        /// <param name="filterSpanWithinTree">If <paramref name="filterTree"/> and <paramref name="filterSpanWithinTree"/> is non-null, report diagnostics within this span in the <paramref name="filterTree"/>.</param>
        public static void CheckCompliance(CSharpCompilation compilation, DiagnosticBag diagnostics, CancellationToken cancellationToken, SyntaxTree filterTree = null, TextSpan? filterSpanWithinTree = null)
        {
            var queue = new ConcurrentQueue<Diagnostic>();
            var checker = new ClsComplianceChecker(compilation, filterTree, filterSpanWithinTree, queue, cancellationToken);
            checker.Visit(compilation.Assembly);
            checker.WaitForWorkers();

            foreach (Diagnostic diag in queue)
            {
                diagnostics.Add(diag);
            }
        }

        public override void VisitAssembly(AssemblySymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            System.Diagnostics.Debug.Assert(symbol is SourceAssemblySymbol);

            Compliance assemblyCompliance = GetDeclaredOrInheritedCompliance(symbol);

            if (assemblyCompliance == Compliance.DeclaredFalse)
            {
                // Our interpretation of an assembly-level CLSCompliant attribute is as follows:
                //   1) If true, then perform all CLS checks.
                //   2) If false, then perform no CLS checks (dev11 still performs a few, mostly around
                //      meaningless attributes).  Our interpretation is that the user ultimately wants
                //      his code to be CLS-compliant, but is temporarily disabling the checks (e.g. during
                //      refactoring/prototyping).
                //   3) If absent, the perform all CLS checks.  Our interpretation is that - in the
                //      absence of an assembly-level attribute - any CLS problems within the compilation
                //      indicate that the user was trying to do something but didn't know how.  It would
                //      be nice if the most common case (i.e. this one) was the cheapest, but we don't
                //      want to confuse novice users.
                return;
            }

            bool assemblyComplianceValue = IsTrue(assemblyCompliance);

            for (int i = 0; i < symbol.Modules.Length; i++)
            {
                ModuleSymbol module = symbol.Modules[i];
                Location attributeLocation;
                bool? moduleDeclaredCompliance = GetDeclaredCompliance(module, out attributeLocation);

                Location warningLocation = i == 0 ? attributeLocation : module.Locations[0];
                System.Diagnostics.Debug.Assert(warningLocation != null || !moduleDeclaredCompliance.HasValue || (i == 0 && _filterTree != null),
                    "Can only be null when the source location is filtered out.");

                if (moduleDeclaredCompliance.HasValue)
                {
                    if (warningLocation != null)
                    {
                        if (!IsDeclared(assemblyCompliance))
                        {
                            // This is not useful on non-source modules, but dev11 reports it anyway.
                            this.AddDiagnostic(ErrorCode.WRN_CLS_NotOnModules, warningLocation);
                        }
                        else if (assemblyComplianceValue != moduleDeclaredCompliance.GetValueOrDefault())
                        {
                            this.AddDiagnostic(ErrorCode.WRN_CLS_NotOnModules2, warningLocation);
                        }
                    }
                }
                else if (assemblyComplianceValue && i > 0)
                {
                    bool sawClsCompliantAttribute = false;
                    var peModule = (Symbols.Metadata.PE.PEModuleSymbol)module;
                    foreach (CSharpAttributeData assemblyLevelAttribute in peModule.GetAssemblyAttributes())
                    {
                        if (assemblyLevelAttribute.IsTargetAttribute(peModule, AttributeDescription.CLSCompliantAttribute))
                        {
                            sawClsCompliantAttribute = true;
                            break;
                        }
                    }

                    if (!sawClsCompliantAttribute)
                    {
                        this.AddDiagnostic(ErrorCode.WRN_CLS_ModuleMissingCLS, warningLocation);
                    }
                }
            }

            if (assemblyComplianceValue)
            {
                CheckForAttributeWithArrayArgument(symbol);
            }

            ModuleSymbol sourceModule = symbol.Modules[0];
            if (IsTrue(GetDeclaredOrInheritedCompliance(sourceModule)))
            {
                CheckForAttributeWithArrayArgument(sourceModule);
            }

            Visit(symbol.GlobalNamespace);
        }

        private void WaitForWorkers()
        {
            var tasks = _compilerTasks;
            if (tasks == null)
            {
                return;
            }

            while (tasks.TryPop(out Task curTask))
            {
                curTask.GetAwaiter().GetResult();
            }
        }

        public override void VisitNamespace(NamespaceSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (DoNotVisit(symbol)) return;

            if (IsTrue(GetDeclaredOrInheritedCompliance(symbol)))
            {
                CheckName(symbol);
                CheckMemberDistinctness(symbol);
            }

            if (ConcurrentAnalysis)
            {
                VisitNamespaceMembersAsTasks(symbol);
            }
            else
            {
                VisitNamespaceMembers(symbol);
            }
        }

        private void VisitNamespaceMembersAsTasks(NamespaceSymbol symbol)
        {
            foreach (var m in symbol.GetMembersUnordered())
            {
                _compilerTasks.Push(Task.Run(UICultureUtilities.WithCurrentUICulture(() =>
                {
                    try
                    {
                        Visit(m);
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }), _cancellationToken));
            }
        }

        private void VisitNamespaceMembers(NamespaceSymbol symbol)
        {
            foreach (var m in symbol.GetMembersUnordered())
            {
                Visit(m);
            }
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", IsParallelEntry = false)]
        public override void VisitNamedType(NamedTypeSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (DoNotVisit(symbol)) return;

            Debug.Assert(!symbol.IsImplicitClass);

            Compliance compliance = GetDeclaredOrInheritedCompliance(symbol);

            if (VisitTypeOrMember(symbol, compliance))
            {
                if (IsTrue(compliance))
                {
                    CheckBaseTypeCompliance(symbol);
                    CheckTypeParameterCompliance(symbol.TypeParameters, symbol);

                    if (symbol.TypeKind == TypeKind.Delegate)
                    {
                        CheckParameterCompliance(symbol.DelegateInvokeMethod.Parameters, symbol);
                    }
                    else if (_compilation.IsAttributeType(symbol) && !HasAcceptableAttributeConstructor(symbol))
                    {
                        this.AddDiagnostic(ErrorCode.WRN_CLS_BadAttributeType, symbol.Locations[0], symbol);
                    }
                }
            }

            // You may assume we could skip the members if this type is inaccessible,
            // but dev11 reports that they are inaccessible as well.
            foreach (var m in symbol.GetMembersUnordered())
            {
                Visit(m);
            }
        }

        private bool HasAcceptableAttributeConstructor(NamedTypeSymbol attributeType)
        {
            foreach (MethodSymbol constructor in attributeType.InstanceConstructors)
            {
                if (IsTrue(GetDeclaredOrInheritedCompliance(constructor)) && IsAccessibleIfContainerIsAccessible(constructor))
                {
                    System.Diagnostics.Debug.Assert(IsAccessibleOutsideAssembly(constructor), "Should be implied by IsAccessibleIfContainerIsAccessible");

                    bool hasUnacceptableParameterType = false;

                    foreach (var paramType in constructor.ParameterTypesWithAnnotations) // Public caller would select type out of parameters.
                    {
                        if (paramType.TypeKind == TypeKind.Array ||
                            paramType.Type.GetAttributeParameterTypedConstantKind(_compilation) == TypedConstantKind.Error)
                        {
                            hasUnacceptableParameterType = true;
                            break;
                        }
                    }

                    if (!hasUnacceptableParameterType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override void VisitMethod(MethodSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (DoNotVisit(symbol)) return;

            Compliance compliance = GetDeclaredOrInheritedCompliance(symbol);

            // Most CLS checks don't apply to accessors.
            if (symbol.IsAccessor())
            {
                CheckForAttributeOnAccessor(symbol);
                CheckForMeaninglessOnParameter(symbol.Parameters);
                CheckForMeaninglessOnReturn(symbol);

                if (IsTrue(compliance))
                {
                    CheckForAttributeWithArrayArgument(symbol);
                }

                return;
            }

            if (!VisitTypeOrMember(symbol, compliance)) return;

            if (IsTrue(compliance))
            {
                CheckParameterCompliance(symbol.Parameters, symbol.ContainingType);
                CheckTypeParameterCompliance(symbol.TypeParameters, symbol.ContainingType);

                if (symbol.IsVararg)
                {
                    this.AddDiagnostic(ErrorCode.WRN_CLS_NoVarArgs, symbol.Locations[0]);
                }
            }
        }

        private void CheckForAttributeOnAccessor(MethodSymbol symbol)
        {
            foreach (CSharpAttributeData attribute in symbol.GetAttributes())
            {
                if (attribute.IsTargetAttribute(symbol, AttributeDescription.CLSCompliantAttribute))
                {
                    Location attributeLocation;
                    if (TryGetAttributeWarningLocation(attribute, out attributeLocation))
                    {
                        AttributeUsageInfo attributeUsage = attribute.AttributeClass.GetAttributeUsageInfo();
                        this.AddDiagnostic(ErrorCode.ERR_AttributeNotOnAccessor, attributeLocation, attribute.AttributeClass.Name, attributeUsage.GetValidTargetsErrorArgument());
                        break;
                    }
                }
            }
        }

        public override void VisitProperty(PropertySymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (DoNotVisit(symbol)) return;

            Compliance compliance = GetDeclaredOrInheritedCompliance(symbol);

            if (!VisitTypeOrMember(symbol, compliance)) return;

            // Rule 28 requires that the accessors "adhere to a special naming pattern".
            // We don't actually need to do anything here, because they automatically
            // will unless they override accessors from metadata - we don't check overrides -
            // or they explicitly implement interface accessors - we don't check non-public
            // members.

            if (IsTrue(compliance))
            {
                CheckParameterCompliance(symbol.Parameters, symbol.ContainingType);
            }
        }

        public override void VisitEvent(EventSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (DoNotVisit(symbol)) return;

            Compliance compliance = GetDeclaredOrInheritedCompliance(symbol);

            // Rule 32 specifies the shapes of the accessor signatures.  However,
            // even though WinRT events have a different shape, dev11 does not
            // report that they are non-compliant.

            // Rule 28 requires that the accessors "adhere to a special naming pattern".
            // We don't actually need to do anything here, because they automatically
            // will unless they override accessors from metadata - we don't check overrides -
            // or they explicitly implement interface accessors - we don't check non-public
            // members.

            if (!VisitTypeOrMember(symbol, compliance)) return;
        }

        public override void VisitField(FieldSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (DoNotVisit(symbol)) return;

            Compliance compliance = GetDeclaredOrInheritedCompliance(symbol);

            if (!VisitTypeOrMember(symbol, compliance)) return;

            if (IsTrue(compliance))
            {
                if (symbol.IsVolatile)
                {
                    this.AddDiagnostic(ErrorCode.WRN_CLS_VolatileField, symbol.Locations[0], symbol);
                }
            }
        }

        /// <returns>False if no further checks are required (because they would be cascading).</returns>
        private bool VisitTypeOrMember(Symbol symbol, Compliance compliance)
        {
            SymbolKind symbolKind = symbol.Kind;

            System.Diagnostics.Debug.Assert(
                symbolKind == SymbolKind.NamedType ||
                symbolKind == SymbolKind.Field ||
                symbolKind == SymbolKind.Property ||
                symbolKind == SymbolKind.Event ||
                symbolKind == SymbolKind.Method);
            System.Diagnostics.Debug.Assert(!symbol.IsAccessor());

            if (!CheckForDeclarationWithoutAssemblyDeclaration(symbol, compliance))
            {
                return false; // Don't cascade from this failure.
            }

            bool isCompliant = IsTrue(compliance);
            bool isAccessibleOutsideAssembly = IsAccessibleOutsideAssembly(symbol);

            if (isAccessibleOutsideAssembly)
            {
                if (isCompliant)
                {
                    CheckName(symbol);
                    CheckForCompliantWithinNonCompliant(symbol);
                    CheckReturnTypeCompliance(symbol);

                    if (symbol.Kind == SymbolKind.NamedType)
                    {
                        CheckMemberDistinctness((NamedTypeSymbol)symbol);
                    }
                }
                else if (GetDeclaredOrInheritedCompliance(symbol.ContainingAssembly) == Compliance.DeclaredTrue && IsTrue(GetInheritedCompliance(symbol)))
                {
                    CheckForNonCompliantAbstractMember(symbol);
                }
            }
            else if (IsDeclared(compliance))
            {
                this.AddDiagnostic(ErrorCode.WRN_CLS_MeaninglessOnPrivateType, symbol.Locations[0], symbol);
                return false; // Don't cascade from this failure.
            }

            if (isCompliant)
            {
                // Independent of accessibility.
                CheckForAttributeWithArrayArgument(symbol);
            }

            // These checks are independent of accessibility and compliance.
            if (symbolKind == SymbolKind.NamedType)
            {
                NamedTypeSymbol type = (NamedTypeSymbol)symbol;
                if (type.TypeKind == TypeKind.Delegate)
                {
                    MethodSymbol method = type.DelegateInvokeMethod;
                    CheckForMeaninglessOnParameter(method.Parameters);
                    CheckForMeaninglessOnReturn(method);
                }
            }
            else if (symbolKind == SymbolKind.Method)
            {
                MethodSymbol method = (MethodSymbol)symbol;
                CheckForMeaninglessOnParameter(method.Parameters);
                CheckForMeaninglessOnReturn(method);
            }
            else if (symbolKind == SymbolKind.Property)
            {
                PropertySymbol property = (PropertySymbol)symbol;
                CheckForMeaninglessOnParameter(property.Parameters);
            }

            // All checks that apply to inaccessible symbols are performed by this method.
            return isAccessibleOutsideAssembly;
        }

        private void CheckForNonCompliantAbstractMember(Symbol symbol)
        {
            System.Diagnostics.Debug.Assert(!IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on non-compliant symbols");

            NamedTypeSymbol containingType = symbol.ContainingType;
            if (containingType is object { IsInterface: true })
            {
                this.AddDiagnostic(ErrorCode.WRN_CLS_BadInterfaceMember, symbol.Locations[0], symbol);
            }
            else if (symbol.IsAbstract && symbol.Kind != SymbolKind.NamedType)
            {
                this.AddDiagnostic(ErrorCode.WRN_CLS_NoAbstractMembers, symbol.Locations[0], symbol);
            }
        }

        private void CheckBaseTypeCompliance(NamedTypeSymbol symbol)
        {
            System.Diagnostics.Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on compliant symbols");

            // NOTE: implemented interfaces do not have to be CLS-compliant (unless the type itself is an interface).

            if (symbol.IsInterface)
            {
                foreach (NamedTypeSymbol interfaceType in symbol.InterfacesNoUseSiteDiagnostics())
                {
                    if (!IsCompliantType(interfaceType, symbol))
                    {
                        // TODO: it would be nice to report this on the base type clause.
                        this.AddDiagnostic(ErrorCode.WRN_CLS_BadInterface, symbol.Locations[0], symbol, interfaceType);
                    }
                }
            }
            else
            {
                NamedTypeSymbol baseType = symbol.EnumUnderlyingType ?? symbol.BaseTypeNoUseSiteDiagnostics; // null for interfaces
                System.Diagnostics.Debug.Assert((object)baseType != null || symbol.SpecialType == SpecialType.System_Object, "Only object has no base.");
                if ((object)baseType != null && !IsCompliantType(baseType, symbol))
                {
                    // TODO: it would be nice to report this on the base type clause.
                    this.AddDiagnostic(ErrorCode.WRN_CLS_BadBase, symbol.Locations[0], symbol, baseType);
                }
            }
        }

        private void CheckForCompliantWithinNonCompliant(Symbol symbol)
        {
            System.Diagnostics.Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on compliant symbols");

            NamedTypeSymbol containingType = symbol.ContainingType;
            System.Diagnostics.Debug.Assert((object)containingType == null || !containingType.IsImplicitClass);
            if ((object)containingType != null && !IsTrue(GetDeclaredOrInheritedCompliance(containingType)))
            {
                this.AddDiagnostic(ErrorCode.WRN_CLS_IllegalTrueInFalse, symbol.Locations[0], symbol, containingType);
            }
        }

        private void CheckTypeParameterCompliance(ImmutableArray<TypeParameterSymbol> typeParameters, NamedTypeSymbol context)
        {
            System.Diagnostics.Debug.Assert(typeParameters.IsEmpty || IsTrue(GetDeclaredOrInheritedCompliance(context)), "Only call on compliant symbols");

            foreach (TypeParameterSymbol typeParameter in typeParameters)
            {
                foreach (TypeWithAnnotations constraintType in typeParameter.ConstraintTypesNoUseSiteDiagnostics)
                {
                    if (!IsCompliantType(constraintType.Type, context))
                    {
                        // TODO: it would be nice to report this on the constraint clause.
                        // NOTE: we're improving over dev11 by reporting on the type parameter declaration,
                        // rather than on the constraint type declaration.
                        this.AddDiagnostic(ErrorCode.WRN_CLS_BadTypeVar, typeParameter.Locations[0], constraintType.Type);
                    }
                }
            }
        }

        private void CheckParameterCompliance(ImmutableArray<ParameterSymbol> parameters, NamedTypeSymbol context)
        {
            System.Diagnostics.Debug.Assert(parameters.IsEmpty || IsTrue(GetDeclaredOrInheritedCompliance(context)), "Only call on compliant symbols");

            foreach (ParameterSymbol parameter in parameters)
            {
                if (!IsCompliantType(parameter.Type, context))
                {
                    this.AddDiagnostic(ErrorCode.WRN_CLS_BadArgType, parameter.Locations[0], parameter.Type);
                }
            }
        }

        private void CheckForAttributeWithArrayArgument(Symbol symbol)
        {
            System.Diagnostics.Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on compliant symbols");
            CheckForAttributeWithArrayArgumentInternal(symbol.GetAttributes());
            if (symbol.Kind == SymbolKind.Method)
            {
                CheckForAttributeWithArrayArgumentInternal(((MethodSymbol)symbol).GetReturnTypeAttributes());
            }
        }

        /// <remarks>
        /// BREAK: Dev11 reports WRN_CLS_ArrayArgumentToAttribute on all symbols, whereas roslyn reports it only
        /// on accessible symbols.
        /// </remarks>
        private void CheckForAttributeWithArrayArgumentInternal(ImmutableArray<CSharpAttributeData> attributes)
        {
            foreach (CSharpAttributeData attribute in attributes)
            {
                foreach (TypedConstant argument in attribute.ConstructorArguments)
                {
                    if (argument.Type.TypeKind == TypeKind.Array)
                    {
                        // TODO: it would be nice to report for each bad argument, but currently it's pointless since they
                        // would all have the same message and location.
                        Location warningLocation;
                        if (TryGetAttributeWarningLocation(attribute, out warningLocation))
                        {
                            this.AddDiagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, warningLocation);
                            return;
                        }
                    }
                }

                foreach (var pair in attribute.NamedArguments)
                {
                    TypedConstant argument = pair.Value;
                    if (argument.Type.TypeKind == TypeKind.Array)
                    {
                        // TODO: it would be nice to report for each bad argument, but currently it's pointless since they
                        // would all have the same message and location.
                        Location warningLocation;
                        if (TryGetAttributeWarningLocation(attribute, out warningLocation))
                        {
                            this.AddDiagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, warningLocation);
                            return;
                        }
                    }
                }

                // This catches things like param arrays and converted null literals.
                if ((object)attribute.AttributeConstructor != null) // Happens in error scenarios.
                {
                    foreach (var type in attribute.AttributeConstructor.ParameterTypesWithAnnotations)
                    {
                        if (type.TypeKind == TypeKind.Array)
                        {
                            // TODO: it would be nice to report for each bad argument, but currently it's pointless since they
                            // would all have the same message and location.
                            Location warningLocation;
                            if (TryGetAttributeWarningLocation(attribute, out warningLocation))
                            {
                                this.AddDiagnostic(ErrorCode.WRN_CLS_ArrayArgumentToAttribute, warningLocation);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private bool TryGetAttributeWarningLocation(CSharpAttributeData attribute, out Location location)
        {
            SyntaxReference syntaxRef = attribute.ApplicationSyntaxReference;
            if (syntaxRef == null && _filterTree == null)
            {
                location = NoLocation.Singleton;
                return true;
            }
            else if (_filterTree == null || (syntaxRef != null && syntaxRef.SyntaxTree == _filterTree))
            {
                System.Diagnostics.Debug.Assert(syntaxRef.SyntaxTree.HasCompilationUnitRoot);
                location = new SourceLocation(syntaxRef);
                return true;
            }

            location = null;
            return false;
        }

        private void CheckForMeaninglessOnParameter(ImmutableArray<ParameterSymbol> parameters)
        {
            if (parameters.IsEmpty) return;

            int startPos = 0;

            Symbol container = parameters[0].ContainingSymbol;
            if (container.Kind == SymbolKind.Method)
            {
                Symbol associated = ((MethodSymbol)container).AssociatedSymbol;
                if ((object)associated != null && associated.Kind == SymbolKind.Property)
                {
                    // Only care about "value" parameter for accessors.
                    // NOTE: public caller would have to count parameters.
                    startPos = ((PropertySymbol)associated).ParameterCount;
                }
            }

            for (int i = startPos; i < parameters.Length; i++)
            {
                Location attributeLocation;
                if (TryGetClsComplianceAttributeLocation(parameters[i].GetAttributes(), parameters[i], out attributeLocation))
                {
                    this.AddDiagnostic(ErrorCode.WRN_CLS_MeaninglessOnParam, attributeLocation);
                }
            }
        }

        private void CheckForMeaninglessOnReturn(MethodSymbol method)
        {
            Location attributeLocation;
            if (TryGetClsComplianceAttributeLocation(method.GetReturnTypeAttributes(), method, out attributeLocation))
            {
                this.AddDiagnostic(ErrorCode.WRN_CLS_MeaninglessOnReturn, attributeLocation);
            }
        }

        private void CheckReturnTypeCompliance(Symbol symbol)
        {
            System.Diagnostics.Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)), "Only call on compliant symbols");

            ErrorCode code;
            TypeSymbol type;
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    code = ErrorCode.WRN_CLS_BadFieldPropType;
                    type = ((FieldSymbol)symbol).Type;
                    break;
                case SymbolKind.Property:
                    code = ErrorCode.WRN_CLS_BadFieldPropType;
                    type = ((PropertySymbol)symbol).Type;
                    break;
                case SymbolKind.Event:
                    code = ErrorCode.WRN_CLS_BadFieldPropType;
                    type = ((EventSymbol)symbol).Type;
                    break;
                case SymbolKind.Method:
                    code = ErrorCode.WRN_CLS_BadReturnType;
                    MethodSymbol method = (MethodSymbol)symbol;
                    type = method.ReturnType;

                    if (method.MethodKind == MethodKind.DelegateInvoke)
                    {
                        System.Diagnostics.Debug.Assert(method.ContainingType.TypeKind == TypeKind.Delegate);
                        symbol = method.ContainingType; // Refer to the delegate type in diagnostics.
                    }

                    // Diagnostic not interesting for accessors.
                    System.Diagnostics.Debug.Assert(!method.IsAccessor());

                    break;
                case SymbolKind.NamedType:
                    symbol = ((NamedTypeSymbol)symbol).DelegateInvokeMethod;
                    if ((object)symbol == null)
                    {
                        return;
                    }
                    else
                    {
                        goto case SymbolKind.Method;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }

            if (!IsCompliantType(type, symbol.ContainingType))
            {
                this.AddDiagnostic(code, symbol.Locations[0], symbol);
            }
        }

        private bool TryGetClsComplianceAttributeLocation(ImmutableArray<CSharpAttributeData> attributes, Symbol targetSymbol, out Location attributeLocation)
        {
            foreach (CSharpAttributeData data in attributes)
            {
                if (data.IsTargetAttribute(targetSymbol, AttributeDescription.CLSCompliantAttribute))
                {
                    if (TryGetAttributeWarningLocation(data, out attributeLocation))
                    {
                        return true;
                    }
                }
            }

            attributeLocation = null;
            return false;
        }

        /// <returns>True if the symbol is okay (i.e. no warnings).</returns>
        private bool CheckForDeclarationWithoutAssemblyDeclaration(Symbol symbol, Compliance compliance)
        {
            if (IsDeclared(compliance))
            {
                Compliance assemblyCompliance = GetDeclaredOrInheritedCompliance(symbol.ContainingAssembly);

                if (!IsDeclared(assemblyCompliance))
                {
                    ErrorCode code = IsTrue(compliance)
                        ? ErrorCode.WRN_CLS_AssemblyNotCLS
                        : ErrorCode.WRN_CLS_AssemblyNotCLS2;
                    this.AddDiagnostic(code, symbol.Locations[0], symbol);
                    return false;
                }
            }
            return true;
        }

        private void CheckMemberDistinctness(NamespaceOrTypeSymbol symbol)
        {
            System.Diagnostics.Debug.Assert(IsAccessibleOutsideAssembly(symbol));
            System.Diagnostics.Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)));

            MultiDictionary<string, Symbol> seenByName = new MultiDictionary<string, Symbol>(CaseInsensitiveComparison.Comparer);

            // For types, we also need to consider collisions with inherited members.
            if (symbol.Kind != SymbolKind.Namespace)
            {
                NamedTypeSymbol type = (NamedTypeSymbol)symbol;

                // NOTE: As in dev11 we're using Interfaces, rather than AllInterfaces.
                // This seems like a bug, but it's easier to reproduce it than to deal
                // with all the potential breaks.
                // NOTE: It's not clear why dev11 is looking in interfaces at all. Maybe
                // it was only supposed to happen for interface types?
                foreach (NamedTypeSymbol @interface in type.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics.Keys) // NOTE: would be hand-rolled in a standalone component.
                {
                    if (!IsAccessibleOutsideAssembly(@interface)) continue;

                    foreach (Symbol member in @interface.GetMembersUnordered())
                    {
                        // NOTE: As in dev11 we filter out overriding methods and properties (but not events).
                        // NOTE: As in dev11, we ignore the CLS compliance of the interface and its members.
                        if (IsAccessibleIfContainerIsAccessible(member) &&
                            (!member.IsOverride || !(member.Kind == SymbolKind.Method || member.Kind == SymbolKind.Property)))
                        {
                            seenByName.Add(member.Name, member);
                        }
                    }
                }

                NamedTypeSymbol baseType = type.BaseTypeNoUseSiteDiagnostics;
                while ((object)baseType != null)
                {
                    foreach (Symbol member in baseType.GetMembersUnordered())
                    {
                        // NOTE: As in dev11 we filter out overriding methods and properties (but not events).
                        // NOTE: Unlike for interface members, we check CLS compliance of base type members.
                        if (IsAccessibleOutsideAssembly(member) &&
                            IsTrue(GetDeclaredOrInheritedCompliance(member)) &&
                            (!member.IsOverride || !(member.Kind == SymbolKind.Method || member.Kind == SymbolKind.Property)))
                        {
                            seenByName.Add(member.Name, member);
                        }
                    }

                    baseType = baseType.BaseTypeNoUseSiteDiagnostics;
                }
            }

            // NOTE: visit the members in order so that the same one is always reported as a conflict.
            foreach (Symbol member in symbol.GetMembers())
            {
                // Filter out uninteresting members:
                if (DoNotVisit(member) ||
                    !IsAccessibleIfContainerIsAccessible(member) || // We already know that the container is accessible.
                    !IsTrue(GetDeclaredOrInheritedCompliance(member)) ||
                    member.IsOverride)
                {
                    continue;
                }

                var name = member.Name;
                var sameNameSymbols = seenByName[name];
                if (sameNameSymbols.Count > 0)
                {
                    CheckSymbolDistinctness(member, name, sameNameSymbols);
                }

                seenByName.Add(name, member);
            }
        }

        /// <remarks>
        /// NOTE: Dev11 behavior - First, it ignores arity,
        /// which seems like a good way to disambiguate symbols (in particular,
        /// CLS Rule 43 says that the name includes backtick-arity).  Second, it
        /// does not consider two members with identical names (i.e. not differing
        /// in case) to collide.
        /// </remarks>
        private void CheckSymbolDistinctness(Symbol symbol, string symbolName, MultiDictionary<string, Symbol>.ValueSet sameNameSymbols)
        {
            Debug.Assert(sameNameSymbols.Count > 0);
            Debug.Assert(symbol.Name == symbolName);

            bool isMethodOrProperty = symbol.Kind == SymbolKind.Method || symbol.Kind == SymbolKind.Property;

            foreach (Symbol other in sameNameSymbols)
            {
                if (other.Name != symbolName && !(isMethodOrProperty && other.Kind == symbol.Kind))
                {
                    // TODO: Shouldn't we somehow reference the conflicting member?  Dev11 doesn't.
                    this.AddDiagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, symbol.Locations[0], symbol);
                    return;
                }
            }

            if (!isMethodOrProperty)
            {
                return;
            }

            foreach (Symbol other in sameNameSymbols)
            {
                // Note: not checking accessor signatures, but checking accessor names.
                ErrorCode code;
                if (symbol.Kind == other.Kind &&
                    !symbol.IsAccessor() &&
                    !other.IsAccessor() &&
                    TryGetCollisionErrorCode(symbol, other, out code))
                {
                    this.AddDiagnostic(code, symbol.Locations[0], symbol);
                    return;
                }
                else if (other.Name != symbolName)
                {
                    // TODO: Shouldn't we somehow reference the conflicting member?  Dev11 doesn't.
                    this.AddDiagnostic(ErrorCode.WRN_CLS_BadIdentifierCase, symbol.Locations[0], symbol);
                    return;
                }
            }
        }

        private void CheckName(Symbol symbol)
        {
            System.Diagnostics.Debug.Assert(IsTrue(GetDeclaredOrInheritedCompliance(symbol)));
            System.Diagnostics.Debug.Assert(IsAccessibleOutsideAssembly(symbol));

            if (!symbol.CanBeReferencedByName || symbol.IsOverride) return;

            string name = symbol.Name;

            // NOTE: The CLI spec says
            //   CLS Rule 4: Assemblies shall follow Annex 7 of Technical Report 15 of the Unicode Standard 3.0 governing 
            //   the set of characters permitted to start and be included in identifiers, available on-line at 
            //   http://www.unicode.org/unicode/reports/tr15/tr15-18.html. Identifiers shall be in the canonical format defined 
            //   by Unicode Normalization Form C. For CLS purposes, two identifiers are the same if their lowercase mappings 
            //   (as specified by the Unicode locale-insensitive, one-to-one lowercase mappings) are the same. That is, for two 
            //   identifiers to be considered different under the CLS they shall differ in more than simply their case. However, 
            //   in order to override an inherited definition the CLI requires the precise encoding of the original declaration be 
            //   used.
            //
            // However, what the native compiler actually does is ignore everything about composed and decomposed characters 
            // (see comment in CompilationPass::checkCLSnaming) and then just checks if the first character is underscore
            // (0x005F or 0xFF3F).  Presumably, it assumes that the language rules have weeded out any other identifiers 
            // forbidden by the unicode spec.

            // NOTE: The parser won't actually accept '\uFF3F' as part of an identifier.
            System.Diagnostics.Debug.Assert(name.Length == 0 || name[0] != '\uFF3F');

            if (name.Length > 0 && name[0] == '\u005F')
            {
                this.AddDiagnostic(ErrorCode.WRN_CLS_BadIdentifier, symbol.Locations[0], name);
            }
        }

        private bool DoNotVisit(Symbol symbol)
        {
            if (symbol.Kind == SymbolKind.Namespace)
            {
                return false;
            }

            // TODO: There's no public equivalent of Symbol.DeclaringCompilation.
            return symbol.DeclaringCompilation != _compilation ||
                symbol.IsImplicitlyDeclared ||
                IsSyntacticallyFilteredOut(symbol);
        }

        private bool IsSyntacticallyFilteredOut(Symbol symbol)
        {
            // TODO: it would be nice to be more precise than this: we only want to
            // warn about the base class if it is listed in the filter tree, not if
            // any part of the type is in the filter tree.
            return _filterTree != null && !symbol.IsDefinedInSourceTree(_filterTree, _filterSpanWithinTree);
        }

        private bool IsCompliantType(TypeSymbol type, NamedTypeSymbol context)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                    return IsCompliantType(((ArrayTypeSymbol)type).ElementType, context);
                case TypeKind.Dynamic:
                    // NOTE: It would probably be most correct to return 
                    // IsCompliantType(this.compilation.GetSpecialType(SpecialType.System_Object), context)
                    // but that's way too much work in the 99.9% case.
                    return true;
                case TypeKind.Pointer:
                    return false;
                case TypeKind.Error:
                case TypeKind.TypeParameter:
                    // Possibly not the most accurate answer, but the gist is that we
                    // don't want to report problems with these types.
                    return true;
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.Delegate:
                case TypeKind.Enum:
                case TypeKind.Submission:
                    return IsCompliantType((NamedTypeSymbol)type, context);
                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }

        private bool IsCompliantType(NamedTypeSymbol type, NamedTypeSymbol context)
        {
            // BREAK: Other than in the cases listed below, dev11 always
            // returns true for predefined types - even if they are constructed
            // with non-compliant type arguments (e.g. System.Action<uint>).
            switch (type.SpecialType)
            {
                case SpecialType.System_TypedReference:
                case SpecialType.System_UIntPtr:
                    // Hard-coded in dev11 (LangCompiler::isCLS_Type).
                    return false;

                case SpecialType.System_SByte: // sic
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    // Dev11 calls these "quasi-simple" types and hard-codes false.
                    return false;
            }

            if (type.TypeKind == TypeKind.Error)
            {
                // Possibly not the most accurate answer, but the gist is that we
                // don't want to report problems with these types.
                return true;
            }

            if (!IsTrue(GetDeclaredOrInheritedCompliance(type.OriginalDefinition)))
            {
                return false;
            }

            if (type.IsTupleType)
            {
                return IsCompliantType(type.TupleUnderlyingType, context);
            }

            foreach (TypeWithAnnotations typeArg in type.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
            {
                if (!IsCompliantType(typeArg.Type, context))
                {
                    return false;
                }
            }

            return !IsInaccessibleBecauseOfConstruction(type, context);
        }

        /// <remarks>
        /// This check (the only one that uses the "context" parameter is based on CLS Rule 46,
        /// as implemented by LangCompiler::IsCLSAccessible.  The idea is that C&lt;int&gt; and C&lt;char&gt;
        /// are separate types in CLS, so they can't touch each other's protected members.
        /// TODO: This should really have a separate error code - it's logically separate and requires explanation.
        /// </remarks>
        /// <param name="type">Check the accessibility of this type (probably a parameter or return type).</param>
        /// <param name="context">Context for the accessibility check (e.g. containing type of method with <paramref name="type"/> as a parameter type.</param>
        private static bool IsInaccessibleBecauseOfConstruction(NamedTypeSymbol type, NamedTypeSymbol context)
        {
            // NOTE: Dev11 (incorrectly) only checks whether "type" is protected - it ignores container accessibility.
            bool sawProtected = type.DeclaredAccessibility.HasProtected();
            bool sawGeneric = false; // Generic "type" doesn't count.
            Dictionary<NamedTypeSymbol, NamedTypeSymbol> containingTypes = null; // maps definition to constructed
            {
                NamedTypeSymbol containingType = type.ContainingType;
                while ((object)containingType != null)
                {
                    if (containingTypes == null)
                    {
                        containingTypes = new Dictionary<NamedTypeSymbol, NamedTypeSymbol>();
                    }

                    sawProtected = sawProtected || containingType.DeclaredAccessibility.HasProtected();
                    sawGeneric = sawGeneric || containingType.Arity > 0;

                    containingTypes.Add(containingType.OriginalDefinition, containingType);

                    containingType = containingType.ContainingType;
                }
            }

            if (!sawProtected || !sawGeneric || containingTypes == null)
            {
                return false;
            }

            while ((object)context != null)
            {
                NamedTypeSymbol contextBaseType = context;
                while ((object)contextBaseType != null)
                {
                    NamedTypeSymbol containingType;
                    if (containingTypes.TryGetValue(contextBaseType.OriginalDefinition, out containingType))
                    {
                        return !TypeSymbol.Equals(containingType, contextBaseType, TypeCompareKind.ConsiderEverything2);
                    }

                    contextBaseType = contextBaseType.BaseTypeNoUseSiteDiagnostics;
                }

                context = context.ContainingType;
            }

            // NOTE: Dev11 seems to return true here.  That's reasonable, since the type is inaccessible,
            // but it seems like the inaccessibility can only be cascading from another error.
            return false;
        }

        private Compliance GetDeclaredOrInheritedCompliance(Symbol symbol)
        {
            System.Diagnostics.Debug.Assert(symbol.Kind == SymbolKind.NamedType || !((symbol is TypeSymbol)),
                "Type kinds without declarations are handled elsewhere.");

            if (symbol.Kind == SymbolKind.Namespace)
            {
                // Don't bother storing entries for namespaces - just go straight to the assembly.
                return GetDeclaredOrInheritedCompliance(symbol.ContainingAssembly);
            }
            else if (symbol.Kind == SymbolKind.Method)
            {
                MethodSymbol method = (MethodSymbol)symbol;
                Symbol associated = method.AssociatedSymbol;
                if ((object)associated != null)
                {
                    // Don't bother storing entries for accessors - just go straight to the property/event.
                    return GetDeclaredOrInheritedCompliance(associated);
                }
            }

            // Not meaningful
            Debug.Assert(symbol.Kind != SymbolKind.Alias);
            Debug.Assert(symbol.Kind != SymbolKind.Label);
            Debug.Assert(symbol.Kind != SymbolKind.Namespace);
            Debug.Assert(symbol.Kind != SymbolKind.Parameter);
            Debug.Assert(symbol.Kind != SymbolKind.RangeVariable);

            Compliance compliance;
            if (_declaredOrInheritedCompliance.TryGetValue(symbol, out compliance))
            {
                return compliance;
            }

            Location ignoredLocation;
            bool? declaredCompliance = GetDeclaredCompliance(symbol, out ignoredLocation);
            if (declaredCompliance.HasValue)
            {
                compliance = declaredCompliance.GetValueOrDefault() ? Compliance.DeclaredTrue : Compliance.DeclaredFalse;
            }
            else if (symbol.Kind == SymbolKind.Assembly)
            {
                // Assemblies are not compliant unless specifically declared to be so.
                compliance = Compliance.ImpliedFalse;
            }
            else
            {
                compliance = IsTrue(GetInheritedCompliance(symbol)) ? Compliance.InheritedTrue : Compliance.InheritedFalse;
            }

            // Don't bother caching methods, etc - they won't be reused.
            return (symbol.Kind == SymbolKind.Assembly || symbol.Kind == SymbolKind.NamedType)
                ? _declaredOrInheritedCompliance.GetOrAdd(symbol, compliance)
                : compliance;
        }

        private Compliance GetInheritedCompliance(Symbol symbol)
        {
            System.Diagnostics.Debug.Assert(symbol.Kind != SymbolKind.Assembly);

            Symbol containing = (Symbol)symbol.ContainingType ?? symbol.ContainingAssembly;
            System.Diagnostics.Debug.Assert((object)containing != null);
            return GetDeclaredOrInheritedCompliance(containing);
        }

        /// <remarks>
        /// As in dev11, we ignore the fact that CLSCompliantAttribute is inherited (i.e. from the base type)
        /// (see CSemanticChecker::CheckSymForCLS).  This should only affect types where the syntactic parent
        /// and the inheritance parent disagree.
        /// </remarks>
        private bool? GetDeclaredCompliance(Symbol symbol, out Location attributeLocation)
        {
            attributeLocation = null;
            foreach (CSharpAttributeData data in symbol.GetAttributes())
            {
                // Check signature before HasErrors to avoid realizing symbols for other attributes.
                if (data.IsTargetAttribute(symbol, AttributeDescription.CLSCompliantAttribute))
                {
                    NamedTypeSymbol attributeClass = data.AttributeClass;
                    if ((object)attributeClass != null)
                    {
                        DiagnosticInfo info = attributeClass.GetUseSiteDiagnostic();
                        if (info != null)
                        {
                            Location location = symbol.Locations.IsEmpty ? NoLocation.Singleton : symbol.Locations[0];
                            _diagnostics.Enqueue(new CSDiagnostic(info, location));
                            if (info.Severity >= DiagnosticSeverity.Error)
                            {
                                continue;
                            }
                        }
                    }

                    if (!data.HasErrors)
                    {
                        if (!TryGetAttributeWarningLocation(data, out attributeLocation))
                        {
                            attributeLocation = null;
                        }

                        ImmutableArray<TypedConstant> args = data.CommonConstructorArguments;
                        System.Diagnostics.Debug.Assert(args.Length == 1, "We already checked the signature and HasErrors.");

                        // Duplicates are reported elsewhere - we only care about the first (error-free) occurrence.
                        return (bool)args[0].Value;
                    }
                }
            }

            return null;
        }

        private static bool IsAccessibleOutsideAssembly(Symbol symbol)
        {
            while ((object)symbol != null && !IsImplicitClass(symbol))
            {
                if (!IsAccessibleIfContainerIsAccessible(symbol))
                {
                    return false;
                }
                symbol = symbol.ContainingType;
            }
            return true;
        }

        private static bool IsAccessibleIfContainerIsAccessible(Symbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    return true;
                case Accessibility.Private:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Internal:
                    return false;
                case Accessibility.NotApplicable:
                    System.Diagnostics.Debug.Assert(symbol.Kind == SymbolKind.ErrorType);
                    return false;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.DeclaredAccessibility);
            }
        }

        private void AddDiagnostic(ErrorCode code, Location location)
        {
            var info = new CSDiagnosticInfo(code);
            var diag = new CSDiagnostic(info, location);
            _diagnostics.Enqueue(diag);
        }

        private void AddDiagnostic(ErrorCode code, Location location, params object[] args)
        {
            var info = new CSDiagnosticInfo(code, args);
            var diag = new CSDiagnostic(info, location);
            _diagnostics.Enqueue(diag);
        }

        private static bool IsImplicitClass(Symbol symbol)
        {
            return symbol.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)symbol).IsImplicitClass;
        }

        private static bool IsTrue(Compliance compliance)
        {
            switch (compliance)
            {
                case Compliance.DeclaredTrue:
                case Compliance.InheritedTrue:
                    return true;
                case Compliance.DeclaredFalse:
                case Compliance.InheritedFalse:
                case Compliance.ImpliedFalse:
                    return false;
                default:
                    throw ExceptionUtilities.UnexpectedValue(compliance);
            }
        }

        private static bool IsDeclared(Compliance compliance)
        {
            switch (compliance)
            {
                case Compliance.DeclaredTrue:
                case Compliance.DeclaredFalse:
                    return true;
                case Compliance.InheritedTrue:
                case Compliance.InheritedFalse:
                case Compliance.ImpliedFalse:
                    return false;
                default:
                    throw ExceptionUtilities.UnexpectedValue(compliance);
            }
        }

        private enum Compliance
        {
            DeclaredTrue,
            DeclaredFalse,
            InheritedTrue,
            InheritedFalse,
            ImpliedFalse,
        }

        /// <remarks>
        /// Based on CompilationPass::CLSReduceSignature.
        /// </remarks>
        private static bool TryGetCollisionErrorCode(Symbol x, Symbol y, out ErrorCode code)
        {
            System.Diagnostics.Debug.Assert((object)x != null);
            System.Diagnostics.Debug.Assert((object)y != null);
            System.Diagnostics.Debug.Assert((object)x != (object)y);
            System.Diagnostics.Debug.Assert(x.Kind == y.Kind);

            code = ErrorCode.Void;

            ImmutableArray<TypeWithAnnotations> xParameterTypes;
            ImmutableArray<TypeWithAnnotations> yParameterTypes;
            ImmutableArray<RefKind> xRefKinds;
            ImmutableArray<RefKind> yRefKinds;
            switch (x.Kind)
            {
                case SymbolKind.Method:
                    var mX = (MethodSymbol)x;
                    xParameterTypes = mX.ParameterTypesWithAnnotations;
                    xRefKinds = mX.ParameterRefKinds;

                    var mY = (MethodSymbol)y;
                    yParameterTypes = mY.ParameterTypesWithAnnotations;
                    yRefKinds = mY.ParameterRefKinds;
                    break;
                case SymbolKind.Property:
                    var pX = (PropertySymbol)x;
                    xParameterTypes = pX.ParameterTypesWithAnnotations;
                    xRefKinds = pX.ParameterRefKinds;

                    var pY = (PropertySymbol)y;
                    yParameterTypes = pY.ParameterTypesWithAnnotations;
                    yRefKinds = pY.ParameterRefKinds;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(x.Kind);
            }

            int numParams = xParameterTypes.Length;

            if (yParameterTypes.Length != numParams)
            {
                return false;
            }

            // Compare parameters without regard for RefKind (or other modifier),
            // array rank, or unnamed array element types (e.g. int[][] == char[][]).

            bool sawRefKindDifference = xRefKinds.IsDefault != yRefKinds.IsDefault;
            bool sawArrayRankDifference = false;
            bool sawArrayOfArraysDifference = false;

            for (int i = 0; i < numParams; i++)
            {
                TypeSymbol xType = xParameterTypes[i].Type;
                TypeSymbol yType = yParameterTypes[i].Type;

                TypeKind typeKind = xType.TypeKind;
                if (yType.TypeKind != typeKind)
                {
                    return false;
                }

                if (typeKind == TypeKind.Array)
                {
                    ArrayTypeSymbol xArrayType = (ArrayTypeSymbol)xType;
                    ArrayTypeSymbol yArrayType = (ArrayTypeSymbol)yType;

                    sawArrayRankDifference = sawArrayRankDifference || xArrayType.Rank != yArrayType.Rank;

                    bool elementTypesDiffer = !TypeSymbol.Equals(xArrayType.ElementType, yArrayType.ElementType, TypeCompareKind.ConsiderEverything2);

                    // You might expect that only unnamed-vs-unnamed would produce a warning, but
                    // dev11 reports unnamed-vs-anything.
                    if (IsArrayOfArrays(xArrayType) || IsArrayOfArrays(yArrayType))
                    {
                        sawArrayOfArraysDifference = sawArrayOfArraysDifference || elementTypesDiffer;
                    }
                    else if (elementTypesDiffer)
                    {
                        return false;
                    }
                }
                else if (!TypeSymbol.Equals(xType, yType, TypeCompareKind.ConsiderEverything2))
                {
                    return false;
                }

                if (!xRefKinds.IsDefault)
                {
                    sawRefKindDifference = sawRefKindDifference || xRefKinds[i] != yRefKinds[i];
                }
            }

            code =
                sawArrayOfArraysDifference ? ErrorCode.WRN_CLS_OverloadUnnamed :
                sawArrayRankDifference ? ErrorCode.WRN_CLS_OverloadRefOut : // Lumping rank difference with refkind is odd, but matches dev11.
                sawRefKindDifference ? ErrorCode.WRN_CLS_OverloadRefOut :
                ErrorCode.Void;

            return code != ErrorCode.Void;
        }

        private static bool IsArrayOfArrays(ArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType.Kind == SymbolKind.ArrayType;
        }
    }
}
