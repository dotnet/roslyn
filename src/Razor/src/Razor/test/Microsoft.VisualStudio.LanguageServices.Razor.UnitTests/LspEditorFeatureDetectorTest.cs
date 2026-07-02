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

    public static TheoryData<bool, bool, bool, bool> IsRemoteClientTestData { get; } = new()
    {
        // isLiveShareHostActive, isLiveShareGuestActive, cloudEnvironmentConnectedActive, expectedResult
        { false, false, false, false },
        { true, false, false, false },
        { false, true, false, true },
        { false, false, true, true },
        { true, false, true, true },
        { true, true, true, true }
    };

    [UITheory]
    [MemberData(nameof(IsRemoteClientTestData))]
    public void IsRemoteClient(bool liveShareHostActive, bool liveShareGuestActive, bool cloudEnvironmentConnectedActive, bool expectedResult)
    {
        // Arrange
        var uiContextService = CreateUIContextService(liveShareHostActive, liveShareGuestActive, cloudEnvironmentConnectedActive);
        var featureDetector = CreateLspEditorFeatureDetector(uiContextService);

        // Act
        var result = featureDetector.IsRemoteClient();

        // Assert
        Assert.Equal(expectedResult, result);
    }

    public static TheoryData<bool, bool, bool, bool> IsLiveShareHostTestData { get; } = new()
    {
        // isLiveShareHostActive, isLiveShareGuestActive, cloudEnvironmentConnectedActive, expectedResult
        { false, false, false, false },
        { true, false, false, true },
        { false, true, false, false },
        { false, false, true, false },
        { true, false, true, true },
        { true, true, true, true }
    };

    [UITheory]
    [MemberData(nameof(IsLiveShareHostTestData))]
    public void IsLiveShareHost(bool liveShareHostActive, bool liveShareGuestActive, bool cloudEnvironmentConnectedActive, bool expectedResult)
    {
        // Arrange
        var uiContextService = CreateUIContextService(liveShareHostActive, liveShareGuestActive, cloudEnvironmentConnectedActive);
        var featureDetector = CreateLspEditorFeatureDetector(uiContextService);

        // Act
        var result = featureDetector.IsLiveShareHost();

        // Assert
        Assert.Equal(expectedResult, result);
    }

    private ILspEditorFeatureDetector CreateLspEditorFeatureDetector(IUIContextService uiContextService)
        => CreateLspEditorFeatureDetector(uiContextService, hasDotNetCoreCSharpCapability: true);

    private ILspEditorFeatureDetector CreateLspEditorFeatureDetector(
        bool hasDotNetCoreCSharpCapability = true)
    {
        return CreateLspEditorFeatureDetector(CreateUIContextService(), hasDotNetCoreCSharpCapability);
    }

    private ILspEditorFeatureDetector CreateLspEditorFeatureDetector(
        IUIContextService uiContextService,
        bool hasDotNetCoreCSharpCapability)
    {
        uiContextService ??= CreateUIContextService();

        var featureDetector = new LspEditorFeatureDetector(
            uiContextService,
            CreateProjectCapabilityResolver(hasDotNetCoreCSharpCapability),
            CreateRazorActivityLog());

        AddDisposable(featureDetector);

        return featureDetector;
    }

    private static IUIContextService CreateUIContextService(
        bool liveShareHostActive = false,
        bool liveShareGuestActive = false,
        bool cloudEnvironmentConnectedActive = false)
    {
        var mock = new StrictMock<IUIContextService>();

        mock.Setup(x => x.IsActive(Guids.LiveShareHostUIContextGuid))
            .Returns(liveShareHostActive);

        mock.Setup(x => x.IsActive(Guids.LiveShareGuestUIContextGuid))
            .Returns(liveShareGuestActive);

        mock.Setup(x => x.IsActive(VSConstants.UICONTEXT.CloudEnvironmentConnected_guid))
            .Returns(cloudEnvironmentConnectedActive);

        return mock.Object;
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
