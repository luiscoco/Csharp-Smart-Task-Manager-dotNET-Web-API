using System.ComponentModel.DataAnnotations;

namespace SmartTaskManager.Api.Contracts.Requests;

public sealed class CreateUserRequest
{
    [Required]
    [StringLength(100)]
    public string UserName { get; init; } = string.Empty;
}
