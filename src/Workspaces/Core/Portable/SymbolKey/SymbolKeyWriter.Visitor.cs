// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal partial class SymbolKeyWriter
    {
        private class Visitor : SymbolVisitor
        {
            private readonly SymbolKeyWriter _writer;

            public Visitor(SymbolKeyWriter writer)
            {
                _writer = writer;
            }

            public override void VisitAlias(IAliasSymbol symbol)
            {
                var name = symbol.Name;
                var target = symbol.Target;
                var filePath = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath ?? "";

                _writer.WriteType(SymbolKeyType.Alias);
                _writer.WriteString(name);
                _writer.WriteSymbol(target);
                _writer.WriteString(filePath);
            }

            public override void VisitArrayType(IArrayTypeSymbol symbol)
            {
                var elementType = symbol.ElementType;
                var rank = symbol.Rank;

                _writer.WriteType(SymbolKeyType.ArrayType);
                _writer.WriteSymbol(symbol.ElementType);
                _writer.WriteInt(symbol.Rank);
            }

            public override void VisitAssembly(IAssemblySymbol symbol)
            {
                // For now, we only store the name portion of an assembly's identity.
                var assemblyName = symbol.Identity.Name;

                _writer.WriteType(SymbolKeyType.Assembly);
                _writer.WriteString(assemblyName);
            }

            public override void VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                _writer.WriteType(SymbolKeyType.DynamicType);
            }

            public override void VisitEvent(IEventSymbol symbol)
            {
                var metadataName = symbol.MetadataName;
                var containingType = symbol.ContainingType;

                _writer.WriteType(SymbolKeyType.Event);
                _writer.WriteString(metadataName);
                _writer.WriteSymbol(containingType);
            }

            public override void VisitField(IFieldSymbol symbol)
            {
                var metadataName = symbol.MetadataName;
                var containingType = symbol.ContainingType;

                _writer.WriteType(SymbolKeyType.Field);
                _writer.WriteString(metadataName);
                _writer.WriteSymbol(containingType);
            }

            public override void VisitLabel(ILabelSymbol symbol)
            {
                WriteBodyLevelSymbol(symbol);
            }

            public override void VisitLocal(ILocalSymbol symbol)
            {
                WriteBodyLevelSymbol(symbol);
            }

            public override void VisitModule(IModuleSymbol symbol)
            {
                var containingSymbol = symbol.ContainingSymbol;

                _writer.WriteType(SymbolKeyType.Module);
                _writer.WriteSymbol(containingSymbol);
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                var metadataName = symbol.MetadataName;
                var isCompilationGlobalNamespace = false;
                var containingSymbol = (ISymbol)symbol.ContainingNamespace;

                // The containing symbol can be one of many things:
                //
                //   1. Null when this is the global namespace for a compilation.
                //   2. The SymbolId for an assembly symbol if this is the global namespace for an assembly.
                //   3. The SymbolId for a module symbol if this is the global namespace for a module.
                //   4. The SymbolId for the containing namespace symbol if this is not a global namespace.

                if (containingSymbol == null)
                {
                    // A global namespace can either belong to a module, assembly or to a compilation.
                    Debug.Assert(symbol.IsGlobalNamespace);

                    switch (symbol.NamespaceKind)
                    {
                        case NamespaceKind.Module:
                            containingSymbol = symbol.ContainingModule;
                            break;
                        case NamespaceKind.Assembly:
                            containingSymbol = symbol.ContainingAssembly;
                            break;
                        case NamespaceKind.Compilation:
                            isCompilationGlobalNamespace = true;
                            break;
                        default:
                            throw new NotSupportedException($"Unsupported namespace kind: {symbol.NamespaceKind}");
                    }
                }

                _writer.WriteType(SymbolKeyType.Namespace);
                _writer.WriteString(metadataName);
                _writer.WriteBool(isCompilationGlobalNamespace);
                _writer.WriteSymbol(containingSymbol);
            }

            public override void VisitMethod(IMethodSymbol symbol)
            {
                switch (symbol.MethodKind)
                {
                    case MethodKind.ReducedExtension:
                        WriteReducedExtensionMethod(symbol);
                        break;
                    case MethodKind.AnonymousFunction:
                        WriteAnonymousFunction(symbol);
                        break;
                    case MethodKind.LocalFunction:
                        WriteBodyLevelSymbol(symbol);
                        break;
                    default:
                        if (!symbol.Equals(symbol.ConstructedFrom))
                        {
                            WriteConstructedMethod(symbol);
                        }
                        else
                        {
                            WriteMethod(symbol);
                        }

                        break;
                }
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (symbol.Kind == SymbolKind.ErrorType)
                {
                    WriteErrorType(symbol);
                }
                else if (symbol.IsTupleType)
                {
                    WriteTupleType(symbol);
                }
                else if (symbol.IsAnonymousType)
                {
                    if (symbol.IsDelegateType())
                    {
                        WriteAnonymousDelegateType(symbol);
                    }
                    else
                    {
                        WriteAnonymousType(symbol);
                    }
                }
                else
                {
                    WriteNamedType(symbol);
                }
            }

            public override void VisitParameter(IParameterSymbol symbol)
            {
                var metadataName = symbol.MetadataName;
                var containingSymbol = symbol.ContainingSymbol;

                _writer.WriteType(SymbolKeyType.Parameter);
                _writer.WriteString(metadataName);
                _writer.WriteSymbol(containingSymbol);
            }

            public override void VisitPointerType(IPointerTypeSymbol symbol)
            {
                var pointedAtType = symbol.PointedAtType;

                _writer.WriteType(SymbolKeyType.PointerType);
                _writer.WriteSymbol(pointedAtType);
            }

            public override void VisitProperty(IPropertySymbol symbol)
            {
                var metadataName = symbol.MetadataName;
                var containingSymbol = symbol.ContainingSymbol;
                var isIndexer = symbol.IsIndexer;
                var originalParameters = symbol.OriginalDefinition.Parameters;

                _writer.WriteType(SymbolKeyType.Property);
                _writer.WriteString(metadataName);
                _writer.WriteSymbol(containingSymbol);
                _writer.WriteBool(isIndexer);
                _writer.WriteParameterRefKindsArray(originalParameters);
                _writer.WriteParameterTypesArray(originalParameters);
            }

            public override void VisitRangeVariable(IRangeVariableSymbol symbol)
            {
                WriteBodyLevelSymbol(symbol);
            }

            public override void VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                if (!TryWriteTypeParameterOrdinal(symbol))
                {
                    WriteTypeParameter(symbol);
                }
            }

            private void WriteAnonymousDelegateType(INamedTypeSymbol symbol)
            {
                Debug.Assert(symbol.IsAnonymousDelegateType());

                // Write out if this was an anonymous delegate or anonymous function.
                // In both cases they'll have the same location (the location of 
                // the lambda that forced them into existence).  When we resolve the
                // symbol later, if it's an anonymous delegate, we'll first resolve to
                // the anonymous-function, then use that anonymous-function to get at
                // the synthesized anonymous delegate.

                var isAnonymousDelegateType = true;
                var location = symbol.Locations.FirstOrDefault();

                _writer.WriteType(SymbolKeyType.AnonymousFunctionOrDelegate);
                _writer.WriteBool(isAnonymousDelegateType);
                _writer.WriteLocation(location);
            }

            private void WriteAnonymousFunction(IMethodSymbol symbol)
            {
                Debug.Assert(symbol.MethodKind == MethodKind.AnonymousFunction);

                // Write out if this was an anonymous delegate or anonymous function.
                // In both cases they'll have the same location (the location of 
                // the lambda that forced them into existence).  When we resolve the
                // symbol later, if it's an anonymous delegate, we'll first resolve to
                // the anonymous-function, then use that anonymous-function to get at
                // the synthesized anonymous delegate.

                var isAnonymousDelegateType = false;
                var location = symbol.Locations.FirstOrDefault();

                _writer.WriteType(SymbolKeyType.AnonymousFunctionOrDelegate);
                _writer.WriteBool(isAnonymousDelegateType);
                _writer.WriteLocation(location);
            }

            private void WriteAnonymousType(INamedTypeSymbol symbol)
            {
                Debug.Assert(symbol.IsAnonymousType);

                var propertyTypes = ArrayBuilder<ITypeSymbol>.GetInstance();
                var propertyNames = ArrayBuilder<string>.GetInstance();
                var propertyIsReadOnly = ArrayBuilder<bool>.GetInstance();
                var propertyLocations = ArrayBuilder<Location>.GetInstance();

                foreach (var member in symbol.GetMembers())
                {
                    if (member is IPropertySymbol propertySymbol)
                    {
                        propertyTypes.Add(propertySymbol.Type);
                        propertyNames.Add(propertySymbol.Name);
                        propertyIsReadOnly.Add(propertySymbol.SetMethod == null);
                        propertyLocations.Add(propertySymbol.Locations.FirstOrDefault());
                    }
                }

                _writer.WriteType(SymbolKeyType.AnonymousType);
                _writer.WriteSymbolArray(propertyTypes.ToImmutableAndFree());
                _writer.WriteStringArray(propertyNames.ToImmutableAndFree());
                _writer.WriteBoolArray(propertyIsReadOnly.ToImmutableAndFree());
                _writer.WriteLocationArray(propertyLocations.ToImmutableAndFree());
            }

            private void WriteBodyLevelSymbol(ISymbol symbol)
            {
                var localName = symbol.Name;
                var containingSymbol = symbol.ContainingSymbol;

                while (!containingSymbol.DeclaringSyntaxReferences.Any())
                {
                    containingSymbol = containingSymbol.ContainingSymbol;
                }

                var compilation = ((ISourceAssemblySymbol)symbol.ContainingAssembly).Compilation;
                var kind = symbol.Kind;
                var ordinal = 0;

                var symbols = GetSymbols(compilation, containingSymbol, kind, localName, _writer._cancellationToken);

                for (int i = 0; i < symbols.Length; i++)
                {
                    var possibleSymbol = symbols[i];

                    if (possibleSymbol.Equals(symbol))
                    {
                        ordinal = i;
                        break;
                    }
                }

                _writer.WriteType(SymbolKeyType.BodyLevel);
                _writer.WriteString(localName);
                _writer.WriteSymbol(containingSymbol);
                _writer.WriteInt(ordinal);
                _writer.WriteInt((int)kind);
            }

            private static ImmutableArray<ISymbol> GetSymbols(
                Compilation compilation, ISymbol containingSymbol,
                SymbolKind kind, string localName,
                CancellationToken cancellationToken)
            {
                var result = ArrayBuilder<ISymbol>.GetInstance();

                foreach (var declaringLocation in containingSymbol.DeclaringSyntaxReferences)
                {
                    // This operation can potentially fail. If containingSymbol came from 
                    // a SpeculativeSemanticModel, containingSymbol.ContainingAssembly.Compilation
                    // may not have been rebuilt to reflect the trees used by the 
                    // SpeculativeSemanticModel to produce containingSymbol. In that case,
                    // asking the ContainingAssembly's compilation for a SemanticModel based
                    // on trees for containingSymbol with throw an ArgumentException.
                    // Unfortunately, the best way to avoid this (currently) is to see if
                    // we're asking for a model for a tree that's part of the compilation.
                    // (There's no way to get back to a SemanticModel from a symbol).

                    // TODO (rchande): It might be better to call compilation.GetSemanticModel
                    // and catch the ArgumentException. The compilation internally has a 
                    // Dictionary<SyntaxTree, ...> that it uses to check if the SyntaxTree
                    // is applicable wheras the public interface requires us to enumerate
                    // the entire IEnumerable of trees in the Compilation.
                    if (!compilation.SyntaxTrees.Contains(declaringLocation.SyntaxTree))
                    {
                        continue;
                    }

                    var node = declaringLocation.GetSyntax(cancellationToken);
                    if (node.Language == LanguageNames.VisualBasic)
                    {
                        node = node.Parent;
                    }

                    var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

                    foreach (var n in node.DescendantNodes())
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(n, cancellationToken);

                        if (symbol != null &&
                            symbol.Kind == kind &&
                            compilation.NamesAreEqual(symbol.Name, localName))
                        {
                            result.Add(symbol);
                        }
                    }
                }

                return result.ToImmutableAndFree();
            }

            private void WriteConstructedMethod(IMethodSymbol symbol)
            {
                var constructedFrom = symbol.ConstructedFrom;
                var typeArguments = symbol.TypeArguments;

                _writer.WriteType(SymbolKeyType.ConstructedMethod);
                _writer.WriteSymbol(constructedFrom);
                _writer.WriteSymbolArray(typeArguments);
            }

            private void WriteErrorType(INamedTypeSymbol symbol)
            {
                var name = symbol.Name;
                var containingSymbol = symbol.ContainingSymbol as INamespaceOrTypeSymbol;
                var arity = symbol.Arity;
                var typeArguments = !symbol.Equals(symbol.ConstructedFrom)
                    ? symbol.TypeArguments
                    : default;

                _writer.WriteType(SymbolKeyType.ErrorType);
                _writer.WriteString(name);
                _writer.WriteSymbol(containingSymbol);
                _writer.WriteInt(arity);
                _writer.WriteSymbolArray(typeArguments);
            }

            private void WriteMethod(IMethodSymbol symbol)
            {
                var metadataName = symbol.MetadataName;
                var containingSymbol = symbol.ContainingSymbol;
                var arity = symbol.Arity;
                var isPartialMethodImplementationPart = symbol.PartialDefinitionPart != null;
                var parameters = symbol.OriginalDefinition.Parameters;
                var returnType = symbol.MethodKind == MethodKind.Conversion
                    ? symbol.ReturnType
                    : null;

                _writer.WriteType(SymbolKeyType.Method);
                _writer.WriteString(metadataName);
                _writer.WriteSymbol(containingSymbol);
                _writer.WriteInt(arity);
                _writer.WriteBool(isPartialMethodImplementationPart);
                _writer.WriteParameterRefKindsArray(parameters);
                _writer.WriteParameterTypesArray(parameters);
                _writer.WriteSymbol(returnType);
            }

            private void WriteNamedType(INamedTypeSymbol symbol)
            {
                var metadataName = symbol.MetadataName;
                var containingSymbol = symbol.ContainingSymbol;
                var arity = symbol.Arity;
                var typeKind = (int)symbol.TypeKind;
                var isUnboundGenericType = symbol.IsUnboundGenericType;
                var typeArguments = !symbol.Equals(symbol.ConstructedFrom) && !symbol.IsUnboundGenericType
                    ? symbol.TypeArguments
                    : default;

                _writer.WriteType(SymbolKeyType.NamedType);
                _writer.WriteString(metadataName);
                _writer.WriteSymbol(containingSymbol);
                _writer.WriteInt(arity);
                _writer.WriteInt(typeKind);
                _writer.WriteBool(isUnboundGenericType);
                _writer.WriteSymbolArray(typeArguments);
            }

            private void WriteReducedExtensionMethod(IMethodSymbol symbol)
            {
                var reducedFrom = symbol.ReducedFrom;
                var receivedType = symbol.ReceiverType;

                _writer.WriteType(SymbolKeyType.ReducedExtensionMethod);
                _writer.WriteSymbol(reducedFrom);
                _writer.WriteSymbol(receivedType);
            }

            private void WriteTupleType(INamedTypeSymbol symbol)
            {
                Debug.Assert(symbol.IsTupleType);

                var friendlyNames = ArrayBuilder<string>.GetInstance();
                var locations = ArrayBuilder<Location>.GetInstance();
                var isError = symbol.TupleUnderlyingType.TypeKind == TypeKind.Error;

                _writer.WriteType(SymbolKeyType.TupleType);

                _writer.WriteBool(isError);

                if (isError)
                {
                    var elementTypes = ArrayBuilder<ISymbol>.GetInstance();

                    foreach (var element in symbol.TupleElements)
                    {
                        elementTypes.Add(element.Type);
                    }

                    _writer.WriteSymbolArray(elementTypes.ToImmutableAndFree());
                }
                else
                {
                    _writer.WriteSymbol(symbol.TupleUnderlyingType);
                }

                foreach (var element in symbol.TupleElements)
                {
                    friendlyNames.Add(element.IsImplicitlyDeclared ? null : element.Name);
                    locations.Add(element.Locations.FirstOrDefault() ?? Location.None);
                }

                _writer.WriteStringArray(friendlyNames.ToImmutableAndFree());
                _writer.WriteLocationArray(locations.ToImmutableAndFree());
            }

            private void WriteTypeParameter(ITypeParameterSymbol symbol)
            {
                var metadataName = symbol.MetadataName;
                var containingSymbol = symbol.ContainingSymbol;

                _writer.WriteType(SymbolKeyType.TypeParameter);
                _writer.WriteString(metadataName);
                _writer.WriteSymbol(containingSymbol);
            }

            private bool TryWriteTypeParameterOrdinal(ITypeParameterSymbol symbol)
            {
                // If this is a method type parameter, then we should try to write out the ordinal to that method
                // to avoid further recursion in cases like M<T>(T t).

                if (symbol.TypeParameterKind != TypeParameterKind.Method ||
                    !_writer._symbolRefIdMap.TryGetValue(symbol.DeclaringMethod, out var methodRefId))
                {
                    return false;
                }

                _writer.WriteType(SymbolKeyType.TypeParameterOrdinal);
                _writer.WriteSpace();
                _writer.WriteSymbolReference(methodRefId);
                _writer.WriteInt(symbol.Ordinal);
                return true;
            }
        }
    }
}
