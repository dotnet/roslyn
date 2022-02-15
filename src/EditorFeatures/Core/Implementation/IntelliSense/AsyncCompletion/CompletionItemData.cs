// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal sealed class CompletionItemData
    {
        private const string RoslynCompletionItemData = nameof(RoslynCompletionItemData);

        public RoslynCompletionItem RoslynItem { get; }

        public Optional<SnapshotPoint> TriggerLocation { get; }

        public bool IsProvidedByRoslynCompletionSource => TriggerLocation.HasValue;

        private CompletionItemData(RoslynCompletionItem roslynItem, SnapshotPoint? triggerLocation)
        {
            RoslynItem = roslynItem;
            TriggerLocation = triggerLocation ?? new();
        }

        public static bool TryGetData(CompletionItem vsCompletionitem, out CompletionItemData data)
            => vsCompletionitem.Properties.TryGetProperty(RoslynCompletionItemData, out data);

        public static RoslynCompletionItem GetOrAddDummyRoslynItem(CompletionItem vsItem)
        {
            if (vsItem.Properties.TryGetProperty(RoslynCompletionItemData, out CompletionItemData data))
                return data.RoslynItem;

            var roslynItem = CreateDummyRoslynItem(vsItem);
            AddData(vsItem, roslynItem, triggerLocation: null);

            return roslynItem;
        }

        public static void AddData(CompletionItem vsCompletionitem, RoslynCompletionItem roslynItem, SnapshotPoint? triggerLocation)
            => vsCompletionitem.Properties[RoslynCompletionItemData] = new CompletionItemData(roslynItem, triggerLocation);

        private static RoslynCompletionItem CreateDummyRoslynItem(CompletionItem vsItem)
            => RoslynCompletionItem.Create(
                displayText: vsItem.DisplayText,
                filterText: vsItem.FilterText,
                sortText: vsItem.SortText,
                displayTextSuffix: vsItem.Suffix);
    }
}
