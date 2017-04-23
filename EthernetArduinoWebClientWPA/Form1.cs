using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using XInputDotNetPure;
using System.Net.Sockets;

namespace EthernetArduinoWebClientWPA
{
    public partial class RCSubmarine : Form
    {


        //Zmienne dzielone miedzy kilka wątków
        float sharedLeftTop = 0;
        float sharedLeftStickX = 0;
        float sharedLeftStickY = 0;
        float sharedRightTop = 0;
        float sharedRightStickX = 0;
        float sharedRightStickY = 0;
        float sharedRightBottom = 0;
        float sharedLeftBottom = 0;

        int sharedA = 0;
        int sharedB = 0;
        int sharedX = 0;
        int sharedY = 0;

        int dPadTop = 0;
        int dPadBottom = 0;
        int dPadLeft = 0;
        int dPadRight = 0;

        //Zmienne do zapobiegania wylaczaniu sie i wlaczaniu funkcji,
        //jesli przycisk kontrolera jest przytrzymany
        int sampleCount = 0;
        int previousSampleCount = 0;

        string[] velocityArray = new string[] { "0", "0", "0", "0" };

        private Timer timer;
        Webcam webcam;
        private readonly float ro = 1.0064f;
        private readonly float g = 9.8145f;

        public float atmospherePressure { get; private set; }

        public RCSubmarine()
        {
            InitializeComponent();
            webcam = new Webcam();
            webcam.InitializeWebCam(ref webcamHandler);
            SetTimer();

            //Wątek dla aktualizacji stanu kontrolera XBOX 360
            System.Threading.Thread updateController = new System.Threading.Thread(new System.Threading.ThreadStart(UpdateState));
            updateController.IsBackground = true;
            updateController.Start();

            //Wątek dla sygnałów sterujących z kontrolera XBOX 360
            System.Threading.Thread sendData = new System.Threading.Thread(new System.Threading.ThreadStart(SendData));
            sendData.Start();


            //GetData3 umozliwia odbieranie danych po protokole UDP ale nie dziala to zbyt dobrze
            //System.Threading.Thread getData = new System.Threading.Thread(new System.Threading.ThreadStart(GetData3));
            //getData.Start(); 

        }
        //private void RCSubmarine_Load(object sender, EventArgs e)
        //{

        //}
        #region cameraHandling
        private void webCamStartButton_Click(object sender, EventArgs e)
        {
            webcam.Start();
        }

        private void webCamStopButton_Click(object sender, EventArgs e)
        {
            webcam.Stop();
        }
        private void settingsButton_Click(object sender, EventArgs e)
        {
            webcam.AdvanceSetting();
        }
        private void resolutionSettingsButton_Click_1(object sender, EventArgs e)
        {
            webcam.ResolutionSetting();
        }
        //private void captureButton_Click(object sender, EventArgs e)
        //{
        //    Helper.SaveImageCapture(webcamCaptured);
        //}
        #endregion
        #region Xbox360 controller handling
        //Delegaci są wymagani, żeby wątek działający w tle mógł aktualizować wątek GUI (czyli główny wątek).
        private delegate void sharedLeftTopDelegate(float s);
        private delegate void sharedRightTopDelegate(float s);

        private delegate void sharedLeftStickXDelegate(float s);
        private delegate void sharedLeftStickYlegate(float s);

        private delegate void sharedRightStickXDelegate(float s);
        private delegate void sharedRightStickYDelegate(float s);

        private delegate void sharedRightBottomDelegate(float s);
        private delegate void sharedLeftBottomDelegate(float s);

        private delegate void sharedAdelegate(int s);
        private delegate void sharedBdelegate(int s);
        private delegate void sharedXdelegate(int s);
        private delegate void sharedYdelegate(int s);

        private delegate void dPadTopDelegate(int s);
        private delegate void dPadBottomDelegate(int s);
        private delegate void dPadLeftDelegate(int s);
        private delegate void dPadRightDelegate(int s);

