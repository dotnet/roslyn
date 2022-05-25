// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder keeps track of the set of constant fields that are currently being evaluated
    /// so that the set can be passed into the next call to SourceFieldSymbol.ConstantValue (and
    /// its callers).
    /// </summary>
    internal sealed class ConstantFieldsInProgressBinder : Binder
    {
        private readonly ConstantFieldsInProgress _inProgress;

        internal ConstantFieldsInProgressBinder(ConstantFieldsInProgress inProgress, Binder next)
            : base(next, BinderFlags.FieldInitializer | next.Flags)
        {
            _inProgress = inProgress;
        }

        internal override ConstantFieldsInProgress ConstantFieldsInProgress
        {
            get
            {
                return _inProgress;
            }
        }
    }
}
