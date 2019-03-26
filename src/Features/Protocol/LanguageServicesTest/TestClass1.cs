using Microsoft.CodeAnalysis.Protocol.LanguageServices;
using Xunit;

namespace LanguageServicesTest
{
    public class TestClass1
    {
        [Fact]
        public void Class1Test()
        {
            var class1 = new Class1();

            Assert.Equal(1, class1.GetInt());
        }

    }
}
