using AutoMapper;
using ProTasker.DTOs.Responses.Board;
using ProTasker.Models;

namespace ProTasker.Mapping
{
    public class BoardMappingProfile : Profile
    {
        public BoardMappingProfile() 
        {
            CreateMap<Board, BoardResponse>();
            CreateMap<Board, BoardSummaryResponse>();
        }
    }
}
