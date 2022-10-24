// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.ResultSetTracking
{
    internal static class IResultSetTrackerExtensions
    {
        /// <summary>
        /// Returns the ID of the <see cref="ReferenceResult"/> for a <see cref="ResultSet"/>.
        /// </summary>
        public static Id<ReferenceResult> GetResultSetReferenceResultId(this IResultSetTracker tracker, ISymbol symbol, IdFactory idFactory)
            => tracker.GetResultIdForSymbol(symbol, Methods.TextDocumentReferencesName, () => new ReferenceResult(idFactory));
    }
}
