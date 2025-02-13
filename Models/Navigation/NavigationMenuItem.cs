namespace FourSPM_WebService.Models.Navigation
{
    public class NavigationMenuItem
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? NavigationTitle { get; set; }
        public string? Icon { get; set; }
        public string? DocumentType { get; set; }
        public string? SecurityKey { get; set; }
        public string? ToolTip { get; set; }
        public string? DocumentParameter { get; set; }
        public Guid? Key { get; set; }
        public NavigationType NavigationType { get; set; }
        public ICollection<NavigationFlags> Flags { get; set; } = new List<NavigationFlags>();
        public ICollection<NavigationMenuItem> Children { get; } = new List<NavigationMenuItem>();
    }

    public enum NavigationType
    {
        Default,
        Project
    }

    public enum NavigationFlags
    {
        Expanded,
        Pinned,
        AllowPinning,
        ShowInCollapseMode,
        Searchable
    }
}
