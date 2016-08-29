// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindReferences;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
{
    internal class MockDefinitionsAndReferencesPresenter : IDefinitionsAndReferencesPresenter
    {
        public DefinitionsAndReferences DefinitionsAndReferences;

        public void DisplayResult(DefinitionsAndReferences definitionsAndReferences)
        {
            DefinitionsAndReferences = definitionsAndReferences;
        }
    }
}