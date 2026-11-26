// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedUnionCtor : SourceConstructorSymbolBase
    {
        private readonly int _memberOffset;

        public SynthesizedUnionCtor(
            SourceMemberContainerTypeSymbol containingType,
            ParameterSyntax parameterSyntax,
            int memberOffset)
            : base(containingType, parameterSyntax.Type!.GetLocation(), parameterSyntax, isIterator: false, MakeModifiersAndFlags())
        {
            _memberOffset = memberOffset;
        }

        private static (DeclarationModifiers, Flags) MakeModifiersAndFlags()
        {
            Flags flags = MakeFlags(
                                    MethodKind.Constructor,
                                    RefKind.None,
                                    DeclarationModifiers.Public,
                                    returnsVoid: true,
                                    returnsVoidIsSet: true,
                                    isExpressionBodied: false,
                                    isExtensionMethod: false,
                                    isVarArg: false,
                                    isNullableAnalysisEnabled: false,
                                    isExplicitInterfaceImplementation: false,
                                    hasThisInitializer: false);

            return (DeclarationModifiers.Public, flags);
        }

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        internal void GenerateMethodBodyStatements(SyntheticBoundNodeFactory F, ArrayBuilder<BoundStatement> statements)
        {
            var parameterSyntax = (ParameterSyntax)SyntaxNode;
            Debug.Assert(parameterSyntax.Type is not null);

            // Write an assignment to Value property
            // {
            //     this._ValueBackingField = parameter
            // }
            var valueProperty = ContainingType.GetMembers(WellKnownMemberNames.ValuePropertyName).OfType<SynthesizedUnionValuePropertySymbol>().Single();
            Debug.Assert(valueProperty.DeclaredBackingField is not null);
            BoundParameter parameter = F.Parameter(Parameters[0]);

            var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            Conversion c = F.Compilation.Conversions.ClassifyImplicitConversionFromType(parameter.Type, valueProperty.Type, ref useSiteInfo);

            if (IsValidParameterTypeConversion(c))
            {
                statements.Add(F.Assignment(F.Field(F.This(), valueProperty.DeclaredBackingField), F.Convert(valueProperty.Type, parameter, c, explicitCastInCode: false)));
            }
            else
            {
                statements.Add(new BoundNoOpStatement(F.Syntax, NoOpStatementFlavor.Default, hasErrors: true));
            }

            // Add a sequence point at the end of the constructor, so that a breakpoint placed on the case type 
            // can be hit whenever a new instance of the union for that case type is created.
            statements.Add(new BoundSequencePoint(parameterSyntax.Type, statementOpt: null));
        }

        public static bool IsValidParameterTypeConversion(Conversion c)
        {
            return c.Exists && c.IsImplicit && (c.IsIdentity || c.IsReference || c.IsBoxing);
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            TypeSymbol parameterType = this.Parameters[0].Type;

            if (!parameterType.IsErrorType())
            {
                // Check that conversion to object is possible
                CSharpCompilation compilation = DeclaringCompilation;
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, compilation.Assembly);
                Conversion c = compilation.Conversions.ClassifyImplicitConversionFromType(parameterType, compilation.GetSpecialType(SpecialType.System_Object), ref useSiteInfo);
                diagnostics.Add(GetFirstLocation(), useSiteInfo);

                if (!IsValidParameterTypeConversion(c))
                {
                    diagnostics.Add(ErrorCode.ERR_NoImplicitConversionToObject, GetFirstLocation(), parameterType);
                }
            }
        }

        protected override SeparatedSyntaxList<ParameterSyntax> GetParameterList()
        {
            var result = SyntaxFactory.SingletonSeparatedList((ParameterSyntax)SyntaxNode);
            Debug.Assert(result[0] == (object)SyntaxNode);
            return result;
        }

        protected override Binder GetParameterListBinder()
        {
            var syntax = (ParameterSyntax)SyntaxNode;
            var binderFactory = this.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree);
            var signatureBinder = binderFactory.GetBinder((ParameterListSyntax)syntax.Parent!, (TypeDeclarationSyntax)syntax.Parent!.Parent!, ContainingType);
            return signatureBinder;
        }

        internal sealed override ExecutableCodeBinder? TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false) => throw ExceptionUtilities.Unreachable();

        internal override bool HasSafeModifier => false; // Like SourceConstructorSymbol
        internal override bool HasUnsafeModifier => false; // Like SourceConstructorSymbol
        internal override bool CanRequireUnsafe => true; // Like SourceConstructorSymbol

        protected override bool IsWithinExpressionOrBlockBody(int position, out int offset)
        {
            // Like SynthesizedPrimaryConstructor
            offset = -1;
            return false;
        }

        protected override CSharpSyntaxNode? GetInitializer() => null;

        internal override bool IsNullableAnalysisEnabled() => false;

        protected override bool AllowRefOrOut => false;

        public override bool IsImplicitlyDeclared => true;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => [];
    }
}

