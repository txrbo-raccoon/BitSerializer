# BitSerializer

A lightweight, binary serializer for .NET that converts objects to and from a compact binary format.

## Features

- **Binary format**: compact serialization with a magic number header and SHA256 field-name verification
- **Type-safe**: generic `Serialize<T>` / `Deserialize<T>` API with compile-time type safety
- **Flexible**: supports `Stream` and `byte[]` I/O, sync and async
- **Order control**: use `[Order(n)]` to control field serialization order
- **Self-contained**: no dependencies beyond .NET 8+

## Supported types

| FieldType | .NET Type |
|-----------|-----------|
| `Bool` | `bool` |
| `Byte` | `byte` |
| `SByte` | `sbyte` |
| `Char` | `char` |
| `Int16` | `short` |
| `UInt16` | `ushort` |
| `Int32` | `int` |
| `UInt32` | `uint` |
| `Int64` | `long` |
| `UInt64` | `ulong` |
| `Single` | `float` |
| `Double` | `double` |
| `Decimal` | `decimal` |
| `String` | `string` |
| `ByteArray` | `byte[]` |
| `DateTime` | `DateTime` |
| `TimeSpan` | `TimeSpan` |
| `Guid` | `Guid` |
| `IntPtr` | `nint` / `nuint` |
| `Enum` | Any enum |

> **Note:** `Object` and `Array` types are not yet supported for serialization.

## Usage

### Basic serialization

```csharp
using BitSerialization;

public class Player
{
    [Order(1)]
    public string Name = "hero";

    [Order(2)]
    public int Health = 100;

    [Order(3)]
    public float Speed = 5.5f;
}

var player = new Player { Name = "Aria", Health = 100, Speed = 5.5f };

// Serialize to bytes
byte[] data = BitSerializer.Serialize(player);

// Deserialize back
Player restored = BitSerializer.Deserialize<Player>(data);
```

### Serialize to a stream

```csharp
using var stream = new MemoryStream();
BitSerializer.SerializeToStream(player, stream, leaveOpen: true);
stream.Position = 0;
var fromStream = BitSerializer.Deserialize<Player>(stream);
```

### Serialize to a file

```csharp
byte[] data = BitSerializer.Serialize(player);
File.WriteAllBytes("save.bin", data);

var loaded = BitSerializer.Deserialize<Player>(File.ReadAllBytes("save.bin"));
```

### Async stream serialization

```csharp
await BitSerializer.SerializeToStreamAsync(player, stream, leaveOpen: true);
```

## Binary format

```
┌─────────────────────────────┐
│ Magic number  (3 bytes)     │  0x10 0x20 0x30
├─────────────────────────────┤
│ Field count   (int32)       │
├─────────────────────────────┤
│ ┌─ Field ─────────────────┐ │
│ │ Order        (int32)    │ │
│ │ NameHash     (SHA256)   │ │
│ │ FieldType    (byte)     │ │
│ │ Value        (variable) │ │
│ └─────────────────────────┘ │
│          ...                │
└─────────────────────────────┘
```

- **Magic number**: `0x10 0x20 0x30` — identifies a valid BitSerializer payload
- **Field count**: 32-bit signed integer
- **NameHash**: SHA-256 hash of the field name (used to match fields during deserialization)
- **FieldType**: A single byte identifying the data type (see `FieldType` enum)
- **Value**: Serialized according to its `FieldType`

### Value encoding

| Type | Encoding |
|------|----------|
| `bool` | 1 byte |
| `byte` | 1 byte |
| `int32` | 4 bytes, little-endian |
| `string` | Length-prefixed (7-bit encoded) UTF-8 |
| `byte[]` | 4-byte length prefix + raw bytes |
| `DateTime` | 8 bytes (`Ticks`, little-endian) |
| `Guid` | 16 bytes |
| *(others)* | Standard `BinaryWriter` / `BinaryReader` encoding |

## Order attribute

Fields are serialized in **declaration order** by default. Use `[Order(n)]` to specify a custom position:

```csharp
[Order(2)]
public string Name = "default";

[Order(1)]
public int Id;
```

Fields without `[Order]` are assigned incrementing order values starting from the last explicit order (or `0`).

## Requirements

- .NET 8.0 or later

## License

MIT
