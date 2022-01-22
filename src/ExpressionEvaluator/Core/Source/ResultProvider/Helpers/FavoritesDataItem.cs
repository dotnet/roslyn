// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// DkmDataItem for passing favorites information between ResultProvider.GetChild(...) and
    /// ResultProvider.IDkmClrResultProvider.GetResult when evaluating child items in an
    /// expansion.
    /// </summary>
    internal class FavoritesDataItem : DkmDataItem
    {
        public readonly bool CanFavorite;
        public readonly bool IsFavorite;

        internal FavoritesDataItem(bool canFavorite, bool isFavorite)
        {
            CanFavorite = canFavorite;
            IsFavorite = isFavorite;
        }
    }
}
