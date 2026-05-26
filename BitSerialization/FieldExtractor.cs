using System.Reflection;

namespace BitSerialization;

internal static class FieldTypeMapper
{
    internal static FieldType MapFromSystemType(Type type)
    {
        if (type.IsEnum)
            return FieldType.Enum;

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => FieldType.Bool,
            TypeCode.Byte => FieldType.Byte,
            TypeCode.SByte => FieldType.SByte,
            TypeCode.Char => FieldType.Char,
            TypeCode.Int16 => FieldType.Int16,
            TypeCode.UInt16 => FieldType.UInt16,
            TypeCode.Int32 => FieldType.Int32,
            TypeCode.UInt32 => FieldType.UInt32,
            TypeCode.Int64 => FieldType.Int64,
            TypeCode.UInt64 => FieldType.UInt64,
            TypeCode.Single => FieldType.Single,
            TypeCode.Double => FieldType.Double,
            TypeCode.Decimal => FieldType.Decimal,
            TypeCode.DateTime => FieldType.DateTime,
            TypeCode.String => FieldType.String,
            _ => MapNonTypeCode(type)
        };
    }

    private static FieldType MapNonTypeCode(Type type)
    {
        if (type == typeof(byte[]))
            return FieldType.ByteArray;
        if (type == typeof(Guid))
            return FieldType.Guid;
        if (type == typeof(TimeSpan))
            return FieldType.TimeSpan;
        if (type == typeof(nint) || type == typeof(nuint))
            return FieldType.IntPtr;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return MapFromSystemType(Nullable.GetUnderlyingType(type)!);
        if (type.IsArray)
            return FieldType.Array;
        if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
            return FieldType.Object;

        return FieldType.Unknown;
    }
}

internal record ClassField(
    int Order,
    string FieldName,
    FieldType FieldType,
    object? Value
);

internal static class FieldExtractor
{
    internal static ClassField[] ExtractFields(Type target, object instance)
    {
        List<ClassField> orderedFields = [];
        foreach (var fi in target.GetFields())
        {
            var serializableType = FieldTypeMapper.MapFromSystemType(fi.FieldType);
            if (serializableType == FieldType.Unknown)
                continue;

            if (fi.GetCustomAttribute(typeof(OrderAttribute)) is OrderAttribute order)
                orderedFields.Add(new ClassField(order.Order, fi.Name, serializableType, fi.GetValue(instance)));
            else
            {
                int lastOrder = orderedFields.Count > 0 ? orderedFields.Last().Order : 0;
                orderedFields.Add(new ClassField(lastOrder, fi.Name, serializableType, fi.GetValue(instance)));
            }
        }

        return orderedFields.OrderBy(field => field.Order).ToArray();
    }

    internal static T RecreateTypeUsingFields<T>(ICollection<ClassField> fields) where T : new()
    {
        var instance = new T();
        foreach (var fi in typeof(T).GetFields())
        {
            var match = fields.FirstOrDefault(field => field.FieldName == fi.Name);
            if (match == null || match.Value == null)
                continue;

            fi.SetValue(instance, match.Value);
        }

        return instance;
    }
}