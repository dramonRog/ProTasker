using AutoMapper;
using ProTasker.DTOs.Responses.Project;
using ProTasker.DTOs.Responses.ProjectMember;
using ProTasker.Models;


namespace ProTasker.Mapping
{
    public class ProjectMappingProfile : Profile
    {
        public ProjectMappingProfile()
        {
            CreateMap<Project, ProjectListItemResponse>();

            CreateMap<Project, ProjectDetailsResponse>()
                .ForCtorParam(
                    nameof(ProjectDetailsResponse.Members),
                    opt => opt.MapFrom(src => src.ProjectMembers));

            CreateMap<ProjectMember, ProjectMemberResponse>()
                .ForCtorParam(
                    nameof(ProjectMemberResponse.FirstName),
                    opt => opt.MapFrom(src => src.User.FirstName))
                .ForCtorParam(
                    nameof(ProjectMemberResponse.LastName),
                    opt => opt.MapFrom(src => src.User.LastName))
                .ForCtorParam(
                    nameof(ProjectMemberResponse.Email),
                    opt => opt.MapFrom(src => src.User.Email));
        }
    }
}
