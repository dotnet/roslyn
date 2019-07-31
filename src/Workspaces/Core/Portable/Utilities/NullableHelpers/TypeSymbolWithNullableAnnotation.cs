using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal static partial class NullableExtensions
    {
        [DebuggerDisplay("{WrappedSymbol,nq}, Nullability = {Nullability,nq}")]
        private abstract class TypeSymbolWithNullableAnnotation : ITypeSymbol
        {
            internal ITypeSymbol WrappedSymbol { get; }
            internal NullableAnnotation Nullability { get; }

            protected TypeSymbolWithNullableAnnotation(ITypeSymbol wrappedSymbol, NullableAnnotation nullability)
            {
                Debug.Assert(!(wrappedSymbol is TypeSymbolWithNullableAnnotation));

                WrappedSymbol = wrappedSymbol;
                Nullability = nullability;
            }

            bool IEquatable<ISymbol>.Equals(ISymbol other)
            {
                return Equals(other, SymbolEqualityComparer.Default);
            }

            public bool Equals(ISymbol other, SymbolEqualityComparer equalityComparer)
            {
                if (other is TypeSymbolWithNullableAnnotation otherWrappingSymbol)
                {
                    return this.Nullability == otherWrappingSymbol.Nullability &&
                           this.WrappedSymbol.Equals(otherWrappingSymbol.WrappedSymbol, equalityComparer);
                }
                else if (other is ITypeSymbol)
                {
                    // Somebody is trying to compare a nullable-wrapped symbol with a regular compiler symbol. By rule Equals must be reflexive,
                    // and since the compiler's Equals won't respect us as being equal, we can't return anything other than false. Flagging this with an assert
                    // is helpful while moving features over, because this comparison might be the reason a feature isn't working right. However, for now disabling
                    // the assert is easiest because we can't update the whole codebase at once. Enabling the assert is tracked in https://github.com/dotnet/roslyn/issues/36045.

                    // Debug.Fail($"A {nameof(TypeSymbolWithNullableAnnotation)} was compared to a regular symbol. This comparison is disallowed.");

                    // We are also going to cheat further: for now, we'll just throw away nullability and compare, because if a core feature (like the type inferrerr) is updated
                    // but other features aren't, we want to keep those working.
                    return this.WrappedSymbol.Equals(other);
                }
                else
                {
                    return false;
                }
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as ISymbol, SymbolEqualityComparer.Default);
            }

            public override int GetHashCode()
            {
                // As a transition mechanism, we allow ourselves to be compared to non-wrapped
                // symbols and we compare with the existing compiler equality and just throw away our
                // top-level nullability. Because of that, we can't incorporate the top-level nullability
                // into our hash code, because if we did we couldn't be both simultaneously equal to
                // something that has top level nullability and something that doesn't.
                return this.WrappedSymbol.GetHashCode();
            }

            #region ITypeSymbol Implementation Forwards

            public TypeKind TypeKind => WrappedSymbol.TypeKind;
            public INamedTypeSymbol BaseType => WrappedSymbol.BaseType;
            public ImmutableArray<INamedTypeSymbol> Interfaces => WrappedSymbol.Interfaces;
            public ImmutableArray<INamedTypeSymbol> AllInterfaces => WrappedSymbol.AllInterfaces;
            public bool IsReferenceType => WrappedSymbol.IsReferenceType;
            public bool IsValueType => WrappedSymbol.IsValueType;
            public bool IsAnonymousType => WrappedSymbol.IsAnonymousType;
            public bool IsTupleType => WrappedSymbol.IsTupleType;
            public ITypeSymbol OriginalDefinition => WrappedSymbol.OriginalDefinition;
            public SpecialType SpecialType => WrappedSymbol.SpecialType;
            public bool IsRefLikeType => WrappedSymbol.IsRefLikeType;
            public bool IsUnmanagedType => WrappedSymbol.IsUnmanagedType;
            public bool IsReadOnly => WrappedSymbol.IsReadOnly;
            public bool IsNamespace => WrappedSymbol.IsNamespace;
            public bool IsType => WrappedSymbol.IsType;
            public SymbolKind Kind => WrappedSymbol.Kind;
            public string Language => WrappedSymbol.Language;
            public string Name => WrappedSymbol.Name;
            public string MetadataName => WrappedSymbol.MetadataName;
            public ISymbol ContainingSymbol => WrappedSymbol.ContainingSymbol;
            public IAssemblySymbol ContainingAssembly => WrappedSymbol.ContainingAssembly;
            public IModuleSymbol ContainingModule => WrappedSymbol.ContainingModule;
            public INamedTypeSymbol ContainingType => WrappedSymbol.ContainingType;
            public INamespaceSymbol ContainingNamespace => WrappedSymbol.ContainingNamespace;
            public bool IsDefinition => WrappedSymbol.IsDefinition;
            public bool IsStatic => WrappedSymbol.IsStatic;
            public bool IsVirtual => WrappedSymbol.IsVirtual;
            public bool IsOverride => WrappedSymbol.IsOverride;
            public bool IsAbstract => WrappedSymbol.IsAbstract;
            public bool IsSealed => WrappedSymbol.IsSealed;
            public bool IsExtern => WrappedSymbol.IsExtern;
            public bool IsImplicitlyDeclared => WrappedSymbol.IsImplicitlyDeclared;
            public bool CanBeReferencedByName => WrappedSymbol.CanBeReferencedByName;
            public ImmutableArray<Location> Locations => WrappedSymbol.Locations;
            public ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => WrappedSymbol.DeclaringSyntaxReferences;
            public Accessibility DeclaredAccessibility => WrappedSymbol.DeclaredAccessibility;
            public bool HasUnsupportedMetadata => WrappedSymbol.HasUnsupportedMetadata;

            ISymbol ISymbol.OriginalDefinition => WrappedSymbol.OriginalDefinition;

            public abstract void Accept(SymbolVisitor visitor);
            public abstract TResult Accept<TResult>(SymbolVisitor<TResult> visitor);

            public ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember)
            {
                return WrappedSymbol.FindImplementationForInterfaceMember(interfaceMember);
            }

            public ImmutableArray<AttributeData> GetAttributes()
            {
                return WrappedSymbol.GetAttributes();
            }

            public string GetDocumentationCommentId()
            {
                return WrappedSymbol.GetDocumentationCommentId();
            }

            public string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            {
                return WrappedSymbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
            }

            public ImmutableArray<ISymbol> GetMembers()
            {
                return WrappedSymbol.GetMembers();
            }

            public ImmutableArray<ISymbol> GetMembers(string name)
            {
                return WrappedSymbol.GetMembers(name);
            }

            public ImmutableArray<INamedTypeSymbol> GetTypeMembers()
            {
                return WrappedSymbol.GetTypeMembers();
            }

            public ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name)
            {
                return WrappedSymbol.GetTypeMembers(name);
            }

            public ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity)
            {
                return WrappedSymbol.GetTypeMembers(name, arity);
            }

            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            {
                return WrappedSymbol.ToDisplayParts(topLevelNullability, format);
            }

            public ImmutableArray<SymbolDisplayPart> ToDisplayParts(SymbolDisplayFormat format = null)
            {
                return WrappedSymbol.ToDisplayParts(format);
            }

            public string ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null)
            {
                return WrappedSymbol.ToDisplayString(topLevelNullability, format);
            }

            public string ToDisplayString(SymbolDisplayFormat format = null)
            {
                return WrappedSymbol.ToDisplayString(format);
            }

            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            {
                return WrappedSymbol.ToMinimalDisplayParts(semanticModel, topLevelNullability, position, format);
            }

            public ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                return WrappedSymbol.ToMinimalDisplayParts(semanticModel, position, format);
            }

            public string ToMinimalDisplayString(SemanticModel semanticModel, NullableFlowState topLevelNullability, int position, SymbolDisplayFormat format = null)
            {
                return WrappedSymbol.ToMinimalDisplayString(semanticModel, topLevelNullability, position, format);
            }

            public string ToMinimalDisplayString(SemanticModel semanticModel, int position, SymbolDisplayFormat format = null)
            {
                return WrappedSymbol.ToMinimalDisplayString(semanticModel, position, format);
            }

            #endregion
        }
    }
}
