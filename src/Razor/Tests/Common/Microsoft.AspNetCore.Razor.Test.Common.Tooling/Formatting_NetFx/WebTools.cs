// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Settings.Internal;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.WebTools.Languages.Shared.Editor.Composition;
using Microsoft.WebTools.Languages.Shared.Editor.Text;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

/// <summary>
///  Provides reflection-based access to the Web Tools LSP infrastructure needed for tests.
/// </summary>
internal static class WebTools
{
    private const string ServerAssemblyName = "Microsoft.WebTools.Languages.LanguageServer.Server, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    private const string EditorAssemblyName = "Microsoft.WebTools.Languages.Shared.Editor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    private const string ApplyFormatEditsHandlerTypeName = "Microsoft.WebTools.Languages.LanguageServer.Server.Html.OperationHandlers.ApplyFormatEditsHandler";
    private const string BufferManagerTypeName = "Microsoft.WebTools.Languages.LanguageServer.Server.Shared.Buffer.BufferManager";
    private const string RequestContextTypeName = "Microsoft.WebTools.Languages.LanguageServer.Server.Shared.Clasp.RequestContext";
    private const string ApplyFormatEditsParamTypeName = "Microsoft.WebTools.Languages.Shared.Editor.LanguageServer.ContainedLanguage.ApplyFormatEditsParam";
    private const string ApplyFormatEditsResponseTypeName = "Microsoft.WebTools.Languages.Shared.Editor.LanguageServer.ContainedLanguage.ApplyFormatEditsResponse";
    private const string TextChangeTypeName = "Microsoft.WebTools.Languages.Shared.Editor.EditorHelpers.TextChange";
    private const string LspLoggerTypeName = "Microsoft.WebTools.Languages.LanguageServer.Server.Shared.Clasp.LspLogger";

    private static Assembly? s_serverAssembly;
    private static Assembly? s_editorAssembly;

    private static Type GetType(Assembly assembly, string name)
        => assembly.GetType(name, throwOnError: true).AssumeNotNull();

    private static object CreateInstance(Type type, params object?[]? args)
        => Activator.CreateInstance(type, args).AssumeNotNull();

    private static MethodInfo GetMethod(Type type, string name)
        => type.GetMethod(name).AssumeNotNull();

    private static MethodInfo GetMethod(Type type, string name, Type[] parameterTypes)
        => type.GetMethod(name, parameterTypes).AssumeNotNull();

    private static PropertyInfo GetProperty(Type type, string name)
        => type.GetProperty(name).AssumeNotNull();

    private static Assembly ServerAssembly
        => s_serverAssembly ?? InterlockedOperations.Initialize(ref s_serverAssembly,
            Assembly.Load(ServerAssemblyName));

    private static Assembly EditorAssembly
        => s_editorAssembly ?? InterlockedOperations.Initialize(ref s_editorAssembly,
            Assembly.Load(EditorAssemblyName));

    public abstract class ReflectedObject(object instance)
    {
        public object Instance => instance;
    }

    public sealed class BufferManager(object instance) : ReflectedObject(instance)
    {
        private static Type? s_type;
        private static MethodInfo? s_createBufferMethod;

        public static Type Type
            => s_type ?? InterlockedOperations.Initialize(ref s_type,
                WebTools.GetType(ServerAssembly, BufferManagerTypeName));

        private static MethodInfo CreateBufferMethod
            => s_createBufferMethod ?? InterlockedOperations.Initialize(ref s_createBufferMethod,
                GetMethod(Type, nameof(CreateBuffer)));

        public ITextSnapshot CreateBuffer(
            Uri documentUri,
            string contentTypeName,
            string initialContent,
            int snapshotVersionFromLSP)
        {
            return (ITextSnapshot)CreateBufferMethod
                .Invoke(Instance, [documentUri, contentTypeName, initialContent, snapshotVersionFromLSP])
                .AssumeNotNull();
        }

        public static BufferManager New(
            IContentTypeRegistryService contentTypeService,
            ITextBufferFactoryService textBufferFactoryService,
            IEnumerable<Lazy<IWebTextBufferListener, IOrderedComponentContentTypes>> textBufferListeners)
        {
            var instance = CreateInstance(Type, contentTypeService, textBufferFactoryService, textBufferListeners);
            return new(instance);
        }
    }

    public sealed class RequestContext(object instance) : ReflectedObject(instance)
    {
        private static Type? s_type;

        public static Type Type
            => s_type ?? InterlockedOperations.Initialize(ref s_type,
                WebTools.GetType(ServerAssembly, RequestContextTypeName));

        public static RequestContext New(ITextSnapshot textSnapshot)
        {
            var instance = CreateInstance(Type, textSnapshot);
            return new(instance);
        }
    }

    public sealed class ApplyFormatEditsParam(object instance) : ReflectedObject(instance)
    {
        private static Type? s_type;

        public static Type Type
            => s_type ?? InterlockedOperations.Initialize(ref s_type,
                WebTools.GetType(EditorAssembly, ApplyFormatEditsParamTypeName));

        public static ApplyFormatEditsParam DeserializeFrom(string jsonText)
        {
            var instance = JsonConvert.DeserializeObject(jsonText, Type).AssumeNotNull();
            return new(instance);
        }
    }

