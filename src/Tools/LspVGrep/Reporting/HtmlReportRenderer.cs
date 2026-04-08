using System.Net;
using System.Text;
using LspVGrepTool.Models;

namespace LspVGrepTool.Reporting;

internal static class HtmlReportRenderer
{
    public static string Render(ToolReport report)
    {
        var builder = new StringBuilder();

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\" />");
        builder.AppendLine("  <title>LspVGrepTool Report</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: Arial, sans-serif; margin: 2rem; color: #1f2937; }");
        builder.AppendLine("    h1, h2, h3 { margin-bottom: 0.5rem; }");
        builder.AppendLine("    .query { border: 1px solid #d1d5db; border-radius: 8px; padding: 1rem; margin-bottom: 1rem; }");
        builder.AppendLine("    .algorithm { border-top: 1px solid #e5e7eb; padding-top: 1rem; margin-top: 1rem; }");
        builder.AppendLine("    .status { font-weight: bold; }");
        builder.AppendLine("    .status-failed { color: #b91c1c; }");
        builder.AppendLine("    .status-succeeded { color: #047857; }");
        builder.AppendLine("    dl { display: grid; grid-template-columns: max-content 1fr; gap: 0.5rem 1rem; }");
        builder.AppendLine("    dt { font-weight: bold; }");
        builder.AppendLine("    pre { background: #f9fafb; border-radius: 6px; padding: 1rem; overflow-x: auto; white-space: pre-wrap; }");
        builder.AppendLine("    .truncated-link { color: #2563eb; cursor: pointer; font-style: italic; }");
        builder.AppendLine("    .truncated-link:hover { text-decoration: underline; }");
        builder.AppendLine("    .full-result { display: none; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <h1>LspVGrepTool Report</h1>");
        builder.AppendLine($"  <p><strong>Directory:</strong> {Encode(report.Directory)}</p>");

        if (!string.IsNullOrWhiteSpace(report.RoslynTargetPath) && !string.IsNullOrWhiteSpace(report.RoslynTargetKind))
        {
            var loadTime = report.RoslynLoadTime.HasValue
                ? report.RoslynLoadTime.Value.TotalSeconds >= 1
                    ? $"{report.RoslynLoadTime.Value.TotalSeconds:F1}s"
                    : $"{report.RoslynLoadTime.Value.TotalMilliseconds:F0}ms"
                : "N/A";

            builder.AppendLine(
                $"  <p><strong>Roslyn target:</strong> {Encode(report.RoslynTargetKind)} - {Encode(report.RoslynTargetPath)} (loaded in {loadTime})</p>");
        }

        builder.AppendLine("  <section>");

        foreach (var query in report.Queries)
        {
            builder.AppendLine("    <article class=\"query\">");
            builder.AppendLine($"      <h2>{Encode(query.Type)}</h2>");
            builder.AppendLine("      <dl>");
            foreach (var field in query.Fields)
            {
                builder.AppendLine($"        <dt>{Encode(field.Key)}</dt><dd>{Encode(field.Value)}</dd>");
            }

            builder.AppendLine("      </dl>");

            foreach (var algorithm in query.Algorithms)
            {
                var statusClass = algorithm.Outcome == AlgorithmOutcome.Succeeded
                    ? "status-succeeded"
                    : "status-failed";

                var summaryFragment = string.IsNullOrEmpty(algorithm.Summary)
                    ? string.Empty
                    : $" | {Encode(algorithm.Summary)}";

                var elapsed = algorithm.ElapsedTime.TotalSeconds >= 1
                    ? $"{algorithm.ElapsedTime.TotalSeconds:F1}s"
                    : $"{algorithm.ElapsedTime.TotalMilliseconds:F0}ms";

                builder.AppendLine("      <section class=\"algorithm\">");
                builder.AppendLine($"        <h3>{Encode(algorithm.AlgorithmName)}</h3>");
                builder.AppendLine(
                    $"        <p><span class=\"status {statusClass}\">{Encode(algorithm.Outcome.ToString())}</span> | <strong>Characters:</strong> {algorithm.CharacterCount} | <strong>Lines:</strong> {algorithm.LineCount} | <strong>Time:</strong> {elapsed}{summaryFragment}</p>");
                RenderResponseText(builder, algorithm.ResponseText);
                builder.AppendLine("      </section>");
            }

            builder.AppendLine("    </article>");
        }

        builder.AppendLine("  </section>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private const int MaxDisplayLines = 10;
    private static int s_expandCounter;

    private static void RenderResponseText(StringBuilder builder, string text)
    {
        var lines = text.Split('\n');
        if (lines.Length <= MaxDisplayLines)
        {
            builder.AppendLine($"        <pre>{Encode(text)}</pre>");
            return;
        }

        var id = $"expand-{s_expandCounter++}";
        var truncated = string.Join("\n", lines.Take(MaxDisplayLines));
        var remaining = lines.Length - MaxDisplayLines;

        builder.AppendLine($"        <pre id=\"{id}-short\">{Encode(truncated)}");
        builder.AppendLine($"<span class=\"truncated-link\" onclick=\"document.getElementById('{id}-short').style.display='none'; document.getElementById('{id}-full').style.display='block';\">... truncated ({remaining} more lines) — click to expand</span></pre>");
        builder.AppendLine($"        <pre id=\"{id}-full\" class=\"full-result\">{Encode(text)}</pre>");
    }
}
