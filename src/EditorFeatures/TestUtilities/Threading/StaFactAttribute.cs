using System;

using Xunit;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Roslyn.Test.Utilities.StaFactDiscoverer", "Roslyn.Services.Test.Utilities")]
    public class StaFactAttribute : FactAttribute { }
}
