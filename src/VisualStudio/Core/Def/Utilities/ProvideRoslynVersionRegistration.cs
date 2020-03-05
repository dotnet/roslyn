using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Utilities
{
    /// <summary>
    /// Adds information to Help &gt; About screen showing the version of the Roslyn package.
    /// </summary>
    internal sealed class ProvideRoslynVersionRegistration : RegistrationAttribute
    {
        private readonly string _packageGuidString;
        private readonly string _productName;
        private readonly int _productNameResourceID;
        private readonly int _detailsResourceID;

        public ProvideRoslynVersionRegistration(string packageGuidString, string productName, int productNameResourceID, int detailsResourceID)
        {
            _packageGuidString = packageGuidString;
            _productName = productName;
            _productNameResourceID = productNameResourceID;
            _detailsResourceID = detailsResourceID;
        }

        private string GetKeyName()
        {
            return "InstalledProducts\\" + _productName;
        }

        public override void Register(RegistrationContext context)
        {
            // Fetch the version of this build. As a reminder, this code runs during the build process, not at runtime -- it's
            // ran by the CreatPkgDef.exe tool by reflecting over built assembly and invoking this method.
            var version = FileVersionInfo.GetVersionInfo(typeof(ProvideRoslynVersionRegistration).Assembly.Location);

            using var key = context.CreateKey(GetKeyName());
            key.SetValue(null, "#" + _productNameResourceID);
            key.SetValue("Package", Guid.Parse(_packageGuidString).ToString("B"));
            key.SetValue("PID", version.ProductVersion);
            key.SetValue("ProductDetails", "#" + _detailsResourceID);
            key.SetValue("UseInterface", false);
            key.SetValue("UseVSProductID", false);
        }

        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(GetKeyName());
        }
    }
}
