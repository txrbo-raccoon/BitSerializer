namespace BitSerialization;

/// <summary>Represents the type of a serializable field.</summary>
public enum FieldType : byte
{
    /// <summary>Unsupported or unrecognized type.</summary>
    Unknown,
    /// <summary>System.Boolean</summary>
    Bool,
    /// <summary>System.Byte</summary>
    Byte,
    /// <summary>System.SByte</summary>
    SByte,
    /// <summary>System.Char</summary>
    Char,
    /// <summary>System.Int16</summary>
    Int16,
    /// <summary>System.UInt16</summary>
    UInt16,
    /// <summary>System.Int32</summary>
    Int32,
    /// <summary>System.UInt32</summary>
    UInt32,
    /// <summary>System.Int64</summary>
    Int64,
    /// <summary>System.UInt64</summary>
    UInt64,
    /// <summary>System.Single (float)</summary>
    Single,
    /// <summary>System.Double (double)</summary>
    Double,
    /// <summary>System.Decimal</summary>
    Decimal,
    /// <summary>System.String</summary>
    String,
    /// <summary>byte[]</summary>
    ByteArray,
    /// <summary>System.DateTime</summary>
    DateTime,
    /// <summary>System.TimeSpan</summary>
    TimeSpan,
    /// <summary>System.Guid</summary>
    Guid,
    /// <summary>System.IntPtr / nint / nuint</summary>
    IntPtr,
    /// <summary>Any enum type (serialized as Int32)</summary>
    Enum,
    /// <summary>Array type (not yet supported for serialization)</summary>
    Array,
    /// <summary>Complex object type (not yet supported for serialization)</summary>
    Object,
}
