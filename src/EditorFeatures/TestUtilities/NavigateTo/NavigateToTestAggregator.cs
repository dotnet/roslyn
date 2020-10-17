// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Moq;
using Roslyn.Utilities;

namespace Roslyn.Test.EditorUtilities.NavigateTo
{
    /// <summary>
    /// A helper class used when writing unit tests for Navigate To. Given a INavigateToItemProvider, this class will
    /// call StartSearch on the provider, collect the results, and return the results synchronously once the provider
    /// says it's results are complete.
    /// </summary>
    public sealed partial class NavigateToTestAggregator
    {
        private readonly INavigateToItemProvider _itemProvider;

        public NavigateToTestAggregator(INavigateToItemProvider itemProvider)
        {
            Contract.ThrowIfNull(itemProvider);

            _itemProvider = itemProvider;
        }

        /// <summary>
        /// Synchronously return the items produced by the INavigateToItemProvider.
        /// </summary>
        public Task<IEnumerable<NavigateToItem>> GetItemsAsync(string searchValue)
        {
            // Create the callback that we will pass to the provider
            var optionsMock = new Mock<INavigateToOptions>();

            var callback = new Callback(optionsMock.Object);
            _itemProvider.StartSearch(callback, searchValue);
            return callback.GetItemsAsync();
        }
    }
}
