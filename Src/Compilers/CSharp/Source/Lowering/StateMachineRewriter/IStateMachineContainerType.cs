using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Compilers.CSharp
{
    internal interface IStateMachineContainerType : ITypeDefinition
    {
        void SetMethodImplementations(List<IMethodImplementation> methodImplementations);
    }
}
