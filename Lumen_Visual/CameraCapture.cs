using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.GPU;
using Emgu.CV.UI;
using Newtonsoft.Json;
using RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using System.IO;
using System.Threading;
using System.Data.OleDb;

namespace Lumen_Visual
{
    public partial class CameraCapture : Form
    {
        public const int RESIZE_WIDTH = 640;
        public const int RESIZE_HEIGHT = 480;

        public Capture capture; //ambil gambar dari kamera        
        private bool captureInProgress; //mengecek apakah gambar telah di ambil        
        bool ready = false;
        MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.5d, 0.5d);
        Image<Gray, byte> gray = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> NamePersons = new List<string>();
        int contTrain, numLabels, t;
        string name, names = null;
        FaceDetection faceDetection;
        HumanDetection humanDetection;
        FaceRecognition faceRecognition;
        FaceTracking faceTracking;
        Connection connection;
        MCvAvgComp faceLoc;
        Image<Bgr, byte> GlobalImage;
        Image<Gray, byte> imageProcced;

        public CameraCapture()
        {
            InitializeComponent();
            this.Text = "Pengenalan Wajah";
            faceDetection = new FaceDetection();
            humanDetection = new HumanDetection();
            faceTracking = new FaceTracking();
        }

        private void ProcessFrameCamera(object sender, EventArgs arg) //mengambil gambar dari camera
        {
            Image<Bgr, Byte> ImageFrame;
            ImageFrame = capture.QueryFrame();

            if (ImageFrame != null)
            {
                //#region proses deteksi wajah
                //var faces = faceDetection.detect(ImageFrame);
                //foreach (var face in faces)
                //{

                    //GlobalImage = ImageFrame;
                    //faceLoc = face;
                    //if (faceRecognition.isTrainingDataExist())
                    //{

                    //    Image<Bgr, byte> result = ImageFrame.Copy(face.rect).Resize(100, 100, INTER.CV_INTER_CUBIC);
                    //    name = faceRecognition.recognize(result);
                    //    nama.Text = name;
                    //    ImageFrame.Draw(name, ref font, new Point(face.rect.X - 2, face.rect.Y - 2), new Bgr(Color.Black));
                    //}
                    //ImageFrame.Draw(face.rect, new Bgr(Color.Red), 3); //mendeteksi muka, dan membuat warna kotak+size kotak                    
                //}
                //NamePersons.Clear();

                //#endregion

                //#region face tracking
                //CircleF[] circles = faceTracking.track(ImageFrame);
                //foreach (CircleF circle in circles)
                //{
                //    CvInvoke.cvCircle(ImageFrame, // draw on the image
                //                      new Point((int)circle.Center.X, (int)circle.Center.Y), //center point of the cicrle
                //                      3, //radius of the circle in pixel
                //                      new MCvScalar(0, 255, 0), //draw pure green
                //                      -1, //thickness of circle in pixel, -1 indicated to fill the circle
                //                      LINE_TYPE.CV_AA, //use AA to smooth the pixel
                //                      0); //no shift
                //    //ImageFrame.Draw(circle, new Bgr(Color.Red), 3);
                //}
                //#endregion

                #region proses deteksi badan manusia bagian atas
                var body = humanDetection.detect(ImageFrame);
                foreach (var bodi in body)
                {
                    ImageFrame.Draw(bodi.rect, new Bgr(Color.Blue), 3);
                }
                #endregion

                camImageBox.Image = ImageFrame.Resize(RESIZE_WIDTH, RESIZE_HEIGHT, 
                    Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, true);
            }

        }

