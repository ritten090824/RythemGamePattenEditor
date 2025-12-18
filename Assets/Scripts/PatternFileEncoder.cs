using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public static class PatternFileEncoder
{
    private const string EncryptionKey = "MySecretKey123";

    // ✅ 저장
    public static void SavePatternFile(string path, PatternData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            string shifted = EncodeSequential(json);
            string xored = EncryptXOR(shifted, EncryptionKey);

            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xored));
            string hex = ToHex(b64);
            string b4 = ToBase4(hex);

            string signature = ComputeSHA256(b4);
            string final = b4 + "::SIGN=" + signature;

            File.WriteAllText(path, final);
            Debug.Log($"패턴 저장 완료: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError("패턴 저장 실패: " + ex.Message);
        }
    }

    // ✅ 불러오기
    public static PatternData LoadPatternFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.LogError("파일이 존재하지 않습니다: " + path);
                return null;
            }

            string raw = File.ReadAllText(path);
            string[] parts = raw.Split(new[] { "::SIGN=" }, StringSplitOptions.None);

            if (parts.Length != 2)
            {
                Debug.LogError("서명이 누락되었거나 형식이 잘못되었습니다.");
                return null;
            }

            string b4 = parts[0];
            string savedSignature = parts[1];
            string computedSignature = ComputeSHA256(b4);

            if (savedSignature != computedSignature)
            {
                Debug.LogError("파일이 수정되었거나 손상되었습니다.");
                return null;
            }

            string hex = FromBase4(b4);
            string b64 = FromHex(hex);
            string xored = Encoding.UTF8.GetString(Convert.FromBase64String(b64));

            string shifted = DecryptXOR(xored, EncryptionKey);
            string json = DecodeSequential(shifted);

            PatternData data = JsonUtility.FromJson<PatternData>(json);
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError("패턴 불러오기 실패: " + ex.Message);
            return null;
        }
    }
    private static string ComputeSHA256(string input)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }

    // ✅ XOR
    private static string EncryptXOR(string input, string key)
    {
        char[] output = new char[input.Length];
        for (int i = 0; i < input.Length; i++)
            output[i] = (char)(input[i] ^ key[i % key.Length]);
        return new string(output);
    }
    private static string DecryptXOR(string input, string key) => EncryptXOR(input, key);

    // ✅ Shift
    private static string EncodeSequential(string input)
    {
        char[] arr = input.ToCharArray();
        for (int i = 0; i < arr.Length; i++) arr[i] = (char)(arr[i] + 1);
        return new string(arr);
    }
    private static string DecodeSequential(string input)
    {
        char[] arr = input.ToCharArray();
        for (int i = 0; i < arr.Length; i++) arr[i] = (char)(arr[i] - 1);
        return new string(arr);
    }

    // ✅ Hex
    private static string ToHex(string input)
    {
        var sb = new StringBuilder();
        foreach (char c in input) sb.AppendFormat("{0:X2}", (int)c);
        return sb.ToString();
    }
    private static string FromHex(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return Encoding.UTF8.GetString(bytes);
    }

    // ✅ Base4 (0,1,2,3 문자만 사용)
    private static string ToBase4(string input)
    {
        var sb = new StringBuilder();
        foreach (char c in input)
        {
            int val = (int)c;
            for (int i = 0; i < 4; i++)
            {
                sb.Insert(sb.Length, val % 4);
                val /= 4;
            }
        }
        return sb.ToString();
    }
    private static string FromBase4(string input)
    {
        var bytes = new List<byte>();
        for (int i = 0; i < input.Length; i += 4)
        {
            int val = 0;
            for (int j = 3; j >= 0; j--)
                val = val * 4 + (input[i + j] - '0');
            bytes.Add((byte)val);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }
}
