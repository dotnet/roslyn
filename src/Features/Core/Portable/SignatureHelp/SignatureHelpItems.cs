﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal class SignatureHelpItems
    {
        /// <summary>
        /// The list of items to present to the user.
        /// </summary>
        public IList<SignatureHelpItem> Items { get; }

        /// <summary>
        /// The span this session applies to.
        /// 
        /// Navigation outside this span will cause signature help to be dismissed.
        /// </summary>
        public TextSpan ApplicableSpan { get; }

        /// <summary>
        /// Returns the specified argument index that the provided position is at in the current document.  This 
        /// index may be greater than the number of arguments in the selected <see cref="SignatureHelpItem"/>.
        /// </summary>
        public int ArgumentIndex { get; }

        /// <summary>
        /// Returns the total number of arguments that have been typed in the current document.  This may be 
        /// greater than the ArgumentIndex if there are additional arguments after the provided position.
        /// </summary>
        public int ArgumentCount { get; }

        /// <summary>
        /// Returns the name of specified argument at the current position in the document.  
        /// This only applies to languages that allow the user to provide named arguments.
        /// If no named argument exists at the current position, then null should be returned. 
        /// 
        /// This value is used to determine which documentation comment should be provided for the current
        /// parameter.  Normally this is determined simply by determining the parameter by index.
        /// </summary>
        public string ArgumentName { get; }

        /// <summary>
        /// The item to select by default.  If this is <code>null</code> then the controller will
        /// pick the first item that has enough arguments to be viable based on what argument 
        /// position the user is currently inside of.
        /// </summary>
        public int? SelectedItemIndex { get; }

        public SignatureHelpItems(
            IList<SignatureHelpItem> items,
            TextSpan applicableSpan,
            int argumentIndex,
            int argumentCount,
            string argumentName,
            int? selectedItem = null)
        {
            Contract.ThrowIfNull(items);
            Contract.ThrowIfTrue(items.IsEmpty());
            Contract.ThrowIfTrue(selectedItem.HasValue && selectedItem.Value >= items.Count);

            if (argumentIndex < 0)
            {
                throw new ArgumentException($"{nameof(argumentIndex)} < 0. {argumentIndex} < 0", nameof(argumentIndex));
            }

            if (argumentCount < argumentIndex)
            {
                throw new ArgumentException($"{nameof(argumentCount)} < {nameof(argumentIndex)}. {argumentCount} < {argumentIndex}", nameof(argumentIndex));
            }

            // Adjust the `selectedItem` index if duplicates are able to be removed.
            var distinctItems = items.Distinct().ToList();
            if (selectedItem.HasValue && items.Count != distinctItems.Count)
            {
                // `selectedItem` index has already been determined to be valid, it now needs to be adjusted to point
                // to the equivalent item in the reduced list to account for duplicates being removed
                // E.g.,
                //   items = {A, A, B, B, C, D}
                //   selectedItem = 4 (index for item C)
                // ergo
                //   distinctItems = {A, B, C, D}
                //   actualItem = C
                //   selectedItem = 2 (index for item C)
                var actualItem = items[selectedItem.Value];
                selectedItem = distinctItems.IndexOf(actualItem);
                Debug.Assert(selectedItem.Value >= 0, "actual item was not part of the final list");
            }

            this.Items = distinctItems;
            this.ApplicableSpan = applicableSpan;
            this.ArgumentIndex = argumentIndex;
            this.ArgumentCount = argumentCount;
            this.SelectedItemIndex = selectedItem;
            this.ArgumentName = argumentName;
        }
    }
}
