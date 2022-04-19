// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;

namespace Templates.EditorConfig.FileGenerator;

public class RoslynEditorConfigFileGenerator
{
    private const BindingFlags PublicBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
    private const BindingFlags NonPublicBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

    public RoslynEditorConfigFileGenerator(DTE2 dte)
    {
        string basePath = Path.GetDirectoryName(dte.FullName);
        string languageServiceSubPath = @"CommonExtensions\Microsoft\VBCSharp\LanguageServices";
        var baseDirectory = Path.Combine(basePath, languageServiceSubPath);

        _microsoftVisualStudioLanguageServicesVisualBasicAssembly =
            new Lazy<Assembly>(() => Assembly.LoadFrom(Path.Combine(baseDirectory, "Microsoft.VisualStudio.LanguageServices.VisualBasic.dll")));

        _microsoftVisualStudioLanguageServicesCSharpAssembly =
            new Lazy<Assembly>(() => Assembly.LoadFrom(Path.Combine(baseDirectory, "Microsoft.VisualStudio.LanguageServices.CSharp.dll")));

        _serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);

        _generatorMethod = new Lazy<MethodInfo>(() =>
        {
            var editorConfigFileGeneratorType = typeof(OptionSet).Assembly.GetType("Microsoft.CodeAnalysis.Options.EditorConfigFileGenerator");
            Type[] targetParams = new Type[]
            {
                typeof(ImmutableArray<(string feature, ImmutableArray<IOption> options)>),
                typeof(OptionSet),
                typeof(string)
            };

            return editorConfigFileGeneratorType.GetMethod("Generate", PublicBindingFlags, binder: null, types: targetParams, modifiers: null);
        });
    }

    private readonly ServiceProvider _serviceProvider;
    private readonly Lazy<Assembly> _microsoftVisualStudioLanguageServicesVisualBasicAssembly;
    private readonly Lazy<Assembly> _microsoftVisualStudioLanguageServicesCSharpAssembly;
    private readonly Lazy<MethodInfo> _generatorMethod;

    private Assembly VisualBasicLanguageServiceAssembly => _microsoftVisualStudioLanguageServicesVisualBasicAssembly.Value;
    private Assembly CSharpLanguageServiceAssembly => _microsoftVisualStudioLanguageServicesCSharpAssembly.Value;

    private OptionSet GetEditorConfigOptionSet()
    {
        try
        {
            // get IOptionService
            var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            var workspace = componentModel?.GetService<VisualStudioWorkspace>();

            var ioptionServiceType = typeof(OptionSet).Assembly.GetType("Microsoft.CodeAnalysis.Options.IOptionService");
            var getServiceGenericMethod = workspace.Services.GetType().GetMethod(nameof(HostWorkspaceServices.GetService));
            var getServiceMethod = getServiceGenericMethod.MakeGenericMethod(ioptionServiceType);
            var optionService = getServiceMethod.Invoke(workspace.Services, null);

            var getOptionsMethod = ioptionServiceType.GetMethod("GetOptions");
            return (OptionSet)getOptionsMethod.Invoke(optionService, null);
        }
        catch (Exception)
        {
            return null;
        }

    }

    private (bool success, ImmutableArray<(string feature, ImmutableArray<IOption> options)>) GetEditorConfigOptions(string language)
    {
        try
        {
            var codeStylePageType = GetCodePage(language);
            Type[] targetParams = new Type[] { };
            var getEditorConfigOptionsMethod = codeStylePageType.GetMethod("GetEditorConfigOptions", NonPublicBindingFlags, binder: null, types: targetParams, modifiers: null);
            return (true, (ImmutableArray<(string feature, ImmutableArray<IOption> options)>)getEditorConfigOptionsMethod.Invoke(null, null));
        }
        catch (Exception)
        {
            return (false, default(ImmutableArray<(string feature, ImmutableArray<IOption> options)>));
        }

        Type GetCodePage(string language)
        {
            var languageName = language == LanguageNames.CSharp ? "CSharp" : "VisualBasic";
            var codePageTypeName = $"Microsoft.VisualStudio.LanguageServices.{languageName}.Options.Formatting.CodeStylePage";
            var langueServiceAssembly = GetLanguageServiceAssembly(language);
            return langueServiceAssembly.GetType(codePageTypeName);
        }

        Assembly GetLanguageServiceAssembly(string language)
        {
            return language switch
            {
                LanguageNames.CSharp => CSharpLanguageServiceAssembly,
                LanguageNames.VisualBasic => VisualBasicLanguageServiceAssembly,
                _ => throw new ArgumentException(nameof(language))
            };
        }
    }

    private string Generate(ImmutableArray<(string feature, ImmutableArray<IOption> options)> groupedOptions, OptionSet optionSet, string language)
    {
        try
        {
            return (string)_generatorMethod.Value.Invoke(null, new object[] { groupedOptions, optionSet, language });
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string Generate(string language)
    {
        var (success, options) = GetEditorConfigOptions(language);
        if (!success)
        {
            return null;
        }
        
        var optionSet = GetEditorConfigOptionSet();
        if (optionSet is null)
        {
            return null;
        }
        
        return Generate(options, optionSet, language);
    }
}
