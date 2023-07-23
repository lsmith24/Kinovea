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

        public FormSignUp()
        {
            InitializeComponent();
        }

        private void FormSignUp_Load(object sender, EventArgs e)
        {
            //var connectionString = Environment.GetEnvironmentVariable("MONGODB_URI");

            var connectionString = ConfigurationManager.ConnectionStrings["MongoDBConnection"].ConnectionString;
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase db = client.GetDatabase("AlopexTest");
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
                                    txt_zip.Text        
                                    );

            collection.InsertOne(user);
            
            this.Close();
        }

        public void readData()
        {

        }
    }

    public class VideoPair
    {
        [BsonId]
        public ObjectId VideoId { get; set; }

        [BsonElement("Video1")]
        public string Video1 { get; set; }

        [BsonElement("Video2")]
        public string Video2 { get; set; }

        [BsonElement("Club")]
        public string Club { get; set; }

        [BsonElement("Notes")]
        public string Notes { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        public VideoPair(string vid1, string vid2)
        {
            Video1 = vid1;
            Video2 = vid2;
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

        [BsonElement("Videos")]

        public VideoPair[] Videos { get; set; }

        public User(string name, string email, string phoneNum, string zipCode)
        {
            Name = name;
            Email = email;
            PhoneNumber = phoneNum;
            ZipCode = zipCode;
        }
    }
}
