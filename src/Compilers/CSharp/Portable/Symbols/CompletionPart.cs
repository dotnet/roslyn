// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This enum describes the types of components that could give
    /// us diagnostics.  We shouldn't read the list of diagnostics
    /// until all of these types are accounted for.
    /// </summary>
    /// <remarks>
    /// PEParameterSymbol reserves all completion part bits and uses them to track the completion state and
    /// presence of well known attributes.
    /// </remarks>
    [Flags]
    internal enum CompletionPart
    {
        // For all symbols
        None = 0,
        Attributes = 1 << 0,

        // For method symbols
        ReturnTypeAttributes = 1 << 1,

        // For methods.
        Parameters = 1 << 2,

        // For symbols with type: method, and field symbols. Properties are handled separately.
        Type = 1 << 3,

        // For named type symbols
        StartBaseType = 1 << 4,
        FinishBaseType = 1 << 5,
        StartInterfaces = 1 << 6,
        FinishInterfaces = 1 << 7,
        EnumUnderlyingType = 1 << 8,
        TypeArguments = 1 << 9,
        TypeParameters = 1 << 10,
        Members = 1 << 11,
        TypeMembers = 1 << 12,
        SynthesizedExplicitImplementations = 1 << 13,
        StartMemberChecks = 1 << 14,
        FinishMemberChecks = 1 << 15,
        MembersCompleted = 1 << 16, // this should be the last (highest-value) part

        All = (1 << 17) - 1,

        // This is the work we can do if ForceComplete is scoped to a particular SyntaxTree.
        NamedTypeSymbolWithLocationAll = Attributes | StartBaseType | FinishBaseType | StartInterfaces | FinishInterfaces | EnumUnderlyingType |
            TypeArguments | TypeParameters | Members | TypeMembers | SynthesizedExplicitImplementations | StartMemberChecks | FinishMemberChecks,

        NamedTypeSymbolAll = NamedTypeSymbolWithLocationAll | MembersCompleted,

        // For Usings
        StartValidatingImports = 1 << 4,
        FinishValidatingImports = 1 << 5,
        ImportsAll = StartValidatingImports | FinishValidatingImports,

        // For namespace symbols
        NameToMembersMap = 1 << 11,
        NamespaceSymbolAll = NameToMembersMap | MembersCompleted,

        // For field symbols
        FixedSize = 1 << 11,
        ConstantValue = 1 << 12,
        FieldSymbolAll = Attributes | Type | FixedSize | ConstantValue,

        // For method symbols
        StartAsyncMethodChecks = 1 << 11,
        FinishAsyncMethodChecks = 1 << 12,
        StartMethodChecks = 1 << 13,
        FinishMethodChecks = 1 << 14,
        MethodSymbolAll = Attributes | ReturnTypeAttributes | Parameters | Type | TypeParameters | StartMethodChecks | FinishMethodChecks | StartAsyncMethodChecks | FinishAsyncMethodChecks,

        // For type parameter symbols
        TypeParameterConstraints = 1 << 11,
        TypeParameterSymbolAll = Attributes | TypeParameterConstraints,

        // For property symbols
        StartPropertyParameters = 1 << 4,
        FinishPropertyParameters = 1 << 5,
        StartPropertyType = 1 << 6,
        FinishPropertyType = 1 << 7,
        PropertySymbolAll = Attributes | StartPropertyParameters | FinishPropertyParameters | StartPropertyType | FinishPropertyType,

        // For alias symbols
        AliasTarget = 1 << 4,

        // For assembly symbols
        StartAttributeChecks = 1 << 4,
        FinishAttributeChecks = 1 << 5,
        Module = 1 << 6,
        StartValidatingAddedModules = 1 << 8,
        FinishValidatingAddedModules = 1 << 9,
        AssemblySymbolAll = Attributes | StartAttributeChecks | FinishAttributeChecks | Module | StartValidatingAddedModules | FinishValidatingAddedModules,

        // For module symbol
        StartValidatingReferencedAssemblies = 1 << 4,
        FinishValidatingReferencedAssemblies = 1 << 5,
    }
}
