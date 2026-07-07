using AutoMapper;
using ProTasker.DTOs.Responses.User;
using ProTasker.Models;

namespace ProTasker.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserResponse>();
        }
    }
}
