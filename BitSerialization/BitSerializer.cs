using System.Security.Cryptography;
using System.Text;

namespace BitSerialization;

public static class BitSerializer
{
    private static readonly byte[] _MagicNumber = [0x10, 0x20, 0x30];

    private static void _writeValue(BinaryWriter bw, FieldType fieldType, object value)
    {
        switch (fieldType)
        {
            case FieldType.Bool: bw.Write((bool)value); break;
            case FieldType.Byte: bw.Write((byte)value); break;
            case FieldType.SByte: bw.Write((sbyte)value); break;
            case FieldType.Char: bw.Write((char)value); break;
            case FieldType.Int16: bw.Write((short)value); break;
            case FieldType.UInt16: bw.Write((ushort)value); break;
            case FieldType.Int32: bw.Write((int)value); break;
            case FieldType.UInt32: bw.Write((uint)value); break;
            case FieldType.Int64: bw.Write((long)value); break;
            case FieldType.UInt64: bw.Write((ulong)value); break;
            case FieldType.Single: bw.Write((float)value); break;
            case FieldType.Double: bw.Write((double)value); break;
            case FieldType.Decimal: bw.Write((decimal)value); break;
            case FieldType.String: bw.Write((string)value); break;
            case FieldType.ByteArray:
                var bytes = (byte[])value;
                bw.Write(bytes.Length);
                bw.Write(bytes);
                break;
            case FieldType.DateTime: bw.Write(((DateTime)value).Ticks); break;
            case FieldType.TimeSpan: bw.Write(((TimeSpan)value).Ticks); break;
            case FieldType.Guid: bw.Write(((Guid)value).ToByteArray()); break;
            case FieldType.IntPtr: bw.Write(((IConvertible)value).ToInt64(null)); break;
            case FieldType.Enum: bw.Write(Convert.ToInt32(value)); break;
            default: throw new NotSupportedException($"Serialization of FieldType '{fieldType}' is not supported");
        }
    }

    private static object? _readValue(BinaryReader br, FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Bool => br.ReadBoolean(),
            FieldType.Byte => br.ReadByte(),
            FieldType.SByte => br.ReadSByte(),
            FieldType.Char => br.ReadChar(),
            FieldType.Int16 => br.ReadInt16(),
            FieldType.UInt16 => br.ReadUInt16(),
            FieldType.Int32 => br.ReadInt32(),
            FieldType.UInt32 => br.ReadUInt32(),
            FieldType.Int64 => br.ReadInt64(),
            FieldType.UInt64 => br.ReadUInt64(),
            FieldType.Single => br.ReadSingle(),
            FieldType.Double => br.ReadDouble(),
            FieldType.Decimal => br.ReadDecimal(),
            FieldType.String => br.ReadString(),
            FieldType.ByteArray => br.ReadBytes(br.ReadInt32()),
            FieldType.DateTime => new DateTime(br.ReadInt64()),
            FieldType.TimeSpan => new TimeSpan(br.ReadInt64()),
            FieldType.Guid => new Guid(br.ReadBytes(16)),
            FieldType.IntPtr => new nint(br.ReadInt64()),
            FieldType.Enum => br.ReadInt32(),
            _ => throw new NotSupportedException($"Deserialization of FieldType '{fieldType}' is not supported")
        };
    }

    private static byte[] _serialize2Bytes(ICollection<ClassField> fields)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write(_MagicNumber);
        bw.Write(fields.Count);

        foreach (var field in fields.Where(f => f.Value != null))
        {
            bw.Write(field.Order);
            bw.Write(SHA256.HashData(Encoding.UTF8.GetBytes(field.FieldName)));
            bw.Write((byte)field.FieldType);
            _writeValue(bw, field.FieldType, field.Value!);
        }

        return ms.ToArray();
    }

    /// <summary>Serializes an object into a byte array.</summary>
    /// <param name="obj">The object instance to serialize.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>A byte array containing the serialized data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is <c>null</c>.</exception>
    public static byte[] Serialize<T>(T obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var extractedFields = FieldExtractor.ExtractFields(typeof(T), obj);
        return _serialize2Bytes(extractedFields);
    }

    /// <summary>Serializes an object and writes the result into a stream.</summary>
    /// <param name="obj">The object instance to serialize.</param>
    /// <param name="stream">The stream to write the serialized data to.</param>
    /// <param name="leaveOpen"><c>true</c> to leave the stream open after writing; otherwise, the stream is disposed.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is <c>null</c>.</exception>
    public static void SerializeToStream<T>(T obj, Stream stream, bool leaveOpen = false)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var data = Serialize(obj);
        stream.Write(data, 0, data.Length);

        if (!leaveOpen)
            stream.Dispose();
    }

    /// <summary>Serializes an object and writes the result into a stream asynchronously.</summary>
    /// <param name="obj">The object instance to serialize.</param>
    /// <param name="stream">The stream to write the serialized data to.</param>
    /// <param name="leaveOpen"><c>true</c> to leave the stream open after writing; otherwise, the stream is disposed.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is <c>null</c>.</exception>
    public static async Task SerializeToStreamAsync<T>(T obj, Stream stream, bool leaveOpen = false)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var data = Serialize(obj);
        await stream.WriteAsync(data.AsMemory(0, data.Length));

        if (!leaveOpen)
            await stream.DisposeAsync();
    }

    /// <summary>Deserializes a byte array back into an object.</summary>
    /// <param name="bytes">The byte array produced by <see cref="Serialize{T}"/>.</param>
    /// <typeparam name="T">The type of the object to reconstruct.</typeparam>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidDataException">Thrown if the data is corrupt or the magic number is invalid.</exception>
    public static T Deserialize<T>(byte[] bytes) where T : new()
    {
        using var ms = new MemoryStream(bytes);
        return Deserialize<T>(ms);
    }

    /// <summary>Deserializes data from a stream back into an object.</summary>
    /// <param name="stream">The stream containing serialized data.</param>
    /// <typeparam name="T">The type of the object to reconstruct.</typeparam>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="InvalidDataException">Thrown if the data is corrupt or the magic number is invalid.</exception>
    public static T Deserialize<T>(Stream stream) where T : new()
    {
        using var br = new BinaryReader(stream);

        var magic = br.ReadBytes(3);
        if (!magic.AsSpan().SequenceEqual(_MagicNumber))
            throw new InvalidDataException("Invalid magic number");

        var fieldCount = br.ReadInt32();
        var serializedFields = new (int Order, byte[] NameHash, FieldType FieldType, object? Value)[fieldCount];

        for (int i = 0; i < fieldCount; i++)
        {
            var order = br.ReadInt32();
            var nameHash = br.ReadBytes(32);
            var fieldType = (FieldType)br.ReadByte();
            var value = _readValue(br, fieldType);
            serializedFields[i] = (order, nameHash, fieldType, value);
        }

        var type = typeof(T);
        var fields = new List<ClassField>(fieldCount);

        foreach (var fi in type.GetFields())
        {
            if (FieldTypeMapper.MapFromSystemType(fi.FieldType) == FieldType.Unknown)
                continue;

            var nameHash = SHA256.HashData(Encoding.UTF8.GetBytes(fi.Name));
            for (int i = 0; i < serializedFields.Length; i++)
            {
                if (serializedFields[i].NameHash.AsSpan().SequenceEqual(nameHash))
                {
                    fields.Add(new ClassField(
                        serializedFields[i].Order,
                        fi.Name,
                        serializedFields[i].FieldType,
                        serializedFields[i].Value));
                    break;
                }
            }
        }

        return FieldExtractor.RecreateTypeUsingFields<T>(fields);
    }
}