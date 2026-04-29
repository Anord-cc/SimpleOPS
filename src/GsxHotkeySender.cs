using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SimpleOps.GsxRamp
{
    internal sealed class GsxHotkeySender
    {
        private readonly GsxPaths _paths;

        public GsxHotkeySender(GsxPaths paths)
        {
            _paths = paths;
        }

        public void OpenMenu()
        {
            FocusSimulatorWindow();

            var binding = ReadHotkeyBinding();
            SendModifierDown(binding.CtrlKey, 0x11);
            SendModifierDown(binding.ShiftKey, 0x10);
            SendModifierDown(binding.AltKey, 0x12);
            TapKey((byte)binding.KeyCode);
            SendModifierUp(binding.AltKey, 0x12);
            SendModifierUp(binding.ShiftKey, 0x10);
            SendModifierUp(binding.CtrlKey, 0x11);
        }

        public void SelectChoice(int zeroBasedChoice)
        {
            FocusSimulatorWindow();
            byte vk = ChoiceToVirtualKey(zeroBasedChoice);
            TapKey(vk);
        }

        private void FocusSimulatorWindow()
        {
            Process simulator = null;
            foreach (var process in Process.GetProcesses())
            {
                if (string.Equals(process.ProcessName, "FlightSimulator2024", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(process.ProcessName, "FlightSimulator", StringComparison.OrdinalIgnoreCase))
                {
                    simulator = process;
                    break;
                }
            }

            if (simulator == null || simulator.MainWindowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("Microsoft Flight Simulator window was not found.");
            }

            ShowWindowAsync(simulator.MainWindowHandle, 9);
            SetForegroundWindow(simulator.MainWindowHandle);
            Thread.Sleep(120);
        }

        private HotkeyBinding ReadHotkeyBinding()
        {
            var text = File.ReadAllText(_paths.GsxHotkeyPath).Trim();
            int jsonStart = text.IndexOf('{');
            int jsonEnd = text.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                throw new InvalidOperationException("GSX hotkey.json format was not recognized.");
            }

            var json = text.Substring(jsonStart, jsonEnd - jsonStart + 1);
            bool ctrl = json.IndexOf(@"""ctrlKey"":true", StringComparison.OrdinalIgnoreCase) >= 0;
            bool shift = json.IndexOf(@"""shiftKey"":true", StringComparison.OrdinalIgnoreCase) >= 0;
            bool alt = json.IndexOf(@"""altKey"":true", StringComparison.OrdinalIgnoreCase) >= 0;

            const string marker = @"""keyCode"":";
            int keyIndex = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0)
            {
                throw new InvalidOperationException("GSX keyCode was not found in hotkey.json.");
            }

            keyIndex += marker.Length;
            int end = keyIndex;
            while (end < json.Length && char.IsDigit(json[end]))
            {
                end++;
            }

            var keyCodeText = json.Substring(keyIndex, end - keyIndex);
            int keyCode = int.Parse(keyCodeText, System.Globalization.CultureInfo.InvariantCulture);
            return new HotkeyBinding(keyCode, ctrl, shift, alt);
        }

        private static byte ChoiceToVirtualKey(int zeroBasedChoice)
        {
            if (zeroBasedChoice < 0)
            {
                throw new ArgumentOutOfRangeException("zeroBasedChoice");
            }

            if (zeroBasedChoice <= 8)
            {
                return (byte)('1' + zeroBasedChoice);
            }

            if (zeroBasedChoice == 9)
            {
                return (byte)'0';
            }

            if (zeroBasedChoice <= 14)
            {
                return (byte)('A' + (zeroBasedChoice - 10));
            }

            throw new InvalidOperationException("GSX menu choice is out of supported range: " + zeroBasedChoice);
        }

        private static void SendModifierDown(bool enabled, byte vk)
        {
            if (enabled) keybd_event(vk, 0, 0, UIntPtr.Zero);
        }

        private static void SendModifierUp(bool enabled, byte vk)
        {
            if (enabled) keybd_event(vk, 0, 0x0002, UIntPtr.Zero);
        }

        private static void TapKey(byte vk)
        {
            keybd_event(vk, 0, 0, UIntPtr.Zero);
            Thread.Sleep(40);
            keybd_event(vk, 0, 0x0002, UIntPtr.Zero);
            Thread.Sleep(120);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    }

    internal sealed class HotkeyBinding
    {
        public readonly int KeyCode;
        public readonly bool CtrlKey;
        public readonly bool ShiftKey;
        public readonly bool AltKey;

        public HotkeyBinding(int keyCode, bool ctrlKey, bool shiftKey, bool altKey)
        {
            KeyCode = keyCode;
            CtrlKey = ctrlKey;
            ShiftKey = shiftKey;
            AltKey = altKey;
        }
    }
}
