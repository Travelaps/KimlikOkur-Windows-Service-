using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.IO;
using WIA;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Drawing.Imaging;

namespace KimlikOkurService//SERKAN GND
{
    public class WsScanner : WebSocketBehavior   
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            //MessageBox.Show(e.Data);
            if (e.Data == "Scanner")
            {
                string s = Scanner();
                Sessions.Broadcast(s);
            }
            else
            {
                Sessions.Broadcast((char)6 + "");//Scanner dışında bir komut gelirse ACK gönder
            }
        }
        public string ImageConvertToBase64(string Path = null)
        {
             
            using (System.Drawing.Image image = Image.FromFile(Path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    image.Save(m, image.RawFormat);
                    byte[] imageBytes = m.ToArray();

                    string base64String = Convert.ToBase64String(imageBytes);
                    return base64String;
                }
            }
        }
        public string Scanner()
        {
            string Path = "";
           
            try
            {
                var deviceManager = new DeviceManager();

                DeviceInfo AvailableScanner = null;

                for (int i = 1; i <= deviceManager.DeviceInfos.Count; i++) // Loop Through the get List Of Devices.
                {
                    if (deviceManager.DeviceInfos[i].Type != WiaDeviceType.ScannerDeviceType) // Skip device If it is not a scanner
                    {
                        continue;
                    }

                    AvailableScanner = deviceManager.DeviceInfos[i];

                    break;
                }


                var device = AvailableScanner.Connect(); //Tarayıcıya bağlanıyor...
                /*NScanner.BalloonTipText = AvailableScanner + " Tarayıcı Bağlantısı Sağlandı";
                NScanner.Visible = true;
                NScanner.ShowBalloonTip(1000, NScanner.BalloonTipTitle, NScanner.BalloonTipText, ToolTipIcon.Info);*/



                var ScanerItem = device.Items[1]; // select the scanner.

                var imgFile = (ImageFile)ScanerItem.Transfer(FormatID.wiaFormatJPEG); //Tarayıcıdaki resmi jpg yap...
                Guid g = Guid.NewGuid();
                string __FileName = g.ToString();

                if (Directory.Exists(@"C:\KimlikOkurWebService"))
                {
                    Directory.CreateDirectory(@"C:\KimlikOkurWebService");
                }
                Path = @"C:\KimlikOkurWebService\" + __FileName + ".jpg";//@"C:\KimlikOkurWebService"; //@"E:\ScanImg.jpg"; // save the image in some path with filename.

              

                imgFile.SaveFile(Path);
                string img = VaryQualityLevel(Path);
                string s = ImageConvertToBase64(img);
                try
                {
                    if (File.Exists(Path))
                    {
                        File.Delete(Path);
                    }
                    if (File.Exists(img))
                    {
                        File.Delete(img);
                    }
                }
                catch (Exception)
                {

                   
                }
                

                return s;

                //pictureBox1.ImageLocation = Path;

            }
            catch (COMException ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;

        }
        private string VaryQualityLevel(string Path)
        {
            // Get a bitmap. The using statement ensures objects  
            // are automatically disposed from memory after use.  
            Image I = Image.FromFile(Path);
            using (Bitmap bmp1 = new Bitmap(I))
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                System.Drawing.Imaging.Encoder myEncoder =
                    System.Drawing.Imaging.Encoder.Quality;

                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                EncoderParameter myEncoderParameter;
                Guid g = Guid.NewGuid();
                string __FileName = g.ToString();
                string _Path = @"C:\KimlikOkurWebService\"+__FileName+"_1"+ ".jpg"; 

                myEncoderParameter = new EncoderParameter(myEncoder, 100L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                bmp1.Save(_Path, jpgEncoder, myEncoderParameters); 
                return _Path;
            }
        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
    public partial class ScannerService : ServiceBase
    {
        public string Path;
        public string EventLogName = "KimlikOkurServiceLog";
        public string LogName = "KimlikOkurLog";
        private int eventId = 1;
        System.Diagnostics.EventLog eventLog;
        public Socket listen;
        public NotifyIcon NScanner;
        public NotifyIcon NServer;
        public bool ServiceStart = false;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
        public ScannerService()
        {
            InitializeComponent();
            eventLog = new System.Diagnostics.EventLog();
            if (!System.Diagnostics.EventLog.SourceExists(EventLogName))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    EventLogName, LogName);
            }
            eventLog.Source = EventLogName;
            eventLog.Log = LogName;
        }

        protected override void OnStart(string[] args)
        {
            eventLog.WriteEntry("KimlikOkurService Start");
            KimlikOkurWebSocketServerStart();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000; // 60 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("KimlikOkurService durduruldu.");
        }
        public void OnTimer(object sender, ElapsedEventArgs args)
        {
           
            
            eventLog.WriteEntry("Servis durumu: Aktif.", EventLogEntryType.Information, eventId++);
           
        }
       
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };
        
      
        
        public void KimlikOkurWebSocketServerStart()
        {
            string ip = "127.0.0.1", port = "8019";
            string url = "ws://" + ip + ":" + port;//"ws://127.0.0.1:8019"
            try
            {
               
               
                WebSocketServer s = new WebSocketServer(url);
                s.AddWebSocketService<WsScanner>("/WsScanner");

                s.Start();
                eventLog.WriteEntry("WebSocket Server Başlatıldı. Port: " + port + " WsUrl : " + url, EventLogEntryType.Information, eventId++);
            }
            catch (Exception ex)
            {

                eventLog.WriteEntry("WebSocket server başlatılırken hata ile karşılaşıldı. Hata detayı: "+ex.Message+" WebSocket Url: "+url, EventLogEntryType.Information, eventId++);
            }
            
        }  


        static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }
        public byte[] SendMsg(string s)
        {
            byte[] _sbyte = Encoding.ASCII.GetBytes(s);
            byte[] send = new byte[_sbyte.Length + 2];
            send[0] = 0x81; // last frame, text
            send[1] = Convert.ToByte(_sbyte.Length); // not masked, length 3
            /*send[2] = 0x41;
            send[3] = 0x42;
            send[4] = 0x43;*/

            int k = 2;
            for (int i = 0; i <= _sbyte.Length - 1; i++)
            {
                send[k] = _sbyte[i];
                k++;
            }
            return send;
        }
 
        public byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }
    }
}
