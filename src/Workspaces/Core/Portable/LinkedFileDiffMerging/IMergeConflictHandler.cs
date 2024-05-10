// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal interface IMergeConflictHandler
{
    List<TextChange> CreateEdits(SourceText originalSourceText, List<UnmergedDocumentChanges> unmergedChanges);
}
