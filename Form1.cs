using System.Windows.Forms;

namespace WebUIApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitUI();
        }

        private void InitUI()
        {
            this.Width = 1200;
            this.Height = 800;

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill
            };

            var sqlTab = new TabPage("SQL Editor");
            sqlTab.Controls.Add(new SqlEditorControl());

            var htmlTab = new TabPage("HTML Editor");
            htmlTab.Controls.Add(new HtmlEditorControl());

            var resultTab = new TabPage("Results");
            resultTab.Controls.Add(new ResultControl());

            tabs.TabPages.Add(sqlTab);
            tabs.TabPages.Add(htmlTab);
            tabs.TabPages.Add(resultTab);

            this.Controls.Add(tabs);
        }
    }
}
