// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder keeps track of the type for which we are trying to
    /// determine whether it is a valid 'params' collection type.
    /// </summary>
    internal sealed class ParamsCollectionTypeInProgressBinder : Binder
    {
        private readonly NamedTypeSymbol _inProgress;
        private readonly MethodSymbol? _constructorInProgress;

        internal ParamsCollectionTypeInProgressBinder(
            NamedTypeSymbol inProgress,
            Binder next,
            bool bindingCollectionExpressionWithArguments,
            MethodSymbol? constructorInProgress = null)
            : base(next, next.Flags | BinderFlags.CollectionExpressionConversionValidation)
        {
            Debug.Assert(inProgress is not null);

            _inProgress = inProgress;
            _constructorInProgress = constructorInProgress;
            this.BindingCollectionExpressionWithArguments = bindingCollectionExpressionWithArguments;
        }

        internal override bool BindingCollectionExpressionWithArguments { get; }

        internal override NamedTypeSymbol ParamsCollectionTypeInProgress => _inProgress;

        internal override MethodSymbol? ParamsCollectionConstructorInProgress => _constructorInProgress;
    }
}
