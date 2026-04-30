using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleOps.GsxRamp
{
    internal sealed class GsxMenuDriver : IGsxMenuController, IDisposable
    {
        private readonly GsxPaths _paths;
        private readonly IntPtr _windowHandle;
        private readonly int _messageId;
        private readonly Action<string> _log;
        private readonly object _sync = new object();

        private SimConnect _simconnect;
        private bool _definitionsRegistered;
        private bool _simConnected;
        private bool _couatlStarted;
        private DateTime _nextConnectAttemptUtc = DateTime.MinValue;

        public GsxMenuDriver(GsxPaths paths, IntPtr windowHandle, int messageId, Action<string> log)
        {
            _paths = paths;
            _windowHandle = windowHandle;
            _messageId = messageId;
            _log = log ?? delegate { };
            StatusText = "GSX Remote Control starting.";
            TryConnect();
        }

        public string StatusText { get; private set; }

        public bool ProcessWindowMessage(ref Message m)
        {
            if (m.Msg != _messageId)
            {
                return false;
            }

            try
            {
                _simconnect?.ReceiveMessage();
            }
            catch (Exception ex)
            {
                StatusText = "GSX Remote receive warning: " + ex.Message;
                _log(StatusText);
                CloseConnection();
            }

            return true;
        }

        public string GetTooltip()
        {
            if (!File.Exists(_paths.GsxTooltipPath))
            {
                return null;
            }

            try
            {
                return string.Join(" ", File.ReadAllLines(_paths.GsxTooltipPath)).Trim();
            }
            catch
            {
                return null;
            }
        }

        public IList<string> GetMenuLines()
        {
            if (!File.Exists(_paths.GsxMenuPath))
            {
                return new List<string>();
            }

            try
            {
                return File.ReadAllLines(_paths.GsxMenuPath)
                    .Select(line => (line ?? string.Empty).Trim())
                    .Where(line => line.Length > 0)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public MenuSelectionResult OpenAndSelect(string reason, params string[] patterns)
        {
            lock (_sync)
            {
                TryConnect();
                if (!_simConnected)
                {
                    StatusText = "GSX Remote not connected to SimConnect yet.";
                    return MenuSelectionResult.NotDetected("GSX Remote Control is not connected to SimConnect yet.");
                }

                if (!_couatlStarted)
                {
                    StatusText = "GSX/Couatl is not ready yet.";
                    return MenuSelectionResult.NotDetected("GSX/Couatl is not ready yet.");
                }

                EnableRemoteControl();
                var openedAtUtc = DateTime.UtcNow;
                SetNumber(GsxDefinition.MenuOpen, 1d);
                var menuLines = WaitForMenuUpdate(openedAtUtc, 8000);
                if (menuLines == null)
                {
                    StatusText = "GSX menu was not detected for " + reason + ".";
                    return MenuSelectionResult.NotDetected("GSX menu was not detected for " + reason + ".");
                }

                return SelectAndSend(menuLines, patterns);
            }
        }

        public MenuSelectionResult TrySelectExisting(params string[] patterns)
        {
            lock (_sync)
            {
                if (!_simConnected || !_couatlStarted)
                {
                    StatusText = "GSX Remote Control is not ready.";
                    return MenuSelectionResult.NotDetected("GSX Remote Control is not ready.");
                }

                return SelectAndSend(GetMenuLines(), patterns);
            }
        }

        public void Dispose()
        {
            CloseConnection();
        }

        private void TryConnect()
        {
            if (_simconnect != null || _windowHandle == IntPtr.Zero || DateTime.UtcNow < _nextConnectAttemptUtc)
            {
                return;
            }

            try
            {
                _simconnect = new SimConnect("SimpleOps GSX Ramp", _windowHandle, (uint)_messageId, null, 0);
                _simconnect.OnRecvOpen += OnRecvOpen;
                _simconnect.OnRecvQuit += OnRecvQuit;
                _simconnect.OnRecvException += OnRecvException;
                _simconnect.OnRecvSimobjectData += OnRecvSimobjectData;
                StatusText = "GSX Remote waiting for SimConnect.";
                _log("GSX Remote SimConnect object created.");
            }
            catch (COMException ex)
            {
                _simconnect = null;
                _simConnected = false;
                _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(2);
                StatusText = "GSX Remote waiting for SimConnect: " + ex.Message;
                _log(StatusText);
            }
            catch (Exception ex)
            {
                _simconnect = null;
                _simConnected = false;
                _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(5);
                StatusText = "GSX Remote startup warning: " + ex.Message;
                _log(StatusText);
            }
        }

        private void OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            _simConnected = true;
            _nextConnectAttemptUtc = DateTime.MinValue;
            StatusText = "GSX Remote connected to MSFS.";
            _log("GSX Remote connected to MSFS.");
            EnsureDefinitionsRegistered();
            EnableRemoteControl();
            RequestState();
        }

        private void OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            StatusText = "GSX Remote disconnected from MSFS.";
            _log("GSX Remote SimConnect quit received.");
            CloseConnection();
        }

        private void OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            string message = "GSX Remote SimConnect exception: " + data.dwException;
            if (data.dwIndex > 0)
            {
                message += " index=" + data.dwIndex;
            }

            if (data.dwSendID > 0)
            {
                message += " sendId=" + data.dwSendID;
            }

            StatusText = message;
            _log(message);
        }

        private void OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            var value = (SingleValueData)data.dwData[0];
            if ((GsxRequest)data.dwRequestID == GsxRequest.CouatlStarted)
            {
                _couatlStarted = value.value != 0d;
                StatusText = _couatlStarted ? "GSX Remote ready." : "GSX Remote waiting for Couatl.";
            }
            else if ((GsxRequest)data.dwRequestID == GsxRequest.RemoteControl)
            {
                if (_simConnected && value.value == 0d)
                {
                    EnableRemoteControl();
                }
            }
        }

        private void EnsureDefinitionsRegistered()
        {
            if (_definitionsRegistered || _simconnect == null)
            {
                return;
            }

            RegisterDefinition(GsxDefinition.CouatlStarted, "L:FSDT_GSX_COUATL_STARTED");
            RegisterDefinition(GsxDefinition.MenuOpen, "L:FSDT_GSX_MENU_OPEN");
            RegisterDefinition(GsxDefinition.MenuChoice, "L:FSDT_GSX_MENU_CHOICE");
            RegisterDefinition(GsxDefinition.RemoteControl, "L:FSDT_GSX_SET_REMOTECONTROL");
            _definitionsRegistered = true;
        }

        private void RegisterDefinition(GsxDefinition definition, string simVar)
        {
            _simconnect.AddToDataDefinition(definition, simVar, "number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
            _simconnect.RegisterDataDefineStruct<SingleValueData>(definition);
        }

        private void EnableRemoteControl()
        {
            SetNumber(GsxDefinition.RemoteControl, 1d);
        }

        private void RequestState()
        {
            _simconnect.RequestDataOnSimObject(GsxRequest.CouatlStarted, GsxDefinition.CouatlStarted, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            _simconnect.RequestDataOnSimObject(GsxRequest.RemoteControl, GsxDefinition.RemoteControl, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        }

        private void SetNumber(GsxDefinition definition, double value)
        {
            if (_simconnect == null)
            {
                throw new InvalidOperationException("GSX Remote SimConnect is not connected.");
            }

            var data = new SingleValueData { value = value };
            _simconnect.SetDataOnSimObject(definition, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, data);
        }

        private MenuSelectionResult SelectAndSend(IList<string> menuLines, params string[] patterns)
        {
            var selection = SelectFromMenu(menuLines, patterns);
            if (selection.WasSelected)
            {
                SetNumber(GsxDefinition.MenuChoice, selection.Index);
                System.Threading.Thread.Sleep(250);
                StatusText = "GSX selected: " + selection.Text;
            }

            return selection;
        }

        private static IList<string> WaitForMenuUpdate(string menuPath, DateTime afterUtc, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(menuPath))
                {
                    var info = new FileInfo(menuPath);
                    var lines = File.ReadAllLines(menuPath)
                        .Select(line => (line ?? string.Empty).Trim())
                        .Where(line => line.Length > 0)
                        .ToList();

                    if (info.LastWriteTimeUtc > afterUtc && lines.Count >= 2)
                    {
                        return lines;
                    }
                }

                System.Threading.Thread.Sleep(150);
            }

            return null;
        }

        private IList<string> WaitForMenuUpdate(DateTime afterUtc, int timeoutMs)
        {
            return WaitForMenuUpdate(_paths.GsxMenuPath, afterUtc, timeoutMs);
        }

        public static MenuSelectionResult SelectFromMenu(IList<string> menuLines, params string[] patterns)
        {
            var match = MatchMenuChoice(menuLines, patterns);
            if (match.Status == MenuMatchStatus.Selected)
            {
                return MenuSelectionResult.Selected(match.Index, match.Text, match.Reason);
            }

            if (match.Status == MenuMatchStatus.Ambiguous)
            {
                return MenuSelectionResult.Ambiguous(match.Reason);
            }

            if (match.Status == MenuMatchStatus.NotDetected)
            {
                return MenuSelectionResult.NotDetected(match.Reason);
            }

            return MenuSelectionResult.NotFound(match.Reason);
        }

        public static MenuMatchResult MatchMenuChoice(IList<string> menuLines, params string[] patterns)
        {
            if (menuLines == null || menuLines.Count < 2)
            {
                return MenuMatchResult.NotDetected("GSX menu is empty or unavailable.");
            }

            var requestVariants = BuildRequestVariants(patterns);
            var options = new List<MenuOption>();
            for (int i = 1; i < menuLines.Count; i++)
            {
                options.Add(new MenuOption(i - 1, menuLines[i]));
            }

            var exactMatches = options.Where(option => requestVariants.Contains(option.Normalized) || requestVariants.Contains(option.Canonical)).ToList();
            if (exactMatches.Count == 1)
            {
                return MenuMatchResult.Selected(exactMatches[0], "Exact normalized menu match.");
            }

            if (exactMatches.Count > 1)
            {
                return MenuMatchResult.Ambiguous("Multiple exact GSX menu matches: " + string.Join(" | ", exactMatches.Select(x => x.Text).ToArray()));
            }

            var containsMatches = options.Where(option => requestVariants.Any(request => option.Normalized.Contains(request) || option.Canonical.Contains(request))).ToList();
            if (containsMatches.Count == 1)
            {
                return MenuMatchResult.Selected(containsMatches[0], "Contains-match menu match.");
            }

            if (containsMatches.Count > 1)
            {
                return MenuMatchResult.Ambiguous("Multiple contains GSX menu matches: " + string.Join(" | ", containsMatches.Select(x => x.Text).ToArray()));
            }

            var tokenMatches = options.Where(option => requestVariants.Any(request => TokenSubsetMatch(option.Canonical, request))).ToList();
            if (tokenMatches.Count == 1)
            {
                return MenuMatchResult.Selected(tokenMatches[0], "Synonym/token menu match.");
            }

            if (tokenMatches.Count > 1)
            {
                return MenuMatchResult.Ambiguous("Multiple synonym GSX menu matches: " + string.Join(" | ", tokenMatches.Select(x => x.Text).ToArray()));
            }

            return MenuMatchResult.NotFound("No GSX menu match found. Available options: " + DescribeOptions(menuLines));
        }

        public static string DescribeOptions(IList<string> menuLines)
        {
            if (menuLines == null || menuLines.Count < 2)
            {
                return "<no options>";
            }

            return string.Join(" | ", menuLines.Skip(1).ToArray());
        }

        private static HashSet<string> BuildRequestVariants(string[] patterns)
        {
            var variants = new HashSet<string>(StringComparer.Ordinal);
            if (patterns == null)
            {
                return variants;
            }

            for (int i = 0; i < patterns.Length; i++)
            {
                var normalized = TextUtility.NormalizeText(patterns[i]);
                if (normalized.Length == 0)
                {
                    continue;
                }

                variants.Add(normalized);
                variants.Add(TextUtility.CanonicalizeMenuText(normalized));
            }

            return variants;
        }

        private static bool TokenSubsetMatch(string option, string request)
        {
            var requestTokens = request.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < requestTokens.Length; i++)
            {
                if (!option.Contains(requestTokens[i]))
                {
                    return false;
                }
            }

            return requestTokens.Length > 0;
        }

        private void CloseConnection()
        {
            try
            {
                _simconnect?.Dispose();
            }
            catch
            {
            }

            _simconnect = null;
            _simConnected = false;
            _couatlStarted = false;
            _definitionsRegistered = false;
            _nextConnectAttemptUtc = DateTime.UtcNow.AddSeconds(2);
        }
    }

    internal enum MenuMatchStatus
    {
        NotDetected = 0,
        NotFound = 1,
        Ambiguous = 2,
        Selected = 3
    }

    internal sealed class MenuMatchResult
    {
        public MenuMatchStatus Status;
        public int Index;
        public string Text;
        public string Reason;

        public static MenuMatchResult Selected(MenuOption option, string reason)
        {
            return new MenuMatchResult { Status = MenuMatchStatus.Selected, Index = option.Index, Text = option.Text, Reason = reason };
        }

        public static MenuMatchResult Ambiguous(string reason)
        {
            return new MenuMatchResult { Status = MenuMatchStatus.Ambiguous, Reason = reason };
        }

        public static MenuMatchResult NotFound(string reason)
        {
            return new MenuMatchResult { Status = MenuMatchStatus.NotFound, Reason = reason };
        }

        public static MenuMatchResult NotDetected(string reason)
        {
            return new MenuMatchResult { Status = MenuMatchStatus.NotDetected, Reason = reason };
        }
    }

    internal sealed class MenuSelectionResult
    {
        public bool WasSelected;
        public int Index;
        public string Text;
        public string Reason;

        public static MenuSelectionResult Selected(int index, string text, string reason)
        {
            return new MenuSelectionResult { WasSelected = true, Index = index, Text = text, Reason = reason };
        }

        public static MenuSelectionResult Ambiguous(string reason)
        {
            return new MenuSelectionResult { WasSelected = false, Reason = reason };
        }

        public static MenuSelectionResult NotFound(string reason)
        {
            return new MenuSelectionResult { WasSelected = false, Reason = reason };
        }

        public static MenuSelectionResult NotDetected(string reason)
        {
            return new MenuSelectionResult { WasSelected = false, Reason = reason };
        }
    }

    internal sealed class MenuOption
    {
        public readonly int Index;
        public readonly string Text;
        public readonly string Normalized;
        public readonly string Canonical;

        public MenuOption(int index, string text)
        {
            Index = index;
            Text = text;
            Normalized = TextUtility.NormalizeText(text);
            Canonical = TextUtility.CanonicalizeMenuText(text);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    internal struct SingleValueData
    {
        public double value;
    }

    internal enum GsxDefinition
    {
        CouatlStarted = 0,
        MenuOpen = 1,
        MenuChoice = 2,
        RemoteControl = 3
    }

    internal enum GsxRequest
    {
        CouatlStarted = 100,
        RemoteControl = 101
    }
}
