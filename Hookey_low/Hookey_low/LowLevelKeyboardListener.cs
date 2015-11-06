using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Collections.Generic;

namespace DesktopWPFAppLowLevelKeyboardHook
{
    public static class Shifts
    {
        static Shifts() { ShiftInt = 0; }
        public static int ShiftInt { get; private set; }
        public static void SetShiftInt(int newInt)
        {
            ShiftInt = newInt;
        }
    }
    public class LowLevelKeyboardListener
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        public int flag = 0;        //Cờ kiểm tra sửa dấu cho nguyên âm, oo -> ô
        public int flagedit = 0;    //Cờ kiểm tra xóa 1 chữ trên màn hình
        public int flag_nguyenam = 0;   //cờ kiểm tra khi có nguyên âm trong từ
        public int pos_nguyenam = -1;
        public int delete_char = 0;
        public int flagedit_word = 0;

        private List<int> waitingchar = new List<int>();    //Hang doi chua cac ki tu phuc vu cho chuyen nguyen am
        private List<int> waitingword = new List<int>();    //Hang doi chua cac ki tu phuc vu cho them dau cho word
        private Stack<int> stack_word = new Stack<int>();   //Stack chua ca ki tu da xoa trong 1 word

        //Import cac thu vien dll cho hook
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //API windows, ham cai dat hook
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        //API windows, go~ hook duoc cai dat trong hook chain
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        //API windows, goi hook tiep theo trong hook chain
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<KeyPressedArgs> OnKeyPressed;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        //Ham khoi tao LowLevelKeyboardListener
        public LowLevelKeyboardListener()
        {
            _proc = HookCallback;
        }

        //Cai dat hook procedure vao hook chain
        public void HookKeyboard()
        {
            _hookID = SetHook(_proc);
        }

        public void UnHookKeyboard()
        {
            UnhookWindowsHookEx(_hookID);
        }
        //Cai dat hook procedure cho ban phim vao hook chain
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        //Hook Procedure
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            
            //Kiem tra xem co thuc hien hay goi tiep ham CallNextHookEx
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                //Kiem tra xem shift co duoc an hay k 
                if (vkCode == 0xA0)
                    Shifts.SetShiftInt(1);
                waitingchar.Add(vkCode);
                waitingword.Add(vkCode);
                chuyenChuCoDau();
                themDau();
                //Kiểm tra khi ấn dấu cách
                if(vkCode == 0x20)
                {
                    waitingword.Clear();
                    stack_word.Clear();
                    pos_nguyenam = -1;
                    delete_char = 0;
                    flagedit_word = 0;
                }

                //Kiem tra việc sửa nguyên âm
                if (flag == 1)
                {
                    //Hien thi chu o^ ra man hinh
                    vkCode = waitingchar[0];
                    waitingchar.RemoveAt(0);
                    delete_char = 1;
                    flag = 0;
                    flagedit = 1;           //Xoa ki tu hien thi tren man hinh 1 ki tu  
                }

                //Kiểm tra sửa dấu cho từ
                if (OnKeyPressed != null && vkCode!=0xA0) { OnKeyPressed(this, new KeyPressedArgs(vkCode)); }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        //Ham xu ly 2 ki tu lien tiep trong hang doi
        public void chuyenChuCoDau()
        {
            if(waitingchar.Count > 1)
            {
                if ((waitingchar[0] == waitingchar[1]) && (waitingchar[0] == 0x4f))
                {
                    waitingchar.RemoveAt(1);
                    waitingchar[0] = 212; //Chữ ô
                    flag = 1;
                    
                    Console.WriteLine("1");
                }
                else
                {
                    waitingchar.RemoveAt(1);
                    waitingchar.RemoveAt(0);
                }
            }
        }

