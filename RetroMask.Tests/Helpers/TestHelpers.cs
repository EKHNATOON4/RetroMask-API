using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RetroMask.Application.Mapping;

namespace RetroMask.Tests.Helpers;

public static class TestHelpers
{
    private static readonly IMapper _mapper;

    static TestHelpers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(_ => { }, typeof(MappingProfile).Assembly);
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();
    }

    public static IMapper CreateMapper() => _mapper;
}
