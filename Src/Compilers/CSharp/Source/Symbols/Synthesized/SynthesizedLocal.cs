// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// To help diagnose compiler transformations, you can enable the naming of compiler-generated locals
// that would otherwise be anonymous.
//#define NAME_TEMPS

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized local variable.
    /// </summary>
    internal class SynthesizedLocal : LocalSymbol
    {
        private readonly MethodSymbol containingMethod;
        private readonly string name;
        private readonly TypeSymbol type;
        private readonly CSharpSyntaxNode syntax;
        private readonly bool isPinned;
        private readonly LocalDeclarationKind declarationKind;
        private readonly RefKind refKind;
        private readonly TempKind tempKind;

#if NAME_TEMPS
        static int nextDebugTempNumber = 0;
#endif

        internal SynthesizedLocal(
            MethodSymbol containingMethod,
            TypeSymbol type,
            string name = null,
            CSharpSyntaxNode syntax = null,
            bool isPinned = false,
            RefKind refKind = RefKind.None,
            LocalDeclarationKind declarationKind = LocalDeclarationKind.CompilerGenerated,
            TempKind tempKind = TempKind.None)
        {
            this.containingMethod = containingMethod;
            Debug.Assert(type.SpecialType != SpecialType.System_Void);
            Debug.Assert((tempKind == TempKind.None) == (syntax == null));
#if NAME_TEMPS
            if (string.IsNullOrEmpty(name)) name = "temp_" + Interlocked.Increment(ref nextDebugTempNumber);
#endif
            this.name = name;
            this.type = type;
            this.syntax = syntax;
            this.isPinned = isPinned;
            this.declarationKind = declarationKind;
            this.refKind = refKind;
            this.tempKind = tempKind;
        }

        internal override RefKind RefKind
        {
            get { return this.refKind; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return this.declarationKind; }
        }

        internal override TempKind TempKind
        {
            get { return this.tempKind; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { return default(SyntaxToken); }
        }

        public override Symbol ContainingSymbol
        {
            get { return this.containingMethod; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override TypeSymbol Type
        {
            get { return type; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return (this.syntax == null) ? ImmutableArray<Location>.Empty : ImmutableArray.Create(this.syntax.GetLocation()); }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return (this.syntax == null) ? ImmutableArray<SyntaxReference>.Empty : ImmutableArray.Create(this.syntax.GetReference()); }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool IsPinned
        {
            get { return this.isPinned; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return true; }
        }

        internal override ConstantValue GetConstantValue(LocalSymbol inProgress)
        {
            return null;
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return default(ImmutableArray<Diagnostic>);
        }
    }
}