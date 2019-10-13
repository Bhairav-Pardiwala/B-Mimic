using HelixToolkit.Wpf;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _3dAppWPF1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string MODEL_PATH = "<insert full path to your stl model.stl>";
        static ModelVisual3D device3D = new ModelVisual3D();
        static EventHubClient eventHubClient;
       static Matrix3D origvalue;
        public MainWindow()
        {
            InitializeComponent();

             Init();
            device3D.Content = Display3d(MODEL_PATH);
            viewPort3d.Children.Add(device3D);
            origvalue = device3D.Transform.Value;
            
          
        }

        

        private static void Rotate(int x,int y,int z,int angle)
        {
            var matrix = device3D.Transform.Value;
            var center = new Point3D(x==1?0.5:x ,y==1?.5:y, z==1?.5:z);
            matrix.RotateAt(new Quaternion(new Vector3D(x, y, z), angle),center);
           // matrix.Rotate(new Quaternion(new Vector3D(x, y, z), angle));
            device3D.Transform = new MatrixTransform3D(matrix);
        }
        private static void Reset()
        {
            var matrix =origvalue ;
          

            device3D.Transform = new MatrixTransform3D(matrix);
        }
        private static async System.Threading.Tasks.Task Init()
        {
            eventHubClient = EventHubClient.CreateFromConnectionString("<azure iot hub conectection string>", "messages/events");

            var partitions = eventHubClient.GetRuntimeInformation().PartitionIds;

            foreach (string partition in partitions)
            {
                AttachHandlerForMessages(partition);
            }


        }
        private async static Task AttachHandlerForMessages(string partition)
        {
            var eventHubReceiver =
            eventHubClient.GetDefaultConsumerGroup().
            CreateReceiver(partition, DateTime.Now);
            while (true)
            {
                EventData eventData = await eventHubReceiver.ReceiveAsync();
                if (eventData == null) continue;

                string data = Encoding.UTF8.GetString(eventData.GetBytes());
               

                ExtractData(data);
            }
        }
        private static void ExtractData(string data)
        {
            data = data.Replace("{{", "{");
            data = data.Replace("}}", "}");
            var sensorEvent = JsonConvert.DeserializeObject<Rootobject>(data);
            Console.WriteLine(sensorEvent.gX+","+sensorEvent.gY+","+sensorEvent.gX);
            var axis = new Vector3D(sensorEvent.gX,sensorEvent.gX , sensorEvent.gZ);
            Reset();
            Rotate(1, 0, 0, 360);
            Rotate(0, 1, 0, -360);
            Rotate(1, 0, 0, AngleFromValue(sensorEvent.gX));
            Rotate(0, 1, 0, AngleFromValue(sensorEvent.gY));
            //Rotate(0, 0, 1, AngleFromValue(sensorEvent.gZ));
            //find out percentage to degree

        }
        private static int AngleFromValue(double val)
        {
            double perc = (val / 1000) * 100;
            int angle = Convert.ToInt16((perc * 90f) / 100f);
            return angle;
        }
        private Model3D Display3d(string model)
        {
            Model3D device = null;
            try
            {
                //Adding a gesture here
                viewPort3d.RotateGesture = new MouseGesture(MouseAction.LeftClick);

                //Import 3D model file
                ModelImporter import = new ModelImporter();

                //Load the 3D model file
                device = import.Load(model);
            }
            catch (Exception e)
            {
                // Handle exception in case can not find the 3D model file
                MessageBox.Show("Exception Error : " + e.StackTrace);
            }
            return device;
        }

    }

    public class Rootobject
    {
        public double gX { get; set; }
        public double gY { get; set; }
        public double gZ { get; set; }
        public string pressure { get; set; }
        public string aX { get; set; }
        public string aY { get; set; }
        public string aZ { get; set; }
    }


}
