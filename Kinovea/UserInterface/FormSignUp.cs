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
    public partial class FormSignUp : Form
    {

        IMongoCollection<User> collection;
        RootKernel kernel;

        public FormSignUp()
        {
            InitializeComponent();
        }

        public FormSignUp(RootKernel kern)
        {
            InitializeComponent();
            kernel = kern;
        }

        private void FormSignUp_Load(object sender, EventArgs e)
        {
            //var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");

            var connectionString = ConfigurationManager.ConnectionStrings["MongoDBConnection"].ConnectionString;
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase db = client.GetDatabase("Alopex");
            collection = db.GetCollection<User>("alopexUsers");
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btn_signUp_Click(object sender, EventArgs e)
        {
            // register user //
            User user = new User(   txt_name.Text, 
                                    txt_email.Text, 
                                    txt_phoneNum.Text, 
                                    txt_zip.Text,
                                    false
                                    );

            collection.InsertOne(user);
            //kernel.currentUsers.Add(user);
            //kernel.RefreshForUser();
            this.Close();
        }

        public void readData()
        {

        }
    }

    public class VideoObj
    {

        //[BsonElement("Video1")]
        //public string Video1 { get; set; }

        //[BsonElement("Video2")]
        //public string Video2 { get; set; }

        public List<string> VideoLinks { get; set; }

        [BsonElement("Club")]
        public string Club { get; set; }

        [BsonElement("Notes")]
        public string Notes { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Session")]
        public Session Session { get; set; }

        public VideoObj()
        {
            VideoLinks = new List<string>();
        }

        public VideoObj(List<string> videoLinks)
        {
            VideoLinks = new List<string>(videoLinks);
        }
    }

    public class Session
    {
        [BsonId]
        public ObjectId Id { get; set; }

        //[BsonElement("Videos")]
        //public List<VideoObj> Videos { get; set; }

        public Session()
        {
            Id = ObjectId.GenerateNewId();
            
        }
    }

    public class User
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Email")]
        public string Email { get; set; }

        [BsonElement("PhoneNumber")]
        public string PhoneNumber { get; set; }

        [BsonElement("ZipCode")]
        public string ZipCode { get; set; }

        [BsonElement("Instructor")]
        public bool Instructor { get; set; }

        [BsonElement("Videos")]
        public List<VideoObj> Videos { get; set; }

        //[BsonElement("Session")]
        //public Session Session { get; set; }

        public User(string name, string email, string phoneNum, string zipCode, bool instructor)
        {
            Name = name;
            Email = email;
            PhoneNumber = phoneNum;
            ZipCode = zipCode;
            Instructor = instructor;
            Videos = new List<VideoObj>();
        }
    }
}
