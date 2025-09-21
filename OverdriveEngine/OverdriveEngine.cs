using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Input;
using System.Windows.Forms.Design.Behavior;

namespace OverdriveEngine
{
    public static class GameProperties
    {
        public static int framerate = 60;
        public static int windowWidth = 800;
        public static int windowHeight = 600;
        public static string name = "My Game!";


        public static GameWindow? window = null;  
    }

    public static class Helpers
    {
        public static Bitmap tempMap = new Bitmap((GameProperties.window ?? throw new Exception("Gamewindow is null")).Buffer);
        public static bool renderThisFrame = false;
    }

    public static class Time
    {
        public static Stopwatch clock = new Stopwatch();
        public static double time = 0;
        public static double deltaTime = 0f;

    }

    public static class WindowSetup
    {
        [STAThread]
        public static void Setup(int w, int h, string name, Bitmap map)
        {
            GameProperties.windowWidth = w;
            GameProperties.windowHeight = h;
            GameProperties.name = name;

            GameProperties.window = new GameWindow(map);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(GameProperties.window);

        }

        public static void Render(Bitmap map)
        {
            (GameProperties.window ?? throw new Exception("Gamewindow is null")).Buffer = map;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            GameProperties.window.Invalidate();
            Helpers.tempMap = new Bitmap((GameProperties.window ?? throw new Exception("Gamewindow is null")).Buffer);
        }
    }

    public static class GameLoop
    {
        private static bool isRunning = false;
        public static Action? Start;
        public static Action<double>? Update;
        public static List<ImplementableBehaviour> behaviours = new List<ImplementableBehaviour>();

        public static void RegisterStartAndUpdate(Action start, Action<double> update)
        {
            Start = start;
            Update = update;
        }

        public static void RegisterBehaviour(ImplementableBehaviour i)
        {
            behaviours.Add(i);
        }

        public static void StartGame()
        {
            foreach (ImplementableBehaviour behaviour in behaviours) {
                behaviour.Start();
            }
            isRunning = true;
            int framerate = GameProperties.framerate;
            int sleepTime = 1000 / framerate;
            int counter = 0;
            double previous = 0f;
            Time.clock.Start();
            while (isRunning)
            {
                Time.clock.Restart();
                Time.time += Time.deltaTime;
                foreach (ImplementableBehaviour beh in behaviours)
                {
                    beh.Update(Time.deltaTime);
                }
                if (Helpers.renderThisFrame)
                {
                    if ((GameProperties.window ?? throw new Exception("Gamewindow is null")).InvokeRequired == false)
                    {
                        WindowSetup.Render(Helpers.tempMap);
                    }
                    else
                    {
                        Action myDelegate = () => WindowSetup.Render(Helpers.tempMap);
                        (GameProperties.window ?? throw new Exception("Gamewindow is null")).BeginInvoke(myDelegate);
                    }
                }
                else
                {
                    Helpers.tempMap = new Bitmap((GameProperties.window ?? throw new Exception("Gamewindow is null")).Buffer);
                }
                Thread.Sleep(Math.Max(0, sleepTime - (int)(Time.deltaTime * 1000)));
                counter++;
                if (counter == framerate)
                {

                    counter = 0;

                }
                Time.deltaTime = Time.clock.Elapsed.TotalSeconds - previous;
                previous = Time.clock.Elapsed.TotalSeconds;

            }
        }

        public static void EndSession()
        {
            foreach (ImplementableBehaviour behaviour in behaviours)
            {
                behaviour.Stop();
            }
            isRunning = false;
            Time.clock.Stop();
            Time.clock.Reset();
            Time.time = 0;
            Time.deltaTime = 0f;
            (GameProperties.window ?? throw new Exception("Gamewindow is null")).Close();
            GameProperties.window.Dispose();
            Helpers.tempMap.Dispose();
            Application.Exit();
        }
    }

    public class GameWindow : Form
    {
        public Bitmap Buffer;
        public Dictionary<int, char> keysPressed = new Dictionary<int, char>();
        public Dictionary<int, Dictionary<string, object>> mousePressed = new Dictionary<int, Dictionary<string, object>>();

        public int currentParsed = 0;
        public int current = 0;
        public int mouseCurrentParsed = 0;
        public int mouseCurrent = 0;
        KeyPressEventHandler keyPressHandler;
        System.Windows.Forms.MouseEventHandler mouseHandler;

        public GameWindow(Bitmap map)
        {
            this.Text = GameProperties.name;
            this.Width = GameProperties.windowWidth;
            this.Height = GameProperties.windowHeight;
            this.Buffer = map;

            using (Graphics g = Graphics.FromImage(Buffer))
            {
                g.Clear(Color.White);
            }

            keyPressHandler = (sender, e) =>
            {
                if (e.KeyChar == (char)Keys.Escape)
                {
                    GameLoop.EndSession();
                }
                else
                {
                    keysPressed.Add(current, e.KeyChar);
                }
                current++;
            };

            mouseHandler = (sender, e) =>
            {
                Dictionary<string, object> mouseButtons = new Dictionary<string, object>
                {
                    { "X", e.X },
                    { "Y", e.Y },
                    { "Button", e.Button },
                    { "ClickCount", e.Clicks },
                    { "Delta", e.Delta },
                    { "Location", e.Location },
                    { "WheelDelta", e.Delta }
                };
                mousePressed.Add(mouseCurrent, mouseButtons);
                mouseCurrent++;
            };

            this.KeyPress += keyPressHandler;
            this.MouseClick += mouseHandler;
            this.MouseMove += mouseHandler;

        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.DrawImage(Buffer, 0, 0);
        }
    }

    public abstract class ImplementableBehaviour
    {
        public virtual void Start()
        {

        }

        public virtual void Update(double deltaTime)
        {

        }

        public virtual void Stop()
        {

        }
    }
}


