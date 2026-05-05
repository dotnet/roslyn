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
    internal sealed class SynthesizedUnionCtor : SynthesizedInstanceConstructor
    {
        private readonly int _memberOffset;

        public SynthesizedUnionCtor(
            SourceMemberContainerTypeSymbol containingType,
            Location location,
            TypeWithAnnotations parameterType,
            int memberOffset)
            : base(containingType)
        {
            _memberOffset = memberOffset;
            Parameters = [SynthesizedParameterSymbol.Create(this, parameterType, ordinal: 0, RefKind.None, ParameterSymbol.ValueParameterName)];
            Locations = [location];
        }

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public override ImmutableArray<Location> Locations { get; }

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        internal override void GenerateMethodBodyStatements(SyntheticBoundNodeFactory F, ArrayBuilder<BoundStatement> statements, BindingDiagnosticBag diagnostics)
        {
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
            Debug.Assert(F.Syntax is TypeDeclarationSyntax);
            statements.Add(new BoundSequencePointWithSpan(F.Syntax, statementOpt: null, Locations[0].SourceSpan)); // https://github.com/dotnet/roslyn/issues/82636: Add test coverage and verify debugging experience.
        }

        public static bool IsValidParameterTypeConversion(Conversion c)
        {
            return c.Exists && c.IsImplicit && (c.IsIdentity || c.IsReference || c.IsBoxing);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            Debug.Assert(IsImplicitlyDeclared);
            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            this.Parameters[0].Type.CheckAllConstraints(DeclaringCompilation, conversions, GetFirstLocation(), diagnostics);
        }
    }
}

