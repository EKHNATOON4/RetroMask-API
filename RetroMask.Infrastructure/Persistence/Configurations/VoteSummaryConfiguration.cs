using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetroMask.Domain.Entities.Voting;

namespace RetroMask.Infrastructure.Persistence.Configurations;

public class VoteSummaryConfiguration : IEntityTypeConfiguration<VoteSummary>
{
    public void Configure(EntityTypeBuilder<VoteSummary> builder)
    {
        builder.HasKey(vs => vs.Id);
        builder.Ignore(vs => vs.TotalVotes);
        builder.Ignore(vs => vs.Score);
    }
}
