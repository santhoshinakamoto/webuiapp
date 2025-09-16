using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebUIApp
{
    public partial class SqlEditorControl : UserControl
    {
        private WebView2 webView;
        private string dbPath = "mydb.db";

        public SqlEditorControl()
        {
            InitializeComponent();
            InitUI();
        }

        private async void InitUI()
        {
            this.Dock = DockStyle.Fill;

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webView);

            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

            // load UI HTML (your ui.html stays same)
            string html = File.ReadAllText("ui.html");
            webView.NavigateToString(html);

            InitDatabase();
        }

        private void InitDatabase()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                // You can create your tables if not exist (students, schools, methods, etc.)
            }
        }

        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string json = e.WebMessageAsJson;
            var msg = JsonConvert.DeserializeObject<AppMessage>(json);

            switch (msg.Type)
            {
                case "runQuery":
                    var result = RunQuery(msg.Sql, msg.Params);
                    await SendToJs("queryResult", result);
                    break;

                case "saveMethod":
                    SaveMethod(msg.Name, msg.Sql);
                    break;

                case "listMethods":
                    var methods = ListMethods();
                    await SendToJs("methodsList", methods);
                    break;

                case "getMethodSql":
                    string sql = GetMethodSql(msg.Name);
                    await SendToJs("methodSql", sql);
                    break;

                case "deleteMethod":
                    DeleteMethod(msg.Name);
                    break;
            }
        }

        private QueryResult RunQuery(string sql, Dictionary<string, string> parameters = null)
        {
            var result = new QueryResult();

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + dbPath))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        if (parameters != null)
                        {
                            foreach (var kvp in parameters)
                            {
                                string paramName = kvp.Key.StartsWith("@") ? kvp.Key : "@" + kvp.Key;
                                object paramValue = string.IsNullOrEmpty(kvp.Value) ? DBNull.Value : (object)kvp.Value;
                                cmd.Parameters.AddWithValue(paramName, paramValue);
                            }
                        }

                        var firstWord = sql.TrimStart().Split(' ')[0].ToUpperInvariant();
                        if (firstWord == "SELECT" || firstWord == "PRAGMA")
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                    result.Columns.Add(new ColumnInfo { ColumnName = reader.GetName(i) });

                                while (reader.Read())
                                {
                                    var row = new List<object>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                        row.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
                                    result.Rows.Add(row);
                                }
                                result.Message = $"{result.Rows.Count} row(s) returned";
                            }
                        }
                        else
                        {
                            int affected = cmd.ExecuteNonQuery();
                            result.RowsAffected = affected;
                            result.Message = $"{affected} row(s) affected";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
            }

            return result;
        }

        private void SaveMethod(string name, string sql)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string check = "SELECT COUNT(*) FROM Methods WHERE Name=@name";
                using (var cmdCheck = new SQLiteCommand(check, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@name", name);
                    long exists = (long)cmdCheck.ExecuteScalar();
                    if (exists > 0)
                    {
                        _ = SendToJs("error", "Method name already exists, choose another.");
                        return;
                    }
                }

                string insert = "INSERT INTO Methods (Name, Sql) VALUES (@name, @sql)";
                using (var cmd = new SQLiteCommand(insert, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@sql", sql);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string[] ListMethods()
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "SELECT Name FROM Methods";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var list = new List<string>();
                    while (reader.Read())
                        list.Add(reader.GetString(0));
                    return list.ToArray();
                }
            }
        }

        private string GetMethodSql(string name)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "SELECT Sql FROM Methods WHERE Name=@name";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        private void DeleteMethod(string name)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "DELETE FROM Methods WHERE Name=@name";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private async Task SendToJs(string type, object data)
        {
            var obj = new { Type = type, Data = data };
            string json = JsonConvert.SerializeObject(obj);
            webView.CoreWebView2.PostWebMessageAsJson(json);
        }
    }
}
