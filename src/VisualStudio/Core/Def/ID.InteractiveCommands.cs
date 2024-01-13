// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static partial class ID
    {
        internal static class InteractiveCommands
        {
            public const int InteractiveToolWindow = 0x0001;
            public const int ResetInteractiveFromProject = 0x0002;

            // TODO: Remove pending https://github.com/dotnet/roslyn/issues/8927 .
            public const int ExecuteInInteractiveWindow = 0x0010C;

            public const string CSharpInteractiveCommandSetIdString = "1492DB0A-85A2-4E43-BF0D-CE55B89A8CC6";
            public static readonly Guid CSharpInteractiveCommandSetId = new(CSharpInteractiveCommandSetIdString);

            public const string VisualBasicInteractiveCommandSetIdString = "93DF185E-D75B-4FDB-9D47-E90F111971C5";
            public static readonly Guid VisualBasicInteractiveCommandSetId = new(VisualBasicInteractiveCommandSetIdString);
        }
    }
}
