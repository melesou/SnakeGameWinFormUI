using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SnakeGameWinFormUI
{
    public partial class Form1 : Form
    {
        #region Dll
        public class KeyboardHook
        {
            private const int WH_KEYBOARD_LL = 13;
            private const int WM_KEYDOWN = 0x0100;

            private static LowLevelKeyboardProc _proc = HookCallback;
            private static IntPtr _hookID = IntPtr.Zero;

            public static void Start()
            {
                _hookID = SetHook(_proc);
            }

            public static void Stop()
            {
                UnhookWindowsHookEx(_hookID);
            }

            private static IntPtr SetHook(LowLevelKeyboardProc proc)
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

            private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    if ((Keys)vkCode == Keys.Up)
                        SnakeMoveDirection = new(0, -1);
                    else if ((Keys)vkCode == Keys.Left)
                        SnakeMoveDirection = new(-1, 0);
                    else if ((Keys)vkCode == Keys.Right)
                        SnakeMoveDirection = new(1, 0);
                    else if ((Keys)vkCode == Keys.Down)
                        SnakeMoveDirection = new(0, 1);
                }

                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);
        }
        #endregion

        //      
        //      
        // GAME 
        //      
        //    

        public Form1()
        {
            InitializeComponent();
            KeyboardHook.Start();
            GameStart();
        }

        private void GameStart()
        {
            StartRenderingField();
            SpawnApple();
            StartSnakeMoveLoop();

        }

        private async void StartSnakeMoveLoop()
        {
            while (true)
            {
                await Task.Delay(GameSpeed);
                SnakeMove(SnakeMoveDirection.X, SnakeMoveDirection.Y);
                this.Text = $"Змейка    |   Счёт: {Points}";
            }
        }

        private async void StartRenderingField()
        {
            dataGridView1.RowCount = 15;
            while (true)
            {
                await Task.Delay(10);
                RenderField();
            }
        }

        public void RenderField()
        {
            try
            {
                dataGridView1.ClearSelection();
                for (int y = 0; y < 15; y++)
                    for (int x = 0; x < 15; x++)
                    {
                        dataGridView1.Rows[y].Cells[x].Style.BackColor = Color.White;
                        dataGridView1.Rows[y].Cells[x].Value = "";
                        
                    }

                var count = 1;
                for (int i = Snake.Count - 1; i >= 0; i--)
                {
                    dataGridView1.Rows[Snake[i].Y].Cells[Snake[i].X].Style.BackColor = count++ % 2 == 0 ? Color.FromArgb(138, 231, 126) : Color.FromArgb(109, 215, 96);
                }

                var head = Snake.Last();
                dataGridView1.Rows[head.Y].Cells[head.X].Value = SnakeEyes;
                dataGridView1.Rows[head.Y].Cells[head.X].Style.ForeColor = Color.Black;

                if (AppleLocation != null)
                {
                    dataGridView1.Rows[AppleLocation.Y].Cells[AppleLocation.X].Style.BackColor = Color.FromArgb(210, 65, 65);
                    dataGridView1.Rows[AppleLocation.Y].Cells[AppleLocation.X].Value = "^";
                    dataGridView1.Rows[AppleLocation.Y].Cells[AppleLocation.X].Style.ForeColor = Color.FromArgb(104, 75, 64);
                }
            }
            catch
            {
                //
            }
        }
        private static void GameOver()
        {
            MessageBox.Show($"YOU LOSE\nВаш счёт:{Points}");
            GameRestart();
        }

        private static void Win()
        {
            MessageBox.Show("YOU WIN!!!");
            //todo добавить таблицу через firebase и топ участников
            GameRestart();
        }

        private static void GameRestart()
        {
            Points = 0;
            GameSpeed = StartGameSpeed;
            Snake = new() { new Position(0, 5), new Position(1, 5), new Position(2, 5), new Position(3, 5) };
            SnakeHeadPrevPosition = new(3, 5);
            SpawnApple();
            SnakeMoveDirection = new(1, 0);
        }


        //
        // GAME SCORE 🏁         
        // 
        //  AND SPEED >>>
        //

        public static int StartGameSpeed = 500; // ms
        public static int GameSpeed = 500;      // ms
        public static int MaxGameSpeed = 200;   // ms

        private static int _points = 0;
        public static int Points
        {
            get => _points;
            set
            {
                _points = value;
                if (GameSpeed > MaxGameSpeed)
                    GameSpeed -= 20;
            }
        }



        //
        //              ~≈~≈~
        // SNAKE     -~≈~   ~≈~≈~   ~≈~≈>:~
        //                      ~≈~≈~
        //

        public static List<Position> Snake = new() { new Position(0, 5), new Position(1, 5), new Position(2, 5), new Position(3, 5) };

        public static Position SnakeHeadPrevPosition = new(3, 5);

        public static string SnakeEyes = "     :";

        private static Position _snakeMoveDirection = new(1, 0);
        public static Position SnakeMoveDirection
        {
            get => _snakeMoveDirection;
            set
            {
                var head = Snake.Last();
                var X = SnakeHeadPrevPosition.X - head.X;
                var Y = SnakeHeadPrevPosition.Y - head.Y;
                if (X == value.X && Y == value.Y)
                    return;

                switch (value)
                {
                    case { X: 0, Y: -1 }:
                        SnakeEyes = @"' '";
                        break;
                    case { X: -1, Y: 0 }:
                        SnakeEyes = @":  ";
                        break;
                    case { X: 1, Y: 0 }:
                        SnakeEyes = @"     :";
                        break;
                    case { X: 0, Y: 1 }:
                        SnakeEyes = @". .";
                        break;
                }

                _snakeMoveDirection = value;
            }
        }

        public static void SnakeMove(int addX, int addY)
        {
            var currentHead = Snake.Last();
            SnakeHeadPrevPosition = currentHead;
            var newHead = new Position(currentHead.X + addX, currentHead.Y + addY);

            Snake.Add(newHead);

            //Подбор яблока
            if (AppleLocation?.X == newHead.X && AppleLocation?.Y == newHead.Y)
            {
                Points++;
                SpawnApple();
                return;
            }
            Snake.Remove(Snake.First());

            if (CheckSnakeMoveOnTail(newHead))
                GameOver();
            if (CheckSnakeMoveOnBorders(newHead))
                GameOver();
            if (Points >= 200)
                Win();
        }

        private static bool CheckSnakeMoveOnTail(Position newHead)
        {
            return Snake.Any(p => p.X == newHead.X && p.Y == newHead.Y && newHead != p);
        }

        public static bool CheckSnakeMoveOnBorders(Position newHead)
        {
            return newHead.X >= 15 || newHead.X < 0 || newHead.Y >= 15 || newHead.Y < 0;
        }



        //                 ,--./,-.
        //                / #      \
        //  APPLE        |          |
        //                \        /    
        //                 `._,._,'

        public static Position? AppleLocation;

        public static Random random = new();
        public static void SpawnApple()
        {
            var x = random.Next(0, 14);
            var y = random.Next(0, 14);
            if (Snake.Any(p => p.X == x && p.Y == y))
            {
                SpawnApple();
                return;
            }
            AppleLocation = new Position(x, y);
        }



        //
        //
        // кОсТыЛь
        //
        //

        private void dataGridView1_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            textBox1.Focus();
        }
    }
}