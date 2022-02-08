// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.VisualStudio.LanguageServices
{
    internal static partial class ID
    {
        /// <summary>
        /// Commands using the old C# command set GUID.
        /// </summary>
        public static class CSharpCommands
        {
            public const int AutoTokenCompletionForward = 0x1800;           // cmdidAutoTokenCompletionForward
            public const int AutoTokenCompletionBackward = 0x1801;          // cmdidAutoTokenCompletionBackward

            public const int ContextMenuGenerateMethodStub = 0x1900;        // cmdidContextMenuGenerateMethodStub
            public const int ContextImplementInterfaceImplicit = 0x1901;    // cmdidContextImplementInterfaceImplicit
            public const int ContextImplementInterfaceExplicit = 0x1902;    // cmdidContextImplementInterfaceExplicit
            public const int ContextImplementAbstractClass = 0x1903;        // cmdidContextImplementAbstractClass

            public const int ContextOrganizeRemoveAndSort = 0x1913;         // cmdidContextOrganizeRemoveAndSort
            public const int OrganizeSortUsings = 0x1922;                   // cmdidCSharpOrganizeSortUsings
            public const int OrganizeRemoveAndSort = 0x1923;                // cmdidCSharpOrganizeRemoveAndSort

            public const int SmartTagRename = 0x2000;                       // cmdidSmartTagRename
            public const int SmartTagRenameWithPreview = 0x2001;            // cmdidSmartTagRenameWithPreview
            public const int SmartTagReorderParameters = 0x2002;            // cmdidSmartTagReorderParameters
            public const int SmartTagRemoveParameters = 0x2003;             // cmdidSmartTagRemoveParameters

            public const int SmartTagImplementImplicit = 0x3000;            // cmdidSmartTagImplementImplicit
            public const int SmartTagImplementExplicit = 0x3001;            // cmdidSmartTagImplementExplicit
            public const int SmartTagImplementAbstract = 0x3002;            // cmdidSmartTagImplementAbstract

            public const int AddUsingForUnboundItem0 = 0x4000;              // cmdidCSharpAddUsingForUnboundItem0
            public const int AddUsingForUnboundItem1 = 0x4001;              // cmdidCSharpAddUsingForUnboundItem1
            public const int AddUsingForUnboundItem2 = 0x4002;              // cmdidCSharpAddUsingForUnboundItem2
            public const int AddUsingForUnboundItem3 = 0x4003;              // cmdidCSharpAddUsingForUnboundItem3
            public const int AddUsingForUnboundItem4 = 0x4004;              // cmdidCSharpAddUsingForUnboundItem4
            public const int AddUsingForUnboundItemMin = AddUsingForUnboundItem0;
            public const int AddUsingForUnboundItemMax = AddUsingForUnboundItem4;

            public const int SmartTagAddUsingForUnboundItem0 = 0x4005;      // cmdidSmartTagAddUsingForUnboundItem0
            public const int SmartTagAddUsingForUnboundItem1 = 0x4006;      // cmdidSmartTagAddUsingForUnboundItem1
            public const int SmartTagAddUsingForUnboundItem2 = 0x4007;      // cmdidSmartTagAddUsingForUnboundItem2
            public const int SmartTagAddUsingForUnboundItem3 = 0x4008;      // cmdidSmartTagAddUsingForUnboundItem3
            public const int SmartTagAddUsingForUnboundItem4 = 0x4009;      // cmdidSmartTagAddUsingForUnboundItem4
            public const int SmartTagAddUsingForUnboundItemMin = SmartTagAddUsingForUnboundItem0;
            public const int SmartTagAddUsingForUnboundItemMax = SmartTagAddUsingForUnboundItem4;

            public const int FullyQualifyUnboundItem0 = 0x4010;             // cmdidCSharpFullyQualifyUnboundItem0
            public const int FullyQualifyUnboundItem1 = 0x4011;             // cmdidCSharpFullyQualifyUnboundItem1
            public const int FullyQualifyUnboundItem2 = 0x4012;             // cmdidCSharpFullyQualifyUnboundItem2
            public const int FullyQualifyUnboundItem3 = 0x4013;             // cmdidCSharpFullyQualifyUnboundItem3
            public const int FullyQualifyUnboundItem4 = 0x4014;             // cmdidCSharpFullyQualifyUnboundItem4
            public const int FullyQualifyUnboundItemMin = FullyQualifyUnboundItem0;
            public const int FullyQualifyUnboundItemMax = FullyQualifyUnboundItem4;

            public const int SmartTagFullyQualifyUnboundItem0 = 0x4015;     // cmdidSmartTagFullyQualifyUnboundItem0
            public const int SmartTagFullyQualifyUnboundItem1 = 0x4016;     // cmdidSmartTagFullyQualifyUnboundItem1
            public const int SmartTagFullyQualifyUnboundItem2 = 0x4017;     // cmdidSmartTagFullyQualifyUnboundItem2
            public const int SmartTagFullyQualifyUnboundItem3 = 0x4018;     // cmdidSmartTagFullyQualifyUnboundItem3
            public const int SmartTagFullyQualifyUnboundItem4 = 0x4019;     // cmdidSmartTagFullyQualifyUnboundItem4
            public const int SmartTagFullyQualifyUnboundItemMin = SmartTagFullyQualifyUnboundItem0;
            public const int SmartTagFullyQualifyUnboundItemMax = SmartTagFullyQualifyUnboundItem4;

            public const int PartialMatchForUnboundItem0 = 0x4020;          // cmdidCSharpPartialMatchForUnboundItem0
            public const int PartialMatchForUnboundItem1 = 0x4021;          // cmdidCSharpPartialMatchForUnboundItem1
            public const int PartialMatchForUnboundItem2 = 0x4022;          // cmdidCSharpPartialMatchForUnboundItem2
            public const int PartialMatchForUnboundItem3 = 0x4023;          // cmdidCSharpPartialMatchForUnboundItem3
            public const int PartialMatchForUnboundItem4 = 0x4024;          // cmdidCSharpPartialMatchForUnboundItem4
            public const int PartialMatchForUnboundItem5 = 0x4025;          // cmdidCSharpPartialMatchForUnboundItem5
            public const int PartialMatchForUnboundItem6 = 0x4026;          // cmdidCSharpPartialMatchForUnboundItem6
            public const int PartialMatchForUnboundItem7 = 0x4027;          // cmdidCSharpPartialMatchForUnboundItem7
            public const int PartialMatchForUnboundItemMin = PartialMatchForUnboundItem0;
            public const int PartialMatchForUnboundItemMax = PartialMatchForUnboundItem7;

            public const int SmartTagPartialMatchForUnboundItem0 = 0x4028;  // cmdidSmartTagPartialMatchForUnboundItem0
            public const int SmartTagPartialMatchForUnboundItem1 = 0x4029;  // cmdidSmartTagPartialMatchForUnboundItem1
            public const int SmartTagPartialMatchForUnboundItem2 = 0x402A;  // cmdidSmartTagPartialMatchForUnboundItem2
            public const int SmartTagPartialMatchForUnboundItem3 = 0x402B;  // cmdidSmartTagPartialMatchForUnboundItem3
            public const int SmartTagPartialMatchForUnboundItem4 = 0x402C;  // cmdidSmartTagPartialMatchForUnboundItem4
            public const int SmartTagPartialMatchForUnboundItem5 = 0x402D;  // cmdidSmartTagPartialMatchForUnboundItem5
            public const int SmartTagPartialMatchForUnboundItem6 = 0x402E;  // cmdidSmartTagPartialMatchForUnboundItem6
            public const int SmartTagPartialMatchForUnboundItem7 = 0x402F;  // cmdidSmartTagPartialMatchForUnboundItem7
            public const int SmartTagPartialMatchForUnboundItemMin = SmartTagPartialMatchForUnboundItem0;
            public const int SmartTagPartialMatchForUnboundItemMax = SmartTagPartialMatchForUnboundItem7;

            public const int SmartTagGenerateMethodStub = 0x5000;           // cmdidSmartTagGenerateMethodStub

            public const int MenuGenerateConstructor = 0x5010;              // cmdidMenuGenerateConstructor
            public const int MenuGenerateProperty = 0x5011;                 // cmdidMenuGenerateProperty
            public const int MenuGenerateClass = 0x5012;                    // cmdidMenuGenerateClass
            public const int MenuGenerateNewType = 0x5013;                  // cmdidMenuGenerateNewType
            public const int MenuGenerateEnumMember = 0x5014;               // cmdidMenuGenerateEnumMember
            public const int MenuGenerateField = 0x5015;                    // cmdidMenuGenerateField

            public const int ContextMenuGenerateConstructor = 0x5020;       // cmdidContextMenuGenerateConstructor
            public const int ContextMenuGenerateProperty = 0x5021;          // cmdidContextMenuGenerateProperty
            public const int ContextMenuGenerateClass = 0x5022;             // cmdidContextMenuGenerateClass
            public const int ContextMenuGenerateNewType = 0x5023;           // cmdidContextMenuGenerateNewType
            public const int ContextMenuGenerateEnumMember = 0x5024;        // cmdidContextMenuGenerateEnumMember
            public const int ContextMenuGenerateField = 0x5025;             // cmdidContextMenuGenerateField

            public const int SmartTagGenerateConstructor = 0x5030;          // cmdidSmartTagGenerateConstructor
            public const int SmartTagGenerateProperty = 0x5031;             // cmdidSmartTagGenerateProperty
            public const int SmartTagGenerateClass = 0x5032;                // cmdidSmartTagGenerateClass
            public const int SmartTagGenerateNewType = 0x5036;              // cmdidSmartTagGenerateNewType
            public const int SmartTagGenerateEnumMember = 0x5037;           // cmdidSmartTagGenerateEnumMember
            public const int SmartTagGenerateField = 0x5038;                // cmdidSmartTagGenerateField

            public const int FormatComment = 0x6000;                        // cmdidCSharpFormatComment
            public const int ContextFormatComment = 0x6001;                 // cmdidContextFormatComment
        }
    }
}
