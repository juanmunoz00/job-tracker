using AutoMapper;
using JobTracker.Core.DTOs;
using JobTracker.Core.Models;

namespace JobTracker.Api.Mappings;

public class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        CreateMap<Application, ApplicationDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<CreateApplicationDto, Application>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ApplicationStatus.Applied))
            .ForMember(dest => dest.StatusDate, opt => opt.MapFrom(_ => DateTime.UtcNow));

        CreateMap<ApplicationDto, Application>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<ApplicationStatus>(src.Status)))
            .ForMember(dest => dest.StatusDate, opt => opt.MapFrom(src => src.StatusDate.ToUniversalTime()));
    }
}