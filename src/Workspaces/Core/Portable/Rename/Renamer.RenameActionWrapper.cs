// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Wrapper class around <see cref="IRenameDocumentAction"/> for 
        /// while waiting for the public API changes to support a better model. 
        /// 
        /// https://github.com/dotnet/roslyn/issues/55539
        /// </summary>
        internal sealed class RenameActionWrapper : RenameDocumentAction
        {
            internal IRenameDocumentAction Action { get; }

            internal RenameActionWrapper(IRenameDocumentAction action)
            {
                Action = action;
            }

            public override string GetDescription(CultureInfo? culture = null) => Action.GetDescription(culture);
        }

    }
}