    public sealed class ApplyFormatEditsResponse(object instance) : ReflectedObject(instance)
    {
        private static Type? s_type;
        private static PropertyInfo? s_textChangesProperty;

        public static Type Type
            => s_type ?? InterlockedOperations.Initialize(ref s_type,
                WebTools.GetType(EditorAssembly, ApplyFormatEditsResponseTypeName));

        private static PropertyInfo TextChangesProperty
            => s_textChangesProperty ?? InterlockedOperations.Initialize(ref s_textChangesProperty,
                GetProperty(Type, nameof(TextChanges)));

        private ImmutableArray<TextChange> _textChanges;

        public ImmutableArray<TextChange> TextChanges
        {
            get
            {
                if (_textChanges.IsDefault)
                {
                    var textChanges = (object[])TextChangesProperty.GetValue(Instance).AssumeNotNull();

                    using var builder = new PooledArrayBuilder<TextChange>(textChanges.Length);

                    foreach (var textChange in textChanges)
                    {
                        builder.Add(new TextChange(textChange));
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _textChanges, builder.ToImmutableAndClear());
                }

                return _textChanges;
            }
        }
    }

    public sealed class TextChange(object instance) : ReflectedObject(instance)
    {
        private static Type? s_type;
        private static PropertyInfo? s_positionProperty;
        private static PropertyInfo? s_lengthProperty;
        private static PropertyInfo? s_newTextProperty;

        public static Type Type
            => s_type ?? InterlockedOperations.Initialize(ref s_type,
                WebTools.GetType(EditorAssembly, TextChangeTypeName));

        private static PropertyInfo PositionProperty
            => s_positionProperty ?? InterlockedOperations.Initialize(ref s_positionProperty,
                GetProperty(Type, nameof(Position)));

        private static PropertyInfo LengthProperty
            => s_lengthProperty ?? InterlockedOperations.Initialize(ref s_lengthProperty,
                GetProperty(Type, nameof(Length)));

        private static PropertyInfo NewTextProperty
            => s_newTextProperty ?? InterlockedOperations.Initialize(ref s_newTextProperty,
                GetProperty(Type, nameof(NewText)));

        private int? _position;
        private int? _length;
        private string? _newText;

        public int Position => _position ??= (int)PositionProperty.GetValue(Instance).AssumeNotNull();
        public int Length => _length ??= (int)LengthProperty.GetValue(Instance).AssumeNotNull();
        public string NewText => _newText ??= (string)NewTextProperty.GetValue(Instance).AssumeNotNull();

        public override int GetHashCode()
            => Instance.GetHashCode();
        public override bool Equals(object? obj)
            => Instance.Equals(obj);
        public override string? ToString()
            => Instance.ToString();
    }

    public sealed class ApplyFormatEditsHandler(object instance) : ReflectedObject(instance)
    {
        private static Type? s_type;
        private static MethodInfo? s_handleRequestAsyncMethod;

        public static Type Type
            => s_type ?? InterlockedOperations.Initialize(ref s_type,
                WebTools.GetType(ServerAssembly, ApplyFormatEditsHandlerTypeName));

        private static MethodInfo HandleRequestAsyncMethod
            => s_handleRequestAsyncMethod ?? InterlockedOperations.Initialize(ref s_handleRequestAsyncMethod,
                GetMethod(
                    Type,
                    nameof(HandleRequestAsync),
                    [ApplyFormatEditsParam.Type, RequestContext.Type, typeof(CancellationToken)]));

        public async Task<ApplyFormatEditsResponse> HandleRequestAsync(
            ApplyFormatEditsParam request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            var task = (Task)HandleRequestAsyncMethod
                .Invoke(Instance, [request.Instance, context.Instance, cancellationToken])
                .AssumeNotNull();

            await task;

            var result = GetProperty(task.GetType(), "Result").GetValue(task).AssumeNotNull();

            return new ApplyFormatEditsResponse(result);
        }

        public static ApplyFormatEditsHandler New(
            ITextBufferFactoryService3 textBufferFactoryService,
            BufferManager bufferManager,
            ILogger logger)
        {
            var instance = CreateInstance(Type, textBufferFactoryService, bufferManager.Instance, LspLogger.New(logger).Instance);
            return new(instance);
        }
    }

    public sealed class LspLogger(object instance) : ReflectedObject(instance)
    {
        private static Type? s_type;

        public static Type Type
            => s_type ?? InterlockedOperations.Initialize(ref s_type,
                WebTools.GetType(ServerAssembly, LspLoggerTypeName));

        public static RequestContext New(ILogger logger)
        {
            var instance = CreateInstance(Type, new MicrosoftExtensionsLoggerWrapper(logger));
            return new(instance);
        }

        private class MicrosoftExtensionsLoggerWrapper(ILogger logger) : Microsoft.Extensions.Logging.ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return NoOpDisposable.Instance;
            }

            public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                var level = logLevel switch
                {
                    Microsoft.Extensions.Logging.LogLevel.Trace => LogLevel.Trace,
                    Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.Debug,
                    Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.Information,
                    Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warning,
                    Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.Error,
                    Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.Critical,
                    Microsoft.Extensions.Logging.LogLevel.None => LogLevel.None,
                    _ => throw new NotImplementedException()
                };

                logger.Log(level, message);
            }
        }
    }
}
