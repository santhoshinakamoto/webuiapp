using System.Windows.Forms;

namespace WebUIApp
{
    public partial class ResultControl : UserControl
    {
        public ResultControl()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;

            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Result Viewer Placeholder",
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lbl);
        }
    }
}