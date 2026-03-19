' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
    Public Enum RenameTestHost
        InProcess
        ' Work out of process, marshaling to/from RenameSymbolAsync
        OutOfProcess_SingleCall
        ' Work out of process, marshaling to/from FindRenameLocations, then marshaling to/from ResolveConflictsAsync
        OutOfProcess_SplitCall
    End Enum
End Namespace
