// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class LocalFunctionOrSourceMemberMethodSymbol : SourceMethodSymbol
    {
        private TypeWithAnnotations.Boxed? _lazyIteratorElementType;

        protected LocalFunctionOrSourceMemberMethodSymbol(SyntaxReference? syntaxReferenceOpt, bool isIterator)
            : base(syntaxReferenceOpt)
        {
            if (isIterator)
            {
                _lazyIteratorElementType = TypeWithAnnotations.Boxed.Sentinel;
            }
        }

        internal sealed override TypeWithAnnotations IteratorElementTypeWithAnnotations
        {
            get
            {
                if (_lazyIteratorElementType == TypeWithAnnotations.Boxed.Sentinel)
                {
                    TypeWithAnnotations elementType = InMethodBinder.GetIteratorElementTypeFromReturnType(DeclaringCompilation, RefKind, ReturnType, errorLocation: null, diagnostics: null);

                    if (elementType.IsDefault)
                    {
                        elementType = TypeWithAnnotations.Create(new ExtendedErrorTypeSymbol(DeclaringCompilation, name: "", arity: 0, errorInfo: null, unreported: false));
                    }

                    Interlocked.CompareExchange(ref _lazyIteratorElementType, new TypeWithAnnotations.Boxed(elementType), TypeWithAnnotations.Boxed.Sentinel);

                    Debug.Assert(TypeSymbol.Equals(_lazyIteratorElementType.Value.Type, elementType.Type, TypeCompareKind.ConsiderEverything));
                }

                return _lazyIteratorElementType?.Value ?? default;
            }
        }

        internal sealed override bool IsIterator => _lazyIteratorElementType is object;

        /// <summary>
        /// Whether the method has the <see langword="unsafe"/> keyword in its signature.
        /// Do not confuse with <see cref="IsCallerUnsafe"/>.
        /// </summary>
        internal abstract bool IsUnsafe { get; }

        internal sealed override bool IsCallerUnsafe
        {
            get
            {
                return ContainingModule.UseUpdatedMemorySafetyRules
                    ? IsUnsafe
                    : this.HasParameterContainingPointerType() || ReturnType.ContainsPointerOrFunctionPointer();
            }
        }
    }
}
