using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;

#nullable enable

namespace Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators
{
    internal static class ITypeSymbolExtensions
    {
        public static bool IsReferenceTypeOrTypeParameter(this ITypeSymbol? type)
            => type != null &&
                !type.IsErrorType() &&
                !type.IsValueType &&
                (type is ITypeParameterSymbol typeParameter
                    ? typeParameter.HasReferenceTypeConstraint
                    : true);

    }
}
