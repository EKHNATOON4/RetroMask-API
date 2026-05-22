using AutoMapper;
using RetroMask.Application.Dtos.Auth;
using RetroMask.Application.Dtos.Teams;
using RetroMask.Application.Dtos.Sessions;
using RetroMask.Application.Dtos.Points;
using RetroMask.Application.Dtos.Voting;
using RetroMask.Application.Dtos.Feedback;
using RetroMask.Application.Dtos.AI;
using RetroMask.Application.Dtos.Reports;
using RetroMask.Application.Dtos.ActionItems;
using RetroMask.Application.Dtos.Game;
using RetroMask.Application.Dtos.Insights;
using RetroMask.Application.Dtos.Notifications;
using RetroMask.Domain.Entities.Identity;
using RetroMask.Domain.Entities.Teams;
using RetroMask.Domain.Entities.Sessions;
using RetroMask.Domain.Entities.Discussion;
using RetroMask.Domain.Entities.Voting;
using RetroMask.Domain.Entities.Feedback;
using RetroMask.Domain.Entities.AI;
using RetroMask.Domain.Entities.ActionItems;
using RetroMask.Domain.Entities.Game;
using RetroMask.Domain.Entities.Insights;
using RetroMask.Domain.Entities.Notifications;

namespace RetroMask.Application.Mapping
{
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Identity
        CreateMap<ApplicationUser, UserProfileDto>()
            .ForMember(d => d.Role, o => o.Ignore());

        // Teams
        CreateMap<Team, TeamDto>()
            .ForMember(d => d.MemberCount, o => o.MapFrom(s => s.Members.Count))
            .ForMember(d => d.MyRole, o => o.Ignore());
        CreateMap<TeamMember, TeamMemberDto>()
            .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.User.DisplayName ?? s.User.Email))
            .ForMember(d => d.Email, o => o.MapFrom(s => s.User.Email))
            .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.User.AvatarUrl));
        CreateMap<TeamInvitation, TeamInvitationDto>().IgnoreAllPropertiesWithAnInaccessibleSetter();

        // Sessions
        CreateMap<Session, SessionDto>()
            .ForMember(d => d.TeamName, o => o.MapFrom(s => s.Team.Name))
            .ForMember(d => d.FacilitatorName, o => o.MapFrom(s => s.Facilitator.DisplayName ?? s.Facilitator.Email))
            .ForMember(d => d.MemberCount, o => o.MapFrom(s => s.Members.Count));
        CreateMap<Session, SessionSummaryDto>()
            .ForMember(d => d.MemberCount, o => o.MapFrom(s => s.Members.Count))
            .ForMember(d => d.PointCount, o => o.Ignore());
        CreateMap<SessionPhase, PhaseDto>()
            .ForMember(d => d.PointCount, o => o.MapFrom(s => s.DiscussionPoints.Count));

        // Discussion
        CreateMap<DiscussionPoint, PointDto>()
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.IsAnonymous ? null : s.Author.DisplayName))
            .ForMember(d => d.VoteCount, o => o.MapFrom(s => s.Votes.Count))
            .ForMember(d => d.CommentCount, o => o.MapFrom(s => s.Comments.Count))
            .ForMember(d => d.Reactions, o => o.Ignore());
        CreateMap<PointTag, TagDto>();
        CreateMap<DiscussionComment, CommentDto>()
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.IsAnonymous ? null : s.Author.DisplayName));

        // Voting
        CreateMap<VoteSummary, VoteResultDto>()
            .ForMember(d => d.MyVote, o => o.Ignore());

        // Feedback
        CreateMap<FriendFeedback, FeedbackDto>()
            .ForMember(d => d.GiverName, o => o.MapFrom(s => s.IsAnonymous ? null : s.Giver.DisplayName))
            .ForMember(d => d.ReceiverName, o => o.MapFrom(s => s.Receiver.DisplayName ?? s.Receiver.Email))
            .ForMember(d => d.Reactions, o => o.Ignore());

        // AI
        CreateMap<AIInsight, AIInsightDto>();
        CreateMap<AICluster, AIClusterDto>()
            .ForMember(d => d.PointIds, o => o.Ignore());
        CreateMap<AIReport, AIReportDto>();
        CreateMap<AIReport, ReportDto>()
            .ForMember(d => d.SessionTitle, o => o.MapFrom(s => s.Session.Title));

        // Action Items
        CreateMap<ActionItem, ActionItemDto>()
            .ForMember(d => d.AssignedToName, o => o.MapFrom(s => s.AssignedTo.DisplayName ?? s.AssignedTo.Email))
            .ForMember(d => d.ProgressPercent, o => o.Ignore());
        CreateMap<ActionItemUpdate, ActionItemUpdateDto>()
            .ForMember(d => d.AuthorName, o => o.MapFrom(s => s.Author.DisplayName ?? s.Author.Email));

        // Game
        CreateMap<IcebreakerGame, GameDto>()
            .ForMember(d => d.ParticipantCount, o => o.MapFrom(s => s.Results.Count));
        CreateMap<GameResult, GameResultDto>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.DisplayName ?? s.User.Email))
            .ForMember(d => d.Rank, o => o.Ignore());

        // Insights
        CreateMap<UserInsight, UserInsightDto>()
            .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.DisplayName ?? s.User.Email))
            .ForMember(d => d.ActionItemCompletionRate, o => o.Ignore());
        CreateMap<UserGrowthSnapshot, GrowthSnapshotDto>()
            .ForMember(d => d.MonthLabel, o => o.Ignore());

        // Notifications
        CreateMap<Notification, NotificationDto>();
    }
}
}

// Placeholder for TeamInvitationDto referenced above
namespace RetroMask.Application.Dtos.Teams
{
    public class TeamInvitationDto
    {
        public Guid Id { get; set; }
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public string InvitedEmail { get; set; } = string.Empty;
        public Domain.Enums.InvitationStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
