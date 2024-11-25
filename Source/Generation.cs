using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public static class Generation
{
    private static readonly Regex _splitNameRegex = new Regex(@"[\W_]+");
    private static readonly ConcurrentDictionary<string, HashSet<string>> _names;

    #region Data
    private static readonly HashSet<string> _csharpKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "as", "do", "if", "in", "is",
        "for", "int", "new", "out", "ref", "try",
        "base", "bool", "byte", "case", "char", "else", "enum", "goto", "lock", "long", "null", "this", "true", "uint", "void",
        "break", "catch", "class", "const", "event", "false", "fixed", "float", "sbyte", "short", "throw", "ulong", "using", "while",
        "double", "extern", "object", "params", "public", "return", "sealed", "sizeof", "static", "string", "struct", "switch", "typeof", "unsafe", "ushort",
        "checked", "decimal", "default", "finally", "foreach", "private", "virtual",
        "abstract", "continue", "delegate", "explicit", "implicit", "internal", "operator", "override", "readonly", "volatile",
        "__arglist", "__makeref", "__reftype", "interface", "namespace", "protected", "unchecked",
        "__refvalue", "stackalloc"
    };

    private static readonly Dictionary<string, string> _csharpTypeAlias = new Dictionary<string, string>(16)
    {
        {"System.Int16", "short"},
        {"System.Int32", "int"},
        {"System.Int64", "long"},
        {"System.String", "string"},
        {"System.Object", "object"},
        {"System.Boolean", "bool"},
        {"System.Void", "void"},
        {"System.Char", "char"},
        {"System.Byte", "byte"},
        {"System.UInt16", "ushort"},
        {"System.UInt32", "uint"},
        {"System.UInt64", "ulong"},
        {"System.SByte", "sbyte"},
        {"System.Single", "float"},
        {"System.Double", "double"},
        {"System.Decimal", "decimal"}
    };
    #endregion

    public static bool IsMixedCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var containsUpper = value.Any(Char.IsUpper);
        var containsLower = value.Any(Char.IsLower);

        return containsLower && containsUpper;
    }

    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        string output = ToPascalCase(value);
        if (output.Length > 2)
            return char.ToLower(output[0]) + output.Substring(1);

        return output.ToLower();
    }

    public static string ToPascalCase(this string value)
    {
        return value.ToPascalCase(_splitNameRegex);
    }

    public static string ToPascalCase(this string value, Regex splitRegex)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var mixedCase = value.IsMixedCase();
        var names = splitRegex.Split(value);
        var output = new StringBuilder();

        if (names.Length > 1)
        {
            foreach (string name in names)
            {
                if (name.Length > 1)
                {
                    output.Append(char.ToUpper(name[0]));
                    output.Append(mixedCase ? name.Substring(1) : name.Substring(1).ToLower());
                }
                else
                {
                    output.Append(name.ToUpper());
                }
            }
        }
        else if (value.Length > 1)
        {
            output.Append(char.ToUpper(value[0]));
            output.Append(mixedCase ? value.Substring(1) : value.Substring(1).ToLower());
        }
        else
        {
            output.Append(value.ToUpper());
        }

        return output.ToString();
    }


    public static string ToClassName(this string name)
    {
        return name.ToPascalCase().ToSafeName();
    }

    public static string ToPropertyName(this string name)
    {
        return name.ToPascalCase().ToSafeName();
    }

    public static string ToFieldName(this string name)
    {
        return "_" + name.ToCamelCase();
    }


    public static string MakeUnique(this string name, string bucketName)
    {
        var hashSet = _names.GetOrAdd(bucketName, k => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var result = MakeUnique(name, hashSet.Contains);
        hashSet.Add(result);

        return result;
    }

    public static string MakeUnique(this string name, Func<string, bool> exists)
    {
        string uniqueName = name;
        int count = 1;

        while (exists(uniqueName))
            uniqueName = string.Concat(name, count++);

        return uniqueName;
    }


    public static bool IsKeyword(this string text)
    {
        return _csharpKeywords.Contains(text);
    }

    public static string ToSafeName(this string name)
    {
        if (!name.IsKeyword())
            return name;

        return "@" + name;
    }


    public static string ToTypeName(this Type type)
    {
        return ToTypeName(type.FullName);
    }

    public static string ToTypeName(this string type)
    {
        if (type == "System.Xml.XmlDocument")
            type = "System.String";

        if (_csharpTypeAlias.TryGetValue(type, out string t))
            return t;

        // drop system from namespace
        string[] parts = type.Split('.');
        if (parts.Length == 2 && parts[0] == "System")
            return parts[1];

        return type;
    }


    public static string ToNullableType(this Type type, bool allowNull = false, bool nullableEnabled = true)
    {
        return ToNullableType(type.FullName, allowNull, IsValueType(type) || nullableEnabled);
    }

    public static string ToNullableType(this string type, bool allowNull = false, bool nullableEnabled = true)
    {
        type = type.ToTypeName();
        return allowNull && nullableEnabled ? type + "?" : type;
    }


    public static bool IsValueType(this Type type)
    {
        return type.IsValueType
            || typeof(DateTimeOffset) == type
            || typeof(DateTime) == type;
    }

    public static bool IsValueType(this string type)
    {
        var t = Type.GetType(type, false);
        if (t == null)
            return false;

        return IsValueType(t);
    }


    public static string ToLiteral(this string value)
    {
        return value.Contains('\n') || value.Contains('\r')
            ? "@\"" + value.Replace("\"", "\"\"") + "\""
            : "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }


}