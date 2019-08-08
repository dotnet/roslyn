// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class NewtonsoftJsonNetApis
    {
        public const string CSharp = @"
namespace Newtonsoft.Json
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;

    [Flags]
    public enum TypeNameHandling
    {
	    None = 0x0,
	    Objects = 0x1,
	    Arrays = 0x2,
	    All = 0x3,
	    Auto = 0x4
    }

    public interface ISerializationBinder
    {
	    Type BindToType(string assemblyName, string typeName);
        void BindToName(Type serializedType, out string assemblyName, out string typeName);
    }

    public class JsonSerializerSettings
    {
        public TypeNameHandling TypeNameHandling { get; set; }
        public SerializationBinder Binder { get; set; }
        public ISerializationBinder SerializationBinder { get; set; }
    }

    public static class JsonConvert
    {
        public static Func<JsonSerializerSettings> DefaultSettings { get; set; }

        public static T DeserializeAnonymousType<T>(string value, T anonymousTypeObject)
        {
            return default(T);
        }

        public static T DeserializeAnonymousType<T>(string value, T anonymousTypeObject, JsonSerializerSettings settings)
        {
            return default(T);
        }

        public static object DeserializeObject(string value)
        {
            return null;
        }

        public static object DeserializeObject(string value, JsonSerializerSettings settings)
        {
            return null;
        }

        public static object DeserializeObject(string value, Type type)
        {
            return null;
        }

        public static T DeserializeObject<T>(string value)
        {
            return default(T);
        }

        public static T DeserializeObject<T>(string value, params JsonConverter[] converters)
        {
            return default(T);
        }

        public static T DeserializeObject<T>(string value, JsonSerializerSettings settings)
        {
            return default(T);
        }

        public static object DeserializeObject(string value, Type type, params JsonConverter[] converters)
        {
            return null;
        }

        public static object DeserializeObject(string value, Type type, JsonSerializerSettings settings)
        {
            return null;
        }

        public static void PopulateObject(string value, object target)
        {
        }

        public static void PopulateObject(string value, object target, JsonSerializerSettings settings)
        {
        }
    }

    public class JsonSerializer
    {
        public static JsonSerializer Create()
        {
            return null;
        }

        public static JsonSerializer Create(JsonSerializerSettings settings)
        {
            return null;
        }

        public static JsonSerializer CreateDefault()
        {
            return null;
        }

        public static JsonSerializer CreateDefault(JsonSerializerSettings settings)
        {
            return null;
        }

        public TypeNameHandling TypeNameHandling { get; set; }

        public SerializationBinder Binder { get; set; }

        public ISerializationBinder SerializationBinder { get; set; }

        public object Deserialize(JsonReader reader)
        {
            return null;
        }

        public object Deserialize(TextReader reader, Type objectType)
        {
            return null;
        }

        public T Deserialize<T>(JsonReader reader)
        {
            return default(T);
        }

        public object Deserialize(JsonReader reader, Type objectType)
        {
            return null;
        }

        public void Populate(TextReader reader, object target)
        {
        }

        public void Populate(JsonReader reader, object target)
        {
        }
    }

    public class JsonReader
    {
    }

    public class JsonConverter
    {
    }
}

public class MyISerializationBinder : Newtonsoft.Json.ISerializationBinder
{
    public Type BindToType(string assemblyName, string typeName)
    {
        throw new NotImplementedException();
    }

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        throw new NotImplementedException();
    }
}

public class MyBinder : System.Runtime.Serialization.SerializationBinder
{
    public Type BindToType(string assemblyName, string typeName)
    {
        throw new NotImplementedException();
    }

    public void BindToName(Type serializedType, out string assemblyName, out string typeName)
    {
        throw new NotImplementedException();
    }
}
";
    }
}
