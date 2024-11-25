using System;
using System.Data;

using SchemaExplorer;

public static class Database
{
    public static string GetPropertyInitializer(this DataObjectBase column, bool includeNullability = false)
    {
        if (column.AllowDBNull == true)
            return string.Empty;
        else if (includeNullability && !column.SystemType.IsValueType)
            return " = default!;";
        else
            return string.Empty;

    }

    public static bool IsNullableType(DataObjectBase column)
    {
        if (!column.AllowDBNull)
            return false;

        if (column.SystemType.IsValueType || typeof(DateTimeOffset) == column.SystemType || typeof(DateTime) == column.SystemType)
            return true;

        return false;
    }

    public static string ToTypeName(this DataObjectBase column, bool includeNullability = false)
    {
        var typeName = column.DataType.ToTypeName() ?? column.SystemType.FullName;
        if (column.AllowDBNull && (column.SystemType.IsValueType() || includeNullability))
            typeName += "?";

        return typeName;
    }

    public static string ToTypeName(this DbType dbType)
    {
        switch (dbType)
        {
            case DbType.AnsiString:
            case DbType.AnsiStringFixedLength:
            case DbType.String:
            case DbType.StringFixedLength:
            case DbType.Xml:
                return "string";
            case DbType.Binary:
                return "byte[]";
            case DbType.Byte:
                return "byte";
            case DbType.Boolean:
                return "bool";
            case DbType.Date:
                return "DateOnly";
            case DbType.DateTime:
            case DbType.DateTime2:
                return "DateTime";
            case DbType.DateTimeOffset:
                return "DateTimeOffset";
            case DbType.Currency:
            case DbType.Decimal:
            case DbType.VarNumeric:
                return "decimal";
            case DbType.Double:
                return "double";
            case DbType.Guid:
                return "Guid";
            case DbType.Int16:
                return "short";
            case DbType.UInt16:
                return "ushort";
            case DbType.Int32:
                return "int";
            case DbType.UInt32:
                return "uint";
            case DbType.Int64:
                return "long";
            case DbType.UInt64:
                return "ulong";
            case DbType.Single:
                return "float";
            case DbType.Time:
                return "TimeOnly";
            case DbType.SByte:
                return "sbyte";
            default:
                return null;
        }
    }

    public static string ToReaderName(this DataObjectBase column)
    {
        var readerName = column.DataType.ToReaderName();
        if (column.AllowDBNull == true)
            readerName += "Null";

        return readerName;
    }

    public static string ToReaderName(this DbType dbType)
    {
        switch (dbType)
        {
            case DbType.AnsiString:
            case DbType.AnsiStringFixedLength:
            case DbType.String:
            case DbType.StringFixedLength:
            case DbType.Xml:
                return "GetString";
            case DbType.Binary:
                return "GetBytes";
            case DbType.Byte:
                return "GetByte";
            case DbType.Boolean:
                return "GetBoolean";
            case DbType.Date:
                return "GetDateOnly";
            case DbType.DateTime:
            case DbType.DateTime2:
                return "GetDateTime";
            case DbType.DateTimeOffset:
                return "GetDateTimeOffset";
            case DbType.Currency:
            case DbType.Decimal:
            case DbType.VarNumeric:
                return "GetDecimal";
            case DbType.Double:
                return "GetDouble";
            case DbType.Guid:
                return "GetGuid";
            case DbType.Int16:
                return "GetInt16";
            case DbType.Int32:
                return "GetInt32";
            case DbType.Int64:
                return "GetInt64";
            case DbType.Single:
                return "GetFloat";
            case DbType.Time:
                return "GetTimeOnly";
            default:
                return "GetValue";
        }
    }
}