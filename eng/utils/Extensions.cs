// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CliWrap;
using CliWrap.Buffered;
using Spectre.Console;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Roslyn.Utils;

public static class Extensions
{
    extension(Command command)
    {
        public async Task<BufferedCommandResult> ExecuteBufferedAsync(Logger logger)
        {
            logger.Log($"Executing command: {command}");

            var originalValidation = command.Validation;
            var result = await command.WithValidation(CommandResultValidation.None).ExecuteBufferedAsync();

            logger.Log($"Command completed ({result.ExitCode}):\nStdout:{result.StandardOutput}\nStderr:{result.StandardError}");

            if (!result.IsSuccess && originalValidation == CommandResultValidation.ZeroExitCode)
            {
                throw new InvalidOperationException($"Command '{command}' failed with exit code {result.ExitCode}.\nStdout:{result.StandardOutput}\nStderr:{result.StandardError}");
            }

            return result;
        }
    }

    extension(IAnsiConsole console)
    {
        public bool ConfirmEx(string prompt, bool defaultValue)
        {
            return new ConfirmationPrompt(prompt)
            {
                DefaultValue = defaultValue,
                DefaultValueStyle = Style.Plain.Foreground(Color.Grey),
                ChoicesStyle = Style.Plain,
            }
            .Show(console);
        }

        /// <summary>
        /// Original <see cref="AnsiConsoleExtensions.Progress"/> can overwrite existing text, this avoids that.
        /// </summary>
        public Progress ProgressLine()
        {
            console.WriteLine();
            console.WriteLine();
            return console.Progress();
        }
    }

    extension(string s)
    {
        public T? ParseJson<T>()
        {
            try
            {
                return JsonSerializer.Deserialize<T>(s, JsonSerializerOptions.Web);
            }
            catch (JsonException e)
            {
                throw new Exception($"Cannot deserialize JSON '{s}'", e);
            }
        }

        public T[]? ParseJsonList<T>()
        {
            try
            {
                return JsonSerializer.Deserialize<T[]>(s, JsonSerializerOptions.Web);
            }
            catch (JsonException e)
            {
                throw new Exception($"Cannot deserialize JSON '{s}'", e);
            }
        }

        public List<T?> ParseJsonNewLineDelimitedList<T>()
        {
            var result = new List<T?>();
            foreach (var line in s.EnumerateLines())
            {
                if (line.IsWhiteSpace())
                {
                    continue;
                }

                try
                {
                    result.Add(JsonSerializer.Deserialize<T>(line, JsonSerializerOptions.Web));
                }
                catch (JsonException e)
                {
                    throw new Exception($"Cannot deserialize JSON '{line}'", e);
                }
            }

            return result;
        }
    }

    extension<T>(TextPrompt<T>)
    {
        /// <summary>
        /// Use this instead of <see cref="TextPromptExtensions.DefaultValue"/> to work around
        /// <see href="https://github.com/spectreconsole/spectre.console/issues/1181"/>.
        /// </summary>
        public static TextPrompt<T> Create(string text, T defaultValue)
        {
            return new TextPrompt<T>($"{text} [grey](default: {Markup.Escape($"{defaultValue}")})[/]:")
                .DefaultValue(defaultValue)
                .HideDefaultValue();
        }
    }

    extension<T>(TextPrompt<T>) where T : struct
    {
        public static TextPrompt<T> Create(string text, T? defaultValueIfNotNull)
        {
            return defaultValueIfNotNull is { } v
                ? TextPrompt<T>.Create(text, defaultValue: v)
                : new TextPrompt<T>($"{text}:");
        }
    }

    extension<T>(TextPrompt<T>) where T : class?
    {
        public static TextPrompt<T> CreateExt(string text, T? defaultValueIfNotNull)
        {
            return defaultValueIfNotNull is { } v
                ? TextPrompt<T>.Create(text, defaultValue: v)
                : new TextPrompt<T>($"{text}:");
        }
    }

    extension(TextPrompt<string>)
    {
        public static TextPrompt<string> Create(string text, string? defaultValueIfNotNullOrEmpty)
        {
            return !string.IsNullOrEmpty(defaultValueIfNotNullOrEmpty)
                ? TextPrompt<string>.Create(text, defaultValue: defaultValueIfNotNullOrEmpty)
                : new TextPrompt<string>($"{text}:");
        }
    }
}
