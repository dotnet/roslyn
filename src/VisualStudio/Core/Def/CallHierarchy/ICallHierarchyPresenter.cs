// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;

namespace Microsoft.CodeAnalysis.Editor.Host;

internal interface ICallHierarchyPresenter
{
    void PresentRoot(CallHierarchyItem root);
}
