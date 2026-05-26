namespace BitSerialization;

/// <summary>Specifies the serialization order for a field or property.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple =  false, Inherited = true)]
public class OrderAttribute(int order) : Attribute
{
    /// <summary>The zero-based order index.</summary>
    internal int Order => order;
}