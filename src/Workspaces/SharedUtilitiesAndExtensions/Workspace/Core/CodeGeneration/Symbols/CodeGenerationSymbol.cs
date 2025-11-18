// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal abstract class CodeGenerationSymbol : ISymbol
{
    protected static ConditionalWeakTable<CodeGenerationSymbol, SyntaxAnnotation[]> annotationsTable = new();

    private readonly ImmutableArray<AttributeData> _attributes;
    protected readonly string _documentationCommentXml;

    public Accessibility DeclaredAccessibility { get; }
    protected internal DeclarationModifiers Modifiers { get; }
    public string Name { get; }
    public INamedTypeSymbol ContainingType { get; protected set; }

    protected CodeGenerationSymbol(
        IAssemblySymbol containingAssembly,
        INamedTypeSymbol containingType,
        ImmutableArray<AttributeData> attributes,
        Accessibility declaredAccessibility,
        DeclarationModifiers modifiers,
        string name,
        string documentationCommentXml = null)
    {
        this.ContainingAssembly = containingAssembly;
        this.ContainingType = containingType;
        _attributes = attributes.NullToEmpty();
        this.DeclaredAccessibility = declaredAccessibility;
        this.Modifiers = modifiers;
        this.Name = name;
        _documentationCommentXml = documentationCommentXml;
    }

    protected abstract CodeGenerationSymbol Clone();

    internal SyntaxAnnotation[] GetAnnotations()
    {
        annotationsTable.TryGetValue(this, out var annotations);
        return annotations ?? [];
    }

    internal CodeGenerationSymbol WithAdditionalAnnotations(params SyntaxAnnotation[] annotations)
    {
        return annotations.IsNullOrEmpty()
            ? this
            : AddAnnotationsTo(this, this.Clone(), annotations);
    }

    private static CodeGenerationSymbol AddAnnotationsTo(
        CodeGenerationSymbol originalDefinition, CodeGenerationSymbol newDefinition, SyntaxAnnotation[] annotations)
    {
        annotationsTable.TryGetValue(originalDefinition, out var originalAnnotations);

        annotations = SyntaxAnnotationExtensions.CombineAnnotations(originalAnnotations, annotations);
        annotationsTable.Add(newDefinition, annotations);

        return newDefinition;
    }

    public abstract SymbolKind Kind { get; }

    public string Language => "Code Generation Agnostic Language";

    public virtual ISymbol ContainingSymbol => null;

    public IAssemblySymbol ContainingAssembly { get; }

    public static IMethodSymbol ContainingMethod => null;

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
            return [];
        }
    }

    public static ImmutableArray<SyntaxNode> DeclaringSyntaxNodes
    {
        get
        {
            return [];
        }
    }

    public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
    {
        get
        {
            return [];
        }
    }

    public ImmutableArray<AttributeData> GetAttributes()
        => _attributes;

    public ImmutableArray<AttributeData> GetAttributes(INamedTypeSymbol attributeType)
        => GetAttributes().WhereAsArray(a => a.AttributeClass.Equals(attributeType));

    public ImmutableArray<AttributeData> GetAttributes(IMethodSymbol attributeConstructor)
        => GetAttributes().WhereAsArray(a => a.AttributeConstructor.Equals(attributeConstructor));

    public ISymbol OriginalDefinition
    {
        get
        {
            return this;
        }
    }

    public abstract void Accept(SymbolVisitor visitor);

    public abstract TResult Accept<TResult>(SymbolVisitor<TResult> visitor);

    public abstract TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument);

    public string GetDocumentationCommentId()
        => null;

    public string GetDocumentationCommentXml(
        CultureInfo preferredCulture,
        bool expandIncludes,
        CancellationToken cancellationToken)
    {
        return _documentationCommentXml ?? "";
    }

    public string ToDisplayString(SymbolDisplayFormat format = null)
        => throw new NotImplementedException();

    public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
        => throw new NotImplementedException();

    public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
        => throw new NotImplementedException();

    public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
        => throw new NotImplementedException();

    public virtual string MetadataName
    {
        get
        {
            return this.Name;
        }
    }

    public int MetadataToken => 0;

    public bool HasUnsupportedMetadata => false;

    public bool Equals(ISymbol other)
        => this.Equals((object)other);

    public bool Equals(ISymbol other, SymbolEqualityComparer equalityComparer)
        => this.Equals(other);
}
