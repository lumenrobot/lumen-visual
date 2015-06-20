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
using System.Threading;

namespace Lumen_Visual
{
    public class FaceDetection
    {
        private HaarCascade haarFrontalFace;

        public FaceDetection()
        {
            haarFrontalFace = new HaarCascade("haarcascade_frontalface_default.xml");
        }

        public MCvAvgComp[] detect(Image<Bgr, byte> ImageFrame)
        {
            MCvAvgComp[] faces;
            MCvAvgComp currFace;

            Image<Gray, byte> grayFrame = ImageFrame.Convert<Gray, byte>();
            faces = this.haarFrontalFace.Detect(grayFrame, 1.4, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(25, 25), Size.Empty);
            List<MCvAvgComp> returnFace = new List<MCvAvgComp>();
            for (int i = 0; i < faces.Length; i++)
            {
                currFace = faces[i];
                bool flag = true;
                for (int j = i; j < faces.Length; j++)
                {
                    if (i != j)
                    {
                        if (currFace.rect.IntersectsWith(faces[j].rect))
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                if (flag)
                {
                    returnFace.Add(faces[i]);
                }
            }

            return returnFace.ToArray();
        }
    }
}