        delegate void SetTextCallback(string text);

        private void UpdateState()
        {
            while (true)
            {
                GamePadState state = GamePad.GetState(PlayerIndex.One);

                //Czyta wartosci analogowe i zapisuje je do zmiennych wspoldzielonych
                sharedLeftStickX = state.ThumbSticks.Left.X;
                leftStickX.Invoke(new sharedLeftStickXDelegate(displayLeftTrigX), sharedLeftStickX);
                sharedLeftStickY = state.ThumbSticks.Left.Y;
                rightStickX.Invoke(new sharedRightStickXDelegate(displayLeftTrigY), sharedLeftStickY);
                sharedRightStickX = state.ThumbSticks.Right.X;
                rightStickX.Invoke(new sharedRightStickXDelegate(displayRightTrigX), sharedRightStickX);
                sharedRightStickY = state.ThumbSticks.Right.Y;
                rightStickY.Invoke(new sharedRightStickYDelegate(displayRightTrigY), sharedRightStickY);

                sharedA = (int)state.Buttons.A;
                aButton.Invoke(new sharedAdelegate(displayButtonA), sharedA);
                sharedB = (int)state.Buttons.B;
                bButton.Invoke(new sharedBdelegate(displayButtonB), sharedB);
                sharedX = (int)state.Buttons.X;
                xButton.Invoke(new sharedXdelegate(displayButtonX), sharedX);
                sharedY = (int)state.Buttons.Y;
                yButton.Invoke(new sharedYdelegate(displayButtonY), sharedY);

                sharedLeftTop = (float)state.Buttons.LeftShoulder;
                leftTop.Invoke(new sharedLeftTopDelegate(displayLeftTop), sharedLeftTop);
                sharedLeftBottom = (float)state.Triggers.Left;
                leftBottomTextBox.Invoke(new sharedLeftBottomDelegate(displayLeftBottom), sharedLeftBottom);

                sharedRightTop = (float)state.Buttons.RightShoulder;
                rightTop.Invoke(new sharedRightTopDelegate(displayRightTop), sharedRightTop);
                sharedRightBottom = (float)state.Triggers.Right;
                rightBottomTextBox.Invoke(new sharedRightBottomDelegate(displayRightBottom), sharedRightBottom);

            }
        }

        private void displayRightBottom(float s)
        {
            rightBottomTextBox.Text = String.Format("{0:0.000}", s);
            if (s == 0)
                rightBottomTextBox.BackColor = Color.LightBlue;
            else
                rightBottomTextBox.BackColor = Color.Orange;
        }

        private void displayRightTop(float s)
        {
            if (s == 0)
                rightTop.BackColor = Color.Orange;
            else
                rightTop.BackColor = Color.LightBlue;
        }

        private void displayLeftBottom(float s)
        {
            leftBottomTextBox.Text = String.Format("{0:0.000}", s);
            if (s == 0)
                leftBottomTextBox.BackColor = Color.LightBlue;
            else
                leftBottomTextBox.BackColor = Color.Orange;
        }

        private void displayLeftTop(float s)
        {
            if (s == 0)
                leftTop.BackColor = Color.Orange;
            else
                leftTop.BackColor = Color.LightBlue;
        }

        private void displayButtonY(int s)
        {
            if (s == 0)
                yButton.BackColor = Color.Orange;
            else
                yButton.BackColor = Color.LightBlue;
        }

        private void displayButtonX(int s)
        {
            if (s == 0)
                xButton.BackColor = Color.Orange;
            else
                xButton.BackColor = Color.LightBlue;
        }

        private void displayButtonB(int s)
        {
            if (s == 0)
                bButton.BackColor = Color.Orange;
            else
                bButton.BackColor = Color.LightBlue;
        }

        private void displayButtonA(int s)
        {
            if (s == 0)
                aButton.BackColor = Color.Orange;
            else
                aButton.BackColor = Color.LightBlue;
        }

