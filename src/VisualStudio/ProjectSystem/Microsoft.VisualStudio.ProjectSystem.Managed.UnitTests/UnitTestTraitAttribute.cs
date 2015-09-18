using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Microsoft.VisualStudio
{
    /// <summary>
    ///     Indicates that a test is a project system unit test.
    /// </summary>
    [TraitDiscoverer("Microsoft.VisualStudio.Testing.ProjectSystemTraitDiscoverer", "Microsoft.VisualStudio.ProjectSystem.Managed.UnitTests")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class UnitTestTraitAttribute : Attribute, ITraitAttribute
    {
        public UnitTestTraitAttribute()
        {
        }
    }
}
