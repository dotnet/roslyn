
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace CommonLanguageServerProtocol.Framework.UnitTests;

public class RequestDispatcherTests
{
    private RequestDispatcher<TestRequestContext> GetRequestDispatcher()
    {
        var lspServices = GetLspServices();

        var requestDispatcher = new RequestDispatcher<TestRequestContext>(lspServices);

        return requestDispatcher;
    }

    private ILspServices GetLspServices()
    {
        var services = new Mock<ILspServices>(MockBehavior.Strict);

        return services.Object;
    }

    [Fact]
    public async Task GetRegisteredMethods()
    {
        var requestDispatcher = GetRequestDispatcher();

        var registeredMethods = requestDispatcher.GetRegisteredMethods();

        throw new NotImplementedException();
    }

    [Fact]
    public async Task ExecuteRequestAsync()
    {
        var requestDispatcher = GetRequestDispatcher();

        throw new NotImplementedException();
    }

    [Fact]
    public async Task ExecuteNotificationAsync()
    {
        var requestDispatcher = GetRequestDispatcher();

        throw new NotImplementedException();
    }

    [Fact]
    public async Task ExecuteParameterlessNotficationAsync()
    {
        var requestDispatcher = GetRequestDispatcher();

        throw new NotImplementedException();
    }

    private class TestRequestContext
    {
    }
}
