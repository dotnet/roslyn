// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class LogIntegrationTestAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        GetLogger(methodUnderTest.DeclaringType.Name).LogInformation($"#### Integration test start: {methodUnderTest.Name}");
    }

    public override void After(MethodInfo methodUnderTest)
    {
        GetLogger(methodUnderTest.DeclaringType.Name).LogInformation($"#### Integration test end: {methodUnderTest.Name}");
    }

    private static ILogger GetLogger(string testName)
    {
        var componentModel = ServiceProvider.GlobalProvider.GetService<SComponentModel, IComponentModel>();
        var loggerFactory = componentModel.GetService<ILoggerFactory>();
        return loggerFactory.GetOrCreateLogger(testName);
    }
}
