// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceConstructorSymbolBase : SourceMemberMethodSymbol
    {
        protected ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations _lazyReturnType;

        protected SourceConstructorSymbolBase(
            SourceMemberContainerTypeSymbol containingType,
            Location location,
            CSharpSyntaxNode syntax,
            bool isIterator)
            : base(containingType, syntax.GetReference(), location, isIterator)
        {
            Debug.Assert(syntax.Kind() is SyntaxKind.ConstructorDeclaration or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration or SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration);
        }

        protected sealed override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            var syntax = (CSharpSyntaxNode)syntaxReferenceOpt.GetSyntax();
            var binderFactory = this.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree);
            ParameterListSyntax parameterList = GetParameterList();

            // NOTE: if we asked for the binder for the body of the constructor, we'd risk a stack overflow because
            // we might still be constructing the member list of the containing type.  However, getting the binder
            // for the parameters should be safe.
            var bodyBinder = binderFactory.GetBinder(parameterList, syntax, this).WithContainingMemberOrLambda(this);

            // Constraint checking for parameter and return types must be delayed until
            // the method has been added to the containing type member list since
            // evaluating the constraints may depend on accessing this method from
            // the container (comparing this method to others to find overrides for
            // instance). Constraints are checked in AfterAddingTypeMembersChecks.
            var signatureBinder = bodyBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

            _lazyParameters = ParameterHelpers.MakeParameters(
                signatureBinder, this, parameterList, out _,
                allowRefOrOut: AllowRefOrOut,
                allowThis: false,
                addRefReadOnlyModifier: false,
                diagnostics: diagnostics).Cast<SourceParameterSymbol, ParameterSymbol>();

            _lazyReturnType = TypeWithAnnotations.Create(bodyBinder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax));

            var location = this.GetFirstLocation();
            // Don't report ERR_StaticConstParam if the ctor symbol name doesn't match the containing type name.
            // This avoids extra unnecessary errors.
            // There will already be a diagnostic saying Method must have a return type.
            if (MethodKind == MethodKind.StaticConstructor && (_lazyParameters.Length != 0) &&
                ContainingType.Name == ((ConstructorDeclarationSyntax)this.SyntaxNode).Identifier.ValueText)
            {
                diagnostics.Add(ErrorCode.ERR_StaticConstParam, location, this);
            }

            this.CheckEffectiveAccessibility(_lazyReturnType, _lazyParameters, diagnostics);
            this.CheckFileTypeUsage(_lazyReturnType, _lazyParameters, diagnostics);

            if (this.IsVararg && (IsGenericMethod || ContainingType.IsGenericType || _lazyParameters.Length > 0 && _lazyParameters[_lazyParameters.Length - 1].IsParams))
            {
                diagnostics.Add(ErrorCode.ERR_BadVarargs, location);
            }
        }

#nullable enable
        protected abstract ParameterListSyntax GetParameterList();

        protected abstract bool AllowRefOrOut { get; }
#nullable disable

        internal sealed override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            var compilation = DeclaringCompilation;
            ParameterHelpers.EnsureIsReadOnlyAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);
            ParameterHelpers.EnsureNativeIntegerAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);
            ParameterHelpers.EnsureScopedRefAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);
            ParameterHelpers.EnsureNullableAttributeExists(compilation, this, Parameters, diagnostics, modifyCompilation: true);
            ParameterHelpers.EnsureRequiresLocationAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true, moduleBuilder: null);

            foreach (var parameter in this.Parameters)
            {
                parameter.Type.CheckAllConstraints(compilation, conversions, parameter.GetFirstLocation(), diagnostics);
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return base.IsImplicitlyDeclared;
            }
        }

        internal sealed override int ParameterCount
        {
            get
            {
                if (!_lazyParameters.IsDefault)
                {
                    return _lazyParameters.Length;
                }

                return GetParameterList().ParameterCount;
            }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                LazyMethodChecks();
                return _lazyParameters;
            }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public sealed override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public sealed override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            => ImmutableArray<TypeParameterConstraintKind>.Empty;

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        public sealed override string Name
        {
            get { return this.IsStatic ? WellKnownMemberNames.StaticConstructorName : WellKnownMemberNames.InstanceConstructorName; }
        }

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetReturnTypeAttributeDeclarations()
        {
            // constructors can't have return type attributes
            return OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));
        }

        internal sealed override bool GenerateDebugInfo
        {
            get { return true; }
        }

        internal sealed override int CalculateLocalSyntaxOffset(int position, SyntaxTree tree)
        {
            Debug.Assert(position >= 0 && tree != null);

            TextSpan span;

            // local/lambda/closure defined within the body of the constructor:
            var ctorSyntax = (CSharpSyntaxNode)syntaxReferenceOpt.GetSyntax();
            if (tree == ctorSyntax.SyntaxTree)
            {
                if (IsWithinExpressionOrBlockBody(position, out int offset))
                {
                    return offset;
                }

                // closure in ctor initializer lifting its parameter(s) spans the constructor declaration:
                if (position == ctorSyntax.SpanStart)
                {
                    // Use a constant that is distinct from any other syntax offset.
                    // -1 works since a field initializer and a constructor declaration header can't squeeze into a single character.
                    return -1;
                }
            }

            // lambdas in ctor initializer:
            int ctorInitializerLength;
            var ctorInitializer = GetInitializer();
            if (tree == ctorInitializer?.SyntaxTree)
            {
                span = ctorInitializer.Span;
                ctorInitializerLength = span.Length;

                if (span.Contains(position))
                {
                    return -ctorInitializerLength + (position - span.Start);
                }
            }
            else
            {
                ctorInitializerLength = 0;
            }

            // lambdas in field/property initializers:
            int syntaxOffset;
            var containingType = (SourceNamedTypeSymbol)this.ContainingType;
            if (containingType.TryCalculateSyntaxOffsetOfPositionInInitializer(position, tree, this.IsStatic, ctorInitializerLength, out syntaxOffset))
            {
                return syntaxOffset;
            }

            // we haven't found the constructor part that declares the variable:
            throw ExceptionUtilities.Unreachable();
        }

        internal abstract override bool IsNullableAnalysisEnabled();

        protected abstract CSharpSyntaxNode GetInitializer();

        protected abstract bool IsWithinExpressionOrBlockBody(int position, out int offset);

#nullable enable
        protected sealed override bool HasSetsRequiredMembersImpl
            => GetEarlyDecodedWellKnownAttributeData()?.HasSetsRequiredMembersAttribute == true;

        internal override (CSharpAttributeData?, BoundAttribute?) EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            if (arguments.SymbolPart == AttributeLocation.None)
            {
                if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.SetsRequiredMembersAttribute))
                {
                    var earlyData = arguments.GetOrCreateData<MethodEarlyWellKnownAttributeData>();
                    earlyData.HasSetsRequiredMembersAttribute = true;

                    if (ContainingType.IsWellKnownSetsRequiredMembersAttribute())
                    {
                        // Avoid a binding cycle for this scenario.
                        return (null, null);
                    }

                    var (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out bool hasAnyDiagnostics);

                    if (!hasAnyDiagnostics)
                    {
                        return (attributeData, boundAttribute);
                    }
                    else
                    {
                        return (null, null);
                    }
                }
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            AddRequiredMembersMarkerAttributes(ref attributes, this);
        }
    }
}
