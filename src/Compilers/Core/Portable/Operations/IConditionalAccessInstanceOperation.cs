// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents the value of a conditionally-accessed operation within <see cref="IConditionalAccessOperation.WhenNotNull" />.
    /// For a conditional access operation of the form <c>someExpr?.Member</c>, this operation is used as the InstanceReceiver for the right operation <c>Member</c>.
    /// See https://github.com/dotnet/roslyn/issues/21279#issuecomment-323153041 for more details.
    /// <para>
    /// Current usage:
    ///  (1) C# conditional access instance expression.
    ///  (2) VB conditional access instance expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConditionalAccessInstanceOperation : IOperation
    {
    }
}
