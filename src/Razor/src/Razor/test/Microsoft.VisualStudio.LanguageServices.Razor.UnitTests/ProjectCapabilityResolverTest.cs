// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor;

public class ProjectCapabilityResolverTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private const string DocumentPath = @"C:\TestProject\Component.razor";
    private const string ProjectPath = @"C:\TestProject\TestProject.csproj";
    private const string HasCapabilityKey = "hasCapability";
    private const string HasProjectPathKey = "hasProjectPath";

    [UITheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CheckCapability_LocalProject_ReturnsCapabilityMatch(bool hasCapability)
    {
        var result = CheckLocalCapability(hasCapability, hasProjectPath: false);

        Assert.Equal(new CapabilityCheckResult(IsInProject: true, HasCapability: hasCapability), result);
    }

    [UITheory]
    [InlineData(false, __VSDOCINPROJECT.DOCINPROJ_DocNotInProject)]
    [InlineData(true, __VSDOCINPROJECT.DOCINPROJ_DocNotInProject)]
    [InlineData(false, __VSDOCINPROJECT.DOCINPROJ_DocInProject)]
    public void CheckCapability_DocumentOutsideProject_ReturnsNoCapability(
        bool hasHierarchy,
        __VSDOCINPROJECT documentInProject)
    {
        var hierarchy = hasHierarchy ? StrictMock.Of<IVsUIHierarchy>() : null;
        var openDocument = CreateOpenDocument(hierarchy, documentInProject);
        var listener = new StrictMock<IProjectCapabilityListener>();
        var resolver = CreateResolver(openDocument, listener.Object);

        var result = resolver.CheckCapability(WellKnownProjectCapabilities.DotNetCoreCSharp, DocumentPath);

        Assert.Equal(new CapabilityCheckResult(IsInProject: false, HasCapability: false), result);
        openDocument.VerifyAll();
        listener.VerifyNoOtherCalls();
    }

    [UIFact]
    public void CheckCapability_ProjectLookupFails_ReturnsNoCapability()
    {
        var openDocument = CreateOpenDocument(StrictMock.Of<IVsUIHierarchy>(), hresult: VSConstants.E_FAIL);
        var listener = new StrictMock<IProjectCapabilityListener>();
        var resolver = CreateResolver(openDocument, listener.Object);

        var result = resolver.CheckCapability(WellKnownProjectCapabilities.DotNetCoreCSharp, DocumentPath);

        Assert.Equal(new CapabilityCheckResult(IsInProject: false, HasCapability: false), result);
        openDocument.VerifyAll();
        listener.VerifyNoOtherCalls();
    }

    [UITheory]
    [InlineData(true)]
    [InlineData(false)]
    public void CheckCapability_LocalProject_NotifiesAllListeners(bool hasCapability)
    {
        var result = CheckLocalCapability(hasCapability, hasProjectPath: true);

        Assert.Equal(new CapabilityCheckResult(IsInProject: true, HasCapability: hasCapability), result);
    }

    private static CapabilityCheckResult CheckLocalCapability(bool hasCapability, bool hasProjectPath)
    {
        // The shell caches its capability evaluator globally, so keep its test services in a separate AppDomain.
        var domain = AppDomain.CreateDomain(
            nameof(ProjectCapabilityResolverTest),
            securityInfo: null,
            new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            });

        try
        {
            domain.SetData(HasCapabilityKey, hasCapability);
            domain.SetData(HasProjectPathKey, hasProjectPath);
            domain.DoCallBack(CheckLocalCapabilityInDomain);
            return new CapabilityCheckResult(
                IsInProject: (bool)domain.GetData(nameof(CapabilityCheckResult.IsInProject)),
                HasCapability: (bool)domain.GetData(nameof(CapabilityCheckResult.HasCapability)));
        }
        finally
        {
            AppDomain.Unload(domain);
        }
    }

    private static void CheckLocalCapabilityInDomain()
    {
        var hasCapability = (bool)AppDomain.CurrentDomain.GetData(HasCapabilityKey);
        var hasProjectPath = (bool)AppDomain.CurrentDomain.GetData(HasProjectPathKey);
        InitializeCapabilityEvaluator();
        var hierarchy = CreateHierarchy(hasCapability);
        var firstListener = new StrictMock<IProjectCapabilityListener>();
        var secondListener = new StrictMock<IProjectCapabilityListener>();

        if (hasProjectPath)
        {
            var projectPath = ProjectPath;
            hierarchy.As<IVsProject>()
                .Setup(p => p.GetMkDocument((uint)VSConstants.VSITEMID.Root, out projectPath))
                .Returns(VSConstants.S_OK);
            firstListener.Setup(l => l.OnProjectCapabilityMatched(ProjectPath, WellKnownProjectCapabilities.DotNetCoreCSharp, hasCapability));
            secondListener.Setup(l => l.OnProjectCapabilityMatched(ProjectPath, WellKnownProjectCapabilities.DotNetCoreCSharp, hasCapability));
        }

        var openDocument = CreateOpenDocument(hierarchy.Object);
        var loggerFactory = new StrictMock<ILoggerFactory>();
        loggerFactory.Setup(f => f.GetOrCreateLogger("Razor.ProjectCapabilityResolver"))
            .Returns(StrictMock.Of<ILogger>());
        using var context = new JoinableTaskContext();
        using var resolver = new ProjectCapabilityResolver(
            VsMocks.CreateVsService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(openDocument),
            [firstListener.Object, secondListener.Object],
            loggerFactory.Object,
            context);

        var result = resolver.CheckCapability(WellKnownProjectCapabilities.DotNetCoreCSharp, DocumentPath);

        openDocument.VerifyAll();
        hierarchy.VerifyAll();

        if (hasProjectPath)
        {
            firstListener.Verify(l => l.OnProjectCapabilityMatched(ProjectPath, WellKnownProjectCapabilities.DotNetCoreCSharp, hasCapability), Times.Once());
            secondListener.Verify(l => l.OnProjectCapabilityMatched(ProjectPath, WellKnownProjectCapabilities.DotNetCoreCSharp, hasCapability), Times.Once());
        }
        else
        {
            firstListener.VerifyNoOtherCalls();
            secondListener.VerifyNoOtherCalls();
        }

        AppDomain.CurrentDomain.SetData(nameof(CapabilityCheckResult.IsInProject), result.IsInProject);
        AppDomain.CurrentDomain.SetData(nameof(CapabilityCheckResult.HasCapability), result.HasCapability);
    }

    private static void InitializeCapabilityEvaluator()
    {
        var evaluator = new StrictMock<IVsBooleanSymbolExpressionEvaluator>();
        evaluator.Setup(e => e.EvaluateExpression(
            WellKnownProjectCapabilities.DotNetCoreCSharp, WellKnownProjectCapabilities.DotNetCoreCSharp))
            .Returns(true);
        evaluator.Setup(e => e.EvaluateExpression(WellKnownProjectCapabilities.DotNetCoreCSharp, string.Empty))
            .Returns(false);
        var registry = new StrictMock<ILocalRegistry>();
        var interfaceId = VSConstants.IID_IUnknown;
        registry.Setup(r => r.CreateInstance(
            typeof(BooleanSymbolExpressionEvaluator).GUID, null, ref interfaceId, 1u, out It.Ref<nint>.IsAny))
            .Callback(new CreateInstanceCallback((Guid _, object? _, ref Guid _, uint _, out nint instance) =>
                instance = Marshal.GetIUnknownForObject(evaluator.Object)))
            .Returns(VSConstants.S_OK);

        var activityLog = new StrictMock<IVsActivityLog>();
        activityLog.Setup(l => l.LogEntry(
            (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION,
            "Package Framework",
            "Initialized ServiceProvider.GlobalProvider from SetSite."))
            .Returns(VSConstants.S_OK);
        ServiceProvider.CreateFromSetSite(new CapabilityServiceProvider(registry.Object, activityLog.Object));
    }

    private delegate void CreateInstanceCallback(Guid classId, object? outer, ref Guid interfaceId, uint classContext, out nint instance);

    private sealed class CapabilityServiceProvider(ILocalRegistry registry, IVsActivityLog activityLog) : Microsoft.VisualStudio.OLE.Interop.IServiceProvider
    {
        public int QueryService(ref Guid serviceId, ref Guid interfaceId, out nint service)
        {
            if (serviceId == typeof(SLocalRegistry).GUID)
            {
                service = Marshal.GetIUnknownForObject(registry);
                return VSConstants.S_OK;
            }

            if (serviceId == typeof(SVsActivityLog).GUID)
            {
                service = Marshal.GetIUnknownForObject(activityLog);
                return VSConstants.S_OK;
            }

            service = 0;
            return VSConstants.E_NOINTERFACE;
        }
    }

    private static StrictMock<IVsUIHierarchy> CreateHierarchy(bool hasCapability)
    {
        var hierarchy = new StrictMock<IVsUIHierarchy>();
        var properties = hierarchy.As<IVsHierarchy>();
        object? checker = null;
        properties.Setup(h => h.GetProperty(
            (uint)VSConstants.VSITEMID.Root,
            (int)__VSHPROPID8.VSHPROPID_ProjectCapabilitiesChecker,
            out checker))
            .Returns(VSConstants.E_NOTIMPL);
        object capabilities = hasCapability ? WellKnownProjectCapabilities.DotNetCoreCSharp : string.Empty;
        properties.Setup(h => h.GetProperty(
            (uint)VSConstants.VSITEMID.Root,
            (int)__VSHPROPID5.VSHPROPID_ProjectCapabilities,
            out capabilities))
            .Returns(VSConstants.S_OK);

        return hierarchy;
    }

    private static StrictMock<IVsUIShellOpenDocument> CreateOpenDocument(
        IVsUIHierarchy? hierarchy,
        __VSDOCINPROJECT documentInProject = __VSDOCINPROJECT.DOCINPROJ_DocInProject,
        int hresult = VSConstants.S_OK)
    {
        var openDocument = new StrictMock<IVsUIShellOpenDocument>();
        uint itemId = 0;
        Microsoft.VisualStudio.OLE.Interop.IServiceProvider? serviceProvider = null;
        var inProject = (int)documentInProject;
        openDocument.Setup(d => d.IsDocumentInAProject(
            DocumentPath, out hierarchy, out itemId, out serviceProvider, out inProject))
            .Returns(hresult);

        return openDocument;
    }

    private ProjectCapabilityResolver CreateResolver(
        Mock<IVsUIShellOpenDocument> openDocument,
        params IProjectCapabilityListener[] listeners)
    {
        var service = VsMocks.CreateVsService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(openDocument);
        var resolver = new ProjectCapabilityResolver(service, listeners, LoggerFactory, JoinableTaskContext);
        AddDisposable(resolver);
        return resolver;
    }
}