        private void displayRightTrigY(float s)
        {
            rightStickY.Text = String.Format("{0:0.000}", s);

            if (s == 0)
            {
                rightStickY.BackColor = Color.LightBlue;
            }
            else
                rightStickY.BackColor = Color.Orange;
        }

        private void displayRightTrigX(float s)
        {
            rightStickX.Text = String.Format("{0:0.000}", s);

            if (s == 0)
            {
                rightStickX.BackColor = Color.LightBlue;
            }
            else
                rightStickX.BackColor = Color.Orange;
        }

        private void displayLeftTrigY(float s)
        {
            leftStickY.Text = String.Format("{0:0.000}", s);

            if (s == 0)
            {
                leftStickY.BackColor = Color.LightBlue;
            }
            else
                leftStickY.BackColor = Color.Orange;
        }

        private void displayLeftTrigX(float s)
        {
            leftStickX.Text = String.Format("{0:0.000}", s);

            if (s == 0)
            {
                leftStickX.BackColor = Color.LightBlue;
            }
            else
                leftStickX.BackColor = Color.Orange;
        }
        #endregion
        #region Sending and receiving data
        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.

            string[] words = text.Split('a');
            int i = 0;
            foreach (string w in words)
            {
                velocityArray[i] = w;
                i++;
            }
            i = 0;

            if (this.m1SpeedTextBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { velocityArray[0].ToString() });
            }
            else
            {
                this.m1SpeedTextBox.Text = velocityArray[0].ToString();
            }

