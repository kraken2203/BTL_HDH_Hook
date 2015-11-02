using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Collections.Generic;

namespace DesktopWPFAppLowLevelKeyboardHook
{
    public class LowLevelKeyboardListener
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        public int flag = 0;
        public int flagedit = 0;

        private List<int> waitingchar = new List<int>();    //Hang doi chua cac ki tu

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
                chuyenChuCoDau();
                if (flag == 1)
                {
                    //Xoa ki tu hien thi tren man hinh 1 ki tu
                    flagedit = 1;
                    //Hien thi chu o^ ra man hinh
                    vkCode = waitingchar[0];
                    waitingchar.RemoveAt(0);
                    flag = 0;
                }
                else waitingchar.Add(vkCode);
                if (OnKeyPressed != null) { OnKeyPressed(this, new KeyPressedArgs(vkCode)); }
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
                }
                else
                {
                    waitingchar.RemoveAt(1);
                    waitingchar.RemoveAt(0);
                }
            }
        }
    }

    public class KeyPressedArgs : EventArgs
    {
        public char KeyPressed { get; private set; }

        public KeyPressedArgs(int key)
        {
            KeyPressed = (char)key;
        }
    }
}
