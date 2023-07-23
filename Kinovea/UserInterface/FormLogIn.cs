using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Configuration;

namespace Kinovea.Root
{
    public partial class fm_login : Form
    {
        IMongoCollection<User> collection;
        public User loggedInUser = null;

        RootKernel kernel;

        public fm_login()
        {
            InitializeComponent();
        }

        public fm_login(RootKernel kern)
        {
            InitializeComponent();
            kernel = kern;
        }

        private void FormLogIn_Load(object sender, EventArgs e)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDBConnection"].ConnectionString;
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase db = client.GetDatabase("AlopexTest");
            collection = db.GetCollection<User>("alopexUsers");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
            FormSignUp signUpForm = new FormSignUp();
            signUpForm.ShowDialog();
            signUpForm.Dispose();
            
        }

        private void btn_logIn_Click(object sender, EventArgs e)
        {
            Console.WriteLine("HI");
            searchForUser();
        }

        private bool searchForUser()
        {
            var filterDefinition = Builders<User>.Filter.Eq(r => r.Email, txt_email.Text);
            var person = collection.Find(filterDefinition).FirstOrDefault();

            if (person == null)
            {
                lbl_usererror.Text = "User Not Found.";
                return false;
            }

            if (!person.Email.Equals(txt_email.Text))
            {
                lbl_usererror.Text = "User Not Found.";
                //Console.WriteLine("Login Failed");
                return false;
            }

            if (person.Name.Equals(txt_name.Text))
            {
                //Console.WriteLine("Login Succeeded");
                kernel.currentUser = person;
                kernel.RefreshForUser();
                this.Close();
                return true;
            }
            else
            {
                lbl_usererror.Text = "User Not Found.";
                //Console.WriteLine("Login Failed");
                return false;
            }
            
        }
    }

    
}
