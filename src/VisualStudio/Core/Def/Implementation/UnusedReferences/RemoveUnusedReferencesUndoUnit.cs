// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences
{
    internal sealed partial class RemoveUnusedReferencesCommandHandler
    {
        internal sealed class UpdateReferencesUndoUnit : IOleUndoUnit
        {
            private readonly ImmutableArray<IUpdateReferenceOperation> _updateOperations;

            public UpdateReferencesUndoUnit(
                ImmutableArray<IUpdateReferenceOperation> updateOperations)
            {
                _updateOperations = updateOperations;
            }

            public void Do(IOleUndoManager pUndoManager)
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    foreach (var operation in _updateOperations)
                    {
                        await operation.RevertAsync(CancellationToken.None).ConfigureAwait(true);
                    }
                });
            }

            public void GetDescription(out string pBstr)
                => pBstr = "Update references";

            public void GetUnitType(out Guid pClsid, out int plID)
                => throw new NotImplementedException();

            public void OnNextAdd()
            {
            }
        }
    }
}
