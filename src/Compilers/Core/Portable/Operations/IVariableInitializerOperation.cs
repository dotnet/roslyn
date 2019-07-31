// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an initialization of a local variable.
    /// <para>
    /// Current usage:
    ///  (1) C# local variable initializer with equals value clause.
    ///  (2) VB local variable initializer with equals value clause or AsNew clause.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IVariableInitializerOperation : ISymbolInitializerOperation
    {
    }
}
