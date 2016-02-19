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

            // TODO: Uncomment pending https://github.com/dotnet/roslyn/issues/8927 .
            //public static readonly string CSharpInteractiveCommandSetIdString = "1492DB0A-85A2-4E43-BF0D-CE55B89A8CC6";
            //public static readonly Guid CSharpInteractiveCommandSetId = new Guid(CSharpInteractiveCommandSetIdString);

            //public static readonly string VisualBasicInteractiveCommandSetIdString = "93DF185E-D75B-4FDB-9D47-E90F111971C5";
            //public static readonly Guid VisualBasicInteractiveCommandSetId = new Guid(VisualBasicInteractiveCommandSetIdString);

            // TODO: Remove pending https://github.com/dotnet/roslyn/issues/8927 .
            public const int SmartExecute = 0x0103;
            public const int AbortExecution = 0x0104;
            public const int Reset = 0x0105;
            public const int HistoryNext = 0x00106;
            public const int HistoryPrevious = 0x00107;
            public const int ClearScreen = 0x00108;
            public const int BreakLine = 0x00109;
            public const int SearchHistoryNext = 0x0010A;
            public const int SearchHistoryPrevious = 0x0010B;
            public const int ExecuteInInteractiveWindow = 0x0010C;
            public const int CopyToInteractiveWindow = 0x0010D;
            public const int CopyCode = 0x0010E;
        }
    }
}
