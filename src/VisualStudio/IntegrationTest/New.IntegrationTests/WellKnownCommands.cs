// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Design;
using Microsoft.VisualStudio;

namespace Roslyn.VisualStudio.IntegrationTests
{
    internal static class WellKnownCommands
    {
        public static class Edit
        {
            public static readonly CommandID RemoveAndSort = new(VSConstants.CMDSETID.CSharpGroup_guid, 6419);
        }
    }
}
