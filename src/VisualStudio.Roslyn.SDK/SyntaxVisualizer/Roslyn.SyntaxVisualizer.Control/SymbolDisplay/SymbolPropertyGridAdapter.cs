using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

#nullable enable

namespace Roslyn.SyntaxVisualizer.Control.SymbolDisplay
{
    internal class SymbolPropertyGridAdapter : BasePropertyGridAdapter
    {
        private readonly Dictionary<string, string> _dictionary;

        public SymbolPropertyGridAdapter(object symbol) => _dictionary = CreateDictionary(symbol);

        private static string GetStringValueOf(object obj)
        {
            var stringResult = obj switch
            {
                bool b => b.ToString(),
                int i => i.ToString(),
                string s => s.Length > 0 ? s : "None",
                IEnumerable<object> e => string.Join(", ", e.Select(x => GetStringValueOf(x))),
                null => "<null>",
                object o => o.ToString(),
            };
            return stringResult;
        }

        private Dictionary<string, string> CreateDictionary(object symbol)
        {
            var dictionry = new Dictionary<string, string>();
            var type = symbol.GetType();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {

                try
                {
                    var value = property.GetValue(symbol, null);
                    var strVal = GetStringValueOf(value);
                    dictionry[property.Name] = strVal;
                }
                catch
                {
                    
                }
            }

            var objectMethods = typeof(object).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.GetParameters().Length == 0 && IsNotInObjectType(method, objectMethods))
                {
                    try
                    {
                        var name = method.Name.Split('_').Last();
                        var result = method.Invoke(symbol, null);
                        var stringResult = GetStringValueOf(result);
                        dictionry[name] = stringResult == string.Empty ? "None" : stringResult;
                    }
                    catch
                    {
                    }
                }
            }
            return dictionry;
        }

        private bool IsNotInObjectType(MethodInfo method, MethodInfo[] objectMethods)
        {
            return !objectMethods.Any(o => o.Name == method.Name);
        }

        public override object GetPropertyOwner(PropertyDescriptor pd)
        {
            return _dictionary;
        }

        public override PropertyDescriptor? GetDefaultProperty()
        {
            return null;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var properties = new List<DictionaryPropertyDescriptor>();
            foreach (var e in _dictionary)
            {
                properties.Add(new DictionaryPropertyDescriptor(_dictionary, e.Key));
            }

            return new PropertyDescriptorCollection(properties.ToArray());
        }

        class DictionaryPropertyDescriptor : PropertyDescriptor
        {
            Dictionary<string, string> _dictionary;
            string _key;

            internal DictionaryPropertyDescriptor(Dictionary<string, string> d, string key)
                : base(key.ToString(), null)
            {
                _dictionary = d;
                _key = key;
            }

            public override Type PropertyType => (_dictionary[_key])?.GetType() ?? typeof(object);

            public override void SetValue(object component, object value)
            {
                throw new InvalidOperationException("SetValue is not allowed!");
            }

            public override object GetValue(object component) => _dictionary[_key];

            public override bool IsReadOnly => true;

            public override Type? ComponentType => null;

            public override bool CanResetValue(object component) => false;

            public override void ResetValue(object component)
            {
            }

            public override bool ShouldSerializeValue(object component) => false;
        }
    }
}
