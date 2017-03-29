// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public static readonly string CSharpInteractiveCommandSetIdString = "1492DB0A-85A2-4E43-BF0D-CE55B89A8CC6";
            public static readonly Guid CSharpInteractiveCommandSetId = new Guid(CSharpInteractiveCommandSetIdString);

            public static readonly string VisualBasicInteractiveCommandSetIdString = "93DF185E-D75B-4FDB-9D47-E90F111971C5";
            public static readonly Guid VisualBasicInteractiveCommandSetId = new Guid(VisualBasicInteractiveCommandSetIdString);
        }
    }
}
