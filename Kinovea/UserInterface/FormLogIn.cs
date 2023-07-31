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
        //public User loggedInUser = null;

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
            IMongoDatabase db = client.GetDatabase("Alopex");
            collection = db.GetCollection<User>("alopexUsers");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
            FormSignUp signUpForm = new FormSignUp(kernel);
            signUpForm.ShowDialog();
            signUpForm.Dispose();
            
        }

        private void btn_logIn_Click(object sender, EventArgs e)
        {
            searchForUser();
        }

        private bool searchForUser()
        {
            // this doesn't work if two people have same email but .. they shouldn't

            var filterDefinition = Builders<User>.Filter.Eq(r => r.Email, txt_email.Text);
            User person = collection.Find(filterDefinition).FirstOrDefault();

            if (person == null)
            {
                // no match found
                lbl_usererror.Text = "Incorrect Login";
                return false;
            }

            if (person.Name.Equals(txt_name.Text))
            {
                // Successful Log In
                kernel.currentUsers.Add(person);
                kernel.RefreshForUser();

                // new session
                //Session newSesh = new Session();
                //var update = Builders<User>.Update.Push<Session>(u => u.Sessions, newSesh);
                //collection.UpdateOne(filterDefinition, update);

                // add new session to map
                Session newSesh = new Session();
                kernel.sessionMap.Add(person, newSesh);

                this.Close();
                return true;
            }
            else
            {
                lbl_usererror.Text = "Incorrect Login";
                //Console.WriteLine("Login Failed");
                return false;
            }
            
        }
    }

    
}
