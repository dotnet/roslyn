// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE80;
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

internal static class EditorConfigFileGeneratorShim
{
    private const BindingFlags PublicBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
    private const BindingFlags NonPublicBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

    public static (bool success, ImmutableArray<(string feature, ImmutableArray<IOption> options)>) GetEditorConfigOptionsVisualBasic(DTE2 dte)
    {
        try
        {
            string basePath = Path.GetDirectoryName(dte.FullName);
            string languageServiceSubPath = @"CommonExtensions\Microsoft\VBCSharp\LanguageServices";
            string fullPath = Path.Combine(languageServiceSubPath, "Microsoft.VisualStudio.LanguageServices.VisualBasic.dll");
            var microsoftVisualStudioLanguageServicesVisualBasicAssembly = Assembly.LoadFrom(fullPath);
            var codeStylePageType = microsoftVisualStudioLanguageServicesVisualBasicAssembly.GetType("Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.Formatting.CodeStylePage");
            Type[] targetParams = new Type[] { };
            var getEditorConfigOptionsMethod = codeStylePageType.GetMethod("GetEditorConfigOptions", NonPublicBindingFlags, binder: null, types: targetParams, modifiers: null);
            return (true, (ImmutableArray<(string feature, ImmutableArray<IOption> options)>)getEditorConfigOptionsMethod.Invoke(null, null));
        }
        catch (Exception)
        {
            return (false, default(ImmutableArray<(string feature, ImmutableArray<IOption> options)>));
        }
    }

    public static (bool success, ImmutableArray<(string feature, ImmutableArray<IOption> options)>) GetEditorConfigOptionsCSharp(DTE2 dte)
    {
        try
        {
            string basePath = Path.GetDirectoryName(dte.FullName);
            string languageServiceSubPath = @"CommonExtensions\Microsoft\VBCSharp\LanguageServices";
            string fullPath = Path.Combine(basePath, languageServiceSubPath, "Microsoft.VisualStudio.LanguageServices.CSharp.dll");
            var microsoftVisualStudioLanguageServicesCSharpAssembly = Assembly.LoadFrom(fullPath);
            var codeStylePageType = microsoftVisualStudioLanguageServicesCSharpAssembly.GetType("Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting.CodeStylePage");
            Type[] targetParams = new Type[] { };
            var getEditorConfigOptionsMethod = codeStylePageType.GetMethod("GetEditorConfigOptions", NonPublicBindingFlags, binder: null, types: targetParams, modifiers: null);
            return (true, (ImmutableArray<(string feature, ImmutableArray<IOption> options)>)getEditorConfigOptionsMethod.Invoke(null, null));
        }
        catch (Exception)
        {
            return (false, default(ImmutableArray<(string feature, ImmutableArray<IOption> options)>));
        }
    }

    public static OptionSet GetEditorConfigOptionSet(DTE2 dte)
    {
        try
        {
            // get IOptionService
            var serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
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

    public static string Generate(
        ImmutableArray<(string feature, ImmutableArray<IOption> options)> groupedOptions,
        OptionSet optionSet,
        string language)
    {
        try
        {
            var editorConfigFileGeneratorType = typeof(OptionSet).Assembly.GetType("Microsoft.CodeAnalysis.Options.EditorConfigFileGenerator");
            Type[] targetParams = new Type[] { groupedOptions.GetType(), optionSet.GetType(), language.GetType() };
            var generatorMethod = editorConfigFileGeneratorType.GetMethod("Generate", PublicBindingFlags, binder: null, types: targetParams, modifiers: null);
            return (string)generatorMethod.Invoke(null, new object[] { groupedOptions, optionSet, language });
        }
        catch (Exception)
        {
            return null;
        }
    }
}
