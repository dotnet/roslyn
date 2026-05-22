// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

internal class RazorExtensionsDiagnosticFactory
{
    private const string DiagnosticPrefix = "RZ";

    internal static readonly RazorDiagnosticDescriptor ViewComponent_CannotFindMethod =
        new($"{DiagnosticPrefix}3900",
            RazorExtensionsResources.ViewComponent_CannotFindMethod,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_CannotFindMethod(string tagHelperType)
        => RazorDiagnostic.Create(
            ViewComponent_CannotFindMethod,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            ViewComponentTypes.SyncMethodName,
            ViewComponentTypes.AsyncMethodName,
            tagHelperType);

    internal static readonly RazorDiagnosticDescriptor ViewComponent_AmbiguousMethods =
        new($"{DiagnosticPrefix}3901",
            RazorExtensionsResources.ViewComponent_AmbiguousMethods,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_AmbiguousMethods(string tagHelperType)
        => RazorDiagnostic.Create(
            ViewComponent_AmbiguousMethods,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            tagHelperType,
            ViewComponentTypes.SyncMethodName,
            ViewComponentTypes.AsyncMethodName);

    internal static readonly RazorDiagnosticDescriptor ViewComponent_AsyncMethod_ShouldReturnTask =
        new($"{DiagnosticPrefix}3902",
            RazorExtensionsResources.ViewComponent_AsyncMethod_ShouldReturnTask,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_AsyncMethod_ShouldReturnTask(string tagHelperType)
        => RazorDiagnostic.Create(
            ViewComponent_AsyncMethod_ShouldReturnTask,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            ViewComponentTypes.AsyncMethodName,
            tagHelperType,
            nameof(Task));

    internal static readonly RazorDiagnosticDescriptor ViewComponent_SyncMethod_ShouldReturnValue =
        new($"{DiagnosticPrefix}3903",
            RazorExtensionsResources.ViewComponent_SyncMethod_ShouldReturnValue,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_SyncMethod_ShouldReturnValue(string tagHelperType)
        => RazorDiagnostic.Create(
            ViewComponent_SyncMethod_ShouldReturnValue,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            ViewComponentTypes.SyncMethodName,
            tagHelperType);

    internal static readonly RazorDiagnosticDescriptor ViewComponent_SyncMethod_CannotReturnTask =
        new($"{DiagnosticPrefix}3904",
            RazorExtensionsResources.ViewComponent_SyncMethod_CannotReturnTask,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_SyncMethod_CannotReturnTask(string tagHelperType)
        => RazorDiagnostic.Create(
            ViewComponent_SyncMethod_CannotReturnTask,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            ViewComponentTypes.SyncMethodName,
            tagHelperType,
            nameof(Task));

    internal static readonly RazorDiagnosticDescriptor PageDirective_CannotBeImported =
        new($"{DiagnosticPrefix}3905",
            RazorExtensionsResources.PageDirectiveCannotBeImported,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreatePageDirective_CannotBeImported(SourceSpan source)
    {
        var fileName = Path.GetFileName(source.FilePath);

        return RazorDiagnostic.Create(PageDirective_CannotBeImported, source, PageDirective.Directive.Directive, fileName);
    }

    internal static readonly RazorDiagnosticDescriptor PageDirective_MustExistAtTheTopOfFile =
        new($"{DiagnosticPrefix}3906",
            RazorExtensionsResources.PageDirectiveMustExistAtTheTopOfFile,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreatePageDirective_MustExistAtTheTopOfFile(SourceSpan source)
        => RazorDiagnostic.Create(PageDirective_MustExistAtTheTopOfFile, source, PageDirective.Directive.Directive);
}