            if (this.m2SpeedTextBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { velocityArray[1].ToString() });
            }
            else
                this.m2SpeedTextBox.Text = velocityArray[1].ToString();
            if (this.stepsLabel.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { velocityArray[2].ToString() });
            }
            else
            {
                this.stepsLabel.Text = velocityArray[2].ToString();
            }
            if (this.deepthTextBox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { velocityArray[3].ToString() });
            }
            else
            {
                this.deepthTextBox.Text = velocityArray[3].ToString();
            }
        }
        //Pobiera informacje o aktualnych wartosciach predkosci silnika,
        //mozliwosc dodania odczytu innych wartosci z sensorow.
        private void GetData()
        {
            string url = "http://192.168.1.177";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            WebResponse resp = req.GetResponse();
            Stream stream = resp.GetResponseStream();
            using (StreamReader sr = new StreamReader(stream))
            {
                int i = 0;
                string tmp;
                while ((tmp = sr.ReadLine()) != null)
                {
                    string resultString = Regex.Match(tmp, @"\d+").Value;

                    if (i == 0)
                    {
                        m1SpeedTextBox.Text = resultString;
                    }
                    if (i == 1)
                    {
                        m2SpeedTextBox.Text = resultString;
                    }
                    if (i == 2)
                    {
                        stepsLabel.Text = resultString;
                    }
                    if(i == 3)
                    {
                        var pressure = ((float.Parse(resultString) - 102.0) * 8.0 / 820.0);
                        var deepth = (pressure - atmospherePressure) / (ro * g);
                        deepthTextBox.Text = deepth.ToString("0.00");

                        i = 0;
                    }
                    tmp = null;
                    i++;
                }
            }
            resp.Close();

        }
        public void SetTimer()
        {
            timer = new Timer();
            timer.Tick += new EventHandler(TimerTick);
            timer.Interval = 500;
            timer.Start();
        }
        private void TimerTick(object sender, EventArgs e)
        {
            GetData();
        }
        private int CalculatePWMForStick(float stick)
        {

            float stickValue = 0;
            stickValue = stick * 255;
            return Math.Abs((int)stickValue);
        }
        private string StepperMotorControl()
        {
            if (sharedRightStickY > 0)
            {
                return "up";
            }
            if (sharedRightStickY < 0)
                return "down";
            else
                return "0";
        }
        private string ActiveStick()
        {
            if (sharedLeftStickY > 0 && sharedLeftStickX == 0) // tylko przod
            {
                if (sharedRightStickY != 0)
                    return "1" + CalculatePWMForStick(sharedLeftStickY).ToString() + StepperMotorControl(); //przod razem ze sterem
                else
                    return "1" + CalculatePWMForStick(sharedLeftStickY).ToString();

            }
            else if (sharedLeftStickY == 0 && sharedLeftStickX > 0) // tylko prawo
            {
                return "2" + CalculatePWMForStick(sharedLeftStickX).ToString();
            }
            else if (sharedLeftStickY < 0 && sharedLeftStickX == 0) // tylko tyl
            {
                return "3" + CalculatePWMForStick(sharedLeftStickY).ToString();
            }
            else if (sharedLeftStickY == 0 && sharedLeftStickX < 0) // tylko lewo
            {
                return "4" + CalculatePWMForStick(sharedLeftStickX).ToString();
            }
            else if (sharedLeftStickY > 0 && sharedLeftStickX > 0) // przod prawo
            {
                return "5" + CalculatePWMForStick(sharedLeftStickY).ToString() + " " + CalculatePWMForStick(sharedLeftStickX).ToString();

            }
            else if (sharedLeftStickY > 0 && sharedLeftStickX < 0) // przod lewo
            {
                return "6" + CalculatePWMForStick(sharedLeftStickY).ToString() + " " + CalculatePWMForStick(sharedLeftStickX).ToString();

            }
            else if (sharedLeftStickY < 0 && sharedLeftStickX > 0) // tył prawo
            {
                return "7" + CalculatePWMForStick(sharedLeftStickY).ToString() + " " + CalculatePWMForStick(sharedLeftStickX).ToString();

            }
            else if (sharedLeftStickY < 0 && sharedLeftStickX < 0) // tył lewo
            {
                return "8" + CalculatePWMForStick(sharedLeftStickY).ToString() + " " + CalculatePWMForStick(sharedLeftStickX).ToString();

            }

            //else if (sharedRightStickY != 0)
            //    return StepperMotorControl();
            else
                return "0";
        }

        private void SendData()
        {
            //Port and IP Data for Socket Client
            var IP = IPAddress.Parse("192.168.1.177");

            int port = 8888;

            string dataToSend = "1";

            while (dataToSend != string.Empty)
            {
                var udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                var sendEndPoint = new IPEndPoint(IP, port);

                dataToSend = string.Empty;
                dataToSend = ActiveStick();

                if (dataToSend == string.Empty) break;
                try
                {
                    // Sends a message to the host to which you have connected.
                    Byte[] sendBytes = Encoding.ASCII.GetBytes(dataToSend);
                    udpClient.SendTo(sendBytes, sendEndPoint);
                    udpClient.Close();

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
            System.Threading.Thread.Sleep(10);
        }
        private void GetData3()
        {
            var IP = IPAddress.Parse("192.168.1.177");

            int port = 8888;
            string returnData = "1";

            while (returnData != string.Empty)
            {
                UdpClient udpServer = new UdpClient(port);

                var remoteEP = new IPEndPoint(IPAddress.Any, port);
                Byte[] data = udpServer.Receive(ref remoteEP);

                try
                {
                    returnData = Encoding.ASCII.GetString(data);
                    udpServer.Close();
                    SetText(returnData.ToString());
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }


            }
            System.Threading.Thread.Sleep(10);
        }
        private void GetData2()
        {
            var IP = IPAddress.Parse("192.168.1.177");

            int port = 8888;

            var clientReturn = new UdpClient(port);

            var receiveEndPoint = new IPEndPoint(IP, port);

            Byte[] receiveBytes;

            string returnData = string.Empty;

            receiveBytes = clientReturn.Receive(ref receiveEndPoint);
            returnData = Encoding.ASCII.GetString(receiveBytes);
            m1SpeedTextBox.Text = returnData;

            clientReturn.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void atmospherePressureButton_Click(object sender, EventArgs e)
        {
            atmospherePressure = float.Parse(atmospherePressureTextBox.Text);
        }
    }
}
#endregion