// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A <see cref="ObjectBinder"/> that records type and reader information during object writing so that it 
    /// can be used to read back the same object later.
    /// </summary>
    internal sealed class SimpleRecordingObjectBinder : ObjectBinder
    {
        private readonly Dictionary<TypeKey, Type> _typeMap =
            new Dictionary<TypeKey, Type>();

        private readonly Dictionary<Type, Func<ObjectReader, object>> _readerMap =
            new Dictionary<Type, Func<ObjectReader, object>>();

        public override Type GetType(TypeKey key)
        {
            Type type;
            if (!_typeMap.TryGetValue(key, out type))
            {
                Debug.Assert(false, key.AssemblyName + "/" + key.TypeName + " don't exist");
            }

            return type;
        }

        public override Func<ObjectReader, object> GetReader(Type type)
        {
            Func<ObjectReader, object> reader;
            if (!_readerMap.TryGetValue(type, out reader))
            {
                Debug.Assert(false, type.ToString() + " reader doesn't exist");
            }

            return reader;
        }

        public override TypeKey GetTypeKey(Type type)
        {
            var key = base.GetTypeKey(type);
            RecordType(type, key);
            return key;
        }

        public override Action<ObjectWriter, Object> GetWriter(Object instance)
        {
            RecordReader(instance);
            return base.GetWriter(instance);
        }

        private void RecordType(Type type, TypeKey key)
        {
            if (type != null)
            {
                if (!_typeMap.ContainsKey(key))
                {
                    _typeMap.Add(key, type);
                }
            }
        }

        private void RecordReader(object instance)
        {
            if (instance != null)
            {
                var type = instance.GetType();
                var key = GetTypeKey(type); // sid-effect records type too

                var readable = instance as IObjectReadable;
                if (readable != null)
                {
                    if (_readerMap.ContainsKey(type))
                    {
                        Debug.Assert(_typeMap.ContainsKey(key));
                    }
                    else
                    {
                        _readerMap.Add(type, readable.GetReader());
                    }
                }
            }
        }
    }
}
