// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
{
    [ExportWorkspaceService(typeof(IChangeSignatureOptionsService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal class TestChangeSignatureOptionsService : IChangeSignatureOptionsService
    {
        public AddedParameterOrExistingIndex[]? UpdatedSignature = null;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestChangeSignatureOptionsService()
        {
        }

        ChangeSignatureOptionsResult IChangeSignatureOptionsService.GetChangeSignatureOptions(
            Document document,
            int positionForTypeBinding,
            ISymbol symbol,
            ParameterConfiguration parameters)
        {
            var list = parameters.ToListOfParameters();
            var updateParameters = UpdatedSignature != null
                ? UpdatedSignature.Select(item => item.IsExisting ? list[item.OldIndex ?? -1] : item.GetAddedParameter(document)).ToImmutableArray()
                : new ImmutableArray<Parameter>();
            return new ChangeSignatureOptionsResult(new SignatureChange(
                    parameters,
                    UpdatedSignature == null
                    ? parameters
                    : ParameterConfiguration.Create(updateParameters, parameters.ThisParameter != null, selectedIndex: 0)), previewChanges: false);
        }
    }
}
