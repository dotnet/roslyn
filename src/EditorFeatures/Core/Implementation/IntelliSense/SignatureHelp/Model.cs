// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal class Model
    {
        private readonly DisconnectedBufferGraph _disconnectedBufferGraph;

        public TextSpan TextSpan { get; }
        public IList<SignatureHelpItem> Items { get; }
        public SignatureHelpItem SelectedItem { get; }
        public int ArgumentIndex { get; }
        public int ArgumentCount { get; }
        public string ArgumentName { get; }
        public int? SelectedParameter { get; }
        public ISignatureHelpProvider Provider { get; }

        public Model(
            DisconnectedBufferGraph disconnectedBufferGraph,
            TextSpan textSpan,
            ISignatureHelpProvider provider,
            IList<SignatureHelpItem> items,
            SignatureHelpItem selectedItem,
            int argumentIndex,
            int argumentCount,
            string argumentName,
            int? selectedParameter)
        {
            Contract.ThrowIfNull(selectedItem);
            Contract.ThrowIfFalse(items.Count != 0, "Must have at least one item.");
            Contract.ThrowIfFalse(items.Contains(selectedItem), "Selected item must be in list of items.");

            _disconnectedBufferGraph = disconnectedBufferGraph;
            this.TextSpan = textSpan;
            this.Items = items;
            this.Provider = provider;
            this.SelectedItem = selectedItem;
            this.ArgumentIndex = argumentIndex;
            this.ArgumentCount = argumentCount;
            this.ArgumentName = argumentName;
            this.SelectedParameter = selectedParameter;
        }

        public Model WithSelectedItem(SignatureHelpItem selectedItem)
        {
            return selectedItem == this.SelectedItem
                ? this
                : new Model(_disconnectedBufferGraph, TextSpan, Provider, Items, selectedItem, ArgumentIndex, ArgumentCount, ArgumentName, SelectedParameter);
        }

        public Model WithSelectedParameter(int? selectedParameter)
        {
            return selectedParameter == this.SelectedParameter
                ? this
                : new Model(_disconnectedBufferGraph, TextSpan, Provider, Items, SelectedItem, ArgumentIndex, ArgumentCount, ArgumentName, selectedParameter);
        }

        public SnapshotSpan GetCurrentSpanInSubjectBuffer(ITextSnapshot bufferSnapshot)
        {
            return _disconnectedBufferGraph.SubjectBufferSnapshot
                .CreateTrackingSpan(this.TextSpan.ToSpan(), SpanTrackingMode.EdgeInclusive)
                .GetSpan(bufferSnapshot);
        }

        public SnapshotSpan GetCurrentSpanInView(ITextSnapshot textSnapshot)
        {
            var originalSpan = _disconnectedBufferGraph.GetSubjectBufferTextSpanInViewBuffer(this.TextSpan);
            var trackingSpan = _disconnectedBufferGraph.ViewSnapshot.CreateTrackingSpan(originalSpan.TextSpan.ToSpan(), SpanTrackingMode.EdgeInclusive);
            return trackingSpan.GetSpan(textSnapshot);
        }
    }
}
