using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using CalendarDock.Core;
using Microsoft.Windows.Widgets.Providers;

namespace CalendarDock.App;

[ComVisible(true)]
[Guid(CLSID)]
[ComDefaultInterface(typeof(IWidgetProvider))]
public sealed class WidgetProvider : IWidgetProvider
{
    public const string CLSID = "7B1D6F52-3C0E-4F7A-9A55-0C7B3A9E4D21";
    private static readonly HashSet<string> ACTIVE = new();

    public static void UpdateAll()
    {
        string[] ids;
        lock (ACTIVE)
        {
            ids = ACTIVE.ToArray();
        }

        foreach (var id in ids)
        {
            Update(id);
        }
    }

    public void CreateWidget(WidgetContext widgetContext)
    {
        lock (ACTIVE)
        {
            ACTIVE.Add(widgetContext.Id);
        }

        Update(widgetContext.Id);
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        lock (ACTIVE)
        {
            ACTIVE.Remove(widgetId);
        }
    }

    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        Update(contextChangedArgs.WidgetContext.Id);
    }

    public void Activate(WidgetContext widgetContext)
    {
        lock (ACTIVE)
        {
            ACTIVE.Add(widgetContext.Id);
        }

        Update(widgetContext.Id);
    }

    public void Deactivate(string widgetId)
    {
        lock (ACTIVE)
        {
            ACTIVE.Remove(widgetId);
        }
    }

    private static void Update(string widgetId)
    {
        var meetings = MeetingAggregator.Upcoming(App.Host?.Meetings ?? Array.Empty<Meeting>(), DateTimeOffset.Now, 3);
        var items = new JsonArray();
        foreach (var meeting in meetings)
        {
            items.Add(new JsonObject
            {
                ["title"] = meeting.Title,
                ["time"] = $"{meeting.Start.LocalDateTime:ddd h:mm tt}",
                ["joinUrl"] = meeting.JoinUrl ?? "https://calendar.google.com",
                ["joinLabel"] = meeting.JoinUrl == null ? "Open" : "Join"
            });
        }

        var data = new JsonObject { ["meetings"] = items, ["empty"] = meetings.Count == 0 };
        var options = new WidgetUpdateRequestOptions(widgetId) { Template = TEMPLATE, Data = data.ToJsonString() };
        WidgetManager.GetDefault().UpdateWidget(options);
    }

    private const string TEMPLATE = """
    {
      "type": "AdaptiveCard",
      "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
      "version": "1.5",
      "body": [
        { "type": "TextBlock", "text": "No upcoming meetings", "isVisible": "${empty}" },
        {
          "type": "Container",
          "$data": "${meetings}",
          "items": [
            {
              "type": "ColumnSet",
              "columns": [
                { "type": "Column", "width": "stretch", "items": [
                  { "type": "TextBlock", "text": "${title}", "weight": "Bolder", "wrap": true },
                  { "type": "TextBlock", "text": "${time}", "isSubtle": true, "spacing": "None" }
                ] },
                { "type": "Column", "width": "auto", "items": [
                  { "type": "ActionSet", "actions": [ { "type": "Action.OpenUrl", "title": "${joinLabel}", "url": "${joinUrl}" } ] }
                ] }
              ]
            }
          ]
        }
      ]
    }
    """;
}
