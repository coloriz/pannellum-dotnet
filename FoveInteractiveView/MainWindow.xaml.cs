using System;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using OpenTK;
using Pannellum;
using Fove.Managed;
using Microsoft.Win32;

namespace FoveInteractiveView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        OpenCvSharp.VideoCapture video;
        FVRHeadset headset;
        OpenCvSharp.Mat currentFrame = new OpenCvSharp.Mat();
        EquirectToRect renderer;
        int video_width, video_height;
        DispatcherTimer renderingTimer;
        string filename;

        public MainWindow()
        {
            InitializeComponent();

            headset = new FVRHeadset(EFVR_ClientCapabilities.Gaze | EFVR_ClientCapabilities.Orientation);

            headset.IsHardwareConnected(out bool isConnected);
            Debug.Assert(isConnected, "Hardware is not connected!");

            headset.IsHardwareReady(out bool isReady);
            Debug.Assert(isReady, "Hardware capabilities are not ready!");

            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                filename = dialog.FileName;
            }
            else
            {
                MessageBox.Show("File is not selected!");
                return;
            }

            video = new OpenCvSharp.VideoCapture(filename);
            if (!video.IsOpened())
            {
                MessageBox.Show("Video is not valid!");
                return;
            }

            video_width = (int)video.Get(OpenCvSharp.CaptureProperty.FrameWidth);
            video_height = (int)video.Get(OpenCvSharp.CaptureProperty.FrameHeight);

            OpenCvSharp.Cv2.NamedWindow("coord", OpenCvSharp.WindowMode.Normal);
            OpenCvSharp.Cv2.ResizeWindow("coord", 1600, 900);

            renderer = new EquirectToRect(new GLControl(), new System.Drawing.Size(1400, 900), 100 * (float)Math.PI / 180);
            renderer.Viewer.Paint += (sender, e) =>
            {
                renderer.Render(currentFrame);
            };
            renderer.Viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            viewer.Child = renderer.Viewer;

            renderingTimer = new DispatcherTimer();
            renderingTimer.Interval = TimeSpan.FromMilliseconds(1);
            renderingTimer.Tick += (s, e) =>
            {
                video.Read(currentFrame);
                if (currentFrame.Empty())
                {
                    video.Set(OpenCvSharp.CaptureProperty.PosFrames, 0);
                    return;
                }

                headset.GetHMDPose(out SFVR_Pose pose);

                using (OpenCvSharp.Mat coordmap = currentFrame.Clone())
                {
                    SFVR_Vec3 direction = pose.orientation * SFVR_Vec3.Forward;
                    GetCoord(direction, video_width, video_height, out int x, out int y);
                    OpenCvSharp.Cv2.Circle(coordmap, new OpenCvSharp.Point(x, y), 50, new OpenCvSharp.Scalar(0, 0, 255), -1);

                    OpenCvSharp.Cv2.ImShow("coord", coordmap);
                    OpenCvSharp.Cv2.WaitKey(1);
                }

                SFVR_Vec3 eulerAngle = pose.orientation.EulerAngles;

                renderer.Pitch = -MathHelper.DegreesToRadians(eulerAngle.x);
                renderer.Yaw = MathHelper.DegreesToRadians(eulerAngle.y);
                renderer.Roll = MathHelper.DegreesToRadians(eulerAngle.z);

                renderer.Viewer.Invalidate();
            };
            renderingTimer.Start();
        }

        static void GetCoord(SFVR_Vec3 vec, int width, int height, out int x_coord, out int y_coord)
        {
            float longitude = (float)Math.Atan2(vec.x, vec.z) * 180f / (float)Math.PI;
            float latitude = (float)Math.Asin(-vec.y) * 180f / (float)Math.PI;

            x_coord = (int)((longitude / 360f + 0.5f) * width);
            y_coord = (int)((latitude / 180f + 0.5f) * height);
        }
    }
}
