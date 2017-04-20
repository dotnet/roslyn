// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "../../../../Perf.Utilities/Roslyn.Test.Performance.Utilities.dll"
#r "../../UnitTests/VisualStudioIntegrationTests/VisualStudioIntegrationTests.dll"

using System.IO;
using System.Collections.Generic;
using Roslyn.Test.Performance.Utilities;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

class CSharpTypingTest : VsPerfTest
{
    public CSharpTypingTest(
        string testTemplateName,
        string[] scenarios)
        : base(testTemplateName,
            testFolderName: "csharp_typing",
            solutionToTest: "Roslyn-CSharp.sln",
            benchviewUploadName: "typing",
            scenarios: scenarios)
    {
    }

    protected override string GetFinalTestSource(string testTemplateWithReplacedSolutionPath, string testFolderDirectory)
    {
        var typingInputPath = Path.Combine(testFolderDirectory, "TypingInputs", "CSharpGoldilocksInput-MultipliedDelay.txt");
        return finalTest.Replace("ReplaceWithTypingInputPath", typingInputPath);
    }
}

TestThisPlease(
    new CSharpTypingTest(
        "CSharpPerfGoldilocksTypingFullSolutionDiagnosticsMultipliedDelayTemplate.xml",
        new string[] { }));

