using System; // tolles Programmn
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.IO;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Timers;
using System.Threading;



    //ToDo Dispose() durch using-Statement ersetzen

namespace AudioRecorder
{
    public partial class Form1 : Form
    {
        private IWaveIn waveIn;
        private WaveFileWriter writer;
        private string outputFilename;
        private readonly string outputFolder;
        public TimeSpan timeleft;
        public long _ElapsedMilliseconds;
        public long ElapsedMilliseconds
        {
            get { return this._ElapsedMilliseconds; }
            set { this.ElapsedMilliseconds = sw.ElapsedMilliseconds; }
        }
        public long _totalTime;
        public long totalTime
        {
            get { return this._totalTime; }
            set { this.totalTime = (ElapsedMilliseconds - 30000) * 10000; }
        }

        Stopwatch sw = new Stopwatch();



        public Form1()
        {

            InitializeComponent();

            Disposed += OnRecordingPanelDisposed;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                LoadWasapiDivicesCombo();
            }
            else
            {
                comboBox1.Enabled = false;
                radioButtonWasapiLoopback.Enabled = false;
            }

            outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\AudioAufnahmen";
            Directory.CreateDirectory(outputFolder);

            //outputFolder = Application.StartupPath + "\\Temp";
            //Directory.CreateDirectory("C:\\Benutzer\\gjuli\\Dokumente\\AudioAufnahmen");

            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);

            toolTip1.SetToolTip(textBox1, "Hier kannst du den Namen der Datei anpassen");
            toolTip2.SetToolTip(comboBox1, "Hier siehst du deine verfügbaren Ein/Ausgabegeräte");
            toolTip3.SetToolTip(panel7, "Hier könnte ihre Werbung stehen");
            pictureBox1.Show();

            label6.Text = "Aktuell keine Aufnahme... Breit zum Aufnehmen";
            label4.Text = "Kommt irgendwann ^_^";

            radioButtonWaveIn.CheckedChanged += (s, a) => Cleanup();
            radioButtonWasapiLoopback.CheckedChanged += (s, a) => Cleanup();

            textBox1.Text = "AudioAufnahme";   
            
            DirectoryInfo dinfo = new DirectoryInfo(outputFolder);
            FileInfo[] Files = dinfo.GetFiles("*.wav");
            foreach (FileInfo file in Files)
            {
                listBox1.Items.Add(file.Name);
            }


