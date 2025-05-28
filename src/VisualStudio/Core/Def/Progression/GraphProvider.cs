// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//if (context.Direction == GraphContextDirection.Self && context.RequestedProperties.Contains(DgmlNodeProperties.ContainsChildren))
//{
//    graphQueries.Add(new ContainsChildrenGraphQuery());
//}

//if (context.Direction == GraphContextDirection.Contains ||
//    (context.Direction == GraphContextDirection.Target && context.LinkCategories.Contains(CodeLinkCategories.Contains)))
//{
//    graphQueries.Add(new ContainsGraphQuery());
//}

//if (context.LinkCategories.Contains(CodeLinkCategories.InheritsFrom))
//{
//    if (context.Direction == GraphContextDirection.Target)
//    {
//        graphQueries.Add(new InheritsGraphQuery());
//    }
//    else if (context.Direction == GraphContextDirection.Source)
//    {
//        graphQueries.Add(new InheritedByGraphQuery());
//    }
//}

//if (context.LinkCategories.Contains(CodeLinkCategories.SourceReferences))
//{
//    graphQueries.Add(new IsUsedByGraphQuery());
//}

//if (context.LinkCategories.Contains(CodeLinkCategories.Calls))
//{
//    if (context.Direction == GraphContextDirection.Target)
//    {
//        graphQueries.Add(new CallsGraphQuery());
//    }
//    else if (context.Direction == GraphContextDirection.Source)
//    {
//        graphQueries.Add(new IsCalledByGraphQuery());
//    }
//}

//if (context.LinkCategories.Contains(CodeLinkCategories.Implements))
//{
//    if (context.Direction == GraphContextDirection.Target)
//    {
//        graphQueries.Add(new ImplementsGraphQuery());
//    }
//    else if (context.Direction == GraphContextDirection.Source)
//    {
//        graphQueries.Add(new ImplementedByGraphQuery());
//    }
//}

//if (context.LinkCategories.Contains(RoslynGraphCategories.Overrides))
//{
//    if (context.Direction == GraphContextDirection.Source)
//    {
//        graphQueries.Add(new OverridesGraphQuery());
//    }
//    else if (context.Direction == GraphContextDirection.Target)
//    {
//        graphQueries.Add(new OverriddenByGraphQuery());
//    }
//}



//EnsureInitialized();

//// Only nodes that explicitly state that they contain children (e.g., source files) and named types should
//// be expandable.
//if (nodes.Any(n => n.Properties.Any(p => p.Key == DgmlNodeProperties.ContainsChildren)) ||
//    nodes.Any(n => IsAnySymbolKind(n, SymbolKind.NamedType)))
//{
//    yield return new GraphCommand(
//        GraphCommandDefinition.Contains,
//        targetCategories: null,
//        linkCategories: [GraphCommonSchema.Contains],
//        trackChanges: true);
//}

//// All graph commands below this point apply only to Roslyn-owned nodes.
//if (!nodes.All(n => IsRoslynNode(n)))
//{
//    yield break;
//}

//// Only show 'Base Types' and 'Derived Types' on a class or interface.
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.NamedType) &&
//                   IsAnyTypeKind(n, TypeKind.Class, TypeKind.Interface, TypeKind.Struct, TypeKind.Enum, TypeKind.Delegate)))
//{
//    yield return new GraphCommand(
//        GraphCommandDefinition.BaseTypes,
//        targetCategories: null,
//        linkCategories: [CodeLinkCategories.InheritsFrom],
//        trackChanges: true);

//    yield return new GraphCommand(
//        GraphCommandDefinition.DerivedTypes,
//        targetCategories: null,
//        linkCategories: [CodeLinkCategories.InheritsFrom],
//        trackChanges: true);
//}

//// Only show 'Calls' on an applicable member in a class or struct
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field)))
//{
//    yield return new GraphCommand(
//        GraphCommandDefinition.Calls,
//        targetCategories: null,
//        linkCategories: [CodeLinkCategories.Calls],
//        trackChanges: true);
//}

//// Only show 'Is Called By' on an applicable member in a class or struct
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
//                   IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct)))
//{
//    yield return new GraphCommand(
//        GraphCommandDefinition.IsCalledBy,
//        targetCategories: null,
//        linkCategories: [CodeLinkCategories.Calls],
//        trackChanges: true);
//}

//// Show 'Is Used By'
//yield return new GraphCommand(
//    GraphCommandDefinition.IsUsedBy,
//    targetCategories: [CodeNodeCategories.SourceLocation],
//    linkCategories: [CodeLinkCategories.SourceReferences],
//    trackChanges: true);

//// Show 'Implements' on a class or struct, or an applicable member in a class or struct.
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.NamedType) &&
//                   IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct)))
//{
//    yield return new GraphCommand(
//        s_implementsCommandDefinition,
//        targetCategories: null,
//        linkCategories: [CodeLinkCategories.Implements],
//        trackChanges: true);
//}

//// Show 'Implements' on public, non-static members of a class or struct.  Note: we should
//// also show it on explicit interface impls in C#.
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
//                   IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct) &&
//                   !GetModifiers(n).IsStatic))
//{
//    if (nodes.Any(n => CheckAccessibility(n, Accessibility.Public) ||
//                       HasExplicitInterfaces(n)))
//    {
//        yield return new GraphCommand(
//            s_implementsCommandDefinition,
//            targetCategories: null,
//            linkCategories: [CodeLinkCategories.Implements],
//            trackChanges: true);
//    }
//}

//// Show 'Implemented By' on an interface.
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.NamedType) &&
//                   IsAnyTypeKind(n, TypeKind.Interface)))
//{
//    yield return new GraphCommand(
//        s_implementedByCommandDefinition,
//        targetCategories: null,
//        linkCategories: [CodeLinkCategories.Implements],
//        trackChanges: true);
//}

//// Show 'Implemented By' on any member of an interface.
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
//                   IsAnyTypeKind(n, TypeKind.Interface)))
//{
//    yield return new GraphCommand(
//        s_implementedByCommandDefinition,
//        targetCategories: null,
//        linkCategories: [CodeLinkCategories.Implements],
//        trackChanges: true);
//}

//// Show 'Overrides' on any applicable member of a class or struct
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
//                   IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct) &&
//                   GetModifiers(n).IsOverride))
//{
//    yield return new GraphCommand(
//        s_overridesCommandDefinition,
//        targetCategories: null,
//        linkCategories: [RoslynGraphCategories.Overrides],
//        trackChanges: true);
//}

//// Show 'Overridden By' on any applicable member of a class or struct
//if (nodes.Any(n => IsAnySymbolKind(n, SymbolKind.Event, SymbolKind.Method, SymbolKind.Property) &&
//                   IsAnyTypeKind(n, TypeKind.Class, TypeKind.Struct) &&
//                   IsOverridable(n)))
//{
//    yield return new GraphCommand(
//        s_overriddenByCommandDefinition,
//        targetCategories: null,
//        linkCategories: [RoslynGraphCategories.Overrides],
//        trackChanges: true);
//}