        //Ham xu ly them dau vao 1 tu
        public void themDau()
        {
            if (waitingword.Count != 0)
            {
                if ( waitingword[waitingword.Count - 1] == 'U')
                {
                    flag_nguyenam = 1;
                    pos_nguyenam = waitingword.Count - 1;
                }
                if (waitingword[waitingword.Count - 1] == 'S')
                {
                    if (flag_nguyenam == 1)
                    {
                        delete_char = waitingword.Count;
                        waitingword.RemoveAt(waitingword.Count - 1);
                        do
                        {
                            Console.WriteLine((char)waitingword[waitingword.Count - 1]);
                            stack_word.Push(waitingword[waitingword.Count - 1]);
                            waitingword.RemoveAt(waitingword.Count - 1);
                        } while ((waitingword.Count - 2) == pos_nguyenam);
                        waitingword[waitingword.Count - 1] = 'ú';  //chuyển thành dấu sắc
                        while (stack_word.Count != 0)
                        {
                            waitingword.Add(stack_word.Pop());
                        }
                        flagedit_word = 1;
                    }
                }
                if (waitingword[waitingword.Count - 1] == 'F')
                {
                    if (flag_nguyenam == 1)
                    {
                        delete_char = waitingword.Count;
                        waitingword.RemoveAt(waitingword.Count - 1);
                        do
                        {
                            Console.WriteLine((char)waitingword[waitingword.Count - 1]);
                            stack_word.Push(waitingword[waitingword.Count - 1]);
                            waitingword.RemoveAt(waitingword.Count - 1);
                        } while ((waitingword.Count - 2) == pos_nguyenam);
                        waitingword[waitingword.Count - 1] = 'ù';  //chuyển thành dấu huyền
                        while (stack_word.Count != 0)
                        {
                            waitingword.Add(stack_word.Pop());
                        }
                        flagedit_word = 1;
                    }
                }
                if (waitingword[waitingword.Count - 1] == 'J')
                {
                    if (flag_nguyenam == 1)
                    {
                        delete_char = waitingword.Count;
                        waitingword.RemoveAt(waitingword.Count - 1);
                        do
                        {
                            Console.WriteLine((char)waitingword[waitingword.Count - 1]);
                            stack_word.Push(waitingword[waitingword.Count - 1]);
                            waitingword.RemoveAt(waitingword.Count - 1);
                        } while ((waitingword.Count - 2) == pos_nguyenam);
                        waitingword[waitingword.Count - 1] = 'ụ';  //chuyển thành dấu nặng
                        while (stack_word.Count != 0)
                        {
                            waitingword.Add(stack_word.Pop());
                        }
                        flagedit_word = 1;
                    }
                }
                if (waitingword[waitingword.Count - 1] == 'X')
                {
                    if (flag_nguyenam == 1)
                    {
                        delete_char = waitingword.Count;
                        waitingword.RemoveAt(waitingword.Count - 1);
                        do
                        {
                            Console.WriteLine((char)waitingword[waitingword.Count - 1]);
                            stack_word.Push(waitingword[waitingword.Count - 1]);
                            waitingword.RemoveAt(waitingword.Count - 1);
                        } while ((waitingword.Count - 2) == pos_nguyenam);
                        waitingword[waitingword.Count - 1] = 'ũ';  //chuyển thành dấu ~
                        while (stack_word.Count != 0)
                        {
                            waitingword.Add(stack_word.Pop());
                        }
                        flagedit_word = 1;
                    }
                }
                if (waitingword[waitingword.Count - 1] == 'R')
                {
                    if (flag_nguyenam == 1)
                    {
                        delete_char = waitingword.Count;
                        waitingword.RemoveAt(waitingword.Count - 1);
                        do
                        {
                            Console.WriteLine((char)waitingword[waitingword.Count - 1]);
                            stack_word.Push(waitingword[waitingword.Count - 1]);
                            waitingword.RemoveAt(waitingword.Count - 1);
                        } while ((waitingword.Count - 2) == pos_nguyenam);
                        waitingword[waitingword.Count - 1] = 'ủ';  //chuyển thành dấu ?
                        while (stack_word.Count != 0)
                        {
                            waitingword.Add(stack_word.Pop());
                        }
                        flagedit_word = 1;
                    }
                }
            }
            Console.WriteLine(pos_nguyenam);
            if (flagedit_word == 1)
            {
                for (int i = 0; i < waitingword.Count; i++)
                {
                    if (i != pos_nguyenam)
                        waitingword[i] = waitingword[i] + 32;
                }
            }
        }

        public char get_word()
        {
            char tmp = '*';
            if (waitingword.Count != 0)
            {
                tmp = (char)waitingword[0];
                waitingword.RemoveAt(0);
            }
            return tmp;
        }

        public int sizeOfWaitingWord()
        {
            return waitingword.Count;
        }
    }

    public class KeyPressedArgs : EventArgs
    {
        public char KeyPressed { get; private set; }

        public KeyPressedArgs(int x)
        {
            if (Shifts.ShiftInt == 0)
                if (x == 32)
                    KeyPressed = (char)x;
                else
                    KeyPressed = (char)(x + 32);
            else
                KeyPressed = (char)x;
            Shifts.SetShiftInt(0);
        }
    }
}
