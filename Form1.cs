using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebUIApp
{
    public partial class Form1 : Form
    {
        private WebView2 webView;

        private string mainDb = "mydb.db";
        private string methodsDb = "methods.db";

        public Form1()
        {
            InitializeComponent();
            InitUI();
        }

        private async void InitUI()
        {
            this.Width = 1000;
            this.Height = 700;

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(webView);

            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;

            // Load UI HTML
            string html = File.ReadAllText("ui.html");
            webView.NavigateToString(html);

            InitDatabases();
        }

        private void InitDatabases()
        {
            if (!File.Exists(mainDb))
            {
                SQLiteConnection.CreateFile(mainDb);
            }
            if (!File.Exists(methodsDb))
            {
                SQLiteConnection.CreateFile(methodsDb);
                using (var conn = new SQLiteConnection($"Data Source={methodsDb}"))
                {
                    conn.Open();
                    string sql = "CREATE TABLE IF NOT EXISTS Methods (Name TEXT PRIMARY KEY, Sql TEXT)";
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
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
                    var result = RunQuery(mainDb, msg.Sql);
                    await SendToJs("queryResult", result);
                    break;

                case "saveMethod":
                    SaveMethod(msg.Name, msg.Sql);
                    break;

                case "listMethods":
                    var methods = ListMethods();
                    await SendToJs("methodsList", methods);
                    break;

                case "runMethod":
                    string sql = GetMethodSql(msg.Name);
                    var methodResult = RunQuery(mainDb, sql);
                    await SendToJs("queryResult", methodResult);
                    break;
            }
        }

        private QueryResult RunQuery(string mainDb, string sql)
        {
            var result = new QueryResult();

            try
            {
                using (var conn = new SQLiteConnection("Data Source=" + mainDb))
                {
                    conn.Open();
                    using (var cmd = new SQLiteCommand(sql, conn))
                    {
                        var firstWord = sql.TrimStart().Split(' ')[0].ToUpperInvariant();

                        if (firstWord == "SELECT" || firstWord == "PRAGMA")
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                // Columns
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    result.Columns.Add(new ColumnInfo { ColumnName = reader.GetName(i) });
                                }

                                // Rows
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
                result.Message = ex.Message;
                result.IsError = true;
            }

            return result;
        }


        private void SaveMethod(string name, string sql)
        {
            using (var conn = new SQLiteConnection($"Data Source={methodsDb}"))
            {
                conn.Open();
                string upsert = "INSERT OR REPLACE INTO Methods (Name, Sql) VALUES (@name, @sql)";
                using (var cmd = new SQLiteCommand(upsert, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@sql", sql);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string[] ListMethods()
        {
            using (var conn = new SQLiteConnection($"Data Source={methodsDb}"))
            {
                conn.Open();
                string sql = "SELECT Name FROM Methods";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var list = new System.Collections.Generic.List<string>();
                    while (reader.Read())
                        list.Add(reader.GetString(0));
                    return list.ToArray();
                }
            }
        }

        private string GetMethodSql(string name)
        {
            using (var conn = new SQLiteConnection($"Data Source={methodsDb}"))
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
