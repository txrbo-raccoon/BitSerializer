# BitSerializer

A lightweight, binary serializer for .NET that converts objects to and from a compact binary format.

## Features

- **Binary format**: compact serialization with a magic number header and SHA256 field-name verification
- **Type-safe**: generic `Serialize<T>` / `Deserialize<T>` API with compile-time type safety
- **Flexible**: supports `Stream` and `byte[]` I/O, sync and async
- **Order control**: use `[Order(n)]` to control field serialization order
- **Compression**: transparent Deflate compression via `SerializerConfig`
- **Encryption**: XOR or AES-256 encryption via `SerializerConfig`
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
| `Enum` | Any enum (serialized as `Int32`) |
| `Array` | `T[]` (primitive and string arrays) |

> **Note:** `Object` and nested `Array` types are not yet supported.

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
File.WriteAllBytes("save.sobj", data);

var loaded = BitSerializer.Deserialize<Player>(File.ReadAllBytes("save.sobj"));
```

### Async stream serialization

```csharp
await BitSerializer.SerializeToStreamAsync(player, stream, leaveOpen: true);
```

### Compression & encryption

Configure compression and/or encryption globally via `BitSerializer.Config`:

```csharp
// Enable Deflate compression
BitSerializer.Config = new SerializerConfig(compress: true);

// Enable AES-256 encryption with a password
BitSerializer.Config = new SerializerConfig(
    encrypt: true,
    encAlg: EncryptionAlgorithm.Aes256,
    encPassword: "my-very-secure-and-secret-key"
);

// Both
BitSerializer.Config = new SerializerConfig(
    compress: true,
    encrypt: true,
    compLevel: CompressionLevel.Optimal,
    encAlg: EncryptionAlgorithm.Aes256,
    encPassword: "my-very-secure-and-secret-key"
);

// Serialize/Deserialize transparently applies the config
byte[] data = BitSerializer.Serialize(player);
var restored = BitSerializer.Deserialize<Player>(data);
```

Processing order: **compress >> encrypt**. Revert order: **decrypt >> decompress**.

## Binary format

### Raw format (no compression / encryption)

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

- **Magic number**: `0x10 0x20 0x30`: identifies a valid BitSerializer payload
- **Field count**: 32-bit signed integer
- **NameHash**: SHA-256 hash of the field name (used to match fields during deserialization)
- **FieldType**: A single byte identifying the data type (see `FieldType` enum)
- **Value**: Serialized according to its `FieldType`

### Processed format (with compression / encryption)

When compression or encryption is enabled, the output is wrapped:

```
┌─────────────────────────────────┐
│ Config flags    (1 byte)        │  bit 0 = compressed
│                                 │  bit 1 = encrypted
├─────────────────────────────────┤
│ Processed payload  (variable)   │  compressed and/or encrypted raw data
└─────────────────────────────────┘
```

Processing order: **compress → encrypt**. Deserialization reverses: **decrypt → decompress**.

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
| `T[]` | Element `FieldType` (byte) + 4-byte length + elements |
| *(others)* | Standard `BinaryWriter` / `BinaryReader` encoding |

## SerializerConfig

`BitSerializer.Config` is a static property that controls compression and encryption behavior.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CompressResult` | `bool` | `false` | Enable Deflate compression |
| `CompressionLevel` | `CompressionLevel` | `SmallestSize` | Compression level |
| `EncryptResult` | `bool` | `false` | Enable encryption |
| `EncryptionAlgorithm` | `EncryptionAlgorithm` | `Aes256` | `XorCypher` or `Aes256` |
| `EncryptionPassword` | `string?` | `""` | Password for encryption |

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
