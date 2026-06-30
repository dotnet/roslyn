// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport;

[DataContract]
internal readonly record struct AddImportOptions(
    [property: DataMember(Order = 0)] SymbolSearchOptions SearchOptions,
    [property: DataMember(Order = 1)] CodeCleanupOptions CleanupOptions,
    [property: DataMember(Order = 2)] MemberDisplayOptions MemberDisplayOptions,
    [property: DataMember(Order = 3)] bool CleanupDocument);

internal static class AddImportOptionsProviders
{
    public static AddImportOptions GetAddImportOptions(
        this IOptionsReader options,
        LanguageServices languageServices,
        SymbolSearchOptions searchOptions,
        bool allowImportsInHiddenRegions,
        bool cleanupDocument)
        => new()
        {
            SearchOptions = searchOptions,
            CleanupOptions = options.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions),
            MemberDisplayOptions = options.GetMemberDisplayOptions(languageServices.Language),
            CleanupDocument = cleanupDocument,
        };

    public static async ValueTask<AddImportOptions> GetAddImportOptionsAsync(
        this Document document,
        SymbolSearchOptions searchOptions,
        bool cleanupDocument,
        CancellationToken cancellationToken)
    {
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetAddImportOptions(
            document.Project.Services, searchOptions, document.AllowImportsInHiddenRegions(), cleanupDocument);
    }
}

internal static class AddImportTrace
{
    private const string DiagnosticPrefix = "AddImport Diagnostic: ";
    private const int MaxBufferedMessages = 2000;

    private static readonly AsyncLocal<BufferedLog?> s_bufferedLog = new();

    private sealed class BufferedLog
    {
        public readonly ConcurrentQueue<string> Messages = new();
        public int Count;
    }

    internal static void ClearBufferedMessages()
        => s_bufferedLog.Value = new BufferedLog();

    internal static string GetAndClearBufferedMessages()
    {
        var log = s_bufferedLog.Value;
        s_bufferedLog.Value = null;

        if (log is null || log.Messages.IsEmpty)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var message in log.Messages)
            builder.AppendLine(message);

        return builder.ToString();
    }

    public static void LogMessage(string message)
    {
        var log = s_bufferedLog.Value;
        if (log is null)
        {
            log = new BufferedLog();
            s_bufferedLog.Value = log;
        }

        var count = Interlocked.Increment(ref log.Count);
        if (count <= MaxBufferedMessages)
        {
            log.Messages.Enqueue(message.StartsWith(DiagnosticPrefix, StringComparison.Ordinal)
                ? message
                : DiagnosticPrefix + message);
        }
        else if (count == MaxBufferedMessages + 1)
        {
            log.Messages.Enqueue(DiagnosticPrefix + "Buffered log truncated.");
        }
    }

    public static void LogException(string message, Exception exception)
    {
        LogMessage($"{message}{Environment.NewLine}{exception}");
        DumpBufferedMessages();
    }

    private static void DumpBufferedMessages()
    {
        var text = GetAndClearBufferedMessages();
        if (text.Length == 0)
            return;

        Trace.WriteLine(text);
        Console.Error.WriteLine(text);
    }

    public static string CreateRemoteCallMessage(
        string phase,
        string documentName,
        string projectName,
        string language,
        TextSpan span,
        string diagnosticId,
        int maxResults,
        AddImportOptions options,
        int packageSourceCount,
        int? resultCount = null,
        bool? remoteClientAvailable = null,
        string? extra = null)
    {
        var searchOptions = options.SearchOptions;
        var cleanupOptions = options.CleanupOptions;
        var addImportOptions = cleanupOptions.AddImportOptions;
        var usingDirectivePlacement = addImportOptions.UsingDirectivePlacement;

        return
            $"AddImport {phase}: " +
            $"Document='{documentName}', Project='{projectName}', Language='{language}', DiagnosticId='{diagnosticId}', " +
            $"Span='{span.Start}..{span.End}', MaxResults={maxResults}, PackageSources={packageSourceCount}, " +
            $"SearchOptions=[ReferencedProjects={searchOptions.SearchReferencedProjectSymbols}, UnreferencedProjectSources={searchOptions.SearchUnreferencedProjectSourceSymbols}, " +
            $"UnreferencedMetadata={searchOptions.SearchUnreferencedMetadataSymbols}, ReferenceAssemblies={searchOptions.SearchReferenceAssemblies}, NuGetPackages={searchOptions.SearchNuGetPackages}], " +
            $"CleanupOptions=[CleanupDocument={options.CleanupDocument}, AllowInHiddenRegions={addImportOptions.AllowInHiddenRegions}, PlaceSystemNamespaceFirst={addImportOptions.PlaceSystemNamespaceFirst}, " +
            $"UsingDirectivePlacement='{usingDirectivePlacement.Value}', UsingDirectivePlacementSeverity='{usingDirectivePlacement.Notification.Severity}', " +
            $"FormattingOptionsType='{cleanupOptions.FormattingOptions.GetType().FullName}', SimplifierOptionsType='{cleanupOptions.SimplifierOptions.GetType().FullName}', " +
            $"DocumentFormattingOptionsType='{cleanupOptions.DocumentFormattingOptions.GetType().FullName}']" +
            (remoteClientAvailable.HasValue ? $", RemoteClientAvailable={remoteClientAvailable.Value}" : "") +
            (resultCount.HasValue ? $", ResultCount={resultCount.Value}" : "") +
            (extra is not null ? $", {extra}" : "");
    }

    public static string CreateFixSummary(ImmutableArray<AddImportFixData> fixes)
        => fixes.IsDefaultOrEmpty
            ? fixes.IsDefault ? "<default>" : "<empty>"
            : string.Join("; ", fixes.Select(static (fix, index) =>
                $"{index}: Kind='{fix.Kind}', Title='{fix.Title ?? "<null>"}', TextChanges={GetLength(fix.TextChanges)}, Tags=[{FormatValues(fix.Tags)}], Priority='{fix.Priority}', ProjectReference='{fix.ProjectReferenceToAdd?.DebugName ?? "<null>"}', MetadataReference='{fix.PortableExecutableReferenceFilePathToAdd ?? "<null>"}', Assembly='{fix.AssemblyReferenceAssemblyName ?? "<null>"}', Type='{fix.AssemblyReferenceFullyQualifiedTypeName ?? "<null>"}', Package='{fix.PackageName ?? "<null>"}', PackageVersion='{fix.PackageVersionOpt ?? "<null>"}', PackageSource='{fix.PackageSource ?? "<null>"}'"));

    private static string GetLength<T>(ImmutableArray<T> values)
        => values.IsDefault ? "<default>" : values.Length.ToString();

    private static string FormatValues<T>(ImmutableArray<T> values)
        => values.IsDefaultOrEmpty
            ? values.IsDefault ? "<default>" : "<empty>"
            : string.Join(", ", values);
}
