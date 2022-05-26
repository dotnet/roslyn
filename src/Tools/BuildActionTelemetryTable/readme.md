# BuildActionTelemetryTable

This tool generates a Kusto datatable containing CodeAction names and telemetry hashes for analyzing VS LightBulbSessions.

## How to use

By default this tool will generate the table for the Roslyn CodeActions. 

```
BuildActionTelemetryTable
```

To generate a table for custom CodeActions you can specify the assembly names to inspect as space separated arguments on the commandline.

```
BuildActionTelemetryTable ./path/StyleCop.Analyzers.dll ./path/StyleCop.Analyzer.CodeFixes.dll
```