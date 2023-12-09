﻿using AutoMapper;
using UTEHY.DatabaseCoursePortal.Api.Configs;
using UTEHY.DatabaseCoursePortal.Api.Data.Entities;
using UTEHY.DatabaseCoursePortal.Api.Helpers;
using UTEHY.DatabaseCoursePortal.Api.Models.Post;
using UTEHY.DatabaseCoursePortal.Api.Models.Question;
using UTEHY.DatabaseCoursePortal.Api.Models.QuestionCategory;
using UTEHY.DatabaseCoursePortal.Api.Models.Teacher;
using UTEHY.DatabaseCoursePortal.Api.Models.User;

namespace UTEHY.DatabaseCoursePortal.Api.Mappers
{
    public class QuestionMapper : Profile
    {
        public QuestionMapper()
        {
            CreateMap<Question, QuestionDto>();

            CreateMap<QuestionDto, Question>();

            CreateMap<CreateQuestionRequest, QuestionDto>();

            CreateMap<CreateQuestionRequest, Question>()
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.QuestionAnswers.Sum(a => a.Score)));
        }
    }
}
