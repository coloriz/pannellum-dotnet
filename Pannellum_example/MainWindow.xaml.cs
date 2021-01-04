using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using Microsoft.Win32;
using OpenTK;
using Pannellum;

namespace Pannellum_example
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int fps = 0;
        private DateTime lastMeasureTime = DateTime.Now;

        OpenCvSharp.VideoCapture video;
        OpenCvSharp.Mat currentFrame = new OpenCvSharp.Mat();
        private EquirectToRect renderer;

        public MainWindow()
        {
            InitializeComponent();

            string filename;
            var fileDlg = new OpenFileDialog();
            if (fileDlg.ShowDialog() == true)
            {
                filename = fileDlg.FileName;
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

            renderer = new EquirectToRect(new GLControl(), new System.Drawing.Size(1400, 900), 100 * (float)Math.PI / 180);
            renderer.Viewer.Paint += (sender, e) =>
            {
                renderer.Render(currentFrame);
                fps++;
            };
            renderer.Viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            viewer.Child = renderer.Viewer;

            DispatcherTimer renderingTimer = new DispatcherTimer();
            renderingTimer.Interval = TimeSpan.FromMilliseconds(/*1000 / video.Fps*/1);
            renderingTimer.Tick += (sender, e) =>
            {
                if (DateTime.Now.Subtract(lastMeasureTime) > TimeSpan.FromSeconds(1))
                {
                    Title = fps + " fps";
                    fps = 0;
                    lastMeasureTime = DateTime.Now;
                }

                video.Read(currentFrame);
                if (currentFrame.Empty())
                    return;
                renderer.Viewer.Invalidate();
            };
            renderingTimer.Start();
        }

        private void viewer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    renderer.Yaw -= 2 * (float)Math.PI / 180;
                    break;
                case Key.Right:
                    renderer.Yaw += 2 * (float)Math.PI / 180;
                    break;
                case Key.Up:
                    renderer.Pitch += 2 * (float)Math.PI / 180;
                    break;
                case Key.Down:
                    renderer.Pitch -= 2 * (float)Math.PI / 180;
                    break;
                case Key.PageUp:
                    renderer.Roll += 2 * (float)Math.PI / 180;
                    break;
                case Key.PageDown:
                    renderer.Roll -= 2 * (float)Math.PI / 180;
                    break;
            }
        }
    }
}
