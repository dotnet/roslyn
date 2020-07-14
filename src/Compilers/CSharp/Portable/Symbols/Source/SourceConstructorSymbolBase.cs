// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceConstructorSymbolBase : SourceMemberMethodSymbol
    {
        protected ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations _lazyReturnType;
        private bool _lazyIsVararg;

        protected SourceConstructorSymbolBase(
            SourceMemberContainerTypeSymbol containingType,
            Location location,
            CSharpSyntaxNode syntax)
            : base(containingType, syntax.GetReference(), ImmutableArray.Create(location), SyntaxFacts.HasYieldOperations(syntax))
        {
            Debug.Assert(
                syntax.IsKind(SyntaxKind.ConstructorDeclaration) ||
                syntax.IsKind(SyntaxKind.RecordDeclaration));
        }

        protected sealed override void MethodChecks(DiagnosticBag diagnostics)
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

            SyntaxToken arglistToken;
            _lazyParameters = ParameterHelpers.MakeParameters(
                signatureBinder, this, parameterList, out arglistToken,
                allowRefOrOut: AllowRefOrOut,
                allowThis: false,
                addRefReadOnlyModifier: false,
                diagnostics: diagnostics);

            _lazyIsVararg = (arglistToken.Kind() == SyntaxKind.ArgListKeyword);
            _lazyReturnType = TypeWithAnnotations.Create(bodyBinder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax));

            var location = this.Locations[0];
            if (MethodKind == MethodKind.StaticConstructor && (_lazyParameters.Length != 0))
            {
                diagnostics.Add(ErrorCode.ERR_StaticConstParam, location, this);
            }

            this.CheckEffectiveAccessibility(_lazyReturnType, _lazyParameters, diagnostics);

            if (_lazyIsVararg && (IsGenericMethod || ContainingType.IsGenericType || _lazyParameters.Length > 0 && _lazyParameters[_lazyParameters.Length - 1].IsParams))
            {
                diagnostics.Add(ErrorCode.ERR_BadVarargs, location);
            }
        }

#nullable enable
        protected abstract ParameterListSyntax GetParameterList();

        protected abstract bool AllowRefOrOut { get; }
#nullable restore

        internal sealed override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            var compilation = DeclaringCompilation;
            ParameterHelpers.EnsureIsReadOnlyAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);
            ParameterHelpers.EnsureNativeIntegerAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);
            ParameterHelpers.EnsureNullableAttributeExists(compilation, this, Parameters, diagnostics, modifyCompilation: true);

            foreach (var parameter in this.Parameters)
            {
                parameter.Type.CheckAllConstraints(compilation, conversions, parameter.Locations[0], diagnostics);
            }
        }

        public sealed override bool IsVararg
        {
            get
            {
                LazyMethodChecks();
                return _lazyIsVararg;
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

        public sealed override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses()
            => ImmutableArray<TypeParameterConstraintClause>.Empty;

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

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

        protected sealed override IAttributeTargetSymbol AttributeOwner
        {
            get
            {
                return base.AttributeOwner;
            }
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
            throw ExceptionUtilities.Unreachable;
        }

        protected abstract CSharpSyntaxNode GetInitializer();

        protected abstract bool IsWithinExpressionOrBlockBody(int position, out int offset);
    }
}
