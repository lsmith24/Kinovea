using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kinovea.Root
{
    public partial class FormEndSession : Form
    {
        RootKernel kernel;

        public FormEndSession()
        {
            InitializeComponent();
        }

        public FormEndSession(RootKernel kern)
        {
            InitializeComponent();
            kernel = kern;
        }

        private void btn_confirm_Click(object sender, EventArgs e)
        {
            if (kernel != null)
            {
                kernel.currentUsers = new List<User>();
                kernel.sessionMap = new Dictionary<User, Session>();
                kernel.RefreshForUser();
            }

            Close();
        }

        private void FormEndSession_Load(object sender, EventArgs e)
        {

        }
    }
}
