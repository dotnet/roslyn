// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(IBinLogPathProvider)), Shared]
internal sealed class BinLogPathProvider : IBinLogPathProvider
{
    /// <summary>
    /// The suffix to use for the binary log name; incremented each time we have a new build. Should be incremented with <see cref="Interlocked.Increment(ref int)"/>.
    /// </summary>
    private int _binaryLogNumericSuffix;

    /// <summary>
    /// A GUID put into all binary log file names, so that way one session doesn't accidentally overwrite the logs from a prior session.
    /// </summary>
    private readonly Guid _binaryLogGuidSuffix = Guid.NewGuid();

    private readonly IGlobalOptionService _globalOptionService;
    private readonly ILogger _logger;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public BinLogPathProvider(IGlobalOptionService globalOptionService, ILoggerFactory loggerFactory)
    {
        _globalOptionService = globalOptionService;
        _logger = loggerFactory.CreateLogger<BinLogPathProvider>();
    }

    public string? GetNewLogPath()
    {
        if (_globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.BinaryLogPath) is not string binaryLogDirectory)
            return null;

        var numericSuffix = Interlocked.Increment(ref _binaryLogNumericSuffix);
        var binaryLogPath = Path.Combine(binaryLogDirectory, $"LanguageServerDesignTimeBuild-{_binaryLogGuidSuffix}-{numericSuffix}.binlog");

        _logger.LogInformation($"Logging design-time builds to {binaryLogPath}");

        return binaryLogPath;
    }
}
