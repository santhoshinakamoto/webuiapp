using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebUIApp
{
    public class AppMessage
    {
        public string Type { get; set; }   // runQuery, saveMethod, listMethods, runMethod
        public string Sql { get; set; }    // SQL text
        public string Name { get; set; }   // Method name

        public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();
    }
}
