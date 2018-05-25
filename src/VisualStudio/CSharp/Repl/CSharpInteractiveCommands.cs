// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    internal static class CSharpInteractiveCommands
    {
        public const int InteractiveToolWindow = 0x0001;
        public const int ResetInteractiveFromProject = 0x0002;

        public const string InteractiveCommandSetIdString = "1492DB0A-85A2-4E43-BF0D-CE55B89A8CC6";
        public static readonly Guid InteractiveCommandSetId = new Guid(InteractiveCommandSetIdString);
    }
}
