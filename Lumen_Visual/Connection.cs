using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using System.Threading;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.GPU;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace Lumen_Visual
{
    class Connection
    {
        public Subscription sub; //pengecekan penerimaan data byte
        public IModel channelSend; //mengkonekkan, sehingga kirim2 via channel ini
        public IModel channelReceive; //channel buat terima command save gambar
        public QueueingBasicConsumer consumer;
        public EventingBasicConsumer consumerReceive;
        public BasicDeliverEventArgs global = new BasicDeliverEventArgs();

        public Connection()
        {
            ConnectionFactory factory = new ConnectionFactory(); //setting konek antara rabbitMQ dan program
            //factory.Uri = "amqp://lumen:lumen@169.254.18.223/%2F"; //memasukkan IP
            factory.Uri = "amqp://localhost/%2F"; //memasukkan IP
            IConnection connection = factory.CreateConnection(); //menyatukan koneksi nya
            IModel channel = connection.CreateModel(); //buat channel baru lagi
            channelSend = connection.CreateModel(); //buat saluran jalannya untuk mengirim
            channelReceive = connection.CreateModel();
            string routing = "avatar.nao1.camera.main"; //sebagai pengarah tujuan
            var arg = new Dictionary<string, object>
            {
                {"x-message-ttl",50}
            };
            QueueDeclareOk queue = channel.QueueDeclare("", true, false, true, arg);
            channel.QueueBind(queue.QueueName, "amq.topic", routing);
            consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(queue.QueueName, true, consumer);

            QueueDeclareOk queueRec = channelReceive.QueueDeclare("", true, false, true, null);
            channelReceive.QueueBind(queueRec.QueueName, "amq.topic", "lumen.visual.command");
            consumerReceive = new EventingBasicConsumer(channelReceive);
            channelReceive.BasicConsume(queueRec.QueueName, true, consumerReceive);
            consumerReceive.Received += new EventHandler<BasicDeliverEventArgs>(consumerReceive_Received);
            Thread QueryTrhead = new Thread(QueryImage);
            QueryTrhead.Start();
            MessageBox.Show("connected to server");

        }

        private void QueryImage()
        {
            BasicDeliverEventArgs ev = null;
            Stopwatch s = new Stopwatch();
            while (true)
            {
                s.Restart();
                ev = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                lock (global)
                {
                    global = ev;
                }
                //Console.WriteLine("time elapsed to get image : " + s.ElapsedMilliseconds + " ms");
            }
        }

        public Image<Bgr, byte> getImage()
        {
            Image<Bgr, Byte> ImageFrame;
            BasicDeliverEventArgs eventBody;
            if (global != null)
            {
                lock (global)
                {
                    eventBody = global;
                }
                if (eventBody.Body != null)
                {
                    string body = Encoding.UTF8.GetString(eventBody.Body);
                    JsonSerializerSettings setting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };
                    ImageObject image = JsonConvert.DeserializeObject<ImageObject>(body, setting);
                    string base64Image = image.ContentUrl.Replace("data:image/jpeg;base64,", "");

                    if (base64Image != null) //proses deteksi wajah
                    {

                        byte[] imageByte = Convert.FromBase64String(base64Image);
                        MemoryStream ms = new MemoryStream(imageByte);
                        Bitmap bmpImage = (Bitmap)Image.FromStream(ms);
                        ImageFrame = new Image<Bgr, byte>(bmpImage);
                        return ImageFrame;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public delegate void command_callback(object sender, string name);
        public event command_callback commandReceive;
        public void consumerReceive_Received(object sender, BasicDeliverEventArgs ev)
        {
            string body = Encoding.UTF8.GetString(ev.Body);
            if (commandReceive != null)
            {
                commandReceive(this, body);
            }
        }
        public void sendFaceLocation(MCvAvgComp face)
        {
            string routingKey = "lumen.visual.face.detection"; //isi routingkey face detection
            Point faceLocation = face.rect.Location; //medeteksi lokasi muka
            int X, Y;
            X = faceLocation.X + (face.rect.Width / 2);
            Y = faceLocation.Y + (face.rect.Height / 2);
            //Console.WriteLine("Face Location:" + " " + X + ", " + Y);

            FaceLocation loc = new FaceLocation { x = X, y = Y };
            string bodyLoc = JsonConvert.SerializeObject(loc);
            byte[] buffer = Encoding.UTF8.GetBytes(bodyLoc); //definisi pengiriman
            channelSend.BasicPublish("amq.topic", routingKey, null, buffer); //proses mengirim
        }

        public void sendBodyLocation(MCvAvgComp body)
        {
            string routingKey = "lumen.visual.human.detection";
            Point bodiLocation = body.rect.Location; //medeteksi lokasi bodi
            int X2, Y2;
            X2 = bodiLocation.X + (body.rect.Width / 2);
            Y2 = bodiLocation.Y + (body.rect.Height / 2);
            //Console.WriteLine("Body Location:" + " " + X2 + ", " + Y2);

            UpperBodyLocation loc = new UpperBodyLocation { x = X2, y = Y2 };
            string bodyLoc = JsonConvert.SerializeObject(loc);
            byte[] buffer = Encoding.UTF8.GetBytes(bodyLoc); //definisi pengiriman
            channelSend.BasicPublish("amq.topic", routingKey, null, buffer); //proses mengirim
        }

        public void sendFaceName(string name)
        {
            string routingKey = "lumen.visual.face.recognition";

            String nameFace = name;

            FaceName loc = new FaceName { Name = name };
            string bodyLoc = JsonConvert.SerializeObject(loc);
            byte[] buffer = Encoding.UTF8.GetBytes(bodyLoc); //definisi pengiriman
            channelSend.BasicPublish("amq.topic", routingKey, null, buffer); //proses mengirim
        }

        public void sendCenterFacePosition(CircleF circles)
        {
            string routingKey = "lumen.visual.face.tracking"; //isi routingkey face detection
            float centerFacePositionX = circles.Center.X; //medeteksi lokasi muka
            float centerFacePositionY = circles.Center.Y;
            float X = centerFacePositionX;
            float Y = centerFacePositionY;
            //Console.WriteLine("Center Face Location:" + " " + X + ", " + Y);

            CenterFaceLocation loc = new CenterFaceLocation { x = X, y = Y };
            string bodyLoc = JsonConvert.SerializeObject(loc);
            byte[] buffer = Encoding.UTF8.GetBytes(bodyLoc); //definisi pengiriman
            channelSend.BasicPublish("amq.topic", routingKey, null, buffer); //proses mengirim
        }
    }
}
