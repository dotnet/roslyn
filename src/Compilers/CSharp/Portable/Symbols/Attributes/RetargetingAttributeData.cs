// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting
{
    /// <summary>
    /// Represents a retargeting custom attribute
    /// </summary>
    internal sealed class RetargetingAttributeData : SourceAttributeData
    {
        internal RetargetingAttributeData(
            SyntaxReference applicationNode,
            NamedTypeSymbol attributeClass,
            MethodSymbol attributeConstructor,
            ImmutableArray<TypedConstant> constructorArguments,
            ImmutableArray<int> constructorArgumentsSourceIndices,
            ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments,
            bool hasErrors,
            bool isConditionallyOmitted)
            : base(applicationNode, attributeClass, attributeConstructor, constructorArguments, constructorArgumentsSourceIndices, namedArguments, hasErrors, isConditionallyOmitted)
        {
        }

        /// <summary>
        /// Gets the retargeted System.Type type symbol.
        /// </summary>
        /// <param name="targetSymbol">Target symbol on which this attribute is applied.</param>
        /// <returns>Retargeted System.Type type symbol.</returns>
        internal override TypeSymbol GetSystemType(Symbol targetSymbol)
        {
            var retargetingAssembly = (RetargetingAssemblySymbol)(targetSymbol.Kind == SymbolKind.Assembly ? targetSymbol : targetSymbol.ContainingAssembly);
            var underlyingAssembly = (SourceAssemblySymbol)retargetingAssembly.UnderlyingAssembly;

            // Get the System.Type from the underlying assembly's Compilation
            TypeSymbol systemType = underlyingAssembly.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type);

            // Retarget the type
            var retargetingModule = (RetargetingModuleSymbol)retargetingAssembly.Modules[0];
            return retargetingModule.RetargetingTranslator.Retarget(systemType, RetargetOptions.RetargetPrimitiveTypesByTypeCode);
        }
    }
}
