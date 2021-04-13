// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Xaml.Features.InlineRename
{
    internal interface IXamlRenameInfo
    {
        /// <summary>
        /// Whether or not the entity at the selected location can be renamed.
        /// </summary>
        bool CanRename { get; }

        /// <summary>
        /// Provides the reason that can be displayed to the user if the entity at the selected 
        /// location cannot be renamed.
        /// </summary>
        string LocalizedErrorMessage { get; }

        /// <summary>
        /// The span of the entity that is being renamed.
        /// </summary>
        TextSpan TriggerSpan { get; }

        /// <summary>
        /// The short name of the symbol being renamed, for use in displaying information to the user.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// The full name of the symbol being renamed, for use in displaying information to the user.
        /// </summary>
        string FullDisplayName { get; }

        /// <summary>
        /// The kind of symbol being renamed, for use in displaying information to the user.
        /// </summary>
        SymbolKind Kind { get; }

        /// <summary>
        /// Find all locations to be renamed.
        /// </summary>
        /// <param name="renameInStrings">Whether or not to rename within strings</param>
        /// <param name="renameInComments">Whether or not to rename within comments</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns a list of DocumentSpans</returns>
        Task<IList<DocumentSpan>> FindRenameLocationsAsync(bool renameInStrings, bool renameInComments, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if the new replacement text is valid for this rename operation.
        /// </summary>
        /// <returns></returns>
        bool IsReplacementTextValid(string replacementText);
    }
}
