using AutoMapper;
using ProTasker.DTOs.Requests.TaskItem;
using ProTasker.DTOs.Responses.TaskItem;
using ProTasker.Models;

namespace ProTasker.Mapping
{
    public class TaskMappingProfile : Profile
    {
        public TaskMappingProfile()
        {
            CreateMap<TaskItem, TaskResponse>();
            CreateMap<CreateTaskItemRequest, TaskItem>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.BoardId, opt => opt.Ignore())
                .ForMember(dest => dest.Project, opt => opt.Ignore())
                .ForMember(dest => dest.Board, opt => opt.Ignore());
        }
    }
}
