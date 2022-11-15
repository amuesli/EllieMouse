using mouseTest;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EllieMouse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TimeSpan _timeout = new TimeSpan(10000000);

        IntPtr hWnd;
        MouseHnd.HookProc _globalLlMouseHookCallback;
        IntPtr _hGlobalLlMouseHook;
        Win32.POINT _arrestPoint;
        bool _arrested = false;
        bool _dragging = false;
        DateTime _hold;

        MouseButtonState LeftButtonState;
        MouseButtonState RightButtonState;

        public MainWindow()
        {
            InitializeComponent();
            HideInterface();
            SetUpHook();
        }

        private void UpdateMouse()
        {
            Win32.POINT p = new Win32.POINT();
            if (Win32.GetCursorPos(out p))
            {
                this.Dispatcher.Invoke((Action)(() => {
                    WindowInteropHelper windowHwnd = new WindowInteropHelper(this);
                    hWnd = windowHwnd.Handle;
                    Win32.SetWindowPos(hWnd, 0, p.X - 75, p.Y - 75, 0, 0, Win32.SWP_NOSIZE);
                }));
            }
        }

        private void ArrestMouse()
        {
            _arrested = true;
            _arrestPoint = new Win32.POINT();
            Win32.GetCursorPos(out _arrestPoint);
            ShowInterface();
        }

        private void FreeMouse()
        {
            _arrested = false;
            MainMouse.IsEnabled = false;

            HideInterface();
        }

        private void DropMouse()
        {
            _dragging = false;
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            FreeMouse();
        }

        private void ShowInterface()
        {
            MainMouse.IsEnabled = true;
            btnDouble.Visibility = Visibility.Visible;
            btnTriple.Visibility = Visibility.Visible;
            btnRight.Visibility = Visibility.Visible;
            btnDrag.Visibility = Visibility.Visible;
            btnDouble.IsEnabled = true;
            btnTriple.IsEnabled = true;
            btnRight.IsEnabled = true;
            btnDrag.IsEnabled = true;
        }

        private void HideInterface()
        {
            btnDouble.Visibility = Visibility.Collapsed;
            btnTriple.Visibility = Visibility.Collapsed;
            btnRight.Visibility = Visibility.Collapsed;
            btnDrag.Visibility = Visibility.Collapsed;
        }

        private void SetUpHook()
        {
            // Create an instance of HookProc.
            _globalLlMouseHookCallback = LowLevelMouseProc;

            _hGlobalLlMouseHook = MouseHnd.NativeMethods.SetWindowsHookEx(
                MouseHnd.HookType.WH_MOUSE_LL,
                _globalLlMouseHookCallback,
                Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
                0);

            if (_hGlobalLlMouseHook == IntPtr.Zero)
            {
                //Logger.Fatal("Unable to set global mouse hook");
                throw new Win32Exception("Unable to set MouseHook");
            }
        }
        private void ClearHook()
        {
            //Logger.Debug("Deleting global mouse hook");

            if (_hGlobalLlMouseHook != IntPtr.Zero)
            {
                // Unhook the low-level mouse hook
                if (!MouseHnd.NativeMethods.UnhookWindowsHookEx(_hGlobalLlMouseHook))
                    throw new Win32Exception("Unable to clear MouseHoo;");

                _hGlobalLlMouseHook = IntPtr.Zero;
            }
        }

        public int LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            int retInt = MouseHnd.NativeMethods.CallNextHookEx(_hGlobalLlMouseHook, nCode, wParam, lParam);

            if (nCode >= 0 && !_arrested)
            {
                var wmMouse = (MouseHnd.MouseMessage)wParam;
                if (wmMouse == MouseHnd.MouseMessage.WM_MOUSEMOVE)
                {
                    UpdateMouse();
                }
                else
                {
                    if (_dragging)
                    {
                        DropMouse();
                        RightButtonState = MouseButtonState.Released;
                        return 1;
                    }
                    // Get the mouse WM from the wParam parameter
                    if (wmMouse == MouseHnd.MouseMessage.WM_LBUTTONDOWN && LeftButtonState == MouseButtonState.Released)
                    {
                        _hold = DateTime.Now;
                        LeftButtonState = MouseButtonState.Pressed;
                    }
                    if (wmMouse == MouseHnd.MouseMessage.WM_LBUTTONUP && LeftButtonState == MouseButtonState.Pressed)
                    {
                        if (DateTime.Now - _hold > _timeout)
                        {
                            LeftButtonState = MouseButtonState.Released;
                            ArrestMouse();
                            return 1;
                        }
                        else
                        {
                            LeftButtonState = MouseButtonState.Released;
                        }
                    }

                    if (wmMouse == MouseHnd.MouseMessage.WM_RBUTTONDOWN && RightButtonState == MouseButtonState.Released)
                    {
                        RightButtonState = MouseButtonState.Pressed;
                        retInt = 1;
                    }
                    if (wmMouse == MouseHnd.MouseMessage.WM_RBUTTONUP && RightButtonState == MouseButtonState.Pressed)
                    {
                        RightButtonState = MouseButtonState.Released;
                        ArrestMouse();
                        retInt = 1;
                    }
                }
            }

            // Pass the hook information to the next hook procedure in chain
            return retInt;
        }

        private void BtnDouble_Click(object sender, RoutedEventArgs e)
        {
            Win32.SetCursorPos(_arrestPoint.X, _arrestPoint.Y);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            FreeMouse();
        }

        private void BtnTriple_Click(object sender, RoutedEventArgs e)
        {
            Win32.SetCursorPos(_arrestPoint.X, _arrestPoint.Y);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            FreeMouse();
        }

        private void BtnRight_Click(object sender, RoutedEventArgs e)
        {
            Win32.SetCursorPos(_arrestPoint.X, _arrestPoint.Y);
            Win32.mouse_event((int)(Win32.MouseEventFlags.RIGHTDOWN), 0, 0, 0, 0);
            Win32.mouse_event((int)(Win32.MouseEventFlags.RIGHTUP), 0, 0, 0, 0);
            FreeMouse();
        }

        private void BtnDrag_Click(object sender, RoutedEventArgs e)
        {
            Win32.SetCursorPos(_arrestPoint.X, _arrestPoint.Y);
            _dragging = true;
            RightButtonState = MouseButtonState.Pressed;
            Win32.mouse_event((int)(Win32.MouseEventFlags.LEFTDOWN), 0, 0, 0, 0);
            FreeMouse();
        }
    }
}
