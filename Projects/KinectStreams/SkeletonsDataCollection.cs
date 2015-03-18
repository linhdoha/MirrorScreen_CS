using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace KinectStreams
{
    [DataContract]
    [KnownType(typeof(SkeletonsDataCollection))]
    class SkeletonsDataCollection
    {
        [DataMember]
        public int BodyCount;
        [DataMember]
        public Skeleton[] SkeletonList;
    }
}
