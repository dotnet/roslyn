// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.Debugger.Clr
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public class DkmClrType
    {
        private readonly DkmClrModuleInstance _module;
        private readonly DkmClrAppDomain _appDomain;
        private readonly Type _lmrType;
        private readonly ReadOnlyCollection<DkmClrEvalAttribute> _evalAttributes;
        private ReadOnlyCollection<DkmClrType> _lazyGenericArguments;

        internal DkmClrType(DkmClrModuleInstance module, DkmClrAppDomain appDomain, Type lmrType)
        {
            _module = module;
            _appDomain = appDomain;
            _lmrType = lmrType;
            _evalAttributes = GetEvalAttributes(lmrType);
        }

        internal DkmClrType(Type lmrType) :
            this(DkmClrRuntimeInstance.DefaultRuntime, lmrType)
        {
        }

        internal DkmClrType(DkmClrRuntimeInstance runtime, Type lmrType) :
            this(runtime.DefaultModule, runtime.DefaultAppDomain, lmrType)
        {
        }

        public DkmClrAppDomain AppDomain
        {
            get { return _appDomain; }
        }

        public DkmClrType ElementType
        {
            get
            {
                var elementType = _lmrType.GetElementType();
                return (elementType == null) ? null : Create(_appDomain, elementType);
            }
        }

        internal DkmClrType MakeGenericType(params DkmClrType[] genericArguments)
        {
            var type = new DkmClrType(
                _module,
                _appDomain,
                _lmrType.MakeGenericType(genericArguments.Select(t => t._lmrType).ToArray()));
            type._lazyGenericArguments = new ReadOnlyCollection<DkmClrType>(genericArguments);
            return type;
        }

        internal DkmClrType MakeArrayType()
        {
            return new DkmClrType(
                _module,
                _appDomain,
                _lmrType.MakeArrayType());
        }

        internal object Instantiate(params object[] args)
        {
            var t = ((TypeImpl)_lmrType).Type;
            return t.Instantiate(args);
        }

        private static readonly ReadOnlyCollection<DkmClrType> s_emptyTypes = new ReadOnlyCollection<DkmClrType>(new DkmClrType[0]);

        public ReadOnlyCollection<DkmClrType> GenericArguments
        {
            get
            {
                if (_lazyGenericArguments == null)
                {
                    var typeArgs = _lmrType.GetGenericArguments();
                    var genericArgs = (typeArgs.Length == 0) ?
                        s_emptyTypes :
                        new ReadOnlyCollection<DkmClrType>(typeArgs.Select(t => DkmClrType.Create(_appDomain, t)).ToArray());
                    Interlocked.CompareExchange(ref _lazyGenericArguments, genericArgs, null);
                }
                return _lazyGenericArguments;
            }
        }

        public virtual Type GetLmrType()
        {
            return _lmrType;
        }

        public ReadOnlyCollection<DkmClrEvalAttribute> GetEvalAttributes()
        {
            return _evalAttributes;
        }

        public DkmClrModuleInstance ModuleInstance
        {
            get { return _module; }
        }

        public DkmClrRuntimeInstance RuntimeInstance
        {
            get { return _module.RuntimeInstance; }
        }

        private string GetDebuggerDisplay()
        {
            var result = _lmrType.ToString();
            var proxyAttribute = _evalAttributes.OfType<DkmClrDebuggerTypeProxyAttribute>().FirstOrDefault();
            result = proxyAttribute != null
                ? string.Format("{0} (Proxy = {1})", result, proxyAttribute.ProxyType.GetLmrType().ToString())
                : result;
            return result;
        }

        public static DkmClrType Create(DkmClrAppDomain appDomain, Type type)
        {
            return new DkmClrType(appDomain.RuntimeInstance.DefaultModule, appDomain, type);
        }

        private static System.Type GetProxyType(System.Type type)
        {
            var attribute = (DebuggerTypeProxyAttribute)type.GetCustomAttributes(typeof(DebuggerTypeProxyAttribute), inherit: false).FirstOrDefault();
            if (attribute == null)
            {
                return null;
            }

            // Assume the proxy type is from the same assembly
            // and strip off the assembly qualifier since that won't
            // resolve to explicitly loaded assemblies.
            var proxyName = attribute.ProxyTypeName;
            int separator = proxyName.IndexOf(',');
            if (separator >= 0)
            {
                proxyName = proxyName.Substring(0, separator);
            }

            var assembly = type.Assembly;
            return assembly.GetType(proxyName);
        }

        private static ReadOnlyCollection<DkmClrEvalAttribute> GetEvalAttributes(Type type)
        {
            var reflectionType = ((TypeImpl)type).Type;
            var attributes = ArrayBuilder<DkmClrEvalAttribute>.GetInstance();

            var proxyType = GetProxyType(reflectionType);
            if (proxyType != null)
            {
                attributes.Add(new DkmClrDebuggerTypeProxyAttribute(new DkmClrType((TypeImpl)proxyType)));
            }

            var members = type.GetMembers(TypeHelpers.MemberBindingFlags).Where(TypeHelpers.IsVisibleMember);
            foreach (var member in members)
            {
                var attribute = GetBrowsableAttribute(type, member);
                if (attribute != null)
                {
                    attributes.Add(attribute);
                }
            }

            var debuggerDisplay = GetDebuggerDisplayAttribute(reflectionType);
            if (debuggerDisplay != null)
            {
                attributes.Add(debuggerDisplay);
            }

            var debuggerVisualizers = GetDebuggerVisualizerAttributes(reflectionType);
            if (debuggerVisualizers != null)
            {
                attributes.AddRange(debuggerVisualizers);
            }

            return attributes.ToImmutableAndFree();
        }

        private static DkmClrDebuggerBrowsableAttribute GetBrowsableAttribute(Type type, MemberInfo member)
        {
            foreach (var attribute in member.GetCustomAttributesData())
            {
                var data = ((CustomAttributeDataImpl)attribute).CustomAttributeData;
                if (data.AttributeType == typeof(DebuggerBrowsableAttribute))
                {
                    var state = (DebuggerBrowsableState)data.ConstructorArguments[0].Value;
                    return new DkmClrDebuggerBrowsableAttribute(member.Name, ConvertBrowsableState(state));
                }
            }
            return null;
        }

        private static DkmClrDebuggerBrowsableAttributeState ConvertBrowsableState(DebuggerBrowsableState state)
        {
            switch (state)
            {
                case DebuggerBrowsableState.Never:
                    return DkmClrDebuggerBrowsableAttributeState.Never;
                case DebuggerBrowsableState.Collapsed:
                    return DkmClrDebuggerBrowsableAttributeState.Collapsed;
                case DebuggerBrowsableState.RootHidden:
                    return DkmClrDebuggerBrowsableAttributeState.RootHidden;
                default:
                    throw ExceptionUtilities.UnexpectedValue(state);
            }
        }

        private static DkmClrDebuggerDisplayAttribute GetDebuggerDisplayAttribute(System.Type type)
        {
            var attributeData = type.GetCustomAttributesData().FirstOrDefault(data => data.AttributeType == typeof(DebuggerDisplayAttribute));
            if (attributeData == null)
            {
                return null;
            }

            return new DkmClrDebuggerDisplayAttribute(type.AssemblyQualifiedName)
            {
                Name = (string)attributeData.NamedArguments.SingleOrDefault(arg => arg.MemberName == "Name").TypedValue.Value,
                Value = (string)attributeData.ConstructorArguments.Single().Value,
                TypeName = (string)attributeData.NamedArguments.SingleOrDefault(arg => arg.MemberName == "Type").TypedValue.Value,
            };
        }

        private static DkmClrDebuggerVisualizerAttribute[] GetDebuggerVisualizerAttributes(System.Type type)
        {
            var attributesData = type.GetCustomAttributesData().Where(data => data.AttributeType == typeof(DebuggerVisualizerAttribute));
            if (attributesData.Count() == 0)
            {
                return null;
            }

            var builder = ArrayBuilder<DkmClrDebuggerVisualizerAttribute>.GetInstance();

            foreach (var attributeData in attributesData)
            {
                var argValueTypeBuilder = ArrayBuilder<System.Type>.GetInstance();
                foreach (var typedArg in attributeData.ConstructorArguments)
                {
                    var argumentType = typedArg.ArgumentType.FullName;

                    System.Type argValueType = null;

                    if (string.Equals(argumentType, "System.String", System.StringComparison.Ordinal))
                    {
                        var typeName = (string)typedArg.Value;
                        var assembly = type.Assembly;
                        argValueType = assembly.GetType(typeName);
                    }
                    else if (string.Equals(argumentType, "System.Type", System.StringComparison.Ordinal))
                    {
                        argValueType = typedArg.Value as System.Type;
                    }

                    if (argValueType != null)
                    {
                        argValueTypeBuilder.Add(argValueType);
                    }
                    else
                    {
                        Debug.Fail("Failed to resolve the type of the arguments for DebuggerVisualizer attribute.");
                        return null;
                    }
                }

                // Attribute not recognized.
                if (argValueTypeBuilder.Count == 0)
                {
                    Debug.Fail("Failed to retrieve the visualizer types from a [DebuggerVisualizer] attribute");
                    return null;
                }

                string uiSideVisualizerTypeName = argValueTypeBuilder[0].FullName;
                string uiSideVisualizerAssemblyName = argValueTypeBuilder[0].Assembly.FullName;
                string debuggeeSideVisualizerTypeName;
                string debuggeeSideVisualizerAssemblyName;

                if (argValueTypeBuilder.Count > 1)
                {
                    System.Type debuggeeSideType = argValueTypeBuilder[1];
                    debuggeeSideVisualizerTypeName = debuggeeSideType.FullName;
                    debuggeeSideVisualizerAssemblyName = debuggeeSideType.Assembly.FullName;
                }
                else
                {
                    debuggeeSideVisualizerTypeName = "Microsoft.VisualStudio.DebuggerVisualizers.VisualizerObjectSource";
                    debuggeeSideVisualizerAssemblyName = "Microsoft.VisualStudio.DebuggerVisualizers, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
                }

                string visualizerDescription = uiSideVisualizerTypeName;

                argValueTypeBuilder.Free();

                // Try to get the Visualizer Description. If no description is specified, we will fall back to the Visualizer Type Name.
                foreach (var namedArg in attributeData.NamedArguments)
                {
                    if (namedArg.MemberInfo.Name == "Description")
                    {
                        visualizerDescription = (string)namedArg.TypedValue.Value;
                        break;
                    }
                }

                builder.Add(new DkmClrDebuggerVisualizerAttribute(
                    targetMember: null,
                    uiSideVisualizerTypeName: uiSideVisualizerTypeName,
                    uiSideVisualizerAssemblyName: uiSideVisualizerAssemblyName,
                    uiSideVisualizerAssemblyLocation: Evaluation.DkmClrCustomVisualizerAssemblyLocation.Unknown,
                    debuggeeSideVisualizerTypeName: debuggeeSideVisualizerTypeName,
                    debuggeeSideVisualizerAssemblyName: debuggeeSideVisualizerAssemblyName,
                    visualizerDescription: visualizerDescription));
            }

            return builder.ToArrayAndFree();
        }
    }
}