            //this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);


        }

        void OnRecordingPanelDisposed(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void LoadWasapiDivicesCombo() //Anzeigen von allen Geräten in Combobox
        {
            var deviceEnum = new MMDeviceEnumerator();
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToList();
            comboBox1.DataSource = devices;
            comboBox1.DisplayMember = "FriendlyName";
        }

        private void button1_Click(object sender, EventArgs e) //Start Button
        {
            
            if (radioButtonWaveIn.Checked)

                Cleanup();

            if (waveIn == null)
            {
                CreateWaveInDevice();
            }
            
            string eingegebenerName = textBox1.Text;                       
            outputFilename = string.Format("{0}_{1:yyy-mm-dd hh-mm-ss}.wav",eingegebenerName ,DateTime.Now);
            writer = new WaveFileWriter(Path.Combine(outputFolder, outputFilename), waveIn.WaveFormat);
            waveIn.StartRecording();
            SetControlState(true);

          
            sw.Start();
            timeleft = new TimeSpan(0, 0, 0);
            label5.Text = timeleft.ToString(@"hh\:mm\:ss");
            timer1.Start();
            timer3.Start();            
            label6.Text = "Aufnahme läuft...";
         
        }

        private void CreateWaveInDevice()
        {
            if (radioButtonWaveIn.Checked)
            {
                waveIn = new WaveIn();
                waveIn.WaveFormat = new WaveFormat(8000, 1);
            }
            else
            {
                waveIn = new WasapiLoopbackCapture();
            }

            waveIn.DataAvailable += OnDataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;
        }
        
        void OnRecordingStopped(object sender, StoppedEventArgs e)      //Aktionen nach Stop Recording
        {
            if(InvokeRequired)
            {
                BeginInvoke(new EventHandler<StoppedEventArgs>(OnRecordingStopped), sender, e);
            }
            else
            {
                FinalizeWaveFile();
                if (e.Exception != null)
                {
                    MessageBox.Show(string.Format("Error{0}", e.Exception.Message));
                }

                
                int newItemIndex = listBox1.Items.Add(outputFilename);
                listBox1.SelectedIndex = newItemIndex;
                SetControlState(false);

            }
        }

        private void Cleanup()
        {
            if (waveIn != null)
            {
                waveIn.Dispose();
                waveIn = null;
            }
            FinalizeWaveFile();
        }

        private void FinalizeWaveFile()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(OnDataAvailable), sender, e);
            }
            else
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                int SecondsRecorded = (int)(writer.Length / writer.WaveFormat.AverageBytesPerSecond);
            }
        }

        public void button2_Click(object sender, EventArgs e) //Stop Button
        {

            if (sw.ElapsedMilliseconds >= 30000) //TODO!!!!!!!!!!!!!!!!!!!!!!!!!!
            {
                autoStop();
            }
            else
            {
                StopRecording();

                sw.Stop();
                _ElapsedMilliseconds = sw.ElapsedMilliseconds;
                _totalTime = (_ElapsedMilliseconds - 30000) * 10000;
                timer1.Stop();
                timer3.Stop();

                label6.Text = "Datei gespeichert unter " + outputFilename;
            }
       
        }


        void StopRecording() 
        {
            Debug.WriteLine("Stop Recording");
            if (waveIn != null) waveIn.StopRecording();       
        }

        private void button3_Click(object sender, EventArgs e) //Abspielen Button
        {
            if (listBox1.SelectedItem != null)
            {
                Process.Start(Path.Combine(outputFolder, outputFilename));           
            }
        }

        public void SetControlState(bool isRecording)
        {
            button1.Enabled = !isRecording;
            button2.Enabled = isRecording;
        }
        
        public void button4_Click(object sender, EventArgs e) //Datei löschen Button
        {
            if(listBox1.SelectedItem != null)
            {
                try
                {
                    File.Delete(Path.Combine(outputFolder, (string)listBox1.SelectedItem));
                    listBox1.Items.Remove(listBox1.SelectedItem);
                    if (listBox1.Items.Count > 0)
                    {
                        listBox1.SelectedIndex = 0;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Aufnahme konnte nicht gelöscht werden");
                }
            }
            
            label6.Text = "Datei wurde gelöscht!";

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            

            if (radioButtonWaveIn.Checked)
            {
                radioButtonWasapiLoopback.Checked = false;
            }
            else
            {
                radioButtonWaveIn.Checked = false;
            }

        }

        private void button5_Click(object sender, EventArgs e)  //Zuschneiden Button
        {
            
            TimeSpan ending = new TimeSpan(0);
            TimeSpan begining = new TimeSpan(_totalTime);

            try
            {
                string inputPath = Path.Combine(outputFolder, (string)listBox1.SelectedItem);
                string trimedPath = outputFolder + outputFilename; //TODO: eigenen Namen

                if (listBox1.SelectedItem != null)
                {

                    TrimWavFile(inputPath, trimedPath, begining, ending); // outputFilename ODER: listBox.selectedItem

                }

                else
                {
                    MessageBox.Show("Es wurden keine Dateien aufgenommen!");
                }
            }
            catch (Exception ey)
            {
                MessageBox.Show(ey.ToString());
            }
         
        }

        public void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd) // Eingeben der zu verkürzenden Zeit + Path
        {
            using (WaveFileReader reader = new WaveFileReader(inPath)) // -> öffnet Pfad der Datei 
            {
                using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat)) // -> schreibt Datei in Pfad
                {
                    int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000; // liest die Größe aus Anfangsdatei

                    int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                    startPos = startPos - startPos % reader.WaveFormat.BlockAlign; // neue Startposition 

                    int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                    endBytes = endBytes - endBytes % reader.WaveFormat.BlockAlign; // neue Endposition
                    int endPos = (int)reader.Length - endBytes;

                    TrimWavFile(reader, writer, startPos, endPos);
                }
            }

        }

        private static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos)
            {//reader.Position < endPos
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);

                    }
                    else if (bytesRead == 0)
                    {
                        break;
                    }
                }
                else if (bytesRequired <0)
                {
                    break;
                
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
                        
            if (waveIn != null)
            {
                DialogResult result = MessageBox.Show("Bist du sicher, dass du das Programm schließen willst?", "Programm schließen", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

            switch (result)
            {
                case DialogResult.OK:
                    button2.PerformClick();
                    
                    break;
                case DialogResult.Cancel:
                    e.Cancel = true;
                    break;

            }
            }
            
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            timeleft = timeleft.Add(TimeSpan.FromSeconds(1));
            label5.Text = timeleft.ToString(@"hh\:mm\:ss");
        }

        

        private void timer3_Tick(object sender, EventArgs e)
        {

            autoStop();
            //TODO: Aufnahme wird wieder gestartet

        }


        void autoStop()
        {
            StopRecording();

            sw.Stop();
            _ElapsedMilliseconds = sw.ElapsedMilliseconds;
            _totalTime = (_ElapsedMilliseconds - 30000) * 10000;
            timer1.Stop();
            timer3.Stop();

            label6.Text = "Datei gespeichert unter " + outputFilename;  //Stop

            try
            {
                writer.Dispose();
                autoTrim();
            }
            catch (Exception)
            {

                throw;
            }
           
        }

        void autoTrim()
        {
            TimeSpan ending = new TimeSpan(0);
            TimeSpan begining = new TimeSpan(_totalTime);

            string inputPath = Path.Combine(outputFolder, outputFilename);
            string trimedPath = outputFolder + outputFilename;

            TrimWavFile(inputPath, trimedPath, begining, ending); // outputFilename ODER: listBox.selectedItem

            try
            {
                File.Delete(inputPath);
            }
            catch (Exception ew)
            {
                MessageBox.Show(ew.ToString());
            }

            File.Move(trimedPath, inputPath);
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            //progressBar1.Minimum = 0;
            //progressBar1.Maximum = 30;
            
            //if(sw.ElapsedMilliseconds <= 30000)
            //{
            //    progressBar1.Value = progressBar1.Value + 1;
            //}
            

        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                button2.PerformClick();
            }
        }
    }
}
