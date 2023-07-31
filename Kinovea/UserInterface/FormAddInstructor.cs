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
    public partial class FormAddInstructor : Form
    {
        RootKernel kernel;
        IMongoCollection<User> collection;

        public FormAddInstructor()
        {
            InitializeComponent();
        }

        public FormAddInstructor(RootKernel kern)
        {
            InitializeComponent();
            kernel = kern;
        }

        private void FormAddInstructor_Load(object sender, EventArgs e)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDBConnection"].ConnectionString;
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase db = client.GetDatabase("Alopex");
            collection = db.GetCollection<User>("alopexUsers");
        }

        private void btn_confirm_Click(object sender, EventArgs e)
        {
            User user = new User(txt_instructorName.Text,
                                    txt_instructorEmail.Text,
                                    txt_instructorPhone.Text,
                                    txt_instructorZip.Text,
                                    true
                                    );
            collection.InsertOne(user);
            Close();
        }
    }
}
