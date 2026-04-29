using System.Collections.Generic;

namespace SimpleOps.GsxRamp
{
    internal interface IGsxMenuController
    {
        string GetTooltip();
        IList<string> GetMenuLines();
        MenuSelectionResult OpenAndSelect(string reason, params string[] patterns);
        MenuSelectionResult TrySelectExisting(params string[] patterns);
    }
}
