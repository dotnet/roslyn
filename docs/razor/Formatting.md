# Troubleshooting formatting issues

Razor formatting can sometimes behave unexpectedly or appear to do nothing. This document
explains common causes and describes concrete steps you can take to reproduce issues and
help us investigate.

## Formatting causes an error or crash

If formatting produces an error, C# or HTML formatting likely produced a change Razor cannot
handle. If this happens you might see an info bar in Visual Studio, or a popup in VS Code, saying
"Formatting error." and "Please report this issue". We can usually fix these errors quickly, but
we need detailed information to find the root cause. Turn on Formatting Logging (see the bottom
of this doc) and attach the resulting logs to your issue so we can reproduce and resolve the crash.

## Formatting appears to do nothing

The formatter deliberately aborts if it detects a change that would modify any non-whitespace
character. We do this to avoid changing your code's behavior while adjusting only formatting.
The formatter also aborts if the number of diagnostics in a document changes before and
after formatting, as we treat that as a sign the formatter introduced a problem.

When either safeguard triggers, Razor writes a message to the Razor log in the Output window.
Check that log first when formatting seems to be a no-op.

## The formatter changes code in ways I don't want

Razor's formatter delegates HTML formatting to your IDE's HTML formatter and C# formatting to
Roslyn. C# formatting options are read from `.editorconfig` sections matching the `.razor`
or `.cshtml` file, not the generated C# file. HTML formatting still depends on your IDE's
HTML formatter settings.

If the formatter changes your code incorrectly, open an issue and include:

- the original source (before)
- the formatted result (after)
- the result you expected

Attach the actual source files rather than screenshots. Reduce the problem to a minimal
repro if you can — smaller repros help us diagnose and fix the issue faster.

Also enable formatting logging and attach the logs to the issue; those logs contain useful
diagnostic information we need to investigate formatting bugs.

## Configure Razor formatting with editorconfig

The IDE and `dotnet format` read these Razor-specific options from `.editorconfig`:

```editorconfig
[*.{razor,cshtml}]
razor_code_block_brace_on_next_line = false
# Valid attribute indent styles: align_with_first, indent_by_one, indent_by_two
razor_attribute_indent_style = align_with_first
razor_ignore_indentation_in_script_or_style_blocks_with_complex_razor = true
```

These are the default values. `razor_attribute_indent_style` also accepts `indent_by_one`
and `indent_by_two`, which indent continuation attributes by one or two levels relative
to the tag instead of aligning them with its first attribute. The other options accept
`true` or `false`.

Valid values override your IDE preferences for the matching files. Missing or invalid
values leave the IDE preference unchanged; `dotnet format` uses the defaults above.
Editor triggers such as format-on-type and format-on-paste remain IDE settings.

## Turn on Formatting Logging

1. Create a folder on your machine to receive logs.
2. Set the environment variable `RazorFormattingLogPath` to that folder.
3. Start your IDE.
4. Reproduce the formatting operation that fails or behaves incorrectly.

Razor creates a sub-folder for each operation and writes several log files there. Zip the
log folder and attach it to the issue you file (either through the IDE feedback mechanism or
by creating an issue in the dotnet/razor repository). The logs help us identify exactly what
went wrong.

> [!NOTE]
> The logs include your Razor file contents and full file paths. Don't upload them to
> public sites if that raises privacy or security concerns.
