using System.Security.Cryptography;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RetroMask.Application.Abstractions;
using RetroMask.Application.Abstractions.Repositories;
using RetroMask.Application.Common;
using RetroMask.Application.Dtos.Teams;
using RetroMask.Application.Services.Teams;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Teams;
using RetroMask.Domain.Enums;

namespace RetroMask.Infrastructure.Services.Teams;

public class TeamService : ITeamService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public TeamService(
        IUnitOfWork uow,
        ICurrentUser currentUser,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService)
    {
        _uow = uow;
        _currentUser = currentUser;
        _mapper = mapper;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<ApiResponse<TeamDto>> CreateTeamAsync(CreateTeamRequest request, CancellationToken ct = default)
    {
        var team = new Team
        {
            Name = request.Name,
            Description = request.Description,
            IsPublic = request.IsPublic,
            InviteCode = GenerateInviteCode(),
            CreatedBy = _currentUser.UserId
        };

        var ownerMember = new TeamMember
        {
            TeamId = team.Id,
            UserId = _currentUser.UserId,
            Role = TeamMemberRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        await _uow.Repository<Team>().AddAsync(team, ct);
        await _uow.Repository<TeamMember>().AddAsync(ownerMember, ct);
        await _uow.SaveChangesAsync(ct);

        var dto = _mapper.Map<TeamDto>(team);
        dto.MemberCount = 1;
        dto.MyRole = TeamMemberRole.Owner.ToString();

        return ApiResponse<TeamDto>.Ok(dto, "Team created successfully.");
    }

    public async Task<ApiResponse<TeamDto>> GetTeamByIdAsync(Guid teamId, CancellationToken ct = default)
    {
        var team = await _uow.Repository<Team>().Query()
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team is null)
            return ApiResponse<TeamDto>.Fail("Team not found.");

        var dto = _mapper.Map<TeamDto>(team);
        var myMembership = team.Members.FirstOrDefault(m => m.UserId == _currentUser.UserId);
        dto.MyRole = myMembership?.Role.ToString();

        return ApiResponse<TeamDto>.Ok(dto);
    }

    public async Task<ApiResponse<IEnumerable<TeamDto>>> GetMyTeamsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var teams = await _uow.Repository<TeamMember>().Query()
            .Where(m => m.UserId == userId && m.IsActive)
            .Include(m => m.Team).ThenInclude(t => t.Members)
            .Select(m => m.Team)
            .ToListAsync(ct);

        var dtos = teams.Select(t =>
        {
            var dto = _mapper.Map<TeamDto>(t);
            var myMembership = t.Members.FirstOrDefault(m => m.UserId == userId);
            dto.MyRole = myMembership?.Role.ToString();
            return dto;
        });

        return ApiResponse<IEnumerable<TeamDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<TeamDto>> UpdateTeamAsync(Guid teamId, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var team = await _uow.Repository<Team>().Query()
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team is null)
            return ApiResponse<TeamDto>.Fail("Team not found.");

        var callerRole = GetCallerRole(team);
        if (callerRole < TeamMemberRole.Admin)
            return ApiResponse<TeamDto>.Fail("Only admins or the owner can update team settings.");

        if (request.Name is not null) team.Name = request.Name;
        if (request.Description is not null) team.Description = request.Description;
        if (request.IsPublic.HasValue) team.IsPublic = request.IsPublic.Value;
        team.UpdatedBy = _currentUser.UserId;

        _uow.Repository<Team>().Update(team);
        await _uow.SaveChangesAsync(ct);

        var dto = _mapper.Map<TeamDto>(team);
        dto.MyRole = callerRole.ToString();

        return ApiResponse<TeamDto>.Ok(dto, "Team updated successfully.");
    }

    public async Task<ApiResponse> DeleteTeamAsync(Guid teamId, CancellationToken ct = default)
    {
        var team = await _uow.Repository<Team>().Query()
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team is null)
            return ApiResponse.Fail("Team not found.");

        if (GetCallerRole(team) != TeamMemberRole.Owner)
            return ApiResponse.Fail("Only the team owner can delete the team.");

        team.IsDeleted = true;
        team.DeletedAt = DateTime.UtcNow;
        team.DeletedBy = _currentUser.UserId;

        _uow.Repository<Team>().Update(team);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Team deleted successfully.");
    }

    public async Task<ApiResponse<IEnumerable<TeamMemberDto>>> GetMembersAsync(Guid teamId, CancellationToken ct = default)
    {
        var teamExists = await _uow.Repository<Team>().AnyAsync(t => t.Id == teamId, ct);
        if (!teamExists)
            return ApiResponse<IEnumerable<TeamMemberDto>>.Fail("Team not found.");

        var members = await _uow.Repository<TeamMember>().Query()
            .Where(m => m.TeamId == teamId && m.IsActive)
            .Include(m => m.User)
            .ToListAsync(ct);

        var dtos = _mapper.Map<IEnumerable<TeamMemberDto>>(members);
        return ApiResponse<IEnumerable<TeamMemberDto>>.Ok(dtos);
    }

    public async Task<ApiResponse> InviteMemberAsync(Guid teamId, InviteMemberRequest request, CancellationToken ct = default)
    {
        var team = await _uow.Repository<Team>().Query()
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team is null)
            return ApiResponse.Fail("Team not found.");

        var callerRole = GetCallerRole(team);
        if (callerRole < TeamMemberRole.Admin)
            return ApiResponse.Fail("Only admins or the owner can invite members.");

        if (request.Role >= callerRole)
            return ApiResponse.Fail("You cannot assign a role equal to or higher than your own.");

        var alreadyMember = team.Members.Any(m =>
            m.User != null && string.Equals(m.User.Email, request.Email, StringComparison.OrdinalIgnoreCase) && m.IsActive);

        if (!alreadyMember)
        {
            var existingMemberByEmail = await _uow.Repository<TeamMember>().Query()
                .Include(m => m.User)
                .Where(m => m.TeamId == teamId && m.IsActive && m.User.Email == request.Email)
                .AnyAsync(ct);

            if (existingMemberByEmail)
                alreadyMember = true;
        }

        if (alreadyMember)
            return ApiResponse.Fail("This user is already a member of the team.");

        var pendingInvite = await _uow.Repository<TeamInvitation>().AnyAsync(
            i => i.TeamId == teamId
                 && i.InvitedEmail == request.Email
                 && i.Status == InvitationStatus.Pending
                 && i.ExpiresAt > DateTime.UtcNow, ct);

        if (pendingInvite)
            return ApiResponse.Fail("A pending invitation already exists for this email.");

        var invitedUser = await _userManager.FindByEmailAsync(request.Email);

        var invitation = new TeamInvitation
        {
            TeamId = teamId,
            InvitedEmail = request.Email,
            InvitedUserId = invitedUser?.Id,
            InvitedById = _currentUser.UserId,
            Token = GenerateInvitationToken(),
            AssignedRole = request.Role,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedBy = _currentUser.UserId
        };

        await _uow.Repository<TeamInvitation>().AddAsync(invitation, ct);
        await _uow.SaveChangesAsync(ct);

        var acceptLink = $"https://retromask.com/invitations/{invitation.Token}/accept";
        var htmlBody = $"<h2>You've been invited to join {team.Name}</h2>"
                     + $"<p>{_currentUser.DisplayName ?? _currentUser.Email} has invited you to join the team <strong>{team.Name}</strong> as a <strong>{request.Role}</strong>.</p>"
                     + $"<p><a href=\"{acceptLink}\">Accept Invitation</a></p>"
                     + $"<p>This invitation expires on {invitation.ExpiresAt:MMMM dd, yyyy}.</p>";

        await _emailService.SendAsync(request.Email, $"RetroMask - Invitation to join {team.Name}", htmlBody, ct);

        return ApiResponse.Ok("Invitation sent successfully.");
    }

    public async Task<ApiResponse> AcceptInvitationAsync(string token, CancellationToken ct = default)
    {
        var invitation = await _uow.Repository<TeamInvitation>().Query()
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Token == token, ct);

        if (invitation is null)
            return ApiResponse.Fail("Invitation not found.");

        if (invitation.Status != InvitationStatus.Pending)
            return ApiResponse.Fail($"Invitation has already been {invitation.Status.ToString().ToLowerInvariant()}.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            _uow.Repository<TeamInvitation>().Update(invitation);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse.Fail("Invitation has expired.");
        }

        var userId = _currentUser.UserId;
        var userEmail = _currentUser.Email;

        if (!string.Equals(invitation.InvitedEmail, userEmail, StringComparison.OrdinalIgnoreCase))
            return ApiResponse.Fail("This invitation was sent to a different email address.");

        var alreadyMember = await _uow.Repository<TeamMember>()
            .AnyAsync(m => m.TeamId == invitation.TeamId && m.UserId == userId && m.IsActive, ct);

        if (alreadyMember)
        {
            invitation.Status = InvitationStatus.Accepted;
            invitation.RespondedAt = DateTime.UtcNow;
            _uow.Repository<TeamInvitation>().Update(invitation);
            await _uow.SaveChangesAsync(ct);
            return ApiResponse.Ok("You are already a member of this team.");
        }

        var member = new TeamMember
        {
            TeamId = invitation.TeamId,
            UserId = userId,
            Role = invitation.AssignedRole,
            JoinedAt = DateTime.UtcNow
        };

        invitation.Status = InvitationStatus.Accepted;
        invitation.RespondedAt = DateTime.UtcNow;
        invitation.InvitedUserId = userId;

        await _uow.Repository<TeamMember>().AddAsync(member, ct);
        _uow.Repository<TeamInvitation>().Update(invitation);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"You have joined {invitation.Team.Name}.");
    }

    public async Task<ApiResponse> DeclineInvitationAsync(string token, CancellationToken ct = default)
    {
        var invitation = await _uow.Repository<TeamInvitation>()
            .FirstOrDefaultAsync(i => i.Token == token, ct);

        if (invitation is null)
            return ApiResponse.Fail("Invitation not found.");

        if (invitation.Status != InvitationStatus.Pending)
            return ApiResponse.Fail($"Invitation has already been {invitation.Status.ToString().ToLowerInvariant()}.");

        if (!string.Equals(invitation.InvitedEmail, _currentUser.Email, StringComparison.OrdinalIgnoreCase))
            return ApiResponse.Fail("This invitation was sent to a different email address.");

        invitation.Status = InvitationStatus.Declined;
        invitation.RespondedAt = DateTime.UtcNow;

        _uow.Repository<TeamInvitation>().Update(invitation);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Invitation declined.");
    }

    public async Task<ApiResponse> RemoveMemberAsync(Guid teamId, string userId, CancellationToken ct = default)
    {
        var team = await _uow.Repository<Team>().Query()
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team is null)
            return ApiResponse.Fail("Team not found.");

        var callerRole = GetCallerRole(team);
        var isSelfRemoval = userId == _currentUser.UserId;

        if (!isSelfRemoval && callerRole < TeamMemberRole.Admin)
            return ApiResponse.Fail("Only admins or the owner can remove members.");

        var member = team.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
        if (member is null)
            return ApiResponse.Fail("Member not found in this team.");

        if (member.Role == TeamMemberRole.Owner)
            return ApiResponse.Fail("The team owner cannot be removed. Transfer ownership first.");

        if (!isSelfRemoval && member.Role >= callerRole)
            return ApiResponse.Fail("You cannot remove a member with an equal or higher role.");

        member.IsActive = false;
        _uow.Repository<TeamMember>().Update(member);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Member removed successfully.");
    }

    public async Task<ApiResponse> UpdateMemberRoleAsync(Guid teamId, string userId, UpdateMemberRoleRequest request, CancellationToken ct = default)
    {
        var team = await _uow.Repository<Team>().Query()
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId, ct);

        if (team is null)
            return ApiResponse.Fail("Team not found.");

        var callerRole = GetCallerRole(team);
        if (callerRole < TeamMemberRole.Admin)
            return ApiResponse.Fail("Only admins or the owner can change member roles.");

        if (request.NewRole == TeamMemberRole.Owner)
            return ApiResponse.Fail("Ownership transfer is not allowed through role update.");

        var member = team.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
        if (member is null)
            return ApiResponse.Fail("Member not found in this team.");

        if (member.Role == TeamMemberRole.Owner)
            return ApiResponse.Fail("Cannot change the owner's role.");

        if (member.Role >= callerRole)
            return ApiResponse.Fail("You cannot change the role of a member with an equal or higher role.");

        if (request.NewRole >= callerRole)
            return ApiResponse.Fail("You cannot assign a role equal to or higher than your own.");

        member.Role = request.NewRole;
        _uow.Repository<TeamMember>().Update(member);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Member role updated successfully.");
    }

    private TeamMemberRole GetCallerRole(Team team)
    {
        var membership = team.Members.FirstOrDefault(m => m.UserId == _currentUser.UserId && m.IsActive);
        return membership?.Role ?? (TeamMemberRole)(-1);
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(RandomNumberGenerator.GetBytes(8)
            .Select(b => chars[b % chars.Length]).ToArray());
    }

    private static string GenerateInvitationToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
