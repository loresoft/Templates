using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CodeSmith.Engine;
using SchemaExplorer;

namespace StoredProcedures
{
    public class Generator
    {
        public string ProcedureNameFormat { get; set; }


        public string EscapeFormat { get; set; } = "[{0}]";

        public string ParameterFormat { get; set; } = "@{0}";


        public bool IncludeOwner { get; set; } = true;

        public List<string> IgnoreColumns { get; set; } = new List<string>();

        public int TabSize { get; set; } = 4;


        public string ParameterKeys(TableSchema table, bool includeType = true, int indent = 1)
        {
            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (!column.IsPrimaryKeyMember)
                    continue;

                if (wroteColumn)
                {
                    s.Append(",");
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                }

                s.Append(ParameterName(column));

                if (includeType)
                {
                    s.Append(" ");
                    s.Append(NativeType(column));
                }

                wroteColumn = true;
            }

            return s.ToString();
        }

        public string Parameters(TableSchema table, bool allowIdentity = false, bool includeType = true, int indent = 1)
        {
            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (IsSkipped(column))
                    continue;

                if (!allowIdentity && IsIdentity(column))
                    continue;

                if (wroteColumn)
                {
                    s.Append(",");
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                }

                s.Append(ParameterName(column));

                if (includeType)
                {
                    s.Append(" ");
                    s.Append(NativeType(column));

                    if (column.AllowDBNull)
                        s.Append(" = NULL");
                }

                wroteColumn = true;
            }

