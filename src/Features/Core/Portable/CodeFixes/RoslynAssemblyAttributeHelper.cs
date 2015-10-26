using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal static class RoslynAssemblyAttributeHelper
    {
        public static bool HasRoslynAssemblyAttribute(object source) => source.GetType().GetTypeInfo().Assembly.GetCustomAttributes<RoslynAssemblyAttribute>()?.Any() == true;
    }
}
