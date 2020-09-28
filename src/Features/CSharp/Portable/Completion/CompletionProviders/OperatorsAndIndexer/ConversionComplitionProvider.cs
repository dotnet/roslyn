// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(ConversionComplitionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(SymbolCompletionProvider))]
    internal class ConversionComplitionProvider : OperatorIndexerCompletionProviderBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConversionComplitionProvider()
        {
        }

        protected override IEnumerable<CompletionItem> GetCompletionItemsForTypeSymbol(ITypeSymbol container, SemanticModel semanticModel, int position)
        {
            var allMembers = container.GetMembers();
            var allExplicitConversions = from m in allMembers.OfType<IMethodSymbol>()
                                         where
                                             m.IsConversion() && // MethodKind.Conversion
                                             m.Name == WellKnownMemberNames.ExplicitConversionName && // op_Explicit
                                             m.Parameters.Length == 1 && // Malformed conversion operator may have more or less than one parameter
                                             container.Equals(m.Parameters[0].Type) // Convert from container type to other type
                                         let typeName = m.ReturnType.ToMinimalDisplayString(semanticModel, position)
                                         select SymbolCompletionItem.CreateWithSymbolId(
                                             displayText: $"({typeName})", // The type to convert to
                                             filterText: typeName,
                                             sortText: $"{SortingPrefix}{typeName}",
                                             symbols: ImmutableList.Create(m),
                                             rules: CompletionItemRules.Default,
                                             contextPosition: position,
                                             properties: CreateCompletionHandlerProperty(CompletionHandlerConversion, (MinimalTypeNamePropertyName, typeName)));
            return allExplicitConversions;
        }
    }
}
