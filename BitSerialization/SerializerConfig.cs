using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace BitSerialization;

public enum EncryptionAlgorithm
{
    XorCypher,
    Aes256
}

public record SerializerConfig(bool compress = false,
                               bool encrypt = false,
                               CompressionLevel compLevel = CompressionLevel.SmallestSize,
                               EncryptionAlgorithm encAlg = EncryptionAlgorithm.Aes256,
                               string encPassword = "")
{
    public bool CompressResult { get; init; } = compress;
    public CompressionLevel CompressionLevel { get; init; } = compLevel;
    public bool EncryptResult { get; init; } = encrypt;
    public EncryptionAlgorithm EncryptionAlgorithm { get; init; } = encAlg;
    public string? EncryptionPassword { get; init; } = encPassword;

    private const int _saltLength = 16;
    private static byte[] _aesSalt => "BitSerializerAES!"u8.ToArray();

    private static byte[] _compress(byte[] data, CompressionLevel level)
    {
        using var output = new MemoryStream();
        using (var dstream = new DeflateStream(output, level, leaveOpen: true))
            dstream.Write(data, 0, data.Length);
        return output.ToArray();
    }

    private static byte[] _decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        using var defStream = new DeflateStream(input, CompressionMode.Decompress);
        defStream.CopyTo(output);
        return output.ToArray();
    }

    private byte[] _encryptXor(byte[] data)
    {
        var key = EncryptionPassword is { Length: > 0 }
            ? Encoding.UTF8.GetBytes(EncryptionPassword) : [];
        if (key.Length == 0) return data;

        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        return result;
    }

    private byte[] _decryptXor(byte[] data) => _encryptXor(data);

    private byte[] _encryptAes(byte[] data)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        var password = EncryptionPassword ?? "";
        using var derive = new Rfc2898DeriveBytes(password, _aesSalt, 100_000, HashAlgorithmName.SHA256);
        aes.Key = derive.GetBytes(32);
        var iv = derive.GetBytes(16);
        aes.IV = iv;

        using var output = new MemoryStream();
        output.Write(iv, 0, iv.Length);
        using (var crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
            crypto.Write(data, 0, data.Length);
        return output.ToArray();
    }

    private byte[] _decryptAes(byte[] data)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        var password = EncryptionPassword ?? "";
        using var derive = new Rfc2898DeriveBytes(password, _aesSalt, 100_000, HashAlgorithmName.SHA256);
        aes.Key = derive.GetBytes(32);

        var iv = data.AsSpan(0, 16).ToArray();
        var cipher = data.AsSpan(16).ToArray();
        aes.IV = iv;

        using var input = new MemoryStream(cipher);
        using var crypto = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var output = new MemoryStream();
        crypto.CopyTo(output);
        return output.ToArray();
    }

    private byte[] _encrypt(byte[] data)
    {
        return EncryptionAlgorithm switch
        {
            EncryptionAlgorithm.XorCypher => _encryptXor(data),
            EncryptionAlgorithm.Aes256 => _encryptAes(data),
            _ => data
        };
    }

    private byte[] _decrypt(byte[] data)
    {
        return EncryptionAlgorithm switch
        {
            EncryptionAlgorithm.XorCypher => _decryptXor(data),
            EncryptionAlgorithm.Aes256 => _decryptAes(data),
            _ => data
        };
    }

    internal byte[] ProcessBytes(byte[] bytes)
    {
        byte flags = 0;
        if (CompressResult) flags |= 0b01;
        if (EncryptResult) flags |= 0b10;

        var data = bytes;
        if (CompressResult)
            data = _compress(data, CompressionLevel);
        if (EncryptResult)
            data = _encrypt(data);

        var result = new byte[1 + data.Length];
        result[0] = flags;
        data.CopyTo(result, 1);
        return result;
    }

    internal byte[] RevertBytes(byte[] bytes)
    {
        byte flags = bytes[0];
        var data = bytes.AsSpan(1).ToArray();

        if ((flags & 0b10) != 0)
            data = _decrypt(data);
        if ((flags & 0b01) != 0)
            data = _decompress(data);

        return data;
    }
}