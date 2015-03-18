using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KinectServerConsole
{
    public static class Extensions
    {
        #region Camera

        public static ImageSource ToBitmap(this ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;
            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static ImageSource ToBitmap(this BodyIndexFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] frameData = new byte[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            //frame.CopyFrameDataToArray(frameData);
            frame.CopyFrameDataToArray(frameData);
            
            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < frameData.Length; ++depthIndex)
            {
                ushort depth = frameData[depthIndex];

                byte intensity = (byte)(0);

                pixels[colorIndex++] = intensity; // Blue
                pixels[colorIndex++] = intensity; // Green
                pixels[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static ImageSource ToBitmap(this ColorFrame frame, double widthRes, double heightRes)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
            ScaleTransform scale = new ScaleTransform((widthRes / bitmap.PixelWidth), (heightRes / bitmap.PixelHeight));
            TransformedBitmap tbBitmap = new TransformedBitmap(bitmap, scale);
            return tbBitmap;
        }

        public static ImageSource ToBitmap(this DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] pixelData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(pixelData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
            {
                ushort depth = pixelData[depthIndex];

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixels[colorIndex++] = intensity; // Blue
                pixels[colorIndex++] = intensity; // Green
                pixels[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static ImageSource ToBitmap(this InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort[] frameData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(frameData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < frameData.Length; infraredIndex++)
            {
                ushort ir = frameData[infraredIndex];

                byte intensity = (byte)(ir >> 7);

                pixels[colorIndex++] = (byte)(intensity / 1); // Blue
                pixels[colorIndex++] = (byte)(intensity / 1); // Green   
                pixels[colorIndex++] = (byte)(intensity / 0.4); // Red

                colorIndex++;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        #endregion

        #region Body

        public static Joint ScaleTo(this Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new Microsoft.Kinect.CameraSpacePoint
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        public static Joint ScaleTo(this Joint joint, double width, double height)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        private static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        #endregion

        #region Drawing

        public static void DrawSkeleton(this Canvas canvas, Body body)
        {
            if (body == null) return;

            foreach (Joint joint in body.Joints.Values)
            {
                canvas.DrawPoint(joint);
            }

            canvas.DrawLine(body.Joints[JointType.Head], body.Joints[JointType.Neck]);
            canvas.DrawLine(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight]);
            canvas.DrawLine(body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid]);
            canvas.DrawLine(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft]);
            canvas.DrawLine(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight]);
            canvas.DrawLine(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]);
            canvas.DrawLine(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]);
            canvas.DrawLine(body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft]);
            canvas.DrawLine(body.Joints[JointType.WristRight], body.Joints[JointType.HandRight]);
            canvas.DrawLine(body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft]);
            canvas.DrawLine(body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight]);
            canvas.DrawLine(body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft]);
            canvas.DrawLine(body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight]);
            canvas.DrawLine(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase]);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft]);
            canvas.DrawLine(body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight]);
            canvas.DrawLine(body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
            canvas.DrawLine(body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
            canvas.DrawLine(body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
            canvas.DrawLine(body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
            canvas.DrawLine(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft]);
            canvas.DrawLine(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight]);
        }

        public static void DrawPoint(this Canvas canvas, Joint joint)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;

            joint = joint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(Colors.LightBlue)
            };

            Canvas.SetLeft(ellipse, joint.Position.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, joint.Position.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }

        public static void DrawLine(this Canvas canvas, Joint first, Joint second)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            first = first.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            second = second.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Line line = new Line
            {
                X1 = first.Position.X,
                Y1 = first.Position.Y,
                X2 = second.Position.X,
                Y2 = second.Position.Y,
                StrokeThickness = 8,
                Stroke = new SolidColorBrush(Colors.LightBlue)
            };

            canvas.Children.Add(line);
        }

        #endregion

        public static Body Default(this IEnumerable<Body> bodies)
        {
            Body result = null;
            double closestBodyDistance = double.MaxValue;
            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    var position = body.Joints[JointType.SpineBase].Position;
                    var distance = position.Length();
                    
                    if (result == null || distance < closestBodyDistance)
                    {
                        result = body;
                        closestBodyDistance = distance;
                    }
                }
            }
            return result;
        }

        public static double Length(this Microsoft.Kinect.CameraSpacePoint point)
        {
            return Math.Sqrt(
            Math.Pow(point.X, 2) +
            Math.Pow(point.Y, 2) +
            Math.Pow(point.Z, 2)
            );
        }

        public static Point ToPoint(this Microsoft.Kinect.CameraSpacePoint position, CoordinateMapper coordinateMapper)
        {
            Point point = new Point();
            
            ColorSpacePoint colorPoint = coordinateMapper.MapCameraPointToColorSpace(position);
            point.X = float.IsInfinity(colorPoint.X) ? 0.0 : colorPoint.X;
            point.Y = float.IsInfinity(colorPoint.Y) ? 0.0 : colorPoint.Y;            

            return point;
        }

        /// <summary>
        /// Serializes an object to JSON.
        /// </summary>
        /// <param name="obj">The specified object.</param>
        /// <returns>A JSON representation of the object.</returns>
        private static string Serialize(object obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                return Encoding.Default.GetString(ms.ToArray());
            }
        }
    }
}
