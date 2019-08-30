// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.SignatureHelp;
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

        /// <summary>UserSelected is true if the SelectedItem is the result of a user selection (up/down arrows).</summary>
        public bool UserSelected { get; }
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
            int? selectedParameter,
            bool userSelected)
        {
            Contract.ThrowIfNull(selectedItem);
            Contract.ThrowIfFalse(items.Count != 0, "Must have at least one item.");
            Contract.ThrowIfFalse(items.Contains(selectedItem), "Selected item must be in list of items.");

            _disconnectedBufferGraph = disconnectedBufferGraph;
            this.TextSpan = textSpan;
            this.Items = items;
            this.Provider = provider;
            this.SelectedItem = selectedItem;
            this.UserSelected = userSelected;
            this.ArgumentIndex = argumentIndex;
            this.ArgumentCount = argumentCount;
            this.ArgumentName = argumentName;
            this.SelectedParameter = selectedParameter;
        }

        public Model WithSelectedItem(SignatureHelpItem selectedItem, bool userSelected)
        {
            return selectedItem == this.SelectedItem && userSelected == this.UserSelected
                ? this
                : new Model(_disconnectedBufferGraph, TextSpan, Provider, Items, selectedItem, ArgumentIndex, ArgumentCount, ArgumentName, SelectedParameter, userSelected);
        }

        public Model WithSelectedParameter(int? selectedParameter)
        {
            return selectedParameter == this.SelectedParameter
                ? this
                : new Model(_disconnectedBufferGraph, TextSpan, Provider, Items, SelectedItem, ArgumentIndex, ArgumentCount, ArgumentName, selectedParameter, UserSelected);
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
