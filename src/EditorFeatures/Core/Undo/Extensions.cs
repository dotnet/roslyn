// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Undo
{
    internal static class Extensions
    {
        /// <summary>
        /// Create a global undo transaction for the given workspace. if the host doesn't support undo transaction,
        /// useFallback flag can be used to indicate whether it should fallback to base implementation or not.
        /// </summary>
        public static IWorkspaceGlobalUndoTransaction OpenGlobalUndoTransaction(this Workspace workspace, string description, bool useFallback = true)
        {
            var undoService = workspace.Services.GetService<IGlobalUndoService>();

            try
            {
                // try using global undo service from host
                return undoService.OpenGlobalUndoTransaction(workspace, description);
            }
            catch (ArgumentException)
            {
                // it looks like it is not supported. 
                // check whether we should use fallback mechanism or not
                if (useFallback)
                {
                    return NoOpGlobalUndoServiceFactory.Transaction;
                }

                throw;
            }
        }
    }
}
