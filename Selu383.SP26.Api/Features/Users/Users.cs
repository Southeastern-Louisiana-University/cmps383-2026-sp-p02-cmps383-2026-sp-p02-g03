using System.ComponentModel.DataAnnotations.Schema;

namespace Selu383.SP26.Api.Data;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";

    [NotMapped]
    public string UserName { get => Username; set => Username = value; }

    public string Password { get; set; } = "";
    public string RoleName { get; set; } = "";
}
