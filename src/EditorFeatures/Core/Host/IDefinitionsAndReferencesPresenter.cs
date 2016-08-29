// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.FindReferences;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface IDefinitionsAndReferencesPresenter
    {
        void DisplayResult(DefinitionsAndReferences definitionsAndReferences);
    }
}