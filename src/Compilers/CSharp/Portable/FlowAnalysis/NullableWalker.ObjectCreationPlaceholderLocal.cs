// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class NullableWalker
    {
        /// <summary>
        /// A symbol to represent a placeholder for an instance being constructed by
        /// <see cref="BoundObjectCreationExpression"/>. It is used to track the state 
        /// of members being initialized.
        /// </summary>
        private sealed class ObjectCreationPlaceholderLocal : LocalSymbol
        {
            private readonly Symbol _containingSymbol;
            private readonly TypeSymbolWithAnnotations _type;
            private readonly BoundExpression _objectCreationExpression;

            public ObjectCreationPlaceholderLocal(Symbol containingSymbol, BoundExpression objectCreationExpression)
            {
                _containingSymbol = containingSymbol;
                _type = TypeSymbolWithAnnotations.Create(objectCreationExpression.Type, NullableAnnotation.NotNullable);
                _objectCreationExpression = objectCreationExpression;
            }

            public override bool Equals(object obj)
            {
                if ((object)this == obj)
                {
                    return true;
                }

                var other = obj as ObjectCreationPlaceholderLocal;

                return (object)other != null && (object)_objectCreationExpression == other._objectCreationExpression;
            }

            public override int GetHashCode()
            {
                return _objectCreationExpression.GetHashCode();
            }

            internal override SyntaxNode ScopeDesignatorOpt
            {
                get
                {
                    return null;
                }
            }

            public override Symbol ContainingSymbol
            {
                get
                {
                    return _containingSymbol;
                }
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            {
                get
                {
                    return ImmutableArray<SyntaxReference>.Empty;
                }
            }

            public override ImmutableArray<Location> Locations
            {
                get
                {
                    return ImmutableArray<Location>.Empty;
                }
            }

            public override TypeSymbolWithAnnotations Type
            {
                get
                {
                    return _type;
                }
            }

            internal override LocalDeclarationKind DeclarationKind
            {
                get
                {
                    return LocalDeclarationKind.None;
                }
            }

            internal override SyntaxToken IdentifierToken
            {
                get
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            internal override bool IsCompilerGenerated
            {
                get
                {
                    return true;
                }
            }

            internal override bool IsImportedFromMetadata
            {
                get
                {
                    return false;
                }
            }

            internal override bool IsPinned
            {
                get
                {
                    return false;
                }
            }

            public override RefKind RefKind
            {
                get
                {
                    return RefKind.None;
                }
            }

            internal override SynthesizedLocalKind SynthesizedKind
            {
                get
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            internal override ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics = null)
            {
                return null;
            }

            internal override ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            internal override SyntaxNode GetDeclaratorSyntax()
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax)
            {
                throw ExceptionUtilities.Unreachable;
            }

            internal override uint ValEscapeScope => throw ExceptionUtilities.Unreachable;

            internal override uint RefEscapeScope => throw ExceptionUtilities.Unreachable;
        }
    }
}
