// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents implicitly declared type for a Simple Program feature.
    /// </summary>
    internal sealed class SimpleProgramNamedTypeSymbol : SourceMemberContainerTypeSymbol
    {
        internal const string UnspeakableName = "$Program";

        internal SimpleProgramNamedTypeSymbol(NamespaceSymbol globalNamespace, MergedTypeDeclaration declaration, DiagnosticBag diagnostics)
            : base(globalNamespace, declaration, diagnostics)
        {
            Debug.Assert(globalNamespace.IsGlobalNamespace);
            Debug.Assert(declaration.Kind == DeclarationKind.SimpleProgram);
            Debug.Assert(declaration.Name == UnspeakableName);

            state.NotePartComplete(CompletionPart.EnumUnderlyingType); // No work to do for this.
        }

        internal static SynthesizedSimpleProgramEntryPointSymbol? GetSimpleProgramEntryPoint(CSharpCompilation compilation)
        {
            return (SynthesizedSimpleProgramEntryPointSymbol?)compilation.SourceModule.GlobalNamespace.GetTypeMembers(UnspeakableName).OfType<SimpleProgramNamedTypeSymbol>().SingleOrDefault()?.GetMembersUnordered().Single();
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
            => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            state.NotePartComplete(CompletionPart.Attributes);
            return ImmutableArray<CSharpAttributeData>.Empty;
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return AttributeUsageInfo.Null;
        }

        protected override Location GetCorrespondingBaseListLocation(NamedTypeSymbol @base)
        {
            return NoLocation.Singleton; // No explicit base list
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
            => this.DeclaringCompilation.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object);

        protected override void CheckBase(DiagnosticBag diagnostics)
        {
            // check that System.Object is available. 
            var info = this.DeclaringCompilation.GetSpecialType(SpecialType.System_Object).GetUseSiteDiagnostic();
            if (info != null)
            {
                Symbol.ReportUseSiteDiagnostic(info, diagnostics, Locations[0]);
            }
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
        {
            return BaseTypeNoUseSiteDiagnostics;
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        protected override void CheckInterfaces(DiagnosticBag diagnostics)
        {
            // nop
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public sealed override bool AreLocalsZeroed
        {
            get { return ContainingModule.AreLocalsZeroed; }
        }

        internal override bool IsComImport
        {
            get { return false; }
        }

        internal override NamedTypeSymbol? ComImportCoClass
        {
            get { return null; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return false; }
        }

        internal sealed override bool IsWindowsRuntimeImport
        {
            get { return false; }
        }

        public sealed override bool IsSerializable
        {
            get { return false; }
        }

        internal sealed override TypeLayout Layout
        {
            get { return default(TypeLayout); }
        }

        internal bool HasStructLayoutAttribute
        {
            get { return false; }
        }

        internal override CharSet MarshallingCharSet
        {
            get { return DefaultMarshallingCharSet; }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get { return null; }
        }

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        protected override MembersAndInitializers BuildMembersAndInitializers(DiagnosticBag diagnostics)
        {
            return new MembersAndInitializers(nonTypeNonIndexerMembers: ImmutableArray.Create<Symbol>(new SynthesizedSimpleProgramEntryPointSymbol(this, declaration, diagnostics)),
                                              staticInitializers: ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>>.Empty,
                                              instanceInitializers: ImmutableArray<ImmutableArray<FieldOrPropertyInitializer>>.Empty,
                                              indexerDeclarations: ImmutableArray<SyntaxReference>.Empty,
                                              staticInitializersSyntaxLength: 0,
                                              instanceInitializersSyntaxLength: 0);
        }

        public override bool IsImplicitlyDeclared => true;
    }
}
