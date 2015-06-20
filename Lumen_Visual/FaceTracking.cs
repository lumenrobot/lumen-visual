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
    class FaceTracking
    {
        public FaceTracking()
        {
        }
        public CircleF[] track(Image<Bgr, byte> ImageFrame)
        {
            Image<Gray, byte> imageProcced = ImageFrame.InRange(new Bgr(0, 0, 175), //minn filter
                                                  new Bgr(100, 100, 256)); //max filter
            imageProcced = imageProcced.SmoothGaussian(9); //call smoothGaussian with one param, that being the x and y size of the filter window (kernel)
            CircleF[] circles = imageProcced.HoughCircles(new Gray(100), //canny threshold
                                                                 new Gray(50), //gray accumulator threshold
                                                                  2, //size of image/this param = "acumulator resolution"
                                                                  imageProcced.Height / 4, //min distance in pixels between the centers of the detected cicrles
                                                                  10, //min rad
                                                                  400)[0]; //max rad
            return circles;

        }
    }
}
