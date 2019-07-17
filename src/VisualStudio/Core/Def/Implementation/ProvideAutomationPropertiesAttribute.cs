// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class ProvideAutomationPropertiesAttribute : RegistrationAttribute
    {
        public Guid PackageGuid { get; set; }
        public Guid? ResourcePackageGuid { get; set; }
        public string Category { get; set; }
        public string Page { get; set; }
        public int ProfileNodeLabelId { get; set; }
        public int ProfileNodeDescriptionId { get; set; }

        public ProvideAutomationPropertiesAttribute(string category, string page, string packageGuid, int profileNodeLabelId, int profileNodeDescriptionId, string resourcePackageGuid = null)
        {
            this.PackageGuid = Guid.Parse(packageGuid);
            this.Category = category ?? throw new ArgumentNullException(nameof(category));
            this.Page = page ?? throw new ArgumentNullException(nameof(page));
            this.ProfileNodeLabelId = profileNodeLabelId;
            this.ProfileNodeDescriptionId = profileNodeDescriptionId;

            if (resourcePackageGuid != null)
            {
                this.ResourcePackageGuid = Guid.Parse(resourcePackageGuid);
            }
        }

        public override void Register(RegistrationContext context)
        {
            using var key = context.CreateKey("AutomationProperties\\" + Category + "\\" + Page);
            key.SetValue(null, "#" + ProfileNodeLabelId.ToString());
            key.SetValue("Description", "#" + ProfileNodeDescriptionId.ToString());
            key.SetValue("Name", Page);
            key.SetValue("Package", PackageGuid.ToString("B"));

            if (ResourcePackageGuid.HasValue)
            {
                key.SetValue("ResourcePackage", ResourcePackageGuid.Value.ToString("B"));
            }

            key.SetValue("ProfileSave", 1);
            key.SetValue("VSSettingsMigration", (int)ProfileMigrationType.PassThrough);
        }

        public override void Unregister(RegistrationContext context)
        {
            // Nothing to do here
        }
    }
}
