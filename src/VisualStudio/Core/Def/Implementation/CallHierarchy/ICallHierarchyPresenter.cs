// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface ICallHierarchyPresenter
    {
        void PresentRoot(CallHierarchyItem root);
    }
}
