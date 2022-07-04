using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sautinsoft.ClientLib;

namespace PdfConverterExample
{
    public partial class Form1 : Form, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string BaseURL { get; set; }
        public string Token { get; set; }
        public string Result { get; set; }        
        public string FileConvert { get; set; }
        public string ObjectID { get; set; }
        public string JobID { get; set; }
        public string ResultObjectID { get; set; }
        public string Direction { get; set; }
        public Form1()
        {
            BaseURL = "https://api.sautinsoft.com/";
            Token = "6295c18ed84ba47376295c18ed84c7ee4b";
            InitializeComponent();
            txtBaseURL.DataBindings.Add("Text", this, "BaseURL");
            txtToken.DataBindings.Add("Text", this, "Token");
            rtxtResult.DataBindings.Add("Text", this, "Result");
            txtFileFoConvert.DataBindings.Add("Text", this, "FileConvert");
            txtObjectID.DataBindings.Add("Text", this, "ObjectID");
            txtJobID.DataBindings.Add("Text", this, "JobID");
            txtConvertResult.DataBindings.Add("Text", this, "ResultObjectID");
            cmbDir.DataBindings.Add("Text", this, "Direction");
        }

        private Client CreateClient()
        {
            if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(BaseURL))
                throw new Exception("Token or BaseURL is empty");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + Token);
            return new Client(BaseURL, httpClient);
        }

        public void OnPropertyChanged(string prop = "")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => { OnPropertyChanged(prop); }));
                return;
            }
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }


        private void btnVersion_Click(object sender, EventArgs e)
        {
            Task.Run(async () => {
                Log("Send GET /version");

                Client client = CreateClient();
                var res = await client.VersionAsync();
                Log("Version: " + res.Data.Version + "\nName: " + res.Data.Name);
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF files|*.pdf";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileConvert = openFileDialog.FileName;
                OnPropertyChanged("FileConvert");
            }
        }
        private void Log(string s)
        {
            Result += s + '\n';
            OnPropertyChanged("Result");
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            Task.Run(async () => {
                try
                {
                    Log("Start uploading");

                    Client client = CreateClient();
                    FileRequest fReq = new FileRequest();
                    fReq.Name = Path.GetFileName(txtFileFoConvert.Text);
                    fReq.ContentType = "application/pdf";

                    //Creating object
                    Log("Creating object (file)");
                    var res = await client.ObjectPOSTAsync(fReq);
                    Log("Created object id: " + res.Data.Id);

                    //Upload file
                    Log("Uploading file for object id: " + res.Data.Id);
                    FileStream fs = new FileStream(txtFileFoConvert.Text, FileMode.Open);
                    FileParameter fp = new FileParameter(fs, fReq.Name, fReq.ContentType);
                    var resUpload = await client.FilePOSTAsync(res.Data.Id, fp);
                    ObjectID = resUpload.Data.Id;
                    OnPropertyChanged("ObjectID");
                    Log("Uploaded file for object id: " + resUpload.Data.Id);
                }
                catch (Exception err)
                {
                    Log(err.Message);
                }
            });
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {
            Task.Run(async () => {
                try
                {
                    Log("Start conversion");

                    Client client = CreateClient();

                    //Creating job
                    Log("Creating job for conversion");
                    CommandRequest cmd = new CommandRequest();
                    cmd.InputObjectId = ObjectID;
                    cmd.Options = new Dictionary<string, string>();
                    cmd.Options.Add("direction", Direction);
                    var res = await client.PdffocusPOSTAsync(cmd);
                    Log("Created job id: " + res.Data.UniqId);
                    JobID = res.Data.UniqId;
                    OnPropertyChanged("JobID");

                    Log("Job created id: " + JobID);

                }
                catch (Exception err)
                {
                    Log(err.Message);
                }

            });
        }

        private void btnConvertStatus_Click(object sender, EventArgs e)
        {
            Task.Run(async () => {
                try
                {
                    Log("Get job status ID:");

                    Client client = CreateClient();

                    //Getting Job status
                    Log("Creating object (file)");
                    var res = await client.PdffocusGET2Async(JobID);
                    if (res.Data.Status == "Done")
                    {
                        Log("Conversion done");
                        ResultObjectID = res.Data.OutputObjects.First();
                        Log("Output objects:");
                        foreach (var item in res.Data.OutputObjects)
                        {
                            Log("--> " + item);
                        }
                    }
                    else
                    {
                        Log("Conversion status: " + res.Data.Status);
                    }                    
                }
                catch (Exception err)
                {
                    Log(err.Message);
                }

            });
        }

        private void btnDownLoad_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog();
//            save.FileName = file.Name;
            if (save.ShowDialog() == DialogResult.OK)
            {
                Task.Run(async () => {
                    try
                    {
                        Log("Start Downloading");
                        Client client = CreateClient();
                        var file = await client.FileGETAsync(ResultObjectID);
                        WriteToFile(file.Stream, save.FileName);
                        //Log("Created object id: " + res.Data.Id);
                        Log($"File Downloaded ({save.FileName})");

                    }
                    catch (Exception err)
                    {
                        Log(err.Message);
                    }
                });
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Task.Run(async () => {
                try
                {
                    Log("Job enqueueing");
                    Client client = CreateClient();
                    var res = await client.StartAsync(JobID);
                    
                    if (res.Data)
                        Log("Job enqueued");
                    else
                        Log($"Return code: {res.Code}");
                    //584b575c-3fde-4bc6-afb7-e7bf32749903
                }
                catch (Exception err)
                {
                    Log(err.Message);
                }
            });

        }
        protected void WriteToFile(Stream stream, string destinationFile)
        {
            using (Stream file = File.Create(destinationFile))
            {
                byte[] buffer = new byte[8 * 1024];
                int len;
                while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    file.Write(buffer, 0, len);
                }
            }
        }

        private void btnStatus_Click(object sender, EventArgs e)
        {

        }
    }
}
