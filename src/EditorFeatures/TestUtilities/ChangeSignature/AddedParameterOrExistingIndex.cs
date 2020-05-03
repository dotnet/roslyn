// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal static AddedParameterOrExistingIndex CreateAdded(
            string fullTypeName,
            string parameterName,
            string callSiteValue = "",
            bool isRequired = true,
            string defaultValue = "",
            bool useNamedArguments = false,
            bool isCallsiteOmitted = false,
            bool isCallsiteTodo = false,
            bool typeBinds = true)
        {
            var parameter = new AddedParameter(
                type: null!, // Filled in later based on the fullTypeName
                typeName: null!, // Not needed for engine testing
                parameterName,
                callSiteValue,
                isRequired,
                defaultValue,
                useNamedArguments,
                isCallsiteOmitted,
                isCallsiteTodo,
                typeBinds);

            return new AddedParameterOrExistingIndex(parameter, fullTypeName);
        }

        public override string ToString()
            => IsExisting ? OldIndex.ToString() : (_addedParameterWithoutTypeSymbol?.ToString() ?? string.Empty);

        internal AddedParameter GetAddedParameter(Document document)
        {
            var semanticModel = document.GetRequiredSemanticModelAsync(CancellationToken.None).Result;

            var type = document.Project.Language switch
            {
                LanguageNames.CSharp => semanticModel.GetSpeculativeTypeInfo(0, CSharp.SyntaxFactory.ParseTypeName(_addedParameterFullyQualifiedTypeName!), SpeculativeBindingOption.BindAsTypeOrNamespace).Type,
                LanguageNames.VisualBasic => semanticModel.GetSpeculativeTypeInfo(0, VisualBasic.SyntaxFactory.ParseTypeName(_addedParameterFullyQualifiedTypeName!), SpeculativeBindingOption.BindAsTypeOrNamespace).Type,
                _ => throw new ArgumentException("Unsupported language")
            };

            if (type == null)
            {
                throw new ArgumentException($"Could not bind type {_addedParameterFullyQualifiedTypeName}", nameof(_addedParameterFullyQualifiedTypeName));
            }

            return new AddedParameter(
                type,
                _addedParameterWithoutTypeSymbol!.TypeName,
                _addedParameterWithoutTypeSymbol.Name,
                _addedParameterWithoutTypeSymbol.CallSiteValue,
                _addedParameterWithoutTypeSymbol.IsRequired,
                _addedParameterWithoutTypeSymbol.DefaultValue,
                _addedParameterWithoutTypeSymbol.UseNamedArguments,
                _addedParameterWithoutTypeSymbol.IsCallsiteOmitted,
                _addedParameterWithoutTypeSymbol.IsCallsiteTodo);
        }
    }
}
