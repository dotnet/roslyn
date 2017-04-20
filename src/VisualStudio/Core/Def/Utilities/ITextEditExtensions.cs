// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Text
{
    internal static class ITextEditExtensions
    {
        /// <summary>
        /// If exceptions are thrown we need to cancel the edit so subsequent edits can succeed.
        /// Exceptions are not handled but thrown so we can still diagnose what the root cause is.
        /// </summary>
        /// <param name="edit"></param>
        internal static void ApplyAndCancelOnException(this ITextEdit edit)
        {
            try
            {
                edit.Apply();
            }
            catch (Exception)
            {
                edit.Cancel();
                throw;
            }
        }
    }
}
