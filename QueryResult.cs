using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUIApp
{
    public class QueryResult
    {
        public List<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
        public List<List<object>> Rows { get; set; } = new List<List<object>>();
        public int RowsAffected { get; set; }
        public string Message { get; set; } = "";
        public bool IsError { get; set; } = false;
    }

    public class ColumnInfo
    {
        public string ColumnName { get; set; }
    }

}
