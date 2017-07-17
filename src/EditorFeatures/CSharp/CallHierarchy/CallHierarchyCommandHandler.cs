// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.CallHierarchy;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CallHierarchy
{
    [ExportCommandHandler("CallHierarchy", ContentTypeNames.CSharpContentType)]
    [Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
    internal class CallHierarchyCommandHandler : AbstractCallHierarchyCommandHandler
    {
        [ImportingConstructor]
        public CallHierarchyCommandHandler([ImportMany] IEnumerable<ICallHierarchyPresenter> presenters, CallHierarchyProvider provider, IWaitIndicator waitIndicator)
            : base(presenters, provider, waitIndicator)
        {
        }
    }
}
