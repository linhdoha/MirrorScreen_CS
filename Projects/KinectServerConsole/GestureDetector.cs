using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System.IO;

namespace KinectServerConsole
{
    class GestureDetector : IDisposable
    {
        private readonly object lockObj = new object();

        // Path to the gesture database
        private string[] databasePaths;

        private int bodyIndex;

        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        private VisualGestureBuilderFrameReader vgbFrameReader = null;

        public List<GestureResult> GestureResults { get; private set; }

        public GestureDetector(int bodyIndex, KinectSensor sensor, List<GestureResult> gestureResults, string[] databasePaths)
        {
            if (sensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            this.databasePaths = databasePaths;
            this.GestureResults = gestureResults;
            this.bodyIndex = bodyIndex;

            lock (lockObj)
            {
                // create the vgb source. The associated body tracking ID will be set when a valid body frame arrives from the sensor.
                this.vgbFrameSource = new VisualGestureBuilderFrameSource(sensor, 0);
                this.vgbFrameSource.TrackingIdLost += this.Source_TrackingIdLost;
     
                // open the reader for the vgb frames
                this.vgbFrameReader = this.vgbFrameSource.OpenReader();
                if (this.vgbFrameReader != null)
                {
                    this.vgbFrameReader.IsPaused = true;
                    this.vgbFrameReader.FrameArrived += this.Reader_GestureFrameArrived;
                }
            addGesturesToResults();
            }
        }

        public void addGesturesToResults()
        {
            String currentPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);

            if (databasePaths != null && databasePaths.Length != 0)
            {
                foreach (string databasePath in databasePaths)
                {
                    String realDatabasePath = Path.Combine(currentPath, databasePath);

                    using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(realDatabasePath))
                    {
                        vgbFrameSource.AddGestures(database.AvailableGestures);
                    }
                }

                foreach (var gesture in vgbFrameSource.Gestures)
                {
                    GestureResults.Add(new GestureResult(bodyIndex, gesture.Name, false, false, 0.0f));
                }
            }   
        }

        /// <summary>
        /// Gets or sets the body tracking ID associated with the current detector
        /// The tracking ID can change whenever a body comes in/out of scope
        /// </summary>
        public ulong TrackingId
        {
            get
            {
                return this.vgbFrameSource.TrackingId;
            }

            set
            {
                if (this.vgbFrameSource.TrackingId != value)
                {
                    this.vgbFrameSource.TrackingId = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the detector is currently paused
        /// If the body tracking ID associated with the detector is not valid, then the detector should be paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return this.vgbFrameReader.IsPaused;
            }

            set
            {
                if (this.vgbFrameReader.IsPaused != value)
                {
                    this.vgbFrameReader.IsPaused = value;
                }
            }
        }

        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader objects
        /// </summary>
        /// <param name="disposing">True if Dispose was called directly, false if the GC handles the disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.vgbFrameReader != null)
                {
                    this.vgbFrameReader.FrameArrived -= this.Reader_GestureFrameArrived;
                    this.vgbFrameReader.Dispose();
                    this.vgbFrameReader = null;
                }

                if (this.vgbFrameSource != null)
                {
                    this.vgbFrameSource.TrackingIdLost -= this.Source_TrackingIdLost;
                    this.vgbFrameSource.Dispose();
                    this.vgbFrameSource = null;
                }
            }
        }

        private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            lock (GestureResults)
            {
                using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
                {
                    if (frame != null)
                    {
                        IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;

                        if (discreteResults != null)
                        {
                            foreach (Gesture gesture in this.vgbFrameSource.Gestures)
                            {
                                if (gesture.GestureType == GestureType.Discrete)
                                {
                                    DiscreteGestureResult result = null;
                                    discreteResults.TryGetValue(gesture, out result);

                                    if (result != null)
                                    {
                                        GestureResult gr = GestureResults.FirstOrDefault(n => n.Name == gesture.Name);
                                        gr.UpdateGestureResult(gesture.Name, true, result.Detected, result.Confidence);
                                    }
                                }
                            }
                        }

                        IReadOnlyDictionary<Gesture, ContinuousGestureResult> continuousResults = frame.ContinuousGestureResults;

                        if (continuousResults != null)
                        {
                            foreach (Gesture gesture in this.vgbFrameSource.Gestures)
                            {
                                if (gesture.GestureType == GestureType.Continuous)
                                {
                                    ContinuousGestureResult result = null;
                                    continuousResults.TryGetValue(gesture, out result);

                                    if (result != null)
                                    {
                                        GestureResult gr = GestureResults.FirstOrDefault(n => n.Name == gesture.Name);
                                        gr.UpdateGestureResult(gesture.Name, true, true, result.Progress);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the TrackingIdLost event for the VisualGestureBuilderSource object
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Source_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            lock (lockObj)
            {
                foreach (var gesture in GestureResults)
                {
                    gesture.UpdateGestureResult("", false, false, 0.0f);
                }
            }            
        }

        public void UpdateGestureDetector(string[] databasePaths)
        {
            if (databasePaths != null)
            {
                databasePaths = this.databasePaths;
                GestureResults.Clear();
                addGesturesToResults();
            }            
        }

        public override string ToString()
        {
            string result = "\nResult:\n";
            foreach (var gesture in GestureResults)
            {
                result += gesture.Name + " | Detected: " + gesture.Detected + " | Confidence: " + gesture.Confidence + "\n";
            }
            return result;
        }
    }
}
