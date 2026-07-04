namespace Meridian.Api.Features.Users;

public interface IUserService
{
    Task<CreateEmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken ct = default);
    Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummaryDto>> ListAsync(int? teamId, int? departmentId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSummaryDto>> GetTeamMembersAsync(int teamId, CancellationToken ct = default);
}
