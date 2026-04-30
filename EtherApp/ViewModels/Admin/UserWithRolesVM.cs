namespace EtherApp.ViewModels.Admin
{
    public class UserWithRolesVM
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}