// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Razor.Logging;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor;

public class LspEditorFeatureDetectorTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    public static TheoryData<bool, bool> IsLspEditorEnabledAndSupportedTestData { get; } = new()
    {
        // hasDotNetCoreCSharpCapability, expectedResult
        { false, false }, // .Net Framework project - always non-LSP
        { true, true },  // .Net Core project
    };

    [UITheory]
    [MemberData(nameof(IsLspEditorEnabledAndSupportedTestData))]
    public void IsLspEditorEnabledAndSupported(
        bool hasDotNetCoreCSharpCapability,
        bool expectedResult)
    {
        // Arrange
        var featureDetector = CreateLspEditorFeatureDetector(hasDotNetCoreCSharpCapability);

        // Act
        var result = featureDetector.IsLspEditorSupported(@"c:\TestProject\TestFile.cshtml");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    private ILspEditorFeatureDetector CreateLspEditorFeatureDetector(
        bool hasDotNetCoreCSharpCapability = true)
    {
        return new LspEditorFeatureDetector(
            CreateProjectCapabilityResolver(hasDotNetCoreCSharpCapability),
            CreateRazorActivityLog());
    }

    private static IProjectCapabilityResolver CreateProjectCapabilityResolver(bool hasDotNetCoreCSharpCapability)
    {
        var projectCapabilityResolverMock = new StrictMock<IProjectCapabilityResolver>();

        projectCapabilityResolverMock
            .Setup(x => x.CheckCapability(WellKnownProjectCapabilities.DotNetCoreCSharp, It.IsAny<string>()))
            .Returns(new CapabilityCheckResult(IsInProject: true, HasCapability: hasDotNetCoreCSharpCapability));

        return projectCapabilityResolverMock.Object;
    }

    private RazorActivityLog CreateRazorActivityLog()
    {
        var vsActivityLogMock = new StrictMock<IVsActivityLog>();
        vsActivityLogMock
            .Setup(x => x.LogEntry(It.IsAny<uint>(), "Razor", It.IsAny<string>()))
            .Callback((uint entryType, string source, string description) =>
            {
                switch ((__ACTIVITYLOG_ENTRYTYPE)entryType)
                {
                    case __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR:
                        Logger.LogError($"Error:{description}");
                        break;

                    case __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING:
                        Logger.LogError($"Warning:{description}");
                        break;

                    case __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION:
                        Logger.LogError($"Info:{description}");
                        break;

                    default:
                        Assumed.Unreachable();
                        break;
                }
            })
            .Returns(VSConstants.S_OK);

        var serviceProvider = VsMocks.CreateAsyncServiceProvider(b =>
            b.AddService<SVsActivityLog, IVsActivityLog>(vsActivityLogMock.Object));

        var activityLog = new RazorActivityLog(serviceProvider);

        AddDisposable(activityLog);

        return activityLog;
    }
}
