// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.Utilities
{
    // This attribute registers a package load key for your package.  
    // Package load keys are used by Visual Studio to validate that 
    // a package can be loaded.    
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class PackageLoadKeyAttribute : RegistrationAttribute
    {
        private string minimumEdition;

        public PackageLoadKeyAttribute(string productVersion, string productName, string companyName)
        {
            Validate.IsNotNull(productVersion, "productVersion");
            Validate.IsNotNull(productName, "productName");
            Validate.IsNotNull(companyName, "companyName");

            ProductVersion = productVersion;
            ProductName = productName;
            CompanyName = companyName;
        }

        // "Standard" for all express skus.
        public string MinimumEdition
        {
            get
            {
                return string.IsNullOrWhiteSpace(minimumEdition) ? "Standard" : minimumEdition;
            }

            set
            {
                minimumEdition = value;
            }
        }

        // Version of the product that this VSPackage implements.
        public string ProductVersion { get; private set; }

        // Name of the product that this VSPackage delivers.
        public string ProductName { get; private set; }

        // Creator of the VSPackage. The literal name (case-sensitive) provided 
        // to Microsoft when registering for a PLK.
        public string CompanyName { get; private set; }

        public short WDExpressId { get; set; }

        public short VWDExpressId { get; set; }

        public short VsWinExpressId { get; set; }

        // <summary>
        // Registry Key name for this package's load key information.
        // </summary>
        // <param name="context"></param>
        // <returns></returns>
        public string RegKeyName(RegistrationContext context)
        {
            return string.Format(CultureInfo.InvariantCulture, "Packages\\{0}", context.ComponentType.GUID.ToString("B"));
        }

        // <include file='doc\ProvideLoadKeyAttribute.uex' path='docs/doc[@for="Register"]' />
        // <devdoc>
        // Called to register this attribute with the given context.  The context
        // contains the location where the registration inforomation should be placed.
        // it also contains such as the type being registered, and path information.
        //
        // This method is called both for registration and unregistration.  The difference is
        // that unregistering just uses a hive that reverses the changes applied to it.
        // </devdoc>
        public override void Register(RegistrationContext context)
        {
            using (Key packageKey = context.CreateKey(RegKeyName(context)))
            {
                if (WDExpressId != 0)
                {
                    packageKey.SetValue("WDExpressId", WDExpressId);
                }

                if (VWDExpressId != 0)
                {
                    packageKey.SetValue("VWDExpressId", VWDExpressId);
                }

                if (VsWinExpressId != 0)
                {
                    packageKey.SetValue("VsWinExpressId", VsWinExpressId);
                }

                packageKey.SetValue("MinEdition", MinimumEdition);
                packageKey.SetValue("ProductVersion", ProductVersion);
                packageKey.SetValue("ProductName", ProductName);
                packageKey.SetValue("CompanyName", CompanyName);
            }
        }

        // <summary>
        // Unregisters this package's load key information
        // </summary>
        // <param name="context"></param>
        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(RegKeyName(context));
        }
    }
}
