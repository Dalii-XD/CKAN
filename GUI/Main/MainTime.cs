using System;

// Don't warn if we use our own obsolete properties
#pragma warning disable 0618

namespace CKAN.GUI
{
    public partial class Main
    {
        private void ViewPlayTimeToolStripMenuItem_Click(object? sender, EventArgs? e)
        {
            PlayTime.loadAllPlayTime(Manager);
            tabController.ShowTab(PlayTimeTabPage.Name, 2);
            DisableMainWindow();
        }

        private void PlayTime_Done()
        {
            UpdateStatusBar();
            tabController.ShowTab(ManageModsTabPage.Name);
            tabController.HideTab(PlayTimeTabPage.Name);
            EnableMainWindow();
        }
    }
}
