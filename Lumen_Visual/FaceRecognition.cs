using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.GPU;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace Lumen_Visual
{
    class FaceRecognition
    {
        private int contTrain;
        private List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        private List<string> labels = new List<string>();
        private List<int> ID = new List<int>();
        private Image<Gray, byte> TrainedFace = null;
        public Image<Gray, byte> result = null;
        private int numLabels;
        private int currentSaved;
        FaceRecognizer efr;
        int n_sample = 0;
        float total_distance = 0.0f;

        public FaceRecognition()
        {
            try
            {
                string LabelsInfo = File.ReadAllText(Application.StartupPath + "/wajah/namaTrain.txt");
                string[] Labels = LabelsInfo.Split('%');
                efr = new EigenFaceRecognizer(80, double.PositiveInfinity);
                numLabels = Labels.Length;
                contTrain = numLabels;
                currentSaved = numLabels;
                string filewajah;
                for (int no = 1; no <= numLabels ; no++)
                {
                    ID.Add(no);
                    filewajah = "wajah" + no + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(Application.StartupPath + "/wajah/" + filewajah));
                    labels.Add(Labels[no - 1]);
                }
                efr.Train(this.trainingImages.ToArray(), ID.ToArray());

            }
            catch
            {
                MessageBox.Show("Data Pelatihan tidak ditemukan" + "\n" + "Lajutkan dengan pembuatan data pelatihan", "Load Wajah Pelatihan", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        public void saveSample()
        {
            if (Program.capture.textBoxName.Text == "")
            {
                MessageBox.Show("Nama Pemilik Gambar Harus Terisi", "Data Pelatihan", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                trainingImages.Add(TrainedFace);

                labels.Add(Program.capture.textBoxName.Text);
                contTrain += 1;
                try
                {

                    StreamWriter ts = File.AppendText(Application.StartupPath + "/wajah/namaTrain.txt");
                    ts.Write(string.Format("%{0}", labels[contTrain - 1]));
                    ts.Close();
                    trainingImages[contTrain - 1].Save(Application.StartupPath + "/wajah/wajah" + contTrain + ".bmp");
                    MessageBox.Show("Wajah " + Program.capture.textBoxName.Text + " telah dideteksi dan ditambahkan pada Data Training", "Proses Data Training", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ID.Add(contTrain);
                    efr.Dispose();
                    efr = new EigenFaceRecognizer(0, double.PositiveInfinity);
                    efr.Train(this.trainingImages.ToArray(), ID.ToArray());
                }
                catch (Emgu.CV.Util.CvException ev)
                {
                    Console.WriteLine(ev.ToString());
                    MessageBox.Show("Kesalahan Proses Training", "Kesalahan Training", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        public void saveSample(string name)
        {
            if (name == "")
            {
                Console.WriteLine("nama harus ada");
            }
            else
            {
                trainingImages.Add(TrainedFace);

                labels.Add(name);
                contTrain += 1;
                try
                {

                    StreamWriter ts = File.AppendText(Application.StartupPath + "/wajah/namaTrain.txt");
                    ts.Write(string.Format("%{0}", labels[contTrain - 1]));
                    ts.Close();
                    trainingImages[contTrain - 1].Save(Application.StartupPath + "/wajah/wajah" + contTrain + ".bmp");
                    ID.Add(contTrain);
                    efr.Dispose();
                    efr = new EigenFaceRecognizer(0, double.PositiveInfinity);
                    efr.Train(this.trainingImages.ToArray(), ID.ToArray());                    
                }
                catch (Emgu.CV.Util.CvException ev)
                {
                    Console.WriteLine(ev.ToString());
                    MessageBox.Show("Kesalahan Proses Training", "Kesalahan Training", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        public void addSample(Image<Bgr, byte> ImageFrame, MCvAvgComp loc)
        {
            try
            {
                TrainedFace = ImageFrame.Copy(loc.rect).Convert<Gray, byte>().Resize(100, 100, INTER.CV_INTER_CUBIC);
                showSample(TrainedFace);
            }
            catch
            {
                Console.WriteLine("ERROR IN ADD SAMPLE");
            }
        }

        public bool isTrainingDataExist()
        {
            return trainingImages.ToArray().Length != 0;
        }
        
        public string recognize(Image<Bgr, byte> ImageFrame)
        {
            float eigen_distance;
            float  eigen_threshold = 2205.151f;         
            float mean_distance;
            string result;
            Image<Gray, byte> gray = ImageFrame.Convert<Gray, byte>();
            MCvTermCriteria e = new MCvTermCriteria(contTrain, 0.001);

            try
            {                
                FaceRecognizer.PredictionResult ER = efr.Predict(gray);
                if (ER.Label != -1)
                {                    
                    eigen_distance = (float)ER.Distance;
                    if (n_sample < 100)
                    {
                        total_distance += eigen_distance;
                        n_sample+=1;
                        Console.WriteLine("distance sample {0} : {1}", n_sample, eigen_distance);
                    }
                    else
                    {
                        mean_distance = total_distance / (float)n_sample;
                        Console.WriteLine("total {0} mean {1}",total_distance, mean_distance);
                    }
                    if (eigen_distance > eigen_threshold)
                        result = labels[ER.Label];
                    else
                        result = "unknown";                    
                }
                else
                {
                    return "unknown";
                    eigen_distance = 0;
                }
            }
            catch (Emgu.CV.Util.CvException er)
            {
                Console.WriteLine(er.ToString());
                result = "error cvException";
            }
            catch
            {
                result = "unknown";
            }
            return result;
        }

        private void showSample(Image<Gray, byte> sample)
        {
            if (Program.capture.InvokeRequired)
            {
                setPictureCallback d = new setPictureCallback(showSample);
                Program.capture.Invoke(d, new object[] { sample });
            }
            else
            {
                Program.capture.imageBox1.Size = sample.Size;
                Program.capture.imageBox1.Image = sample;
            }
        }
        private delegate void setPictureCallback(Image<Gray, byte> sample);
    }
}
