// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedSimpleProgramEntryPointSymbol : SourceMemberMethodSymbol
    {
        /// <summary>
        /// The corresponding <see cref="SingleTypeDeclaration"/>. 
        /// </summary>
        private readonly SingleTypeDeclaration _declaration;

        private readonly TypeSymbol _returnType;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private WeakReference<ExecutableCodeBinder>? _weakBodyBinder;
        private WeakReference<ExecutableCodeBinder>? _weakIgnoreAccessibilityBodyBinder;

        internal SynthesizedSimpleProgramEntryPointSymbol(SourceMemberContainerTypeSymbol containingType, SingleTypeDeclaration declaration, BindingDiagnosticBag diagnostics)
            : base(containingType, syntaxReferenceOpt: declaration.SyntaxReference, declaration.SyntaxReference.GetLocation(), isIterator: declaration.IsIterator,
                   MakeModifiersAndFlags(containingType, declaration))
        {
            Debug.Assert(declaration.SyntaxReference.GetSyntax() is CompilationUnitSyntax);
            _declaration = declaration;

            bool hasAwait = declaration.HasAwaitExpressions;
            bool hasReturnWithExpression = declaration.HasReturnWithExpression;

            CSharpCompilation compilation = containingType.DeclaringCompilation;
            switch (hasAwait, hasReturnWithExpression)
            {
                case (true, false):
                    _returnType = Binder.GetWellKnownType(compilation, WellKnownType.System_Threading_Tasks_Task, diagnostics, NoLocation.Singleton);
                    break;
                case (false, false):
                    _returnType = Binder.GetSpecialType(compilation, SpecialType.System_Void, NoLocation.Singleton, diagnostics);
                    break;
                case (true, true):
                    _returnType = Binder.GetWellKnownType(compilation, WellKnownType.System_Threading_Tasks_Task_T, diagnostics, NoLocation.Singleton).
                                      Construct(Binder.GetSpecialType(compilation, SpecialType.System_Int32, NoLocation.Singleton, diagnostics));
                    break;
                case (false, true):
                    _returnType = Binder.GetSpecialType(compilation, SpecialType.System_Int32, NoLocation.Singleton, diagnostics);
                    break;
            }

            _parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(this,
                              TypeWithAnnotations.Create(
                                  ArrayTypeSymbol.CreateCSharpArray(compilation.Assembly,
                                      TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_String, NoLocation.Singleton, diagnostics)))), 0, RefKind.None, "args"));
        }

        private static (DeclarationModifiers, Flags) MakeModifiersAndFlags(SourceMemberContainerTypeSymbol containingType, SingleTypeDeclaration declaration)
        {
            bool hasAwait = declaration.HasAwaitExpressions;
            bool hasReturnWithExpression = declaration.HasReturnWithExpression;

            DeclarationModifiers declarationModifiers = DeclarationModifiers.Static | DeclarationModifiers.Private | (hasAwait ? DeclarationModifiers.Async : DeclarationModifiers.None);
            CSharpCompilation compilation = containingType.DeclaringCompilation;
            var compilationUnit = (CompilationUnitSyntax)declaration.SyntaxReference.GetSyntax();
            bool isNullableAnalysisEnabled = IsNullableAnalysisEnabled(compilation, compilationUnit);
            Flags flags = MakeFlags(
                                    MethodKind.Ordinary,
                                    RefKind.None,
                                    declarationModifiers,
                                    returnsVoid: !hasAwait && !hasReturnWithExpression,
                                    returnsVoidIsSet: true,
                                    isExpressionBodied: false,
                                    isExtensionMethod: false,
                                    isNullableAnalysisEnabled: isNullableAnalysisEnabled,
                                    isVarArg: false,
                                    isExplicitInterfaceImplementation: false,
                                    hasThisInitializer: false);

            return (declarationModifiers, flags);
        }

        internal static SynthesizedSimpleProgramEntryPointSymbol? GetSimpleProgramEntryPoint(CSharpCompilation compilation, CompilationUnitSyntax compilationUnit, bool fallbackToMainEntryPoint)
        {
            var type = GetSimpleProgramNamedTypeSymbol(compilation);
            if (type is null)
            {
                return null;
            }

            ImmutableArray<SynthesizedSimpleProgramEntryPointSymbol> entryPoints = type.GetSimpleProgramEntryPoints();

            foreach (var entryPoint in entryPoints)
            {
                if (entryPoint.SyntaxTree == compilationUnit.SyntaxTree && entryPoint.SyntaxNode == compilationUnit)
                {
                    return entryPoint;
                }
            }

            return fallbackToMainEntryPoint ? entryPoints[0] : null;
        }

        internal static SynthesizedSimpleProgramEntryPointSymbol? GetSimpleProgramEntryPoint(CSharpCompilation compilation)
        {
            return GetSimpleProgramNamedTypeSymbol(compilation)?.GetSimpleProgramEntryPoints().First();
        }

        private static SourceNamedTypeSymbol? GetSimpleProgramNamedTypeSymbol(CSharpCompilation compilation)
        {
            return compilation.SourceModule.GlobalNamespace.GetTypeMembers(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName).OfType<SourceNamedTypeSymbol>().SingleOrDefault(s => s.IsSimpleProgram);
        }

        public override string Name
        {
            get
            {
                return WellKnownMemberNames.TopLevelStatementsEntryPointMethodName;
            }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        internal override int ParameterCount
        {
            get
            {
                return 1;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                return _parameters;
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                return TypeWithAnnotations.Create(_returnType);
            }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return false;
            }
        }

        internal sealed override bool GenerateDebugInfo
        {
            get
            {
                return true;
            }
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return localPosition;
        }

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
        }

        public override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            => ImmutableArray<TypeParameterConstraintKind>.Empty;

        protected override object MethodChecksLockObject => _declaration;

        internal CompilationUnitSyntax CompilationUnit => (CompilationUnitSyntax)SyntaxNode;

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            return GetBodyBinder(ignoreAccessibility);
        }

        private ExecutableCodeBinder CreateBodyBinder(bool ignoreAccessibility)
        {
            CSharpCompilation compilation = DeclaringCompilation;

            var syntaxNode = SyntaxNode;
            Binder result = new BuckStopsHereBinder(compilation, FileIdentifier.Create(syntaxNode.SyntaxTree, compilation.Options.SourceReferenceResolver));
            var globalNamespace = compilation.GlobalNamespace;
            var declaringSymbol = (SourceNamespaceSymbol)compilation.SourceModule.GlobalNamespace;
            result = WithExternAndUsingAliasesBinder.Create(declaringSymbol, syntaxNode, WithUsingNamespacesAndTypesBinder.Create(declaringSymbol, syntaxNode, result));
            result = new InContainerBinder(globalNamespace, result);
            result = new InContainerBinder(ContainingType, result);
            result = new InMethodBinder(this, result);
            result = result.WithAdditionalFlags(ignoreAccessibility ? BinderFlags.IgnoreAccessibility : BinderFlags.None);

            return new ExecutableCodeBinder(syntaxNode, this, result);
        }

        internal ExecutableCodeBinder GetBodyBinder(bool ignoreAccessibility)
        {
            ref WeakReference<ExecutableCodeBinder>? weakBinder = ref ignoreAccessibility ? ref _weakIgnoreAccessibilityBodyBinder : ref _weakBodyBinder;

            while (true)
            {
                var previousWeakReference = weakBinder;
                if (previousWeakReference != null && previousWeakReference.TryGetTarget(out ExecutableCodeBinder? previousBinder))
                {
                    return previousBinder;
                }

                ExecutableCodeBinder newBinder = CreateBodyBinder(ignoreAccessibility);
                if (Interlocked.CompareExchange(ref weakBinder, new WeakReference<ExecutableCodeBinder>(newBinder), previousWeakReference) == previousWeakReference)
                {
                    return newBinder;
                }
            }
        }

        public override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken)
        {
            if (_declaration.SyntaxReference.SyntaxTree == tree)
            {
                if (!definedWithinSpan.HasValue)
                {
                    return true;
                }
                else
                {
                    var span = definedWithinSpan.GetValueOrDefault();

                    foreach (var global in ((CompilationUnitSyntax)tree.GetRoot(cancellationToken)).Members.OfType<GlobalStatementSyntax>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (global.Span.IntersectsWith(span))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public SyntaxNode ReturnTypeSyntax => CompilationUnit.Members.First(m => m.Kind() == SyntaxKind.GlobalStatement);

        private static bool IsNullableAnalysisEnabled(CSharpCompilation compilation, CompilationUnitSyntax syntax)
        {
            foreach (var member in syntax.Members)
            {
                if (member.Kind() == SyntaxKind.GlobalStatement && compilation.IsNullableAnalysisEnabledIn(member))
                {
                    return true;
                }
            }
            return false;
        }

        internal sealed override int? TryGetOverloadResolutionPriority()
        {
            return null;
        }
    }
}
