using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DirectoryMenuTray
{
  public class DirectoryMenuApplicationContext : ApplicationContext
  {
    private NotifyIcon trayIcon;
    private string MainPath;

    public DirectoryMenuApplicationContext(string path)
    {
      MainPath = path;
      // Initialize Tray Icon
      Bitmap theBitmap = Properties.Resources.FolderClose;
      IntPtr Hicon = theBitmap.GetHicon();// Get an Hicon for myBitmap.

      trayIcon = new NotifyIcon()
      {
        Icon = Icon.FromHandle(Hicon),
        ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", Exit)
            }),
        Visible = true
      };
      trayIcon.Click += TrayIcon_Click;
    }

    private void TrayIcon_Click(object sender, EventArgs e)
    {
      var menu = new ContextMenuStrip();
      menu.BackColor = Color.Black;
      menu.ForeColor = Color.White;
      menu.Renderer = new DarkRenderer();
      var itms = GetMenuItems(this.MainPath);
      if (itms == null || itms.Count <= 0)
        return;
      menu.Items.AddRange(itms.ToArray());
      menu.Items.Add(new ToolStripSeparator());
      menu.Items.Add(new ToolStripMenuItem("Cancel"));

      menu.AutoClose = true;
      menu.Capture = true;
      menu.MouseEnter += Menu_MouseEnter;
      menu.UseWaitCursor = false;
      menu.Cursor = Cursors.Hand;
      menu.Show(Control.MousePosition);
      SetTimer(menu,1000);
    }

		private void Menu_MouseEnter(object sender, EventArgs e)
		{
      SetTimer(sender, 500);
    }

    #region timer
    private Timer m_LeaveTimer;
    private void SetTimer(object menu, int interval)
    {
      DestroyTimer();
      m_LeaveTimer = new Timer();
      // Hook up the Elapsed event for the timer. 
      m_LeaveTimer.Interval = interval;
			m_LeaveTimer.Tick += OnTimedEvent; ;
      m_LeaveTimer.Enabled = true;
      m_LeaveTimer.Tag = menu;
    }

		private void OnTimedEvent(object sender, EventArgs e)
    {

      if(m_LeaveTimer!=null)
			{
        var menu = m_LeaveTimer.Tag as ContextMenuStrip;
        var mousePos = Cursor.Position;
        m_LeaveTimer.Stop();
        if (HitTest(mousePos, menu))
				{
          m_LeaveTimer.Start();
          return;
        }
        if (menu != null)
        {
          menu.Hide();
          menu.Dispose();
        }
      }
      DestroyTimer();
    }

    private bool HitTest(Point mouseLoc, ToolStripDropDown menu)
		{
      if (menu.ClientRectangle.Contains(menu.PointToClient(mouseLoc)))
        return true;
      for (int i = 0; i < menu.Items.Count; i++)
      {
        var itm = menu.Items[i] as ToolStripMenuItem;
        if (itm == null)
          continue;
        if (itm.DropDown != null && itm.DropDown.Visible)
        {
          if (HitTest(mouseLoc, itm.DropDown))
            return true;
        }
      }

      return false;
		}

    private void DestroyTimer()
		{
      if (m_LeaveTimer != null)
      {
        try
        {
          m_LeaveTimer.Stop();
          m_LeaveTimer.Dispose();
        }
        catch { }
        m_LeaveTimer = null;
      }

    }
    #endregion
    void Exit(object sender, EventArgs e)
    {
      // Hide tray icon, otherwise it will remain shown until user mouses over it
      trayIcon.Visible = false;

      Application.Exit();
    }

    List<ToolStripMenuItem> GetMenuItems(string path)
    {
      var ret = new List<ToolStripMenuItem>();
      var files = Directory.EnumerateFileSystemEntries(path);
      var shell = new Shell32.Shell();
      var folder = shell.NameSpace(path);

      foreach (var file in files)
      {
        var fi = new FileInfo(file);


        FileAttributes fattrib = File.GetAttributes(file);
        if (fattrib.HasFlag(FileAttributes.Hidden))
          continue;
        ToolStripMenuItem itm = null;
        if (string.Equals(fi.Extension, ".url", StringComparison.InvariantCultureIgnoreCase) ||
          string.Equals(fi.Extension, ".lnk", StringComparison.InvariantCultureIgnoreCase) 
          )
        {
          var folderItem = folder.ParseName(fi.Name);
          Shell32.IShellLinkDual link = folderItem.GetLink;

          string icon;
          link.GetIconLocation(out icon);
          var lnkpath = link.Path;
          string args = string.Empty;
          try
          {
            args = link.Arguments;
          }
          catch { }
          var workingfolder = link.WorkingDirectory;

          Image bmp = null;
          try
          {
            bmp = Bitmap.FromFile(new Uri(icon).LocalPath);
          }
          catch
          {
            bmp = System.Drawing.Icon.ExtractAssociatedIcon(file).ToBitmap();
          }
          itm = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(fi.Name), bmp);



          itm.Tag = file;
        }
        else if (fattrib.HasFlag(FileAttributes.Directory))
				{

          var subItems = GetMenuItems(file);
          if(subItems!=null && subItems.Count>=0)
					{
            var bmp = ShellIcon.GetLargeFolderIcon().ToBitmap();
            itm = new ToolStripMenuItem(fi.Name,bmp);
            itm.DropDownItems.AddRange(subItems.ToArray());
            itm.DropDown.ForeColor = Color.White;
          }
        }
        else
				{
          Image bmp = System.Drawing.Icon.ExtractAssociatedIcon(file).ToBitmap();
          itm = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(file), bmp);
          itm.Tag = file;
        }

        if(itm!=null)
				{
          itm.MouseDown += MenuItem_Click;
          ret.Add(itm);
        }
      }
      return ret;
    }

		void MenuItem_Click(object sender, MouseEventArgs e)
    {

      var item = sender as ToolStripMenuItem;
      if (item == null)
        return;
      var file = item.Tag as string;
      if (string.IsNullOrEmpty(file))
        return;



      var startInfo = new System.Diagnostics.ProcessStartInfo();
      startInfo.FileName = file;
      if (e.Button == MouseButtons.Right)
      {
        startInfo.Verb= "properties";
      }
      
      System.Diagnostics.Process process = new System.Diagnostics.Process();
      process.StartInfo = startInfo;
      process.Start();
    }
    protected override void Dispose(bool disposing)
    {
      trayIcon.Visible = false;
      base.Dispose(disposing);
    }

    public class DarkRenderer : ToolStripProfessionalRenderer
    {
      public DarkRenderer()
        :base(new ProfessionalColorTableDark())
      { }
      protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
      {
        var tsMenuItem = e.Item as ToolStripMenuItem;
        if (tsMenuItem != null)
          e.ArrowColor = Color.White;
        base.OnRenderArrow(e);
      }
    }
    private class ProfessionalColorTableDark : ProfessionalColorTable
    {
      internal Color background = Color.Black;
      private Color highlighted = Color.FromArgb(32, 32, 32);
      private Color highlightedBorder = Color.FromArgb(64, 64, 64);
      private Color gripDark = Color.Pink;
      private Color menuBackground = Color.Black;
      private Color overflow = Color.Green;
      private Color separatorDark = Color.White;
      private Color separatorLight = Color.White;
      private Color imageMargin = Color.Black;


      public override Color ToolStripGradientBegin { get { return background; } }
      public override Color ToolStripGradientMiddle { get { return background; } }
      public override Color ToolStripGradientEnd { get { return background; } }

      public override Color ButtonSelectedHighlight { get { return highlighted; } }
      public override Color ButtonSelectedHighlightBorder { get { return highlightedBorder; } }

      public override Color ButtonPressedHighlight { get { return highlighted; } }
      public override Color ButtonPressedHighlightBorder { get { return highlightedBorder; } }

      public override Color ButtonCheckedHighlight { get { return highlighted; } }
      public override Color ButtonCheckedHighlightBorder { get { return highlightedBorder; } }

      public override Color ToolStripBorder { get { return background; } }

      public override Color CheckBackground { get { return highlighted; } }
      public override Color CheckPressedBackground { get { return highlighted; } }
      public override Color CheckSelectedBackground { get { return highlighted; } }

      public override Color ButtonCheckedGradientBegin { get { return highlighted; } }
      public override Color ButtonCheckedGradientEnd { get { return highlighted; } }
      public override Color ButtonCheckedGradientMiddle { get { return highlighted; } }
      public override Color ButtonPressedBorder { get { return highlightedBorder; } }
      public override Color ButtonPressedGradientBegin { get { return highlighted; } }
      public override Color ButtonPressedGradientEnd { get { return highlighted; } }
      public override Color ButtonPressedGradientMiddle { get { return highlighted; } }
      public override Color ButtonSelectedBorder { get { return highlightedBorder; } }
      public override Color ButtonSelectedGradientBegin { get { return highlighted; } }
      public override Color ButtonSelectedGradientEnd { get { return highlighted; } }
      public override Color ButtonSelectedGradientMiddle { get { return highlighted; } }

      public override Color GripDark { get { return Color.Transparent; } }
      public override Color GripLight { get { return gripDark; } }

      public override Color MenuBorder { get { return menuBackground; } }
      public override Color MenuItemBorder { get { return highlightedBorder; } }
      public override Color MenuItemPressedGradientBegin { get { return highlighted; } }
      public override Color MenuItemPressedGradientEnd { get { return highlighted; } }
      public override Color MenuItemPressedGradientMiddle { get { return highlighted; } }
      public override Color MenuItemSelected { get { return highlighted; } }
      public override Color MenuItemSelectedGradientBegin { get { return highlighted; } }
      public override Color MenuItemSelectedGradientEnd { get { return highlighted; } }
      public override Color MenuStripGradientBegin { get { return menuBackground; } }
      public override Color MenuStripGradientEnd { get { return menuBackground; } }
      public override Color OverflowButtonGradientBegin { get { return overflow; } }
      public override Color OverflowButtonGradientEnd { get { return overflow; } }
      public override Color OverflowButtonGradientMiddle { get { return overflow; } }
      public override Color SeparatorDark { get { return separatorDark; } }
      public override Color SeparatorLight { get { return separatorLight; } }
      public override Color StatusStripGradientBegin { get { return background; } }
      public override Color StatusStripGradientEnd { get { return background; } }
      public override Color ToolStripContentPanelGradientBegin { get { return Color.Red; } }
      public override Color ToolStripContentPanelGradientEnd { get { return background; } }
      public override Color ToolStripDropDownBackground { get { return Color.Black; } }
      public override Color ToolStripPanelGradientBegin { get { return background; } }
      public override Color ToolStripPanelGradientEnd { get { return background; } }
      public override Color RaftingContainerGradientBegin { get { return Color.Red; } }
      public override Color ImageMarginGradientBegin { get { return imageMargin; } }
      public override Color ImageMarginGradientEnd { get { return imageMargin; } }
      public override Color ImageMarginGradientMiddle { get { return imageMargin; } }
      public override Color ImageMarginRevealedGradientBegin { get { return Color.Green; } }
    }
  }
}
