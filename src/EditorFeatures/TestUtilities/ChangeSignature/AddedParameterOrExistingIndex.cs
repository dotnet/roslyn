// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature
{
    internal sealed class AddedParameterOrExistingIndex
    {
        public bool IsExisting { get; }

        public int? OldIndex { get; }

        private readonly AddedParameter? _addedParameterWithoutTypeSymbol;
        private readonly string? _addedParameterFullyQualifiedTypeName;

        public AddedParameterOrExistingIndex(int index)
        {
            OldIndex = index;
            IsExisting = true;
            _addedParameterWithoutTypeSymbol = null;
            _addedParameterFullyQualifiedTypeName = null;
        }

        public AddedParameterOrExistingIndex(AddedParameter addedParameterWithoutTypeSymbol, string addedParameterFullyQualifiedTypeName)
        {
            OldIndex = null;
            IsExisting = false;
            _addedParameterWithoutTypeSymbol = addedParameterWithoutTypeSymbol;
            _addedParameterFullyQualifiedTypeName = addedParameterFullyQualifiedTypeName;
        }

        public override string ToString()
            => IsExisting ? OldIndex.ToString() : (_addedParameterWithoutTypeSymbol?.ToString() ?? string.Empty);

        internal AddedParameter GetAddedParameter(Document document)
        {
            var semanticModel = document.GetRequiredSemanticModelAsync(CancellationToken.None).Result;

            var type = document.Project.Language switch
            {
                LanguageNames.CSharp => semanticModel.GetSpeculativeTypeInfo(0, CSharp.SyntaxFactory.ParseTypeName(_addedParameterFullyQualifiedTypeName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type,
                LanguageNames.VisualBasic => semanticModel.GetSpeculativeTypeInfo(0, VisualBasic.SyntaxFactory.ParseTypeName(_addedParameterFullyQualifiedTypeName), SpeculativeBindingOption.BindAsTypeOrNamespace).Type,
                _ => throw new ArgumentException("Unsupported language")
            };

            return new AddedParameter(
                type!,
                _addedParameterWithoutTypeSymbol!.TypeNameDisplayWithErrorIndicator,
                _addedParameterWithoutTypeSymbol.ParameterName,
                _addedParameterWithoutTypeSymbol.CallSiteValue,
                _addedParameterWithoutTypeSymbol.IsRequired,
                _addedParameterWithoutTypeSymbol.DefaultValue,
                _addedParameterWithoutTypeSymbol.IsCallsiteOmitted,
                _addedParameterWithoutTypeSymbol.IsCallsiteError);
        }
    }
}
