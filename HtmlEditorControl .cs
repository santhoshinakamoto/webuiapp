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
    public partial class HtmlEditorControl : UserControl
    {
        private WebView2 webView;
        private string dbPath = "mydb.db"; // reuse same db file as SqlEditorControl

        public HtmlEditorControl()
        {
            InitializeComponent();
            InitUI();
        }

        private async void InitUI()
        {
            this.Dock = DockStyle.Fill;
            webView = new WebView2 { Dock = DockStyle.Fill };
            this.Controls.Add(webView);

            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

            string html = File.ReadAllText("html_editor.html");
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

                // ensure tables
                string createPages = "CREATE TABLE IF NOT EXISTS Pages (Title TEXT PRIMARY KEY, Html TEXT)";
                using (var cmd = new SQLiteCommand(createPages, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string createMethods = "CREATE TABLE IF NOT EXISTS Methods (Name TEXT PRIMARY KEY, Sql TEXT)";
                using (var cmd = new SQLiteCommand(createMethods, conn))
                {
                    cmd.ExecuteNonQuery();
                }
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

                case "runMethod":
                    string sql = GetMethodSql(msg.Name);
                    var methodResult = RunQuery(sql, msg.Params);
                    await SendToJs("queryResult", methodResult);
                    break;

                case "savePage":
                    SavePage(msg.Name, msg.Sql);
                    break;

                case "loadPage":
                    string html = LoadPage(msg.Name);
                    await SendToJs("pageContent", html);
                    break;

                case "listPages":
                    var pages = ListPages();
                    await SendToJs("pagesList", pages);
                    break;
            }
        }

        // ---------------- DB methods ----------------
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

        private void SavePage(string title, string html)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();

                string check = "SELECT COUNT(*) FROM Pages WHERE Title=@title";
                using (var cmdCheck = new SQLiteCommand(check, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@title", title);
                    long exists = (long)cmdCheck.ExecuteScalar();
                    if (exists > 0)
                    {
                        string update = "UPDATE Pages SET Html=@html WHERE Title=@title";
                        using (var cmd2 = new SQLiteCommand(update, conn))
                        {
                            cmd2.Parameters.AddWithValue("@title", title);
                            cmd2.Parameters.AddWithValue("@html", html);
                            cmd2.ExecuteNonQuery();
                        }
                        return;
                    }
                }

                string insert = "INSERT INTO Pages (Title, Html) VALUES (@title,@html)";
                using (var cmd = new SQLiteCommand(insert, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@html", html);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string LoadPage(string title)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "SELECT Html FROM Pages WHERE Title=@title";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    var result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "";
                }
            }
        }

        private string[] ListPages()
        {
            var list = new List<string>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                string sql = "SELECT Title FROM Pages ORDER BY Title";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) list.Add(reader.GetString(0));
                }
            }
            return list.ToArray();
        }

        // ---------------- Helper ----------------
        private async Task SendToJs(string type, object data)
        {
            var obj = new { Type = type, Data = data };
            string json = JsonConvert.SerializeObject(obj);
            webView.CoreWebView2.PostWebMessageAsJson(json);
        }
    }
}
