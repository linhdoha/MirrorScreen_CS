using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.IO;
using System.Windows;

namespace KinectStreams
{
    public static class JSONBodySerializer
    {
        [DataContract]
        class JSONSkeletonCollection
        {
            [DataMember(Name = "skeletons")]
            public List<JSONSkeleton> Skeletons { get; set; }
        }
        [DataContract]
        class JSONSkeleton
        {
            [DataMember(Name = "id")]
            public string ID { get; set; }
            [DataMember(Name = "joints")]
            public List<JSONJoint> Joints { get; set; }
        }
        [DataContract]
        class JSONJoint
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }
            [DataMember(Name = "x")]
            public double X { get; set; }
            [DataMember(Name = "y")]
            public double Y { get; set; }
            [DataMember(Name = "z")]
            public double Z { get; set; }
        }

        /// <summary>
        /// Serializes an array of Kinect skeletons into an array of JSON skeletons.
        /// </summary>
        /// <param name="skeletons">The Kinect skeletons.</param>
        /// <param name="mapper">The coordinate mapper.</param>
        /// <param name="mode">Mode (color or depth).</param>
        /// <returns>A JSON representation of the skeletons.</returns>
        public static string Serialize(this List<Body> skeletons, CoordinateMapper mapper, Mode mode)
        {
            JSONSkeletonCollection jsonSkeletons = new JSONSkeletonCollection { Skeletons = new List<JSONSkeleton>() };
            foreach (var skeleton in skeletons)
            {
                JSONSkeleton jsonSkeleton = new JSONSkeleton
                {
                    ID = skeleton.TrackingId.ToString(),
                    Joints = new List<JSONJoint>()
                };
                foreach (var joint in skeleton.Joints)
                {
                    Point point = new Point();
                    switch (mode)
                    {
                        case Mode.Color:                            
                            ColorSpacePoint colorPoint = mapper.MapCameraPointToColorSpace(joint.Value.Position);                            
                            point.X = colorPoint.X;
                            point.Y = colorPoint.Y;
                            break;
                        case Mode.Depth:
                            DepthSpacePoint depthPoint = mapper.MapCameraPointToDepthSpace(joint.Value.Position);
                            point.X = depthPoint.X;
                            point.Y = depthPoint.Y;
                            break;
                        default:
                            break;
                    }
                    jsonSkeleton.Joints.Add(new JSONJoint
                    {
                        Name = joint.Key.ToString().ToLower(),
                        X = point.X,
                        Y = point.Y,
                        Z = joint.Value.Position.Z
                    });
                }
                jsonSkeletons.Skeletons.Add(jsonSkeleton);
            }
            return Serialize(jsonSkeletons);
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
