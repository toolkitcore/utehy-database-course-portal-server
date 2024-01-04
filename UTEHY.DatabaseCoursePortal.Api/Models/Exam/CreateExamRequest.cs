﻿using Microsoft.AspNetCore.Mvc;
using UTEHY.DatabaseCoursePortal.Api.Models.Question;

namespace UTEHY.DatabaseCoursePortal.Api.Models.Exam
{
    public class CreateExamRequest
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Duration { get; set; }

        public List<ExamQuestionRequest>? Questions { get; set; }
    }
}
