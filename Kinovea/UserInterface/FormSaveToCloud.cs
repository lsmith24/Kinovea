using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using Kinovea.ScreenManager;
using MongoDB.Bson;

namespace Kinovea.Root
{
    public partial class FormSaveToCloud : Form
    {
        private RootKernel kernel;
        IMongoCollection<User> collection;
        User currentUser;

        public FormSaveToCloud()
        {
            InitializeComponent();
        }

        public FormSaveToCloud(RootKernel kern)
        {
            InitializeComponent();
            kernel = kern;
        }

        // upload button
        private void button1_Click(object sender, EventArgs e)
        {
            // need to get local filepaths of videos
            // put links to those videos in s3 bucket here

            Session currSesh = kernel.sessionMap[currentUser];

            string v1 = "video1_testlink";
            string v2 = "video2_testlink";

            VideoObj vidPair = new VideoObj
            {
                Club = txt_Club.Text,
                Notes = txtbx_Notes.Text,
                Name = txt_swingName.Text,
                Session = currSesh
            };

            vidPair.VideoLinks.Add(v1);
            vidPair.VideoLinks.Add(v2);

            // check if user has been selected
            if (currentUser == null)
            {
                lbl_user.ForeColor = Color.Red;
                return;
            }

            var filterDefinition = Builders<User>.Filter.Eq(u => u.Email, currentUser.Email);
            var update = Builders<User>.Update.Push<VideoObj>(u => u.Videos, vidPair);

            //// add video pair to the user's video array
            collection.FindOneAndUpdate(filterDefinition, update);
            Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void label1_Click_2(object sender, EventArgs e)
        {

        }

        private void FormSaveToCloud_Load(object sender, EventArgs e)
        {
            lbl_dataFill.Text = DateTime.Now.ToString(@"MM\/dd\/yyyy h\:mm tt");
            if (kernel.currentUsers.Count == 0)
            {
                //lbl_userName.Text = "No User";
                //lbl_userName.ForeColor = Color.Red;
                //Close();

                btnUploadToCloud.Enabled = false;
                return;
            }

            //lbl_userName.Text = kernel.currentUser.Name;
            //currentUser = kernel.currentUser;

            // list the logged in users so they can select which one is in the videos

            foreach (User u in kernel.currentUsers)
            {
                list_users.Items.Add(u.Name);
            }

            var connectionString = ConfigurationManager.ConnectionStrings["MongoDBConnection"].ConnectionString;
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase db = client.GetDatabase("Alopex");
            collection = db.GetCollection<User>("alopexUsers");

            //IEnumerable<CaptureScreen> capScreens = kernel.ScreenManager.GetCaptureScreens();
            //Console.WriteLine(capScreens.Count());
        }

        private void btnCancelUpload_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cmbox_InstructorList_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void list_users_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = list_users.SelectedIndex;
            currentUser = kernel.currentUsers[index];
            Console.WriteLine(currentUser.Name);
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
    }
}
