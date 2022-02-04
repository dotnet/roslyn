// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider
    {
        private struct AddConstructorParameterResult
        {
            internal readonly ImmutableArray<AddConstructorParametersCodeAction> RequiredParameterActions;
            internal readonly ImmutableArray<AddConstructorParametersCodeAction> OptionalParameterActions;
            internal readonly bool UseSubMenu;

            public AddConstructorParameterResult(
                ImmutableArray<AddConstructorParametersCodeAction> requiredParameterActions,
                ImmutableArray<AddConstructorParametersCodeAction> optionalParameterActions,
                bool useSubMenu)
            {
                RequiredParameterActions = requiredParameterActions;
                OptionalParameterActions = optionalParameterActions;
                UseSubMenu = useSubMenu;
            }
        }
    }
}
