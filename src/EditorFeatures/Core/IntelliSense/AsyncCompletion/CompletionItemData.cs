// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal sealed record class CompletionItemData(RoslynCompletionItem RoslynItem, SnapshotPoint? TriggerLocation)
    {
        private const string RoslynCompletionItemData = nameof(RoslynCompletionItemData);

        public static bool TryGetData(CompletionItem vsCompletionItem, out CompletionItemData data)
            => vsCompletionItem.Properties.TryGetProperty(RoslynCompletionItemData, out data);

        public static RoslynCompletionItem GetOrAddDummyRoslynItem(CompletionItem vsItem)
        {
            if (TryGetData(vsItem, out var data))
                return data.RoslynItem;

            // TriggerLocation is null for items provided by non-roslyn completion source
            var roslynItem = CreateDummyRoslynItem(vsItem);
            AddData(vsItem, roslynItem, triggerLocation: null);

            return roslynItem;
        }

        public static void AddData(CompletionItem vsCompletionItem, RoslynCompletionItem roslynItem, SnapshotPoint? triggerLocation)
            => vsCompletionItem.Properties[RoslynCompletionItemData] = new CompletionItemData(roslynItem, triggerLocation);

        private static RoslynCompletionItem CreateDummyRoslynItem(CompletionItem vsItem)
            => RoslynCompletionItem.Create(
                displayText: vsItem.DisplayText,
                filterText: vsItem.FilterText,
                sortText: vsItem.SortText,
                displayTextSuffix: vsItem.Suffix);
    }
}
