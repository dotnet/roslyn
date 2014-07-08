// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class TypeSubstitutedLocalSymbol : LocalSymbol
    {
        private readonly LocalSymbol originalVariable;
        private readonly TypeSymbol type;
        private readonly Symbol containingSymbol;

        public TypeSubstitutedLocalSymbol(LocalSymbol originalVariable, TypeSymbol type, Symbol containingSymbol)
        {
            Debug.Assert(originalVariable != null);
            Debug.Assert(type != null);
            Debug.Assert(containingSymbol != null);
                
            this.originalVariable = originalVariable;
            this.type = type;
            this.containingSymbol = containingSymbol;
        }

        public override string Name
        {
            get { return originalVariable.Name; }
        }

        public override Symbol ContainingSymbol
        {
            get { return containingSymbol; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return originalVariable.DeclaringSyntaxReferences; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return originalVariable.Locations; }
        }

        public override TypeSymbol Type
        {
            get { return type; }
        }

        internal override LocalDeclarationKind DeclarationKind
        {
            get { return originalVariable.DeclarationKind; }
        }

        internal override SyntaxToken IdentifierToken
        {
            get { return originalVariable.IdentifierToken; }
        }

        internal override bool IsCompilerGenerated
        {
            get { return originalVariable.IsCompilerGenerated; }
        }

        internal override bool IsPinned
        {
            get { return originalVariable.IsPinned; }
        }

        internal override RefKind RefKind
        {
            get { return originalVariable.RefKind; }
        }

        internal override SynthesizedLocalKind SynthesizedLocalKind
        {
            get { return originalVariable.SynthesizedLocalKind; }
        }

        internal override ConstantValue GetConstantValue(LocalSymbol inProgress)
        {
            return originalVariable.GetConstantValue(inProgress);
        }

        internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
        {
            return originalVariable.GetConstantValueDiagnostics(boundInitValue);
        }
    }
}
