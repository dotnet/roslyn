// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.RazorExtension;

internal sealed class AboutDialogInfoAttribute : RegistrationAttribute
{
    private readonly string _detailsId;
    private readonly string _name;
    private readonly string _nameId;
    private readonly string _packageGuid;

    // nameId and detailsId are resource IDs, they should start with #
    public AboutDialogInfoAttribute(string packageGuid, string name, string nameId, string detailsId)
    {
        _packageGuid = packageGuid;
        _name = name;
        _nameId = nameId;
        _detailsId = detailsId;
    }

    // This is a resource ID it should start with #
    public string? IconResourceID { get; set; }

    private string GetKeyName()
    {
        return "InstalledProducts\\" + _name;
    }

    public override void Register(RegistrationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var attribute = typeof(AboutDialogInfoAttribute).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var version = attribute?.InformationalVersion;

        using var key = context.CreateKey(GetKeyName());
        key.SetValue(null, _nameId);
        key.SetValue("Package", Guid.Parse(_packageGuid).ToString("B", CultureInfo.InvariantCulture));
        key.SetValue("ProductDetails", _detailsId);
        key.SetValue("UseInterface", false);
        key.SetValue("UseVSProductID", false);

        if (version != null)
        {
            key.SetValue("PID", version);
        }

        if (IconResourceID != null)
        {
            key.SetValue("LogoID", IconResourceID);
        }
    }

    public override void Unregister(RegistrationContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.RemoveKey(GetKeyName());
    }
}
