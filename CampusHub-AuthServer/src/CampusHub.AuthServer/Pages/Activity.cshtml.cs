using CampusHub.AuthServer.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.AuthServer.Pages;

/// <summary>
/// A live view of what the server is doing: each step of the sign-in flow, and the reason when
/// one is rejected. It shows the same messages as the console window.
/// </summary>
public class ActivityModel(ActivityLog log) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public bool Paused { get; set; }

    public IReadOnlyList<ActivityEntry> Entries { get; private set; } = [];

    public void OnGet() => Entries = log.Snapshot();

    public IActionResult OnPostClear()
    {
        log.Clear();
        return RedirectToPage(new { Paused });
    }
}
