using AutoMapper;
using ProTasker.DTOs.Responses.TaskComment;
using ProTasker.Models;

namespace ProTasker.Mapping
{
    public class TaskCommentMappingProfile : Profile
    {
        public TaskCommentMappingProfile()
        {
            CreateMap<TaskComment, TaskCommentResponse>()
                .ForCtorParam(
                    nameof(TaskCommentResponse.AuthorName),
                    opt => opt.MapFrom(tc => tc.User != null ? $"{tc.User.FirstName} {tc.User.LastName}" : "Deleted User"));
        }
    }
}