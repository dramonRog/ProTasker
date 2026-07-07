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
            CreateMap<CreateTaskItemRequest, TaskItem>();
        }
    }
}