            return s.ToString();
        }

        public string ParameterClause(TableSchema table, IEnumerable<string> columns, bool includeType = true, int indent = 1)
        {
            if (columns == null)
                return null;

            var list = columns.ToList();
            if (list.Count == 0)
                return null;


            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (!list.Contains(column.Name))
                    continue;

                if (wroteColumn)
                {
                    s.Append(",");
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                }

                s.Append(ParameterName(column));

                if (includeType)
                {
                    s.Append(" ");
                    s.Append(NativeType(column));
                }

                wroteColumn = true;
            }

            return s.ToString();
        }


        public string ColumnKeys(TableSchema table, string alias = null, bool firstOnly = false, int indent = 2)
        {
            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (!column.IsPrimaryKeyMember)
                    continue;

                if (wroteColumn)
                {
                    s.Append(", ");
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                }

                if (!string.IsNullOrEmpty(alias))
                    s.AppendFormat("{0}.", alias);

                s.Append(ColumnName(column));

                wroteColumn = true;

                if (firstOnly)
                    break;
            }

            return s.ToString();
        }

        public string Columns(TableSchema table, string alias = null, bool allowIdentity = false, bool allowSkipped = false, int indent = 2)
        {
            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (!allowSkipped && IsSkipped(column))
                    continue;

                if (!allowIdentity && IsIdentity(column))
                    continue;

                if (wroteColumn)
                {
                    s.Append(", ");
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                }

                if (!string.IsNullOrEmpty(alias))
                    s.AppendFormat("{0}.", alias);

                s.Append(ColumnName(column));
                
                wroteColumn = true;
            }

            return s.ToString();
        }


        public string UpdateFromParameter(TableSchema table, int indent = 2)
        {
            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (IsSkipped(column) || column.IsPrimaryKeyMember)
                    continue;

                if (wroteColumn)
                {
                    s.Append(", ");
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                }

                s.Append(ColumnName(column));
                s.Append(" = ");
                s.Append(ParameterName(column));

                wroteColumn = true;
            }
            return s.ToString();
        }

        public string UpdateFromSource(TableSchema table, string sourceAlias = "s", string targetAlias = "t", int indent = 2)
        {
            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (IsSkipped(column) || column.IsPrimaryKeyMember)
                    continue;

                if (wroteColumn)
                {
                    s.Append(", ");
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                }

                var columnName = ColumnName(column);

                s.AppendFormat("{0}.{1}", targetAlias, columnName);
                s.AppendFormat(" = {0}.{1}", sourceAlias, columnName);

                wroteColumn = true;
            }
            return s.ToString();
        }


        public string WhereFromKey(TableSchema table, string alias = null, int indent = 2)
        {
            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (!column.IsPrimaryKeyMember)
                    continue;

                if (wroteColumn)
                {
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                    s.Append("AND ");
                }

                if (!string.IsNullOrEmpty(alias))
                    s.AppendFormat("{0}.", alias);

                s.Append(ColumnName(column));
                s.Append(" = ");
                s.Append(ParameterName(column));

                wroteColumn = true;
            }
            return s.ToString();
        }

        public string WhereFromSource(TableSchema table, string sourceAlias = "s", string targetAlias = "t", int indent = 2)
        {
            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                if (!column.IsPrimaryKeyMember)
                    continue;

                if (wroteColumn)
                {
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                    s.Append("AND ");
                }

                var columnName = ColumnName(column);

                s.AppendFormat("{0}.{1}", targetAlias, columnName);
                s.AppendFormat(" = {0}.{1}", sourceAlias, columnName);

                wroteColumn = true;
            }

            return s.ToString();
        }

        public string WhereClause(TableSchema table, IEnumerable<string> columns, string alias = null, int indent = 2)
        {
            if (columns == null)
                return null;

            var list = columns.ToList();
            if (list.Count == 0)
                return null;

            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {

                if (!list.Contains(column.Name))
                    continue;

                if (wroteColumn)
                {
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                    s.Append("AND ");
                }

                if (!string.IsNullOrEmpty(alias))
                    s.AppendFormat("{0}.", alias);

                s.Append(ColumnName(column));
                s.Append(" = ");
                s.Append(ParameterName(column));

                wroteColumn = true;
            }
            return s.ToString();
        }


        public string SortClause(TableSchema table, IEnumerable<string> columns, bool sortDescending, string alias = null, int indent = 2)
        {
            var list = columns?.ToList() ?? new List<string>();

            var s = new StringBuilder();
            bool wroteColumn = false;

            foreach (var column in table.Columns)
            {
                bool isMatch = list.Contains(column.Name);
                bool isKey = list.Count == 0 && column.IsPrimaryKeyMember;
                if (!isMatch && !isKey)
                    continue;

                if (wroteColumn)
                {
                    s.Append(", ");
                    s.AppendLine();
                    s.Append(' ', TabSize * indent);
                }

                if (!string.IsNullOrEmpty(alias))
                    s.AppendFormat("{0}.", alias);

                s.Append(ColumnName(column));

                if (sortDescending)
                    s.Append(" DESC");

                wroteColumn = true;
            }

            return s.ToString();
        }


        public string TableName(TableSchema table)
        {
            var s = new StringBuilder();
            string format = EscapeFormat + ".";

            if (IncludeOwner && !string.IsNullOrEmpty(table.Owner))
                s.AppendFormat(format, table.Owner);

            s.AppendFormat(EscapeFormat, table.Name);
            return s.ToString();
        }

        public string ProcedureName(TableSchema table)
        {
            var s = new StringBuilder();
            var format = EscapeFormat + ".";

            if (IncludeOwner && !string.IsNullOrEmpty(table.Owner))
                s.AppendFormat(format, table.Owner);

            var name = string.Format(ProcedureNameFormat, table.Name);
            s.AppendFormat(EscapeFormat, name);
            return s.ToString();
        }

        public string ColumnName(DataObjectBase column)
        {
            var name = column.Name;
            name = string.Format(EscapeFormat, name);

            return name;
        }

        public string ParameterName(DataObjectBase column)
        {
            var name = column.Name;
            name = StringUtil.ToPascalCase(name);
            name = string.Format(ParameterFormat, name);

            return name;
        }


        public bool IsSkipped(DataObjectBase column)
        {
            if (IgnoreColumns != null)
                foreach (string expression in IgnoreColumns)
                    if (Regex.IsMatch(column.Name, expression))
                        return true;

            if (IsRowVersion(column))
                return true;

            return false;
        }

        public bool IsIdentity(DataObjectBase column)
        {
            bool isIdentity = false;
            try
            {
                if (column.ExtendedProperties.Contains(ExtendedPropertyNames.IsIdentity))
                {
                    var value = column.ExtendedProperties[ExtendedPropertyNames.IsIdentity].Value.ToString();
                    bool.TryParse(value, out isIdentity);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
            }

            return isIdentity;
        }

        public bool IsRowVersion(DataObjectBase column)
        {
            bool isTimeStamp = column.NativeType.Equals(
                "timestamp", StringComparison.OrdinalIgnoreCase);
            bool isRowVersion = column.NativeType.Equals(
                "rowversion", StringComparison.OrdinalIgnoreCase);

            return (isTimeStamp || isRowVersion);
        }


        public string NativeType(DataObjectBase column)
        {
            var s = new StringBuilder();

            var nativeType = column.NativeType.ToUpper();
            s.Append(nativeType);

            switch (nativeType)
            {
                case "BINARY":
                case "VARBINARY":
                case "CHAR":
                case "NCHAR":
                case "VARCHAR":
                case "NVARCHAR":
                    s.Append("(");

                    if (column.Size == -1)
                        s.Append("MAX");
                    else
                        s.Append(column.Size);

                    s.Append(")");
                    break;
                case "DECIMAL":
                case "NUMERIC":
                    s.Append("(");
                    s.Append(column.Precision);
                    s.Append(",");
                    s.Append(column.Scale);
                    s.Append(")");
                    break;
                case "DATETIME2":
                case "DATETIMEOFFSET":
                case "TIME":
                    if (column.Scale != 7)
                    {
                        s.Append("(");
                        s.Append(column.Scale);
                        s.Append(")");
                    }
                    break;
            }

            return s.ToString();
        }
    }
}
