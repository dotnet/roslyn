// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class UsedAssembliesRecorder : BoundTreeWalkerWithStackGuard
    {
        private readonly CSharpCompilation _compilation;

        public static void RecordUsedAssemblies(CSharpCompilation compilation, BoundNode node, DiagnosticBag diagnostics)
        {
            //Debug.Assert(node != null);

            //try
            //{
            //    var visitor = new UsedAssembliesRecorder(compilation);
            //    visitor.Visit(node);
            //}
            //catch (CancelledByStackGuardException ex)
            //{
            //    // PROTOTYPE(UsedAssemblyReferences): This error might not be cached, but its presence might affect cached full set of used assemblies. 
            //    //                                    We would report all assemblies as used, even though no one will ever see this error and under
            //    //                                    different environment state the pass could succeed causing us to return different set of used assemblies
            //    //                                    with no apparent reason for the difference from the consumer's point of view. 
            //    //                                    We will have the same problem with any BoundTreeWalker invoked under umbrella of GetUsedAssemblyReferences API.
            //    //                                    Including flow analysis, lowering, etc.
            //    ex.AddAnError(diagnostics);
            //}
        }

        private UsedAssembliesRecorder(CSharpCompilation compilation)
        {
            _compilation = compilation;
        }

        //private void AddAssembliesUsedBySymbolReference(BoundExpression? receiverOpt, Symbol symbol)
        //{
        //    if (symbol.IsStatic && receiverOpt?.Kind != BoundKind.TypeExpression)
        //    {
        //        _compilation.AddAssembliesUsedByTypeReference(symbol.ContainingType);
        //    }

        //    if (DefinitionMightReferenceNoPiaLocalTypes(symbol))
        //    {
        //        // Need to make sure we record assemblies containing canonical definitions for NoPia embedded types
        //        switch (symbol.OriginalDefinition)
        //        {
        //            case FieldSymbol field:
        //                addFromType(field.TypeWithAnnotations);
        //                break;
        //            case PropertySymbol property:
        //                addFromType(property.TypeWithAnnotations);
        //                addFromModifiers(property.RefCustomModifiers);
        //                addFromParameters(property.Parameters);
        //                break;
        //            case MethodSymbol method:
        //                addFromType(method.ReturnTypeWithAnnotations);
        //                addFromModifiers(method.RefCustomModifiers);
        //                addFromParameters(method.Parameters);
        //                break;
        //            case EventSymbol @event:
        //                addFromType(@event.TypeWithAnnotations);
        //                break;
        //            default:
        //                throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        //        }
        //    }

        //    void addFromParameters(ImmutableArray<ParameterSymbol> parameters)
        //    {
        //        foreach (var parameter in parameters)
        //        {
        //            addFromType(parameter.TypeWithAnnotations);
        //            addFromModifiers(parameter.RefCustomModifiers);
        //        }
        //    }

        //    void addFromType(TypeWithAnnotations type)
        //    {
        //        _compilation.AddAssembliesUsedByTypeReference(type.Type);
        //        addFromModifiers(type.CustomModifiers);
        //    }

        //    void addFromModifiers(ImmutableArray<CustomModifier> customModifiers)
        //    {
        //        foreach (CSharpCustomModifier modifier in customModifiers)
        //        {
        //            _compilation.AddAssembliesUsedByTypeReference(modifier.ModifierSymbol);
        //        }
        //    }
        //}

        //private bool DefinitionMightReferenceNoPiaLocalTypes(Symbol symbol)
        //{
        //    ModuleSymbol containingModule = symbol.ContainingModule;
        //    return containingModule is object &&
        //           containingModule != _compilation.SourceModule &&
        //           containingModule.MightContainNoPiaLocalTypes();
        //}

        //public override BoundNode? VisitFieldAccess(BoundFieldAccess node)
        //{
        //    AddAssembliesUsedBySymbolReference(node.ReceiverOpt, node.FieldSymbol);
        //    return base.VisitFieldAccess(node);
        //}

        //public override BoundNode? VisitPropertyAccess(BoundPropertyAccess node)
        //{
        //    AddAssembliesUsedBySymbolReference(node.ReceiverOpt, node.PropertySymbol);
        //    return base.VisitPropertyAccess(node);
        //}

        //public override BoundNode? VisitIndexerAccess(BoundIndexerAccess node)
        //{
        //    AddAssembliesUsedBySymbolReference(node.ReceiverOpt, node.Indexer);
        //    return base.VisitIndexerAccess(node);
        //}

        //public override BoundNode? VisitCall(BoundCall node)
        //{
        //    AddAssembliesUsedBySymbolReference(node.ReceiverOpt, node.Method);
        //    return base.VisitCall(node);
        //}

        //public override BoundNode? VisitEventAccess(BoundEventAccess node)
        //{
        //    AddAssembliesUsedBySymbolReference(node.ReceiverOpt, node.EventSymbol);
        //    return base.VisitEventAccess(node);
        //}

        //public override BoundNode? VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        //{
        //    AddAssembliesUsedBySymbolReference(node.ReceiverOpt, node.Event);
        //    return base.VisitEventAssignmentOperator(node);
        //}

        //public override BoundNode? VisitNameOfOperator(BoundNameOfOperator node)
        //{
        //    switch (node.Argument)
        //    {
        //        case BoundNamespaceExpression nsExpr:
        //            Debug.Assert(!nsExpr.NamespaceSymbol.IsGlobalNamespace);
        //            _compilation.AddAssembliesUsedByNamespaceReference(nsExpr.NamespaceSymbol);
        //            break;

        //        case BoundMethodGroup methodGroup:
        //            if (methodGroup.ReceiverOpt?.Kind != BoundKind.TypeExpression)
        //            {
        //                foreach (var symbol in methodGroup.Methods)
        //                {
        //                    _compilation.AddAssembliesUsedByTypeReference(symbol.ContainingType);
        //                }
        //            }
        //            break;
        //        case BoundPropertyGroup _:
        //            ExceptionUtilities.UnexpectedValue(node.Argument.Kind);
        //            break;
        //    }

        //    return base.VisitNameOfOperator(node);
        //}

        //public override BoundNode? VisitConversion(BoundConversion node)
        //{
        //    VisitConversion(node.Conversion);

        //    // Need to make sure we record assemblies containing canonical definitions for NoPia embedded types
        //    if (!node.Conversion.IsIdentity)
        //    {
        //        addUsedAssemblies(node.Operand.Type, ConsList<TypeSymbol>.Empty);
        //    }

        //    return base.VisitConversion(node);

        //    void addUsedAssemblies(TypeSymbol? typeOpt, ConsList<TypeSymbol> basesBeingResolved)
        //    {
        //        while (true)
        //        {
        //            switch (typeOpt)
        //            {
        //                case null:
        //                case DynamicTypeSymbol _:
        //                    break;
        //                case PointerTypeSymbol pointer:
        //                    typeOpt = pointer.PointedAtTypeWithAnnotations.DefaultType;
        //                    continue;
        //                case ArrayTypeSymbol array:
        //                    typeOpt = array.ElementTypeWithAnnotations.DefaultType;
        //                    continue;
        //                case NamedTypeSymbol named:
        //                    named = named.TupleUnderlyingTypeOrSelf();
        //                    addAssembliesUsedByTypeArguments(named, basesBeingResolved);

        //                    named = named.OriginalDefinition;
        //                    if (basesBeingResolved.ContainsReference(named))
        //                    {
        //                        break;
        //                    }

        //                    basesBeingResolved = basesBeingResolved.Prepend(named);
        //                    addAssembliesUsedByImplementedInterfaces(named, basesBeingResolved);

        //                    typeOpt = named.BaseTypeNoUseSiteDiagnostics;
        //                    continue;
        //                case TypeParameterSymbol typeParameter:
        //                    Debug.Assert(typeParameter.IsDefinition);
        //                    addAssembliesUsedByConstraints(typeParameter.OriginalDefinition, basesBeingResolved);
        //                    break;
        //                default:
        //                    throw ExceptionUtilities.UnexpectedValue(typeOpt.TypeKind);
        //            }

        //            break;
        //        }
        //    }

        //    void addAssembliesUsedByTypeArguments(NamedTypeSymbol current, ConsList<TypeSymbol> basesBeingResolved)
        //    {
        //        do
        //        {
        //            foreach (var typeArgument in current.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics)
        //            {
        //                addUsedAssemblies(typeArgument.Type, basesBeingResolved);
        //            }

        //            current = current.ContainingType;
        //        }
        //        while (current is object);
        //    }

        //    void addAssembliesUsedByImplementedInterfaces(NamedTypeSymbol current, ConsList<TypeSymbol> basesBeingResolved)
        //    {
        //        Debug.Assert(current.IsDefinition);
        //        bool definitionMightReferenceNoPiaLocalTypes = DefinitionMightReferenceNoPiaLocalTypes(current);

        //        foreach (var @interface in current.InterfacesNoUseSiteDiagnostics())
        //        {
        //            if (definitionMightReferenceNoPiaLocalTypes)
        //            {
        //                _compilation.AddAssembliesUsedByTypeReference(@interface);
        //            }

        //            addUsedAssemblies(@interface, basesBeingResolved);
        //        }
        //    }

        //    void addAssembliesUsedByConstraints(TypeParameterSymbol current, ConsList<TypeSymbol> basesBeingResolved)
        //    {
        //        Debug.Assert(current.IsDefinition);
        //        if (current.ContainingModule == _compilation.SourceModule && !basesBeingResolved.ContainsReference(current))
        //        {
        //            basesBeingResolved = basesBeingResolved.Prepend(current);
        //            foreach (var type in current.ConstraintTypesNoUseSiteDiagnostics)
        //            {
        //                addUsedAssemblies(type.Type, basesBeingResolved);
        //            }
        //        }
        //    }
        //}

        //private void VisitConversion(Conversion conversion)
        //{
        //    foreach (var underlying in conversion.UnderlyingConversions.NullToEmpty())
        //    {
        //        VisitConversion(underlying);
        //    }

        //    if (conversion.Kind == ConversionKind.Deconstruction)
        //    {
        //        Visit(conversion.DeconstructionInfo.Invocation);
        //    }
        //}

        //public override BoundNode? VisitCollectionElementInitializer(BoundCollectionElementInitializer node)
        //{
        //    AddAssembliesUsedBySymbolReference(receiverOpt: null, node.AddMethod);
        //    return base.VisitCollectionElementInitializer(node);
        //}

        public override BoundNode? VisitBinaryOperator(BoundBinaryOperator node)
        {
            // It is very common for bound trees to be left-heavy binary operators, eg,
            // a + b + c + d + ...
            // To avoid blowing the stack, do not recurse down the left hand side.

            // In order to avoid blowing the stack, we end up visiting right children
            // before left children; this should not be a problem

            BoundBinaryOperator current = node;
            while (true)
            {
                Visit(current.Right);
                if (current.Left.Kind == BoundKind.BinaryOperator)
                {
                    current = (BoundBinaryOperator)current.Left;
                }
                else
                {
                    Visit(current.Left);
                    break;
                }
            }

            return null;
        }

        public override BoundNode? VisitUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator node)
        {
            // In order to avoid blowing the stack, we end up visiting right children
            // before left children; this should not be a problem

            BoundUserDefinedConditionalLogicalOperator current = node;
            while (true)
            {
                Visit(current.Right);
                if (current.Left.Kind == BoundKind.UserDefinedConditionalLogicalOperator)
                {
                    current = (BoundUserDefinedConditionalLogicalOperator)current.Left;
                }
                else
                {
                    Visit(current.Left);
                    break;
                }
            }

            return null;
        }
    }
}
