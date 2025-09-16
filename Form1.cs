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
    public partial class Form1 : Form
    {
        private WebView2 webView;
        private string appDb = "appdata.db";

        public Form1()
        {
            InitializeComponent();
            InitUI();
        }

        private async void InitUI()
        {
            this.Width = 1000;
            this.Height = 700;

            webView = new WebView2 { Dock = DockStyle.Fill };
            this.Controls.Add(webView);

            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

            string html = File.ReadAllText("ui.html");
            webView.NavigateToString(html);

            InitDatabases();
        }

        private void InitDatabases()
        {
            if (!File.Exists(appDb))
            {
                SQLiteConnection.CreateFile(appDb);
            }
            using (var conn = new SQLiteConnection($"Data Source={appDb}"))
            {
                conn.Open();
                string sql = "CREATE TABLE IF NOT EXISTS Methods (Name TEXT PRIMARY KEY, Sql TEXT)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private async void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var msg = JsonConvert.DeserializeObject<AppMessage>(e.WebMessageAsJson);

            switch (msg.Type)
            {
                case "runQuery":
                    var result = RunQuery(appDb, msg.Sql, msg.Params);
                    await SendToJs("queryResult", result);
                    break;

                case "saveMethod":
                    await SaveMethod(msg.Name, msg.Sql);
                    break;

                case "listMethods":
                    await SendToJs("methodsList", ListMethods());
                    break;

                case "deleteMethod":
                    DeleteMethod(msg.Name);
                    await SendToJs("methodsList", ListMethods());
                    break;

                case "getMethodSql":
                    var sql = GetMethodSql(msg.Name);
                    await SendToJs("methodSql", new { Name = msg.Name, Sql = sql });
                    break;
            }
        }

        private QueryResult RunQuery(string dbPath, string sql, Dictionary<string, string> parameters = null)
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

                        string firstWord = sql.TrimStart().Split(' ')[0].ToUpperInvariant();
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

        private async Task SaveMethod(string name, string sql)
        {
            using (var conn = new SQLiteConnection($"Data Source={appDb}"))
            {
                conn.Open();

                // check duplicate
                string check = "SELECT COUNT(*) FROM Methods WHERE Name=@name";
                using (var cmd = new SQLiteCommand(check, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    long count = (long)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        await SendToJs("error", "Method already exists. Choose another name.");
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
            await SendToJs("methodsList", ListMethods());
        }

        private string[] ListMethods()
        {
            var list = new List<string>();
            using (var conn = new SQLiteConnection($"Data Source={appDb}"))
            {
                conn.Open();
                string sql = "SELECT Name FROM Methods ORDER BY Name";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) list.Add(reader.GetString(0));
                }
            }
            return list.ToArray();
        }

        private void DeleteMethod(string name)
        {
            using (var conn = new SQLiteConnection($"Data Source={appDb}"))
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

        private string GetMethodSql(string name)
        {
            using (var conn = new SQLiteConnection($"Data Source={appDb}"))
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

        private async Task SendToJs(string type, object data)
        {
            var obj = new { Type = type, Data = data };
            string json = JsonConvert.SerializeObject(obj);
            webView.CoreWebView2.PostWebMessageAsJson(json);
        }
    }
}
