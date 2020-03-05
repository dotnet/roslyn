﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents implicit, script and submission classes.
    /// </summary>
    internal sealed class ImplicitNamedTypeSymbol : SourceMemberContainerTypeSymbol
    {
        internal ImplicitNamedTypeSymbol(NamespaceOrTypeSymbol containingSymbol, MergedTypeDeclaration declaration, DiagnosticBag diagnostics)
            : base(containingSymbol, declaration, diagnostics)
        {
            Debug.Assert(declaration.Kind == DeclarationKind.ImplicitClass ||
                         declaration.Kind == DeclarationKind.Submission ||
                         declaration.Kind == DeclarationKind.Script);

            state.NotePartComplete(CompletionPart.EnumUnderlyingType); // No work to do for this.
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
            // A script class may implement interfaces in hosted scenarios.
            // The interface definitions are specified via API, not in compilation source.
            return NoLocation.Singleton;
        }

        /// <summary>
        /// Returns null for a submission class.
        /// This ensures that a submission class does not inherit methods such as ToString or GetHashCode.
        /// </summary>
        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
            => IsScriptClass ? null : this.DeclaringCompilation.GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object);

        protected override void CheckBase(DiagnosticBag diagnostics)
        {
            // check that System.Object is available. 
            // Although submission semantically doesn't have a base class we need to emit one.
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
            get { return ContainingType?.AreLocalsZeroed ?? ContainingModule.AreLocalsZeroed; }
        }

        internal override bool IsComImport
        {
            get { return false; }
        }

        internal override NamedTypeSymbol ComImportCoClass
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

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;
    }
}
