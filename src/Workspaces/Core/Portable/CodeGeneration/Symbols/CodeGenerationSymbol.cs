// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    internal abstract class CodeGenerationSymbol : ISymbol
    {
        protected static ConditionalWeakTable<CodeGenerationSymbol, SyntaxAnnotation[]> annotationsTable =
            new ConditionalWeakTable<CodeGenerationSymbol, SyntaxAnnotation[]>();

        private ImmutableArray<AttributeData> _attributes;

        public Accessibility DeclaredAccessibility { get; }
        protected internal DeclarationModifiers Modifiers { get; }
        public string Name { get; }
        public INamedTypeSymbol ContainingType { get; protected set; }

        protected CodeGenerationSymbol(
            INamedTypeSymbol containingType,
            ImmutableArray<AttributeData> attributes,
            Accessibility declaredAccessibility,
            DeclarationModifiers modifiers,
            string name)
        {
            this.ContainingType = containingType;
            _attributes = attributes.NullToEmpty();
            this.DeclaredAccessibility = declaredAccessibility;
            this.Modifiers = modifiers;
            this.Name = name;
        }

        protected abstract CodeGenerationSymbol Clone();

        internal SyntaxAnnotation[] GetAnnotations()
        {
            annotationsTable.TryGetValue(this, out var annotations);
            return annotations ?? Array.Empty<SyntaxAnnotation>();
        }

        internal CodeGenerationSymbol WithAdditionalAnnotations(params SyntaxAnnotation[] annotations)
        {
            return annotations.IsNullOrEmpty()
                ? this
                : AddAnnotationsTo(this, this.Clone(), annotations);
        }

        private CodeGenerationSymbol AddAnnotationsTo(
            CodeGenerationSymbol originalDefinition, CodeGenerationSymbol newDefinition, SyntaxAnnotation[] annotations)
        {
            annotationsTable.TryGetValue(originalDefinition, out var originalAnnotations);

            annotations = SyntaxAnnotationExtensions.CombineAnnotations(originalAnnotations, annotations);
            annotationsTable.Add(newDefinition, annotations);

            return newDefinition;
        }

        public abstract SymbolKind Kind { get; }

        public string Language => "Code Generation Agnostic Language";

        public ISymbol ContainingSymbol => null;

        public IAssemblySymbol ContainingAssembly => null;

        public IMethodSymbol ContainingMethod => null;

        public IModuleSymbol ContainingModule => null;

        public INamespaceSymbol ContainingNamespace => null;

        public bool IsDefinition => true;

        public bool IsStatic
        {
            get
            {
                return this.Modifiers.IsStatic;
            }
        }

        public bool IsVirtual
        {
            get
            {
                return this.Modifiers.IsVirtual;
            }
        }

        public bool IsOverride
        {
            get
            {
                return this.Modifiers.IsOverride;
            }
        }

        public bool IsAbstract
        {
            get
            {
                return this.Modifiers.IsAbstract;
            }
        }

        public bool IsSealed
        {
            get
            {
                return this.Modifiers.IsSealed;
            }
        }

        public bool IsExtern => false;

        public bool IsImplicitlyDeclared => false;

        public bool CanBeReferencedByName => true;

        public ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create<Location>();
            }
        }

        public ImmutableArray<SyntaxNode> DeclaringSyntaxNodes
        {
            get
            {
                return ImmutableArray.Create<SyntaxNode>();
            }
        }

        public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create<SyntaxReference>();
            }
        }

        public ImmutableArray<AttributeData> GetAttributes()
        {
            return _attributes;
        }

        public ImmutableArray<AttributeData> GetAttributes(INamedTypeSymbol attributeType)
        {
            return GetAttributes().WhereAsArray(a => a.AttributeClass.Equals(attributeType));
        }

        public ImmutableArray<AttributeData> GetAttributes(IMethodSymbol attributeConstructor)
        {
            return GetAttributes().WhereAsArray(a => a.AttributeConstructor.Equals(attributeConstructor));
        }

        public ISymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        public abstract void Accept(SymbolVisitor visitor);

        public abstract TResult Accept<TResult>(SymbolVisitor<TResult> visitor);

        public string GetDocumentationCommentId()
        {
            return null;
        }

        public string GetDocumentationCommentXml(
            CultureInfo preferredCulture,
            bool expandIncludes,
            CancellationToken cancellationToken)
        {
            return "";
        }

        public string ToDisplayString(SymbolDisplayFormat format = null)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
        {
            throw new NotImplementedException();
        }

        public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
        {
            throw new NotImplementedException();
        }

        public virtual string MetadataName
        {
            get
            {
                return this.Name;
            }
        }

        public bool HasUnsupportedMetadata => false;

        public bool Equals(ISymbol other)
        {
            return this.Equals((object)other);
        }

        public bool Equals(ISymbol other, SymbolEqualityComparer equalityComparer)
        {
            return equalityComparer.Equals(this, other);
        }
    }
}