        private void ProcessFrameNAO(object sender, EventArgs arg) //mengambil dan menyimpan frame dari kamera nao
        {
            Image<Bgr, byte> ImageFrame = connection.getImage();
            if (ImageFrame != null)
            {
                Image<Gray, byte> grayframe = ImageFrame.Convert<Gray, byte>();

                #region face detection and recognize
                var faces = faceDetection.detect(ImageFrame);
                foreach (var face in faces)
                {
                    GlobalImage = ImageFrame;
                    faceLoc = face;
                    string name = "face unrecognized";
                    if (faceRecognition.isTrainingDataExist())
                    {

                        Image<Bgr, byte> result = ImageFrame.Copy(face.rect).Resize(100, 100, INTER.CV_INTER_CUBIC);
                        name = faceRecognition.recognize(result);
                        nama.Text = name;
                        ImageFrame.Draw(name, ref font, new Point(face.rect.X - 2, face.rect.Y - 2), new Bgr(Color.Black));
                    }
                    ImageFrame.Draw(face.rect, new Bgr(Color.Red), 3); //mendeteksi muka, dan membuat warna kotak+size kotak                
                    connection.sendFaceLocation(face);
                    connection.sendFaceName(name);
                }
                #endregion

                #region face tracking
                CircleF[] circles = faceTracking.track(ImageFrame);
                foreach (CircleF circle in circles)
                {
                    CvInvoke.cvCircle(ImageFrame, // draw on the image
                                      new Point((int)circle.Center.X, (int)circle.Center.Y), //center point of the cicrle
                                      3, //radius of the circle in pixel
                                      new MCvScalar(0, 255, 0), //draw pure green
                                      -1, //thickness of circle in pixel, -1 indicated to fill the circle
                                      LINE_TYPE.CV_AA, //use AA to smooth the pixel
                                      0); //no shift
                    //ImageFrame.Draw(circle, new Bgr(Color.Red), 3);
                    connection.sendCenterFacePosition(circle);
                }
                #endregion

                #region detect body
                var upperbody2 = humanDetection.detect(ImageFrame);
                foreach (var upperbodi2 in upperbody2)
                {
                    ImageFrame.Draw(upperbodi2.rect, new Bgr(Color.Blue), 3);
                    connection.sendBodyLocation(upperbodi2);
                }
                #endregion

                camImageBox.Image = ImageFrame.Resize(RESIZE_WIDTH, RESIZE_HEIGHT, 
                    Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR, true);
            }
        }

        private void btnStart_Click(object sender, EventArgs e) //proses button start
        {



            #region if capture is not created, create is now
            if (ready == false) //jika kamera belum aktif
            {
                try //mengecek error saat terjadi penangkapan kamera
                {
                    ready = true;
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            #endregion

            if (ready == true) //jika kamera aktif
            {
                if (captureInProgress)
                {
                    btnStart.Text = "Start";
                    Application.Idle -= ProcessFrameNAO;
                }
                else
                {
                    btnStart.Text = "Stop";
                    Application.Idle += ProcessFrameNAO;
                }
                captureInProgress = !captureInProgress;
            }
        }

        private void ReleaseData()//membuang objek setelah di ambil dari kamera
        {
            //if (capture != null)
            //    capture.Dispose();

        }

        private void button1_Click(object sender, EventArgs e) //tombol konek to server
        {
            faceRecognition = new FaceRecognition();
            humanDetection = new HumanDetection();
            connection = new Connection();
            connection.commandReceive+=new Connection.command_callback(connection_commandReceive);
            ready = true;
        }
        private void connection_commandReceive(object sender, string name)
        {
            Console.WriteLine(name);
            faceRecognition.addSample(GlobalImage, faceLoc);
            faceRecognition.saveSample(name);
        }

        private void button2_Click(object sender, EventArgs e) //tombol kamera
        {
            faceRecognition = new FaceRecognition();
            humanDetection = new HumanDetection();

            #region if capture is not created, create is now
            if (capture == null) //jika kamera belum aktif
            {
                try //mengecek error saat terjadi penangkapan kamera
                {
                    capture = new Capture();
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            #endregion

            if (capture != null) //jika kamera aktif
            {
                if (captureInProgress)
                {
                    btnStart.Text = "Start";
                    Application.Idle -= ProcessFrameCamera;
                }
                else
                {
                    btnStart.Text = "Stop";
                    Application.Idle += ProcessFrameCamera;
                }
                captureInProgress = !captureInProgress;
            }
        }

        private void btnAmbil_Click(object sender, EventArgs e) //proses button ambil sampel wajah
        {
            faceRecognition.addSample(GlobalImage, faceLoc);
        }

        private void button4_Click(object sender, EventArgs e) //proses simpan sampel wajah
        {
            faceRecognition.saveSample();
        }

    }
}
