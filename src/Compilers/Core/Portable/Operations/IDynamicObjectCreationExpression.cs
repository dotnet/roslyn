﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a dynamically bound new/New expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDynamicObjectCreationExpression : IHasDynamicArgumentsExpression
    {
        /// <summary>
        /// Object or collection initializer, if any.
        /// </summary>
        IObjectOrCollectionInitializerExpression Initializer { get; }
    }
}
