using System;
using System.Threading.Tasks;
using ReferencedLibrary;

namespace InspectedLibrary
{
    [SomeMetadata]
    public class InspectedClass
    {
        public Task DoAsync()
        {
            throw new NotImplementedException();
        }
    }
}
