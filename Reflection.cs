using BenchmarkDotNet.Attributes;
using System.Linq.Expressions;
using System.Reflection;

namespace PerformanceDemo
{
    public class Item
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    public class Reflection
    {
        private static readonly List<Dictionary<string, object>> _rows = GenerateRows(1000);

        private static List<Dictionary<string, object>> GenerateRows(int count)
        {
            var rows = new List<Dictionary<string, object>>(count);
            for (int i = 0; i < count; i++)
            {
                rows.Add(new Dictionary<string, object>
                {
                    ["Id"] = Guid.NewGuid(),
                    ["Name"] = Guid.NewGuid().ToString(),
                    ["CreatedDate"] = DateTime.UtcNow,
                    ["Other"] = Guid.NewGuid().ToString(),
                });
            }
            return rows;
        }

        [Benchmark]
        public void HardCoded()
        {
            foreach (var row in _rows)
            {
                _ = new Item
                {
                    Id = (Guid)row["Id"],
                    Name = (string)row["Name"],
                    CreatedDate = (DateTime)row["CreatedDate"]
                };
            }
        }

        [Benchmark]
        public void JsonSerialization()
        {
            foreach (var row in _rows)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(row);
                _ = System.Text.Json.JsonSerializer.Deserialize<Item>(json);
            }
        }

        [Benchmark]
        public void InefficientReflection()
        {
            foreach(var row in _rows)
            {
                InefficientReflectionFactory.Create<Item>(row);
            }
        }

        class InefficientReflectionFactory
        {
            public static T Create<T>(Dictionary<string, object> row) where T : new()
            {
                var item = new T();
                var properties = item.GetType().GetProperties();
                foreach (var kv in row)
                {
                    var property = properties.FirstOrDefault(e => e.Name == kv.Key);
                    property?.SetValue(item, kv.Value);
                }
                return item;
            }
        }

        [Benchmark]
        public void CachedReflection()
        {
            foreach (var row in _rows)
            {
                CachedReflectionFactory.Create<Item>(row);
            }
        }

        class CachedReflectionFactory
        {
            // use ConcurrentDictionary instead of Dictionary for thread safety
            private static Dictionary<Type, PropertyMapping[]> _typeMappings = new();

            public static T Create<T>(Dictionary<string, object> row) where T : new()
            {
                var item = new T();
                foreach (var property in GetMapping(typeof(T), row))
                {
                    property.Property.SetValue(item, row[property.Key]);
                }
                return item;
            }

            private static PropertyMapping[] GetMapping(Type type, Dictionary<string, object> row)
            {
                if (!_typeMappings.TryGetValue(type, out var mapping))
                {
                    var newMapping = new List<PropertyMapping>();
                    var properties = type.GetProperties();
                    foreach(var kv in row)
                    {
                        var property = properties.FirstOrDefault(e => e.Name == kv.Key);
                        if (property != null)
                        {
                            newMapping.Add(new PropertyMapping(kv.Key, property));
                        }
                    }
                    mapping = newMapping.ToArray();
                    _typeMappings[type] = mapping;
                }
                return mapping;
            }

            private record PropertyMapping(string Key, PropertyInfo Property);
        }      

        [Benchmark]
        public void CompiledCachedReflection()
        {
            foreach (var row in _rows)
            {
                CompiledCachedReflectionFactory.Create<Item>(row);
            }
        }

        class CompiledCachedReflectionFactory
        {
            // use ConcurrentDictionary instead of Dictionary for thread safety
            private static Dictionary<Type, PropertyMapping[]> _typeMappings = new();

            public static T Create<T>(Dictionary<string, object> row) where T : new()
            {
                var item = new T();
                var mapping = GetMapping(typeof(T), row);
                for (int i = 0; i < mapping.Length; i++)
                {
                    var property = mapping[i];
                    property.Setter.Invoke(item, row[property.Key]);
                }
                return item;
            }

            private static PropertyMapping[] GetMapping(Type type, Dictionary<string, object> row)
            {
                if (!_typeMappings.TryGetValue(type, out var mapping))
                {
                    var newMapping = new List<PropertyMapping>();
                    var properties = type.GetProperties();
                    foreach (var kv in row)
                    {
                        var property = properties.FirstOrDefault(e => e.Name.Equals(kv.Key, StringComparison.OrdinalIgnoreCase));
                        if (property != null)
                        {
                            newMapping.Add(new PropertyMapping(kv.Key, CreatePropertySetter(property)));
                        }
                    }
                    mapping = newMapping.ToArray();
                    _typeMappings[type] = mapping;
                }
                return mapping;
            }

            private static Action<object, object> CreatePropertySetter(PropertyInfo propertyInfo)
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var value = Expression.Parameter(typeof(object), "value");
                // Convert instance and value to the correct types  
                var instanceCast = Expression.Convert(instance, propertyInfo.DeclaringType!);
                var converter = Expression.Convert(value, propertyInfo.PropertyType);

                // Create the setter call expression  
                var setterCall = Expression.Call(instanceCast, propertyInfo.SetMethod!, converter);

                // Compile the expression  
                var setter = (Action<object, object>)Expression.Lambda(setterCall, instance, value).Compile();
                return setter;
            }

            private record PropertyMapping(string Key, Action<object, object> Setter);
        }
    }
}
