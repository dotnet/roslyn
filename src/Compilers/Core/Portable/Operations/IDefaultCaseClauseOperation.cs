﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a default case clause.
    /// <para>
    /// Current usage:
    ///  (1) C# default clause.
    ///  (2) VB Case Else clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDefaultCaseClauseOperation : ICaseClauseOperation
    {
    }
}
