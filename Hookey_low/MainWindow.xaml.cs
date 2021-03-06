﻿using DesktopWPFAppLowLevelKeyboardHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//Class hien thi cac phim go tu ban phim
namespace Hookey_low
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LowLevelKeyboardListener _listener;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _listener = new LowLevelKeyboardListener();
            _listener.OnKeyPressed += _listener_OnKeyPressed;
            _listener.HookKeyboard();
        }

        private void _listener_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            if (_listener.flagedit == 1)
            {
                this.textBox_DisplayKeyboardInput.Text = this.textBox_DisplayKeyboardInput.Text.Substring(0, this.textBox_DisplayKeyboardInput.Text.Length - 2);
                this.textBox_DisplayKeyboardInput.Text += e.KeyPressed;
                _listener.flagedit = 0;
                Console.WriteLine("dasdasdsa");
            }
            else
                this.textBox_DisplayKeyboardInput.Text += e.KeyPressed;
            Console.WriteLine("done");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _listener.UnHookKeyboard();
        }
    }
}
