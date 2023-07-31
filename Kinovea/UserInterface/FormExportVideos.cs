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
    public partial class FormExportVideos : Form
    {
        RootKernel kernel;
        IMongoCollection<User> collection;
        List<User> instructors;

        public FormExportVideos()
        {
            InitializeComponent();
        }

        public FormExportVideos(RootKernel kern)
        {
            InitializeComponent();
            kernel = kern;
        }

        private void FormExportVideos_Load(object sender, EventArgs e)
        {
            lbl_date2.Text = DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt");
            if (kernel.currentUsers.Count == 0)
            {
                lbl_userName.Text = "No User";
                lbl_userName.ForeColor = Color.Red;
                //Close();
                return;
            }
            lbl_userName.Text = kernel.currentUsers[0].Name;
            lbl_userEmail.Text = kernel.currentUsers[0].Email;

            // connect to database
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDBConnection"].ConnectionString;
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase db = client.GetDatabase("Alopex");
            collection = db.GetCollection<User>("alopexUsers");

            // search database for instructors
            var filter = Builders<User>.Filter.Eq(r => r.Instructor, true);
            instructors = collection.Find(filter).ToList();

            foreach (User instr in instructors)
            {
                Console.WriteLine(instr.Name);
                list_instructors.Items.Add(instr.Name);
            }

            
        }

        private void list_instructors_SelectedIndexChanged(object sender, EventArgs e)
        {
            string instruct = list_instructors.SelectedItem.ToString();

            // find instructor email (TODO: better way to do this)
            foreach (User n in instructors)
            {
                if (n.Name.Equals(instruct))
                {
                    lbl_instructorEmail.Text = n.Email;
                    return;
                }
            }
        }
    }
}
