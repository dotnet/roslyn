using Microsoft.CodeAnalysis.Protocol.LanguageServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace LanguageServicesTest
{
    public class TestClass1
    {
        [WpfFact, Trait(Traits.Feature, "TestFeature")]
        public void Class1Test()
        {
            var class1 = new Class1();

            Assert.Equal(1, class1.GetInt());
        }

    }
}
