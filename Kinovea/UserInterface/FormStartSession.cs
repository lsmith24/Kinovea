using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kinovea.Root
{
    public partial class FormStartSession : Form
    {
        int numUsers;
        RootKernel kernel;
        IMongoCollection<User> collection;

        public FormStartSession()
        {
            InitializeComponent();
        }

        public FormStartSession(RootKernel kern)
        {
            InitializeComponent();
            kernel = kern;
        }

        private void FormStartSession_Load(object sender, EventArgs e)
        {
            numUsers = 1; // default to 1

            //var connectionString = ConfigurationManager.ConnectionStrings["MongoDBConnection"].ConnectionString;
            //IMongoClient client = new MongoClient(connectionString);
            //IMongoDatabase db = client.GetDatabase("Alopex");
            //collection = db.GetCollection<User>("alopexUsers");
        }

        private void list_numUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            numUsers = list_numUsers.SelectedIndex + 1;
            Console.WriteLine(numUsers);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < numUsers; i++)
            {
                fm_login loginForm = new fm_login(kernel);
                loginForm.ShowDialog();
                loginForm.Dispose();              
            }
            Close();
        }
    }
}
