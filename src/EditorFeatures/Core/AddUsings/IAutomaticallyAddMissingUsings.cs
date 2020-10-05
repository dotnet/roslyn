// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.AddMissingImports
{
    internal interface IAutomaticallyAddMissingUsingsService : IWorkspaceService
    {
        void AddMissingUsings(Document document, TextSpan textSpan, IUIThreadOperationContext operationContext);
    }
}
