using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace XSerializer
{
    internal static class JsonSerializerFactory
    {
        private static readonly ConcurrentDictionary<Tuple<Type, bool, JsonMappings>, IJsonSerializerInternal> _cache =
            new ConcurrentDictionary<Tuple<Type, bool, JsonMappings>, IJsonSerializerInternal>();

        public static IJsonSerializerInternal GetSerializer(
            Type type,
            bool encrypt,
            IDictionary<Type, Type> mappingsByType,
            IDictionary<PropertyInfo, Type> mappingsByProperty,
            bool shouldUseAttributeDefinedInInterface)
        {
            return GetSerializer(
                type, encrypt,
                new JsonMappings(mappingsByType, mappingsByProperty),
                shouldUseAttributeDefinedInInterface);
        }

        public static IJsonSerializerInternal GetSerializer(
            Type type,
            bool encrypt,
            JsonMappings mappings,
            bool shouldUseAttributeDefinedInInterface)
        {
            return _cache.GetOrAdd(
                Tuple.Create(type, encrypt, mappings),
                tuple =>
                {
                    if (type == typeof(object))
                    {
                        return DynamicJsonSerializer.Get(encrypt, mappings, shouldUseAttributeDefinedInInterface);
                    }

                    if (type.IsJsonStringType())
                    {
                        return StringJsonSerializer.Get(type, encrypt);
                    }

                    if (type.IsJsonNumericType())
                    {
                        return NumberJsonSerializer.Get(type, encrypt);
                    }

                    if (type.IsJsonBooleanType())
                    {
                        return BooleanJsonSerializer.Get(encrypt, type == typeof(bool?));
                    }

                    if (type.IsAssignableToGenericIDictionary()
                        || typeof(IDictionary).IsAssignableFrom(type))
                    {
                        return DictionaryJsonSerializer.Get(type, encrypt, mappings, shouldUseAttributeDefinedInInterface);
                    }

                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        return ListJsonSerializer.Get(type, encrypt, mappings, shouldUseAttributeDefinedInInterface);
                    }

                    // TODO: Handle more types or possibly black-list some types or types of types.

                    return CustomJsonSerializer.Get(type, encrypt, mappings, shouldUseAttributeDefinedInInterface);
                });
        }
    }
}